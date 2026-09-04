using RustArchon.Rcon.Containers;
using RustArchon.Rcon.Entities;
using RustArchon.Rcon.EventArgs;
using RustArchon.Rcon.Messages;
using RustArchon.Rcon.Parsers;
using System.Net.WebSockets;
using System.Text.Json;
using Websocket.Client;
using Websocket.Client.Models;

namespace RustArchon.Rcon;

/// <summary>
/// A WebRCON client for Rust game servers - connects over the game's WebSocket-based RCON protocol,
/// dispatches sent commands to the right response parser, and raises typed events for the message
/// shapes Rust/Oxide/Carbon are known to send.
/// </summary>
/// <remarks>
/// This used to be split across four projects (a generic <c>RconClient</c> base meant to support
/// multiple protocols, a Source-engine RCON implementation, a protocol-agnostic WebRCON layer, and
/// this Rust-specific layer on top). Consolidated into one project/class - Rust's WebRCON is the only
/// protocol this codebase needs, and the extra layers of abstraction weren't earning their keep.
/// </remarks>
public class RustWebRconClient : IDisposable
{
    /// <summary>
    /// Default cap on <see cref="MessageLog"/>'s size - see the constructor's <c>maxMessageLogSize</c>
    /// parameter. This client may run for as long as a Rust server stays registered, so the log must
    /// not grow unbounded.
    /// </summary>
    public const int DefaultMaxMessageLogSize = 200;

    #region Events
    /// <summary>Fires for every raw response frame, before Rust-specific parsing.</summary>
    public event EventHandler<MessageReceivedEventArgs>? MessageReceived;
    public event EventHandler<ConnectionChangedEventArgs>? ConnectionChanged;
    public event EventHandler<BanEventArgs>? BansReceived;
    public event EventHandler<ChatEventArgs>? ChatReceived;
    public event EventHandler<ConsoleEventArgs>? ConsoleReceived;
    public event EventHandler<OxidePluginsEventArgs>? OxidePluginsReceived;
    public event EventHandler<CarbonPluginsEventArgs>? CarbonPluginsReceived;
    public event EventHandler<PlayerListEventArgs>? PlayerListReceived;
    public event EventHandler<ServerInfoEventArgs>? ServerInfoReceived;
    public event EventHandler<UnprocessedEventArgs>? UnprocessedMessageReceived;
    public event EventHandler<ModFrameworkVersionEventArgs>? ModFrameworkVersionReceived;

    /// <summary>Fires exactly once per connection, as soon as the mod framework is determined - see
    /// <see cref="DetectModFrameworkAsync"/>.</summary>
    public event EventHandler<ModFrameworkDetectedEventArgs>? ModFrameworkDetected;

    /// <summary>
    /// Fires when processing an inbound frame or a connection-state transition throws - from this
    /// class's own code (e.g. malformed JSON from the server) or from a subscriber to
    /// <see cref="MessageReceived"/>/<see cref="ConnectionChanged"/>/one of the typed events. See the
    /// three <c>Socket_On*</c> handlers' remarks for why this exists: Websocket.Client invokes its
    /// <c>MessageReceived</c>/<c>DisconnectionHappened</c>/<c>ReconnectionHappened</c> subjects
    /// synchronously from its own receive loop with no try/catch of its own, so an exception raised
    /// anywhere in this call chain - including inside a subscriber's handler - would otherwise
    /// propagate back into that loop, which treats it as a fatal connection error and force-reconnects.
    /// Confirmed live: this is exactly what was turning one bad frame into an endless reconnect loop,
    /// with the resulting abrupt socket teardown surfacing as unrelated-looking transport exceptions
    /// (<c>SocketException</c> 10053/10054, etc.) that looked like a network problem but weren't one.
    /// </summary>
    public event EventHandler<Exception>? ProcessingError;
    #endregion

    #region Public Properties
    public string Name { get; set; }
    public string Hostname { get; set; }
    public int Port { get; set; }
    public string Password { get; set; }
    public bool IsConnected => _socket?.IsRunning ?? false;

    /// <summary>
    /// The server's mod framework, once known - <c>null</c> until <see cref="DetectModFrameworkAsync"/>
    /// resolves (which happens automatically as soon as the connection opens, so this is usually
    /// populated within one round trip of <see cref="ConnectionChanged"/> reporting connected).
    /// </summary>
    public ModFrameworkKind? DetectedModFramework { get; private set; }
    #endregion

    #region Private Variables
    private readonly WebsocketClient _socket;
    private readonly Sequence<int> _sequence = new(0);

    private readonly int _maxMessageLogSize;
    private readonly object _messageLogLock = new();
    private readonly List<WebRconMessageBase> _messageLog = new();

    /// <summary>
    /// The most recent sent/received messages, oldest first, capped at the size passed to the
    /// constructor (see <see cref="DefaultMaxMessageLogSize"/>) - a snapshot, not a live view, so the
    /// caller doesn't need to worry about it mutating out from under them while enumerating.
    /// </summary>
    public IReadOnlyList<WebRconMessageBase> MessageLog
    {
        get
        {
            lock (_messageLogLock)
            {
                return _messageLog.ToArray();
            }
        }
    }

    private readonly object _outstandingRequestsLock = new();
    private readonly Dictionary<int, ParserBase> _outstandingRequests = new();
    private readonly List<int> _debugRequests = new();

    private readonly List<ParserBase> _parsers = new();

    private readonly object _modFrameworkDetectionLock = new();
    private TaskCompletionSource<ModFrameworkKind>? _modFrameworkDetection;
    private int _pendingModFrameworkResponses;

    private readonly object _pendingCommandsLock = new();
    private readonly Dictionary<int, TaskCompletionSource<WebRconResponse>> _pendingCommands = new();

    // Identifiers sent via SendCommandAsync(..., raiseMessageReceived: false) - see that parameter's
    // remarks. Populated before the request goes out (so there's no window where the response could
    // race ahead of this registration) and consumed exactly once, in OnMessageReceived.
    private readonly object _quietRequestsLock = new();
    private readonly HashSet<int> _quietRequests = new();
    #endregion

    #region Constructor
    /// <param name="maxMessageLogSize">
    /// The maximum number of messages <see cref="MessageLog"/> retains before the oldest are dropped.
    /// Pass 0 to disable logging entirely.
    /// </param>
    /// <param name="reconnectTimeout">
    /// How long the connection can go without receiving any message before it's treated as dead and
    /// reconnected - Websocket.Client's own watchdog, not something this class implements itself.
    /// Defaults to the library's own default (1 minute) when omitted.
    /// </param>
    /// <param name="errorReconnectTimeout">
    /// The delay between reconnect attempts after a connection error. This is a fixed interval, not
    /// exponential backoff - Websocket.Client doesn't implement backoff itself, and this class doesn't
    /// layer one on top; it relies entirely on the library's own (indefinite) retry behavior. Defaults
    /// to the library's own default (1 minute) when omitted.
    /// </param>
    public RustWebRconClient(
        string name,
        string hostname,
        int port,
        string password,
        int maxMessageLogSize = DefaultMaxMessageLogSize,
        TimeSpan? reconnectTimeout = null,
        TimeSpan? errorReconnectTimeout = null)
    {
        Name = name;
        Hostname = hostname;
        Port = port;
        Password = password;
        _maxMessageLogSize = maxMessageLogSize;

        var uri = new Uri($"ws://{hostname}:{port}/{password}");
        _socket = new WebsocketClient(uri);
        if (reconnectTimeout.HasValue)
        {
            _socket.ReconnectTimeout = reconnectTimeout;
        }
        if (errorReconnectTimeout.HasValue)
        {
            _socket.ErrorReconnectTimeout = errorReconnectTimeout;
        }

        // Wrapped in SafeExecute, not called directly - see ProcessingError's remarks. This is the
        // one boundary between our code (including every subscriber's handler, invoked synchronously
        // from inside these) and Websocket.Client's own receive loop, so it's the one place that
        // matters for stopping an exception from ever reaching back into the library.
        _socket.MessageReceived.Subscribe(message => SafeExecute(() => Socket_OnMessage(message)));
        _socket.DisconnectionHappened.Subscribe(info => SafeExecute(() => Socket_OnClose(info)));
        _socket.ReconnectionHappened.Subscribe(info => SafeExecute(() => Socket_OnOpen(info)));

        InitializeParsers();
    }
    #endregion

    #region Socket Events
    /// <summary>
    /// Runs <paramref name="action"/>, reporting (via <see cref="ProcessingError"/>) rather than
    /// propagating any exception it throws. See that event's remarks for why this exists - every
    /// <c>Socket_On*</c> handler below runs on Websocket.Client's own receive loop and synchronously
    /// invokes whatever subscribers are attached to this client's events, so this is the only place
    /// that can catch a bug in either this class's own frame handling or a subscriber's, before it
    /// reaches back into the library and gets misread as a dead connection.
    /// </summary>
    private void SafeExecute(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            ProcessingError?.Invoke(this, ex);
        }
    }

    private void Socket_OnMessage(ResponseMessage message)
    {
        if (message.MessageType == WebSocketMessageType.Text)
        {
            var response = JsonSerializer.Deserialize<WebRconResponse>(message.Text!);
            if (response != null)
            {
                AddToLog(response);
                OnMessageReceived(response, handled: false);
            }
        }
    }

    private void Socket_OnClose(DisconnectionInfo disconnectionInfo)
    {
        var detail = disconnectionInfo.CloseStatus.HasValue
            ? $"{disconnectionInfo.Type} ({disconnectionInfo.CloseStatus})"
            : disconnectionInfo.Type.ToString();
        if (!string.IsNullOrEmpty(disconnectionInfo.CloseStatusDescription))
        {
            detail += $": {disconnectionInfo.CloseStatusDescription}";
        }
        else if (disconnectionInfo.Exception is { } exception)
        {
            // CloseStatusDescription only ever comes from a graceful WebSocket-level close - a lower-
            // level transport failure (TCP reset, TLS handshake failure, DNS failure, the handshake
            // getting rejected before it completes, ...) leaves it empty, with the actual reason
            // sitting in Exception instead. That exception previously never left this process at all -
            // ConnectionChangedEventArgs carried it all the way to ServerConnectionActor, but only ever
            // for a local LogWarning call, never folded into the Detail string that actually reaches
            // ConnectionStatusChanged and, from there, the Panel's connection log - confirmed live: a
            // real "TCP connects, WebSocket handshake gets forcibly closed" failure showed up there as
            // a bare "Error", indistinguishable from any other unexplained drop. Walking to the
            // innermost exception surfaces the actually-useful message - the outer WebSocketException's
            // own message is just "Unable to connect to the remote server" (unhelpful), while the
            // SocketException several layers down says the real thing: "An existing connection was
            // forcibly closed by the remote host".
            var innermost = exception;
            while (innermost.InnerException is { } inner)
            {
                innermost = inner;
            }

            detail += $": {innermost.Message}";
        }

        OnConnectionChanged(false, detail, disconnectionInfo.Exception);
    }

    private void Socket_OnOpen(ReconnectionInfo reconnectionInfo)
    {
        OnConnectionChanged(true, reconnectionInfo.Type.ToString());

        // Determine the mod framework as soon as possible after connecting, rather than waiting for
        // a caller to ask - once known, this is a no-op (a reconnect doesn't change what's installed).
        if (!DetectedModFramework.HasValue)
        {
            EnsureModFrameworkDetectionStarted();
        }
    }
    #endregion

    #region Message Dispatch
    private void OnMessageReceived(WebRconResponse response, bool handled)
    {
        TaskCompletionSource<WebRconResponse>? pendingCommand;
        lock (_pendingCommandsLock)
        {
            _pendingCommands.TryGetValue(response.Identifier, out pendingCommand);
        }

        if (pendingCommand != null)
        {
            // An awaited SendCommandAsync call is waiting on this specific response - hand it over
            // directly rather than also running it through parser dispatch/the Unknown fallback below,
            // which would be redundant noise for a caller that's already getting the raw response.
            pendingCommand.TrySetResult(response);

            if (!WasSentQuietly(response.Identifier))
            {
                MessageReceived?.Invoke(this, new MessageReceivedEventArgs(response, handled: true));
            }

            return;
        }

        lock (_outstandingRequestsLock)
        {
            _debugRequests.Remove(response.Identifier);
        }

        ParserBase? responseParser;
        lock (_outstandingRequestsLock)
        {
            _outstandingRequests.TryGetValue(response.Identifier, out responseParser);
        }

        if (responseParser != null)
        {
            ModFrameworkKind? detectedFramework = null;

            if (responseParser.TryParseMessage(response, out var parsedMessage) && parsedMessage != null)
            {
                responseParser.FireEvent?.Invoke(parsedMessage);
                if (responseParser is not NoParser)
                {
                    handled = true;
                }

                if (parsedMessage is ModFrameworkVersion version)
                {
                    detectedFramework = ParseModFrameworkKind(version.ModFramework);
                }
            }
            else
            {
                // Covers both a genuine parse failure (TryParseMessage returned false) and a
                // successful-but-empty parse (returned true with a null entity) - either way, the
                // caller gets a signal instead of the response silently vanishing.
                OnUnknownMessageReceived(response);
            }

            if (responseParser is OxideVersionParser or CarbonVersionParser)
            {
                HandleModFrameworkDetectionResponse(detectedFramework);
            }

            lock (_outstandingRequestsLock)
            {
                _outstandingRequests.Remove(response.Identifier);
            }
        }
        else
        {
            OnUnknownMessageReceived(response);
        }

        // A genuinely unsolicited frame's identifier was never ours to register as quiet in the first
        // place, so this only ever actually suppresses something for a request this client itself
        // sent with raiseMessageReceived: false - see SendCommand's remarks.
        if (!WasSentQuietly(response.Identifier))
        {
            MessageReceived?.Invoke(this, new MessageReceivedEventArgs(response, handled));
        }
    }

    /// <summary>
    /// Checks whether <paramref name="identifier"/> was registered as a quiet request (see
    /// <see cref="SendCommandAsync"/>'s and <see cref="SendCommand"/>'s <c>raiseMessageReceived</c>
    /// remarks), consuming the registration if so - each identifier is only ever checked once, right
    /// when its response arrives.
    /// </summary>
    private bool WasSentQuietly(int identifier)
    {
        lock (_quietRequestsLock)
        {
            return _quietRequests.Remove(identifier);
        }
    }

    private void OnConnectionChanged(bool connected, string? detail = null, Exception? exception = null)
    {
        ConnectionChanged?.Invoke(this, new ConnectionChangedEventArgs(connected, detail, exception));
    }
    #endregion

    #region Event Sources
    protected void OnChatReceived(List<ChatMessage> chats) => ChatReceived?.Invoke(this, new ChatEventArgs(chats));
    protected void OnConsoleReceived(List<ConsoleMessage> consoles) => ConsoleReceived?.Invoke(this, new ConsoleEventArgs(consoles));
    protected void OnBansReceived(List<Ban> bans) => BansReceived?.Invoke(this, new BanEventArgs(bans));
    protected void OnOxidePluginListReceived(List<OxidePlugin> oxidePlugins) => OxidePluginsReceived?.Invoke(this, new OxidePluginsEventArgs(oxidePlugins));
    protected void OnCarbonPluginListReceived(List<CarbonPlugin> carbonPlugins) => CarbonPluginsReceived?.Invoke(this, new CarbonPluginsEventArgs(carbonPlugins));
    protected void OnModFrameworkVersionReceived(ModFrameworkVersion version) => ModFrameworkVersionReceived?.Invoke(this, new ModFrameworkVersionEventArgs(version));
    protected void OnPlayerListReceived(List<Player> playerList) => PlayerListReceived?.Invoke(this, new PlayerListEventArgs(playerList));
    protected void OnServerInfoReceived(ServerInfo serverInfo) => ServerInfoReceived?.Invoke(this, new ServerInfoEventArgs(serverInfo));
    protected void OnUnprocessedMessageReceived(Unprocessed unprocessed) => UnprocessedMessageReceived?.Invoke(this, new UnprocessedEventArgs(unprocessed));

    protected void OnUnknownMessageReceived(WebRconResponse response)
    {
        var unprocessed = new Unprocessed
        {
            Identifier = response.Identifier,
            Id = response.Id,
            Message = response.Message,
            Type = response.Type,
            StackTrace = response.Stacktrace
        };
        OnUnprocessedMessageReceived(unprocessed);
    }
    #endregion

    #region Public Connection Methods
    /// <param name="exception">
    /// The exception <see cref="_socket"/>.Start() threw, if this returns <c>false</c> - previously
    /// swallowed entirely with no way for a caller to report it. <c>null</c> when this returns
    /// <c>true</c>.
    /// </param>
    public bool Connect(out Exception? exception)
    {
        try
        {
            _socket.Start();
        }
        catch (Exception ex)
        {
            exception = ex;
            return false;
        }
        exception = null;
        return true;
    }

    public void Disconnect()
    {
        try
        {
            if (_socket.IsRunning)
            {
                _socket.Stop(WebSocketCloseStatus.NormalClosure, string.Empty);
            }
        }
        catch
        {
            // Best-effort - the socket may already be in a faulted/closed state.
        }
    }

    private bool SendRequest(WebRconRequest request)
    {
        if (!_socket.IsRunning)
        {
            return false;
        }

        request.Name = Name;
        var json = JsonSerializer.Serialize(request);
        AddToLog(request);
        _socket.Send(json);
        return true;
    }
    #endregion

    #region Public Commands
    public void GetBanList() => SendCommand("bans", _parsers.First(p => p is BanParser));
    public void GetChatHistory(int lineCount) => SendCommand($"chat.tail {lineCount}", _parsers.First(p => p is ChatParser));
    public void GetConsoleHistory(int lineCount) => SendCommand($"console.tail {lineCount}", _parsers.First(p => p is ConsoleParser));
    public void GetOxidePlugins() => SendCommand("o.plugins", _parsers.First(p => p is OxidePluginListParser));
    public void GetCarbonPlugins() => SendCommand("c.plugins", _parsers.First(p => p is CarbonPluginListParser));
    public void GetPlayerList() => SendCommand("playerlist", _parsers.First(p => p is PlayerParser));
    public void GetServerInfo() => SendCommand("serverinfo", _parsers.First(p => p is ServerInfoParser));

    /// <summary>
    /// Re-queries the mod framework's version. Once <see cref="DetectedModFramework"/> is known, this
    /// sends only that framework's version command (or nothing at all, for <see cref="ModFrameworkKind.None"/>)
    /// instead of guessing with both - see <see cref="DetectModFrameworkAsync"/> for the one-time
    /// detection this relies on, which normally already ran automatically right after connecting.
    /// </summary>
    public void GetModFrameworkVersion()
    {
        switch (DetectedModFramework)
        {
            case ModFrameworkKind.Oxide:
                SendCommand("o.version", _parsers.First(p => p is OxideVersionParser));
                return;
            case ModFrameworkKind.Carbon:
                SendCommand("c.version", _parsers.First(p => p is CarbonVersionParser));
                return;
            case ModFrameworkKind.None:
                return; // nothing installed - nothing to ask for
            case null:
                EnsureModFrameworkDetectionStarted();
                return;
        }
    }

    /// <summary>
    /// Resolves to the server's mod framework - <see cref="ModFrameworkKind.Oxide"/>,
    /// <see cref="ModFrameworkKind.Carbon"/>, or <see cref="ModFrameworkKind.None"/> if neither
    /// responded. Returns immediately if already known (from a previous call, or from the automatic
    /// detection that runs right after connecting) rather than re-querying the server.
    /// </summary>
    /// <remarks>
    /// Concurrent callers all await the same in-flight detection rather than each triggering their
    /// own round of commands.
    /// </remarks>
    public Task<ModFrameworkKind> DetectModFrameworkAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        if (DetectedModFramework is { } known)
        {
            return Task.FromResult(known);
        }

        var detection = EnsureModFrameworkDetectionStarted();
        return timeout.HasValue
            ? detection.Task.WaitAsync(timeout.Value, cancellationToken)
            : detection.Task.WaitAsync(cancellationToken);
    }

    /// <summary>Sends an arbitrary command with no specific response parser - its response (if any)
    /// surfaces via <see cref="UnprocessedMessageReceived"/>. Prefer <see cref="SendCommandAsync"/>
    /// when the caller actually needs the response rather than just observing it via events.</summary>
    public void Send(string command)
    {
        var request = new WebRconRequest { Message = command, Identifier = _sequence.GetValue() };
        lock (_outstandingRequestsLock)
        {
            _debugRequests.Add(request.Identifier);
        }
        SendRequest(request);
    }

    /// <summary>
    /// Sends an arbitrary command and awaits its raw response, correlated by this connection's own
    /// <c>Identifier</c> sequence - the general-purpose counterpart to the typed <c>GetXxx</c> methods,
    /// for commands with no dedicated parser (or callers that just want the raw text back).
    /// </summary>
    /// <param name="raiseMessageReceived">
    /// Whether this response should also be broadcast via <see cref="MessageReceived"/> once the
    /// caller has it. Defaults to <c>true</c>, matching every other way of receiving a response. Pass
    /// <c>false</c> for a request the caller wants privately, without it also reaching whatever else
    /// is observing the general raw stream - e.g. a caller polling a status command on its own
    /// schedule for internal bookkeeping, where that response isn't part of the server's own
    /// console/chat history.
    /// </param>
    /// <exception cref="InvalidOperationException">The socket isn't currently connected.</exception>
    /// <exception cref="TimeoutException"><paramref name="timeout"/> elapsed with no response.</exception>
    public async Task<WebRconResponse> SendCommandAsync(
        string command, TimeSpan? timeout = null, CancellationToken cancellationToken = default, bool raiseMessageReceived = true)
    {
        var request = new WebRconRequest { Message = command, Identifier = _sequence.GetValue() };
        var completionSource = new TaskCompletionSource<WebRconResponse>(TaskCreationOptions.RunContinuationsAsynchronously);

        lock (_pendingCommandsLock)
        {
            _pendingCommands[request.Identifier] = completionSource;
        }

        if (!raiseMessageReceived)
        {
            // Registered before SendRequest goes out, not after awaiting it - the response can arrive
            // (and OnMessageReceived can run) before this method's own call to SendRequest even
            // returns, so there is no safe point after sending to register this.
            lock (_quietRequestsLock)
            {
                _quietRequests.Add(request.Identifier);
            }
        }

        try
        {
            if (!SendRequest(request))
            {
                throw new InvalidOperationException($"Cannot send a command to {Hostname}:{Port} - the socket isn't connected.");
            }

            return timeout.HasValue
                ? await completionSource.Task.WaitAsync(timeout.Value, cancellationToken)
                : await completionSource.Task.WaitAsync(cancellationToken);
        }
        finally
        {
            lock (_pendingCommandsLock)
            {
                _pendingCommands.Remove(request.Identifier);
            }

            // Normally already consumed by OnMessageReceived once the response arrives - this only
            // does anything if it never got that far (the send itself failed, or the wait above timed
            // out/was cancelled first), so a quiet request's flag doesn't linger forever.
            if (!raiseMessageReceived)
            {
                lock (_quietRequestsLock)
                {
                    _quietRequests.Remove(request.Identifier);
                }
            }
        }
    }
    #endregion

    #region Private Methods
    /// <param name="raiseMessageReceived">See <see cref="SendCommandAsync"/>'s remarks on the same
    /// parameter - same meaning, just for this fire-and-forget/parser-dispatch path instead.</param>
    private void SendCommand(string command, ParserBase parser, bool raiseMessageReceived = true)
    {
        var request = new WebRconRequest { Message = command, Identifier = _sequence.GetValue() };
        lock (_outstandingRequestsLock)
        {
            _outstandingRequests.Add(request.Identifier, parser);
        }

        if (!raiseMessageReceived)
        {
            // Same reasoning as SendCommandAsync's own quiet registration - done before SendRequest,
            // not after, since the response can arrive before this call even returns.
            lock (_quietRequestsLock)
            {
                _quietRequests.Add(request.Identifier);
            }
        }

        SendRequest(request);
    }

    /// <summary>
    /// Starts the two-command mod-framework detection if it isn't already known or already in
    /// flight, and returns the (possibly already-existing) in-flight <see cref="TaskCompletionSource{TResult}"/>
    /// so every caller - <see cref="GetModFrameworkVersion"/>, <see cref="DetectModFrameworkAsync"/>,
    /// and the automatic post-connect attempt - shares one detection attempt instead of each firing
    /// their own commands.
    /// </summary>
    private TaskCompletionSource<ModFrameworkKind> EnsureModFrameworkDetectionStarted()
    {
        lock (_modFrameworkDetectionLock)
        {
            if (_modFrameworkDetection != null)
            {
                return _modFrameworkDetection;
            }

            _modFrameworkDetection = new TaskCompletionSource<ModFrameworkKind>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pendingModFrameworkResponses = 2;
        }

        // Sent outside the lock - SendRequest/SendCommand take their own locks, and there's no need
        // to hold this one while a message goes out over the socket. Quiet regardless of which of the
        // three call sites triggered this (automatic post-connect, GetModFrameworkVersion, or
        // DetectModFrameworkAsync) - every caller already gets the answer through the typed
        // ModFrameworkVersionReceived/ModFrameworkDetected surface, so none of them need the raw
        // version-string frame to also show up wherever the general MessageReceived stream ends up
        // (e.g. RustArchon's own server console tail) - confirmed live: every reconnect was showing a
        // Carbon/Oxide version banner in the console purely as a side effect of this auto-detection,
        // not anything the server itself broadcast unprompted.
        SendCommand("o.version", _parsers.First(p => p is OxideVersionParser), raiseMessageReceived: false);
        SendCommand("c.version", _parsers.First(p => p is CarbonVersionParser), raiseMessageReceived: false);

        return _modFrameworkDetection;
    }

    /// <summary>
    /// Called for every response to an in-flight <c>o.version</c>/<c>c.version</c> detection command,
    /// whether it matched or not. Resolves detection immediately on a match; concludes
    /// <see cref="ModFrameworkKind.None"/> once both commands have responded without one.
    /// </summary>
    private void HandleModFrameworkDetectionResponse(ModFrameworkKind? detected)
    {
        if (detected.HasValue)
        {
            CompleteModFrameworkDetection(detected.Value);
            return;
        }

        lock (_modFrameworkDetectionLock)
        {
            if (DetectedModFramework.HasValue)
            {
                return;
            }

            _pendingModFrameworkResponses--;
            if (_pendingModFrameworkResponses > 0)
            {
                return;
            }
        }

        CompleteModFrameworkDetection(ModFrameworkKind.None);
    }

    private void CompleteModFrameworkDetection(ModFrameworkKind kind)
    {
        lock (_modFrameworkDetectionLock)
        {
            if (DetectedModFramework.HasValue)
            {
                return;
            }

            DetectedModFramework = kind;
            _modFrameworkDetection?.TrySetResult(kind);
            _modFrameworkDetection = null;
        }

        ModFrameworkDetected?.Invoke(this, new ModFrameworkDetectedEventArgs(kind));
    }

    private static ModFrameworkKind? ParseModFrameworkKind(string modFramework) => modFramework switch
    {
        "Oxide" => ModFrameworkKind.Oxide,
        "Carbon" => ModFrameworkKind.Carbon,
        _ => null
    };

    private void AddToLog(WebRconMessageBase message)
    {
        if (_maxMessageLogSize <= 0)
        {
            return;
        }

        lock (_messageLogLock)
        {
            _messageLog.Add(message);
            while (_messageLog.Count > _maxMessageLogSize)
            {
                _messageLog.RemoveAt(0);
            }
        }
    }

    private void InitializeParsers()
    {
        _parsers.Add(new NoParser(message => OnUnprocessedMessageReceived((Unprocessed)message)));
        _parsers.Add(new ServerInfoParser(message => OnServerInfoReceived((ServerInfo)message)));
        _parsers.Add(new BanParser(message => OnBansReceived(((BanList)message).Bans)));
        _parsers.Add(new ChatParser(message => OnChatReceived(((ChatMessageList)message).Chats)));
        _parsers.Add(new ConsoleParser(message => OnConsoleReceived(((ConsoleMessageList)message).Consoles)));
        _parsers.Add(new PlayerParser(message => OnPlayerListReceived(((PlayerList)message).Players)));
        _parsers.Add(new OxidePluginListParser(message => OnOxidePluginListReceived(((OxidePluginList)message).Plugins)));
        _parsers.Add(new CarbonPluginListParser(message => OnCarbonPluginListReceived(((CarbonPluginList)message).Plugins)));
        _parsers.Add(new CarbonVersionParser(message => OnModFrameworkVersionReceived((ModFrameworkVersion)message)));
        _parsers.Add(new OxideVersionParser(message => OnModFrameworkVersionReceived((ModFrameworkVersion)message)));
    }
    #endregion

    #region IDisposable
    /// <summary>Disconnects (if connected) and releases the underlying socket. Not itself
    /// thread-safe against concurrent <see cref="Connect"/>/<see cref="SendCommandAsync"/> calls -
    /// callers are expected to be done issuing new work before disposing, the same as any other
    /// <see cref="IDisposable"/>.</summary>
    public void Dispose()
    {
        Disconnect();
        _socket.Dispose();
        GC.SuppressFinalize(this);
    }
    #endregion
}

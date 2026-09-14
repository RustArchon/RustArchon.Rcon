namespace RustArchon.Rcon;

/// <summary>
/// Carries the application-level "why was this command sent" fact through <see cref="RustWebRconClient"/>'s
/// generic <c>object? userData</c> mechanism (see <see cref="RustWebRconClient.SendCommandAsync"/>) and
/// back out on <see cref="RustWebRconClient.MessageReceived"/> - this client itself makes no
/// interactive/background decision at all; it just carries whatever a caller attaches to a request back
/// to whoever handles that request's response.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Interactive"/> is a required constructor argument, deliberately with no default value.
/// This exists because of a real, twice-recurring incident: a backend-initiated command (a periodic
/// poll, a page-load side effect) showed up in the Panel's Console tab as if a human had typed it,
/// fixed once for the two loops that existed at the time, then reintroduced when a later feature reused
/// the same generic send-a-command pathway without anyone connecting it back to that constraint -
/// because that pathway (see <c>RustArchon.Messaging.Contracts.SendRconCommand</c>) had no field to
/// remind them the question existed at all. A required field here (and on that message contract, and on
/// <c>ServerConnectionActor.SendCommandAsync</c>'s own public signature) makes it a compile error to add
/// a new caller without answering the question, rather than a runtime default that quietly picks one
/// answer and lets the other slip through - which is what let this recur twice already.
/// </para>
/// <para>
/// Named "Context", not "Metadata"/"Options"/"Parameters" - it doesn't configure how the command
/// executes (ruling out Options/Parameters), and matches this codebase's existing use of "Context" for
/// the same shape of concept elsewhere (<c>Correlate.ICorrelationContextAccessor</c>'s
/// <c>CorrelationContext</c> - ambient information carried alongside an operation, available when
/// handling its result).
/// </para>
/// </remarks>
public sealed record RconCommandContext(bool Interactive)
{
    /// <summary>
    /// A ready-made instance for the common "this is a poll/background fetch, not something an
    /// ordinary user should see in the Console tab" case - the direction worth a shorthand, since it's
    /// the one every new background call site needs to remember to reach for. This no longer keeps the
    /// event out of the database (see <c>RconFrameCaptured</c>'s remarks) - it's still persisted, just
    /// filtered out at read/delivery time for anyone who isn't a site admin looking at this tenant with
    /// the unfiltered view on. The interactive case is left as
    /// <c>new RconCommandContext(Interactive: true)</c> at each call site instead: it's exactly as
    /// self-documenting, and a human-triggered call site is usually already naming what triggered it
    /// (a button handler, a console-input submit), so the explicit constructor reads naturally there.
    /// </summary>
    public static readonly RconCommandContext Background = new(Interactive: false);
}

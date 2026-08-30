# RustArchon.Rcon

A standalone Rust WebRCON client library: connect, send commands, and parse
chat/console/player-list/ban-list/plugin-list responses, including detecting Oxide vs. Carbon. Not
RustArchon-specific business logic - it doesn't know RustArchon exists, and has no dependency on
anything else in the RustArchon system beyond the `Websocket.Client` NuGet package. Used by
[RustArchon.Worker](https://github.com/RustArchon/RustArchon.Worker) to hold one client per server
it owns.

Part of the [RustArchon](https://github.com/RustArchon/RustArchon) system - see that repo for the
full architecture and how to run the whole stack locally or via Docker Compose.

## Key files

- `RustWebRconClient.cs` - the client itself: connect, send commands, dispatch received frames to
  the right parser and event.
- `Parsers/` - one parser per response shape (`ChatParser`, `ConsoleParser`, `PlayerParser`,
  `BanParser`, `{Oxide,Carbon}PluginListParser`, `{Oxide,Carbon}VersionParser` - detecting which mod
  framework a server runs, if any).
- `Entities/` - the parsed domain types (`ChatMessage`, `ConsoleMessage`, `Player`, `Ban`,
  `ServerInfo`, plugin types).
- `EventArgs/` - one event per entity type, raised as `RustWebRconClient` receives and parses frames.
- `Messages/WebRcon{Request,Response}.cs` - the raw WebRCON protocol frame shapes, before parsing.

## License

AGPL-3.0-or-later - see [`LICENSE`](LICENSE). See [`NOTICE.md`](NOTICE.md) for how this project
relates to [JumpStart](https://github.com/cyberknet/JumpStart) elsewhere in the RustArchon system
(this project itself has no JumpStart dependency).

## Building standalone

This repo has no RustArchon-internal dependencies and builds fully standalone:

```bash
git clone https://github.com/RustArchon/RustArchon.Rcon.git
cd RustArchon.Rcon
dotnet build
```

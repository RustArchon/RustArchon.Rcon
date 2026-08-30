# RustArchon.Rcon

A standalone Rust WebRCON client library: connect, send commands, and parse
chat/console/player-list/ban-list/plugin-list responses, including detecting Oxide vs. Carbon. Not
RustArchon-specific business logic - it doesn't know RustArchon exists, and has no dependency on
anything else in the RustArchon system beyond the `Websocket.Client` NuGet package. Used by
[RustArchon.Worker](https://github.com/RustArchon/RustArchon.Worker) to hold one client per server
it owns.

Part of the [RustArchon](https://github.com/RustArchon/RustArchon) system - see that repo for the
full architecture and how to run the whole stack locally or via Docker Compose.

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

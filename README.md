# Crypt

`Crypt` is a C#/MonoGame dungeon game in which the floor steadily turns to lava. It is a clean rewrite of the first playable slice from the archived Java prototype, not a history rewrite or line-by-line port.

## Current playable slice

- Explore a generated 8×10 room.
- Reveal and collect a hidden key, then reach the exit.
- Each visited floor tile cracks and eventually becomes lava.
- A breadth-first reachability check detects when the run has become unwinnable.
- Swing a sword with visible timing and a single, test-covered damage window.

## Controls

- `WASD`, `ZQSD`, or arrow keys: move
- `Space`: swing the sword
- `Enter` or `Z`: advance dialogue / retry after death
- `R`: generate a new room
- `Escape`: quit

## Development

This project targets .NET 10 and MonoGame DesktopGL. The DesktopGL target runs on Windows, macOS, and 64-bit Linux.

```bash
dotnet tool restore
dotnet run --project Crypt.csproj
dotnet test Crypt.sln
```

The code is intentionally split into three projects:

- `Crypt.Core`: framework-independent game rules, entities, combat, and pathfinding.
- `Crypt`: the MonoGame DesktopGL renderer, input adapter, and content pipeline.
- `tests/Crypt.Core.Tests`: unit tests for the gameplay rules.

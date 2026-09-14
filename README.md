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
- `P`: pass from a cleared level to the next available level
- `Escape`: quit

### Rune cipher

Stepping on the glowing rune tablet opens a cipher panel. Each new room randomly selects a dungeon-themed phrase and rotation, then encrypts it at runtime. The number after the encrypted phrase is its rotation count. Type the decrypted phrase and press `Enter`. You have three attempts; click the `!` in the upper-right corner for a loose rotation hint.

### Level two

Level two replaces lava with a permanently cracked floor. Search for the folded paper hidden in the room. It reveals a randomly chosen move pattern and a strict one-based `ROW/COLUMN` starting tile exactly once, then disappears. Stand on that start tile and enter the pattern to reveal the key; a wrong input resets the pattern.

One in four completed level-two sequences is a playful trap: after the final correct move, the dungeon accuses the player of photographing its move list, then opens a sinkhole after the accusation is dismissed. The game shuffles one trap into every set of four generated level-two rooms, so it cannot disappear behind a long unlucky streak.

### Level three

Level three is a single-attempt math trial. Every room generates a fresh square-root, linear, or bracketed equation. Read the equation above the room and step onto one of four numbered sigils. The correct answer reveals the key; a wrong answer or timeout triggers a sinkhole. Simple square roots allow 10 seconds, linear equations allow 8, and bracketed equations allow 6.

### Level four: The Vigenere Library

Level four is a calm library puzzle with no lava. Find the glowing keyword book, read and memorize its one-time keyword, then bring it to the rune lectern. The lectern displays a fresh Vigenere-encrypted phrase; repeat the book's keyword across the letters to decrypt it. Three wrong answers open a sinkhole.

### Level five: The Quiet Minefield

Level five opens a full 8×10 Minesweeper board. It is intentionally gentler than standard difficulty: there are only eight mines, and the first revealed cell plus its neighboring cells are always safe. Left-click to reveal a cell, right-click to place or remove a flag, and clear every safe cell to reveal the dungeon key. Hitting a mine opens a sinkhole.

### Death counter

The header records both total deaths in the current game session and deaths on the active stage. On the third death of a stage, the dungeon pauses after the death animation to deliver a secret roast before the usual retry screen.

## Development

This project targets .NET 10 and MonoGame DesktopGL. The DesktopGL target runs on Windows, macOS, and 64-bit Linux.

```bash
dotnet tool restore
dotnet run --project Crypt.csproj
dotnet test Crypt.Core/tests/Crypt.Core.Tests/Crypt.Core.Tests.csproj
```

The code is intentionally split into three projects:

- `Crypt.Core`: framework-independent game rules, entities, combat, and pathfinding.
- `Crypt`: the MonoGame DesktopGL renderer, input adapter, and content pipeline.
- `tests/Crypt.Core.Tests`: unit tests for the gameplay rules.

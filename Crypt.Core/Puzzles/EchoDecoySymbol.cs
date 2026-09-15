using Crypt.Core.Entities;
using Crypt.Core.Utility;
using Crypt.Core.World;

namespace Crypt.Core.Puzzles;

public sealed record EchoDecoySymbol(GridPosition Position, int Variant);

public static class EchoRoomPatternCatalog
{
    private static readonly IReadOnlyList<SpecialMovePattern> NoobPatterns =
    [
        new("Warmup",
        [
            Facing.Up, Facing.Right, Facing.Down, Facing.Right,
            Facing.Up, Facing.Left, Facing.Down, Facing.Left,
        ]),

        new("Loop",
        [
            Facing.Right, Facing.Up, Facing.Left, Facing.Up,
            Facing.Right, Facing.Down, Facing.Left, Facing.Down,
        ]),
    ];

    private static readonly IReadOnlyList<SpecialMovePattern> Patterns =
    [
        new("Whisper",
        [
            Facing.Up, Facing.Right, Facing.Down, Facing.Right,
            Facing.Up, Facing.Left, Facing.Up, Facing.Right,
            Facing.Down, Facing.Down, Facing.Left, Facing.Left,
            Facing.Up, Facing.Right,
        ]),

        new("Doubt",
        [
            Facing.Right, Facing.Up, Facing.Right, Facing.Down,
            Facing.Down, Facing.Left, Facing.Up, Facing.Left,
            Facing.Down, Facing.Right, Facing.Right, Facing.Up,
            Facing.Left, Facing.Up,
        ]),

        new("Mirror",
        [
            Facing.Left, Facing.Up, Facing.Up, Facing.Right,
            Facing.Down, Facing.Right, Facing.Up, Facing.Left,
            Facing.Down, Facing.Down, Facing.Right, Facing.Up,
            Facing.Left, Facing.Left,
        ]),
    ];

    public static SpecialMovePattern Pick(
        Random random,
        GameDifficulty difficulty = GameDifficulty.Difficult)
    {
        ArgumentNullException.ThrowIfNull(random);

        var patterns = difficulty == GameDifficulty.Noob
            ? NoobPatterns
            : Patterns;

        return patterns[random.Next(patterns.Count)];
    }
}
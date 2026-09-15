using Crypt.Core.Entities;
using Crypt.Core.Utility;

namespace Crypt.Core.Puzzles;

/// <summary>Reusable, hand-authored directional patterns for the level-two memory puzzle.</summary>
public sealed record SpecialMovePattern(string Name, IReadOnlyList<Facing> Sequence)
{
    public string InstructionText => string.Join(' ', Sequence.Select(DirectionName));

    private static string DirectionName(Facing direction) => direction switch
    {
        Facing.Up => "UP",
        Facing.Down => "DOWN",
        Facing.Left => "LEFT",
        Facing.Right => "RIGHT",
        _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
    };
}

public static class SpecialMovePatternCatalog
{
    private static readonly IReadOnlyList<SpecialMovePattern> Patterns =
    [
        new("Echo", [Facing.Up, Facing.Up, Facing.Down, Facing.Down, Facing.Left, Facing.Right, Facing.Left, Facing.Right, Facing.Left, Facing.Left]),
        new("Spiral", [Facing.Up, Facing.Right, Facing.Right, Facing.Down, Facing.Down, Facing.Left, Facing.Left, Facing.Up]),
        new("Crown", [Facing.Left, Facing.Up, Facing.Right, Facing.Right, Facing.Down, Facing.Right, Facing.Up, Facing.Left]),
        new("Fang", [Facing.Down, Facing.Right, Facing.Up, Facing.Right, Facing.Down, Facing.Down, Facing.Left, Facing.Left]),
        new("Tide", [Facing.Right, Facing.Up, Facing.Left, Facing.Up, Facing.Right, Facing.Down, Facing.Right, Facing.Down]),
        new("Lantern", [Facing.Up, Facing.Left, Facing.Down, Facing.Left, Facing.Up, Facing.Right, Facing.Right, Facing.Down]),
        new("Warden", [Facing.Right, Facing.Right, Facing.Up, Facing.Left, Facing.Up, Facing.Right, Facing.Down, Facing.Down]),
        new("Ash", [Facing.Left, Facing.Down, Facing.Right, Facing.Up, Facing.Right, Facing.Down, Facing.Left, Facing.Up]),
    ];

    public static int Count => Patterns.Count;

    public static SpecialMovePattern Pick(Random random)
    {
        ArgumentNullException.ThrowIfNull(random);
        return Patterns[random.Next(Patterns.Count)];
    }

    public static SpecialMovePattern Pick(Random random, GameDifficulty difficulty)
    {
        ArgumentNullException.ThrowIfNull(random);

        var pool = difficulty switch
        {
            GameDifficulty.Noob => Patterns.Take(3).ToArray(),
            GameDifficulty.Difficult => Patterns,
            GameDifficulty.Extreme => Patterns,
            _ => Patterns,
        };

        return pool[random.Next(pool.Count)];
    }
}

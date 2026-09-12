using Crypt.Core.Entities;
using Crypt.Core.Puzzles;
using Crypt.Core.Utility;

namespace Crypt.Core.World;

/// <summary>level one room layouts.</summary>
public sealed class RoomGenerator
{
    private readonly Random _random;

    public RoomGenerator()
        : this(new Random())
    {
    }

    public RoomGenerator(Random random)
    {
        ArgumentNullException.ThrowIfNull(random);
        _random = random;
    }

    public Room GenerateLevelOne()
    {
        var entrance = new GridPosition(5, 0);
        var door = new GridPosition(2, GameConstants.RoomColumns - 1);
        var candidates = KeyCandidates(entrance, door).ToArray();
        var hiddenKey = candidates[_random.Next(candidates.Length)];
        var route = new[] { Facing.Right, Facing.Right, Facing.Up };
        var plateCandidates = candidates
            .Where(position =>
                position != hiddenKey &&
                position.ManhattanDistanceTo(entrance) >= 4 &&
                position.Row >= 1 &&
                position.Column <= GameConstants.RoomColumns - 3 &&
                position.Move(-1, 2) != door)
            .ToArray();
        var pressurePlate = plateCandidates[_random.Next(plateCandidates.Length)];

        return new Room(entrance, door, hiddenKey, pressurePlate, new CaesarRunePuzzle(route));
    }

    private static IEnumerable<GridPosition> KeyCandidates(GridPosition entrance, GridPosition door)
    {
        for (var row = 0; row < GameConstants.RoomRows; row++)
        {
            for (var column = 0; column < GameConstants.RoomColumns; column++)
            {
                var position = new GridPosition(row, column);
                if (position != entrance && position != door && position.ManhattanDistanceTo(entrance) > 1)
                {
                    yield return position;
                }
            }
        }
    }
}

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
        var plateCandidates = candidates
            .Where(position => position != hiddenKey && position.ManhattanDistanceTo(entrance) >= 4)
            .ToArray();
        var pressurePlate = plateCandidates[_random.Next(plateCandidates.Length)];

        return new Room(entrance, door, hiddenKey, pressurePlate);
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
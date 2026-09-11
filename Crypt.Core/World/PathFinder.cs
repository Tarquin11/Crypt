using Crypt.Core.Utility;

namespace Crypt.Core.World;

/// <summary>Breadth-first reachability checks used by the protected-last-path rule.</summary>
public static class PathFinder
{
    private static readonly GridPosition[] CardinalDirections =
    [
        new(-1, 0),
        new(1, 0),
        new(0, -1),
        new(0, 1),
    ];

    public static bool HasPath(Room room, GridPosition start, GridPosition target, GridPosition? blocked = null) =>
        Distances(room, start, blocked)[target.Row, target.Column] >= 0;

    public static int[,] Distances(Room room, GridPosition start, GridPosition? blocked = null)
    {
        var distances = new int[GameConstants.RoomRows, GameConstants.RoomColumns];

        for (var row = 0; row < GameConstants.RoomRows; row++)
        {
            for (var column = 0; column < GameConstants.RoomColumns; column++)
            {
                distances[row, column] = -1;
            }
        }

        if (!room.IsWalkable(start, blocked))
        {
            return distances;
        }

        var queue = new Queue<GridPosition>();
        queue.Enqueue(start);
        distances[start.Row, start.Column] = 0;

        while (queue.TryDequeue(out var current))
        {
            foreach (var direction in CardinalDirections)
            {
                var next = current.Move(direction.Row, direction.Column);
                if (!room.IsInside(next) || distances[next.Row, next.Column] >= 0 || !room.IsWalkable(next, blocked))
                {
                    continue;
                }

                distances[next.Row, next.Column] = distances[current.Row, current.Column] + 1;
                queue.Enqueue(next);
            }
        }

        return distances;
    }
}
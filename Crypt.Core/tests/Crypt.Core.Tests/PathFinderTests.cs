using Crypt.Core.World;

namespace Crypt.Core.Tests;

public sealed class PathFinderTests
{
    private static readonly GridPosition Start = new(0, 0);
    private static readonly GridPosition Middle = new(0, 1);
    private static readonly GridPosition Target = new(0, 2);

    [Fact]
    public void Finds_the_shortest_path_in_a_room()
    {
        var room = new Room(Start, Target, Middle, new GridPosition(1, 0));

        Assert.True(PathFinder.HasPath(room, Start, Target));
        Assert.Equal(2, PathFinder.Distances(room, Start)[Target.Row, Target.Column]);
    }

    [Fact]
    public void Treats_the_only_route_as_blocked()
    {
        var room = CorridorRoom();

        Assert.True(PathFinder.HasPath(room, Start, Target));
        Assert.False(PathFinder.HasPath(room, Start, Target, Middle));
    }

    private static Room CorridorRoom()
    {
        var room = new Room(Start, Target, Middle, new GridPosition(1, 0));

        foreach (var position in room.Positions())
        {
            if (position != Start && position != Middle && position != Target)
            {
                room.TileAt(position).TurnToLava();
            }
        }

        return room;
    }
}
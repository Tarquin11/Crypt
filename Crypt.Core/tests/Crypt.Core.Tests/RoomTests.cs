using Crypt.Core.Entities;
using Crypt.Core.Utility;
using Crypt.Core.World;

namespace Crypt.Core.Tests;

public sealed class RoomTests
{
    private static readonly GridPosition Start = new(0, 0);
    private static readonly GridPosition Bridge = new(0, 1);
    private static readonly GridPosition Key = new(0, 2);
    private static readonly GridPosition Door = new(0, 3);

    [Fact]
    public void Lava_can_remove_the_only_path_to_the_key()
    {
        var room = OnePathRoom();
        var player = new Player(Start);
        var enteredAt = TimeSpan.FromTicks(1);

        player.MoveTo(Bridge);
        room.Enter(player, enteredAt);
        player.MoveTo(Start);
        room.Update(player, enteredAt + GameConstants.CrackDuration);

        Assert.Equal(TileType.Lava, room.TileAt(Bridge).Type);
        Assert.False(room.HasPathToObjective(player));
    }

    [Fact]
    public void Hidden_key_reveals_a_reachable_key_that_the_player_can_collect()
    {
        var start = new GridPosition(5, 0);
        var door = new GridPosition(2, 9);
        var hiddenKey = new GridPosition(5, 4);
        var room = new Room(start, door, hiddenKey, new GridPosition(1, 1));
        var player = new Player(hiddenKey);

        room.Enter(player, TimeSpan.FromTicks(1));

        Assert.True(room.IsKeyRevealed);
        var revealedKey = Assert.IsType<GridPosition>(room.RevealedKey);
        Assert.True(PathFinder.HasPath(room, player.Position, revealedKey));

        player.MoveTo(revealedKey);
        room.Enter(player, TimeSpan.FromTicks(2));

        Assert.True(player.HasKey);
        Assert.Null(room.RevealedKey);
    }

    private static Room OnePathRoom()
    {
        var room = new Room(Start, Door, Key, new GridPosition(1, 0));

        foreach (var position in room.Positions())
        {
            if (position != Start && position != Bridge && position != Key && position != Door)
            {
                room.TileAt(position).TurnToLava();
            }
        }

        return room;
    }
}
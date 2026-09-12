using Crypt.Core.Entities;
using Crypt.Core.Puzzles;
using Crypt.Core.World;

namespace Crypt.Core.Tests;

public sealed class CaesarRunePuzzleTests
{
    [Fact]
    public void Encodes_and_decodes_a_route_without_changing_its_separators()
    {
        var puzzle = new CaesarRunePuzzle([Facing.Right, Facing.Right, Facing.Up]);

        Assert.Equal("HDVW · HDVW · QRUWK", puzzle.EncodedRoute);
        Assert.Equal("EAST · EAST · NORTH", CaesarRunePuzzle.Decode(puzzle.EncodedRoute));
    }

    [Fact]
    public void Correct_decoded_route_reveals_the_key()
    {
        var tablet = new GridPosition(3, 2);
        var room = new Room(
            new GridPosition(5, 0),
            new GridPosition(2, 9),
            new GridPosition(0, 8),
            tablet,
            new CaesarRunePuzzle([Facing.Right, Facing.Right, Facing.Up]));
        var player = new Player(tablet);

        room.Enter(player, TimeSpan.Zero);
        player.MoveTo(new GridPosition(3, 3));
        room.FollowCipherMove(tablet, player);
        player.MoveTo(new GridPosition(3, 4));
        room.FollowCipherMove(new GridPosition(3, 3), player);
        player.MoveTo(new GridPosition(2, 4));
        var message = room.FollowCipherMove(new GridPosition(3, 4), player);

        Assert.True(room.IsKeyRevealed);
        Assert.False(room.IsCipherActive);
        Assert.Contains("key", message!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Wrong_direction_resets_the_decoded_route()
    {
        var tablet = new GridPosition(3, 2);
        var room = new Room(
            new GridPosition(5, 0),
            new GridPosition(2, 9),
            new GridPosition(0, 8),
            tablet,
            new CaesarRunePuzzle([Facing.Right, Facing.Right, Facing.Up]));
        var player = new Player(tablet);

        room.Enter(player, TimeSpan.Zero);
        player.MoveTo(new GridPosition(2, 2));
        room.FollowCipherMove(tablet, player);

        Assert.Equal(0, room.CipherProgress);
        Assert.True(room.IsCipherActive);
    }
}

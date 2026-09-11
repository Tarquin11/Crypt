using Crypt.Core.Utility;
using Crypt.Core.World;

namespace Crypt.Core.Tests;

public sealed class RoomGeneratorTests
{
    [Fact]
    public void Same_seed_places_the_pressure_plate_in_the_same_cell()
    {
        var firstRoom = new RoomGenerator(new Random(42)).GenerateLevelOne();
        var secondRoom = new RoomGenerator(new Random(42)).GenerateLevelOne();

        Assert.Equal(FindPressurePlate(firstRoom), FindPressurePlate(secondRoom));
    }

    [Fact]
    public void Level_one_contains_exactly_one_pressure_plate()
    {
        var room = new RoomGenerator(new Random(42)).GenerateLevelOne();
        var pressurePlateCount = room.Positions().Count(position => room.TileAt(position).IsPressurePlate);

        Assert.Equal(1, pressurePlateCount);
        Assert.Equal(GameConstants.RoomRows * GameConstants.RoomColumns, room.Positions().Count());
    }

    private static GridPosition FindPressurePlate(Room room) =>
        room.Positions().Single(position => room.TileAt(position).IsPressurePlate);
}
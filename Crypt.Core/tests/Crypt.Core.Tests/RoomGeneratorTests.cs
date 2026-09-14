using Crypt.Core.Utility;
using Crypt.Core.Puzzles;
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

    [Fact]
    public void Level_two_has_a_stable_cracked_floor_and_a_special_move_key()
    {
        var room = new RoomGenerator(new Random(42)).GenerateLevelTwo();
        var player = new Crypt.Core.Entities.Player(room.HiddenKey);

        room.Enter(player, TimeSpan.Zero);
        room.Update(player, TimeSpan.FromSeconds(30));

        Assert.False(room.LavaEnabled);
        Assert.False(room.AllowsExplorationKeyReveal);
        Assert.False(room.IsKeyRevealed);
        Assert.True(room.HasSpecialMovePaper);
        Assert.NotNull(room.SpecialMovePaper);
        Assert.NotNull(room.SpecialMoveStart);
        Assert.NotNull(room.SpecialMovePattern);
        Assert.All(room.Positions(), position => Assert.Equal(TileType.Cracked, room.TileAt(position).Type));

        room.RevealKeyFromSpecialMove(player);

        Assert.True(room.IsKeyRevealed);
    }

    [Fact]
    public void Level_two_paper_is_collected_once_and_then_disappears()
    {
        var room = new RoomGenerator(new Random(42)).GenerateLevelTwo();
        var player = new Crypt.Core.Entities.Player(room.SpecialMovePaper!.Value);

        var message = room.Enter(player, TimeSpan.Zero);

        Assert.Equal("You found a folded paper.", message);
        Assert.False(room.HasSpecialMovePaper);
        Assert.True(room.IsSpecialMovePaperCollected);
    }

    [Fact]
    public void Level_three_places_four_math_answer_sigils_on_a_stable_floor()
    {
        var room = new RoomGenerator(new Random(42)).GenerateLevelThree();
        var puzzle = Assert.IsType<AnswerSigilPuzzle>(room.AnswerSigilPuzzle);

        Assert.False(room.LavaEnabled);
        Assert.False(room.AllowsExplorationKeyReveal);
        Assert.Equal(ChallengeTopic.Math, puzzle.Challenge.Topic);
        Assert.Equal(4, puzzle.Positions.Count);
        Assert.Equal(4, puzzle.Positions.Distinct().Count());
        Assert.All(room.Positions(), position => Assert.Equal(TileType.Cracked, room.TileAt(position).Type));
    }

    [Fact]
    public void Level_four_places_a_keyword_book_and_a_vigenere_lectern()
    {
        var room = new RoomGenerator(new Random(42)).GenerateLevelFour();

        Assert.False(room.LavaEnabled);
        Assert.False(room.AllowsExplorationKeyReveal);
        Assert.True(room.HasLibraryBook);
        Assert.NotNull(room.LibraryBook);
        Assert.IsType<VigenereRunePuzzle>(room.CipherPuzzle);
        Assert.Single(room.Positions(), position => room.TileAt(position).IsPressurePlate);
        Assert.All(room.Positions(), position => Assert.Equal(TileType.Cracked, room.TileAt(position).Type));
    }

    [Fact]
    public void Level_five_has_a_stable_floor_for_the_minesweeper_trial()
    {
        var room = new RoomGenerator(new Random(42)).GenerateLevelFive();

        Assert.False(room.LavaEnabled);
        Assert.False(room.AllowsExplorationKeyReveal);
        Assert.False(room.IsKeyRevealed);
        Assert.All(room.Positions(), position => Assert.Equal(TileType.Cracked, room.TileAt(position).Type));
    }

    [Fact]
    public void Level_two_generation_has_exactly_one_troll_outcome_per_four_runs()
    {
        var generator = new RoomGenerator(new Random(42));
        var firstDeck = Enumerable.Range(0, 4)
            .Select(_ => generator.GenerateLevelTwo().TriggersSequenceCheatTrap)
            .ToArray();
        var secondDeck = Enumerable.Range(0, 4)
            .Select(_ => generator.GenerateLevelTwo().TriggersSequenceCheatTrap)
            .ToArray();

        Assert.Equal(1, firstDeck.Count(result => result));
        Assert.Equal(1, secondDeck.Count(result => result));
    }

    private static GridPosition FindPressurePlate(Room room) =>
        room.Positions().Single(position => room.TileAt(position).IsPressurePlate);
}

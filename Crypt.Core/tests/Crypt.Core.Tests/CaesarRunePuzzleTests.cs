using Crypt.Core.Entities;
using Crypt.Core.Puzzles;
using Crypt.Core.World;

namespace Crypt.Core.Tests;

public sealed class CaesarRunePuzzleTests
{
    [Fact]
    public void Encodes_and_decodes_a_generated_phrase()
    {
        var puzzle = new CaesarRunePuzzle("EAST EAST NORTH", 3);

        Assert.NotEqual(puzzle.DecodedText, puzzle.EncodedText);
        Assert.Equal(puzzle.DecodedText, CaesarRunePuzzle.Decode(puzzle.EncodedText, puzzle.Shift));
        Assert.EndsWith(" 3", puzzle.DisplayedRuneText);
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
            new CaesarRunePuzzle("EAST EAST NORTH", 3));
        var player = new Player(tablet);

        room.Enter(player, TimeSpan.Zero);
        var message = room.SolveCipher(player);

        Assert.True(room.IsKeyRevealed);
        Assert.False(room.IsCipherActive);
        Assert.Contains("key", message!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Correct_typed_answer_accepts_letters_without_needing_separators()
    {
        var attempts = new CipherAttemptState(new CaesarRunePuzzle("EAST EAST NORTH", 3));

        attempts.Append("east east north");

        Assert.Equal(CipherSubmission.Correct, attempts.Submit());
        Assert.Equal(3, attempts.AttemptsRemaining);
    }

    [Fact]
    public void Typed_spaces_are_preserved_for_a_readable_answer_field()
    {
        var attempts = new CipherAttemptState(new CaesarRunePuzzle("EAST EAST NORTH", 3));

        attempts.Append("east east north");

        Assert.Equal("EAST EAST NORTH", attempts.Guess);
    }

    [Fact]
    public void Three_wrong_answers_exhaust_the_cipher_attempts()
    {
        var attempts = new CipherAttemptState(new CaesarRunePuzzle("EAST EAST NORTH", 3));

        attempts.Append("west");
        Assert.Equal(CipherSubmission.Incorrect, attempts.Submit());
        attempts.Append("south");
        Assert.Equal(CipherSubmission.Incorrect, attempts.Submit());
        attempts.Append("north");

        Assert.Equal(CipherSubmission.Exhausted, attempts.Submit());
        Assert.Equal(0, attempts.AttemptsRemaining);
    }

    [Fact]
    public void Activated_tablet_stays_safe_while_the_answer_panel_is_open()
    {
        var tablet = new GridPosition(3, 2);
        var room = new Room(
            new GridPosition(5, 0),
            new GridPosition(2, 9),
            new GridPosition(0, 8),
            tablet,
            new CaesarRunePuzzle("EAST EAST NORTH", 3));
        var player = new Player(tablet);

        room.Enter(player, TimeSpan.Zero);
        room.Update(player, TimeSpan.FromSeconds(10));

        Assert.Equal(TileType.Safe, room.TileAt(tablet).Type);
        Assert.True(room.HasPathToObjective(player));
    }

    [Fact]
    public void Randomized_puzzles_have_a_valid_shift_and_matching_plaintext()
    {
        var puzzle = CaesarRunePuzzle.CreateRandom(new Random(42));

        Assert.InRange(puzzle.Shift, 1, 25);
        Assert.Equal(puzzle.DecodedText, CaesarRunePuzzle.Decode(puzzle.EncodedText, puzzle.Shift));
    }

    [Fact]
    public void Accented_latin_letters_are_normalized_to_a_typeable_ascii_phrase()
    {
        var puzzle = new CaesarRunePuzzle("SVERIGE ÄR BÄSTEN", 3);

        Assert.Equal("SVERIGE AR BASTEN", puzzle.DecodedText);
        Assert.Equal(puzzle.DecodedText, CaesarRunePuzzle.Decode(puzzle.EncodedText, puzzle.Shift));
    }
}

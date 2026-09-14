using Crypt.Core.Entities;
using Crypt.Core.Puzzles;
using Crypt.Core.World;

namespace Crypt.Core.Tests;

public sealed class VigenereRunePuzzleTests
{
    [Fact]
    public void Encodes_and_decodes_a_classic_vigenere_example()
    {
        const string plainText = "ATTACK AT DAWN";
        const string key = "LEMON";

        var encrypted = VigenereCipher.Encode(plainText, key);

        Assert.Equal("LXFOPV EF RNHR", encrypted);
        Assert.Equal(plainText, VigenereCipher.Decode(encrypted, key));
    }

    [Fact]
    public void Keyword_puzzle_accepts_the_decrypted_phrase()
    {
        var puzzle = new VigenereRunePuzzle("FIND THE SILENT KEY", "RAVEN");
        var attempt = new CipherAttemptState(puzzle);

        attempt.Append("find the silent key");

        Assert.Equal(CipherSubmission.Correct, attempt.Submit());
        Assert.Equal(puzzle.DecodedText, VigenereCipher.Decode(puzzle.EncodedText, puzzle.Key));
    }

    [Fact]
    public void Library_lectern_requires_collecting_the_book_first()
    {
        var room = new RoomGenerator(new Random(42)).GenerateLevelFour();
        var book = room.LibraryBook!.Value;
        var lectern = room.Positions().Single(position => room.TileAt(position).IsPressurePlate);

        var player = new Player(lectern);
        Assert.Equal("The rune lectern is sealed. Find its keyword book.", room.Enter(player, TimeSpan.Zero));
        Assert.False(room.IsCipherActive);

        player = new Player(book);
        Assert.Equal("You found a keyword book.", room.Enter(player, TimeSpan.Zero));
        Assert.True(room.IsLibraryBookCollected);

        player = new Player(lectern);
        room.Enter(player, TimeSpan.Zero);
        Assert.True(room.IsCipherActive);
    }
}

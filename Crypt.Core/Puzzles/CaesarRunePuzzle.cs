using System.Globalization;
using System.Text;

namespace Crypt.Core.Puzzles;

/// <summary>
/// caesar cipher puzzle that uses a rune-like font to display the encoded text. The player must decode the text by reversing the letter shift.
/// </summary>
public sealed class CaesarRunePuzzle : ITextCipherPuzzle
{
    private static readonly string[] PhraseBank =
    [
        "HASTA EL FINAL VAMOS REAL",
        "HALA MADRID",
        "THE GOAT WALKS AMONG US",
        "RONALDO NEVER MISSES",
        "SIUUUUUUU",
        "THE SNOW SPEAKS FINNISH",
        "THE DUNGEON HAS NO WIFI",
        "THE TREES SPEAK VIETNAMESE",
        "THERE IS NO SPOON",
        "PRESS F TO PAY RESPECTS",
        "THE CAKE IS A LIE",
        "YOUR LIFE DOSENT HAVE A PAUSE BUTTON",
        "THE PRINCESS IS IN ANOTHER CASTLE",
        "YOU SHOULD HAVE BROUGHT A MAP",
        "THIS WAS A BAD IDEA"
    ];

    public CaesarRunePuzzle(string decodedText, int shift)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(decodedText);

        if (shift is < 1 or > 50)
        {
            throw new ArgumentOutOfRangeException(nameof(shift), "A Caesar shift must be from 1 through 50.");
        }

        DecodedText = NormalizePlainText(decodedText);
        Shift = shift;
    }

    public string DecodedText { get; }

    public int Shift { get; }

    public string EncodedText => Encode(DecodedText, Shift);

    /// <summary>The clue shown to the player: encrypted text followed by its rotation count.</summary>
    public string DisplayedRuneText => $"{EncodedText}  {Shift}";

    public string Hint => "A ROTATION MOVES EVERY LETTER. REVERSE IT.";

    public string PanelTitle => "RUNE CIPHER";

    public string HintFooter => "THE NUMBER AFTER THE RUNES IS IMPORTANT.";

    public static CaesarRunePuzzle CreateRandom(Random random)
    {
        ArgumentNullException.ThrowIfNull(random);
        return new CaesarRunePuzzle(PhraseBank[random.Next(PhraseBank.Length)], random.Next(1, 51));
    }

    public static string Encode(string value, int shift)
    {
        ArgumentNullException.ThrowIfNull(value);

        return string.Concat(value.Select(character => ShiftLetter(character, shift)));
    }

    public static string Decode(string value, int shift) => Encode(value, -shift);

    private static char ShiftLetter(char character, int shift)
    {
        if (!char.IsAsciiLetter(character))
        {
            return character;
        }

        var firstLetter = char.IsUpper(character) ? 'A' : 'a';
        var offset = ((character - firstLetter + shift) % 26 + 26) % 26;
        return (char)(firstLetter + offset);
    }

    private static string NormalizePlainText(string value)
    {
        var normalized = new StringBuilder();

        foreach (var character in value.Normalize(NormalizationForm.FormD))
        {
            if (char.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsAsciiLetter(character))
            {
                normalized.Append(char.ToUpperInvariant(character));
            }
            else if (char.IsWhiteSpace(character) && normalized.Length > 0 && normalized[^1] != ' ')
            {
                normalized.Append(' ');
            }
        }

        var result = normalized.ToString().Trim();
        if (result.Length == 0)
        {
            throw new ArgumentException("A cipher phrase needs at least one Latin letter.", nameof(value));
        }

        return result;
    }
}

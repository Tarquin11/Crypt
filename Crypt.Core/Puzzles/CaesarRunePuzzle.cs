using System.Globalization;
using System.Text;
using Crypt.Core.Utility;

namespace Crypt.Core.Puzzles;

/// <summary>
/// caesar cipher puzzle that uses a rune-like font to display the encoded text. The player must decode the text by reversing the letter shift.
/// </summary>
public sealed class CaesarRunePuzzle : ITextCipherPuzzle
{
    private static readonly string[] NoobPhraseBank =
    [
        "FIND THE KEY",
        "OPEN THE DOOR",
        "GO NORTH",
        "GO EAST",
        "THE KEY IS HERE",
    ];

    private static readonly string[] DifficultPhraseBank =
    [
        "HASTA EL FINAL VAMOS REAL",
        "THE GOAT WALKS AMONG US",
        "THE SNOW SPEAKS FINNISH",
        "THE DUNGEON HAS NO WIFI",
        "THE TREES SPEAK VIETNAMESE",
        "PRESS F TO PAY RESPECTS",
        "THE CAKE IS A LIE",
        "THIS WAS A BAD IDEA",
    ];

    private static readonly string[] ExtremePhraseBank =
    [
        "THE PRINCESS IS IN ANOTHER CASTLE",
        "KAZAKHSTAN NUMBER ONE EXPORTER OF POTASSIUM",
        "YOUR LIFE DOSENT HAVE A PAUSE BUTTON",
        "THE EXIT IS NEVER WHAT IT SEEMS",
    ];

    public CaesarRunePuzzle(string decodedText, int shift)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(decodedText);

        if (shift is < 1 or > 25)
        {
            throw new ArgumentOutOfRangeException(
                nameof(shift),
                "A Caesar shift must be from 1 through 25.");
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

    public static CaesarRunePuzzle CreateRandom(Random random) =>
        CreateRandom(random, GameDifficulty.Difficult);

    public static CaesarRunePuzzle CreateRandom(
        Random random,
        GameDifficulty difficulty)
    {
        ArgumentNullException.ThrowIfNull(random);

        var (phrases, minShift, maxShiftExclusive) = difficulty switch
        {
            GameDifficulty.Noob => (NoobPhraseBank, 1, 6),
            GameDifficulty.Extreme => (ExtremePhraseBank, 13, 26),
            _ => (DifficultPhraseBank, 1, 26),
        };

        return new CaesarRunePuzzle(
            phrases[random.Next(phrases.Length)],
            random.Next(minShift, maxShiftExclusive));
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

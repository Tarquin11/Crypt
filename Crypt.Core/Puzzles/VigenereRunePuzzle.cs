using Crypt.Core.Utility;

namespace Crypt.Core.Puzzles;

/// <summary>A Vigenere challenge whose repeating keyword is discovered in the level.</summary>
public sealed class VigenereRunePuzzle : ITextCipherPuzzle
{
    private static readonly (string Phrase, string Key)[] NoobPuzzleBank =
    [
        ("FIND THE KEY", "MOON"),
        ("OPEN THE VAULT", "TOME"),
        ("THE BOOK IS HERE", "RUNE"),
    ];

    private static readonly (string Phrase, string Key)[] DifficultPuzzleBank =
    [
        ("FIND THE SILENT KEY", "RAVEN"),
        ("THE BOOK KNOWS YOUR NAME", "CANDLE"),
        ("THE ARCHIVE WATCHES", "MOON"),
        ("READ BETWEEN THE SHELVES", "TOME"),
        ("THE LIBRARY KEEPS SECRETS", "IVORY"),
        ("WHISPERS GUARD THE STACKS", "QUILL"),
    ];

    private static readonly (string Phrase, string Key)[] ExtremePuzzleBank =
    [
        ("THE LIBRARY FORGETS NOTHING", "OBSIDIAN"),
        ("THE ARCHIVE KNOWS YOUR FEAR", "NIGHTFALL"),
        ("SILENCE HIDES THE FINAL KEY", "CIPHER"),
    ];

    public VigenereRunePuzzle(string decodedText, string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(decodedText);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        DecodedText = Normalize(decodedText, nameof(decodedText));
        Key = Normalize(key, nameof(key)).Replace(" ", string.Empty, StringComparison.Ordinal);
    }

    public string DecodedText { get; }

    /// <summary>The word found in the library book; it repeats across the ciphertext.</summary>
    public string Key { get; }

    public string EncodedText => VigenereCipher.Encode(DecodedText, Key);

    public string DisplayedRuneText => EncodedText;

    public string PanelTitle => "VIGENERE LOCK";

    public string Hint => "THE BOOK HOLDS A REPEATING KEYWORD.";

    public string HintFooter => "USE THE KEYWORD AGAIN AND AGAIN.";

    public static VigenereRunePuzzle CreateRandom(Random random) =>
        CreateRandom(random, GameDifficulty.Difficult);

    public static VigenereRunePuzzle CreateRandom(
        Random random,
        GameDifficulty difficulty)
    {
        ArgumentNullException.ThrowIfNull(random);

        var puzzleBank = difficulty switch
        {
            GameDifficulty.Noob => NoobPuzzleBank,
            GameDifficulty.Extreme => ExtremePuzzleBank,
            _ => DifficultPuzzleBank,
        };

        var (phrase, key) = puzzleBank[random.Next(puzzleBank.Length)];

        return new VigenereRunePuzzle(phrase, key);
    }

    private static string Normalize(string value, string parameterName)
    {
        var normalized = string.Concat(value
            .Where(character => char.IsAsciiLetter(character) || char.IsWhiteSpace(character))
            .Select(character => char.IsAsciiLetter(character) ? char.ToUpperInvariant(character) : ' '));
        normalized = string.Join(' ', normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        if (normalized.Length == 0)
        {
            throw new ArgumentException("A Vigenere puzzle needs Latin letters.", parameterName);
        }

        return normalized;
    }
}

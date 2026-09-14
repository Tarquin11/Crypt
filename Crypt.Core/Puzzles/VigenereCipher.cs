namespace Crypt.Core.Puzzles;

/// <summary>Classical Vigenere encryption used by future text-cipher challenges.</summary>
public static class VigenereCipher
{
    public static string Encode(string plaintext, string key) => Transform(plaintext, key, 1);

    public static string Decode(string ciphertext, string key) => Transform(ciphertext, key, -1);

    private static string Transform(string value, string key, int direction)
    {
        ArgumentNullException.ThrowIfNull(value);
        var usableKey = string.Concat((key ?? string.Empty).Where(char.IsAsciiLetter).Select(char.ToUpperInvariant));
        if (usableKey.Length == 0)
        {
            throw new ArgumentException("A Vigenere key needs at least one letter.", nameof(key));
        }

        var keyIndex = 0;
        return string.Concat(value.Select(character =>
        {
            if (!char.IsAsciiLetter(character))
            {
                return character;
            }

            var shift = direction * (usableKey[keyIndex++ % usableKey.Length] - 'A');
            return CaesarRunePuzzle.Encode(character.ToString(), shift)[0];
        }));
    }
}

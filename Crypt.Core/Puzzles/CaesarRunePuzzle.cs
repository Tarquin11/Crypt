using Crypt.Core.Entities;

namespace Crypt.Core.Puzzles;

/// <summary>
/// A small, inspectable Caesar-cipher puzzle. The player decodes a sequence of
/// cardinal directions, then walks that sequence after activating its rune tablet.
/// </summary>
public sealed class CaesarRunePuzzle
{
    public const int Shift = 3;

    private readonly Facing[] _route;

    public CaesarRunePuzzle(IEnumerable<Facing> route)
    {
        ArgumentNullException.ThrowIfNull(route);
        _route = route.ToArray();

        if (_route.Length == 0)
        {
            throw new ArgumentException("A rune puzzle needs at least one direction.", nameof(route));
        }
    }

    public IReadOnlyList<Facing> Route => _route;

    public string EncodedRoute => Encode(string.Join(" · ", _route.Select(DirectionName)));

    public string Hint => $"TURN EACH RUNE BACK {Shift}.";

    public static string Encode(string value, int shift = Shift)
    {
        ArgumentNullException.ThrowIfNull(value);

        return string.Concat(value.Select(character => ShiftLetter(character, shift)));
    }

    public static string Decode(string value, int shift = Shift) => Encode(value, -shift);

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

    private static string DirectionName(Facing direction) => direction switch
    {
        Facing.Up => "NORTH",
        Facing.Down => "SOUTH",
        Facing.Left => "WEST",
        Facing.Right => "EAST",
        _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
    };
}

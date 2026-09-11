namespace Crypt.Core.World;

/// <summary>Mutable state for a single room tile.</summary>
public sealed class Tile
{
    private TimeSpan _crackedAt;

    public TileType Type { get; private set; } = TileType.Safe;

    public bool IsPressurePlate { get; set; }

    public void Crack(TimeSpan now)
    {
        if (Type != TileType.Safe)
        {
            return;
        }

        Type = TileType.Cracked;
        _crackedAt = now;
    }

    public bool HasCrackedFor(TimeSpan now, TimeSpan duration) => Type == TileType.Cracked && now - _crackedAt >= duration;

    public bool Stabilize()
    {
        if (Type != TileType.Cracked)
        {
            return false;
        }

        Type = TileType.Safe;
        _crackedAt = TimeSpan.Zero;
        return true;
    }

    public void TurnToLava() => Type = TileType.Lava;
}
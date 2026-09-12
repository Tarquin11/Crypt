using Crypt.Core.Entities;
using Crypt.Core.Puzzles;
using Crypt.Core.Utility;

namespace Crypt.Core.World;

/// <summary>Owns level-one tile decay, key logic, and objective reachability.</summary>
public sealed class Room
{
    private static readonly GridPosition[] CardinalDirections =
    [
        new(-1, 0),
        new(1, 0),
        new(0, -1),
        new(0, 1),
    ];

    private readonly Tile[,] _tiles = new Tile[GameConstants.RoomRows, GameConstants.RoomColumns];
    private TimeSpan _lastSpreadAt;

    public Room(
        GridPosition entrance,
        GridPosition door,
        GridPosition hiddenKey,
        GridPosition pressurePlate,
        CaesarRunePuzzle? cipherPuzzle = null)
    {
        Entrance = entrance;
        Door = door;
        HiddenKey = hiddenKey;
        CipherPuzzle = cipherPuzzle;

        for (var row = 0; row < GameConstants.RoomRows; row++)
        {
            for (var column = 0; column < GameConstants.RoomColumns; column++)
            {
                _tiles[row, column] = new Tile();
            }
        }

        TileAt(pressurePlate).IsPressurePlate = true;
    }

    public GridPosition Entrance { get; }

    public GridPosition Door { get; }

    public GridPosition HiddenKey { get; }

    public GridPosition? RevealedKey { get; private set; }

    public bool IsKeyRevealed { get; private set; }

    public CaesarRunePuzzle? CipherPuzzle { get; }

    public bool IsCipherActive { get; private set; }

    public int CipherProgress { get; private set; }

    public string? Enter(Player player, TimeSpan now)
    {
        var position = player.Position;
        var tile = TileAt(position);
        string? gameEvent = null;

        if (!IsKeyRevealed && CipherPuzzle is null && position == HiddenKey)
        {
            gameEvent = RevealKey(player, fromPressurePlate: false);
        }
        else if (!IsKeyRevealed && tile.IsPressurePlate)
        {
            gameEvent = CipherPuzzle is null
                ? RevealKey(player, fromPressurePlate: true)
                : ActivateCipher();
        }

        if (IsKeyRevealed && RevealedKey is { } revealedKey && position == revealedKey)
        {
            player.CollectKey();
            RevealedKey = null;
            gameEvent = "Key secured. Reach the locked door!";
        }

        Crack(position, now);
        return gameEvent;
    }

    /// <summary>Records a completed move after the rune tablet has been activated.</summary>
    public string? FollowCipherMove(GridPosition from, Player player)
    {
        if (CipherPuzzle is null || !IsCipherActive || IsKeyRevealed)
        {
            return null;
        }

        var direction = DirectionFrom(from, player.Position);
        if (direction is null || direction != CipherPuzzle.Route[CipherProgress])
        {
            CipherProgress = 0;
            return "The rune sequence fades. Begin the decoded route again.";
        }

        CipherProgress++;
        if (CipherProgress < CipherPuzzle.Route.Count)
        {
            return $"A rune answers ({CipherProgress}/{CipherPuzzle.Route.Count}).";
        }

        IsCipherActive = false;
        return RevealKey(player, fromPressurePlate: true) ?? "The final rune answers, but nothing moves.";
    }

    public string? Update(Player player, TimeSpan now)
    {
        var forcedReveal = ForceRevealIfNeeded(player);
        ConvertExpiredCracks(player, now);
        SpreadLava(player, now);
        return forcedReveal ?? ForceRevealIfNeeded(player);
    }

    public bool IsInside(GridPosition position) =>
        position.Row >= 0 && position.Row < GameConstants.RoomRows &&
        position.Column >= 0 && position.Column < GameConstants.RoomColumns;

    public bool IsWalkable(GridPosition position, GridPosition? blocked = null) =>
        IsInside(position) &&
        (!blocked.HasValue || position != blocked.Value) &&
        TileAt(position).Type != TileType.Lava;

    public bool IsDoor(GridPosition position) => position == Door;

    public bool IsLava(GridPosition position) => TileAt(position).Type == TileType.Lava;

    public bool HasPathToObjective(Player player)
    {
        var objective = CurrentObjective(player);
        return objective.HasValue && PathFinder.HasPath(this, player.Position, objective.Value);
    }

    public Tile TileAt(GridPosition position)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(position.Row);
        ArgumentOutOfRangeException.ThrowIfNegative(position.Column);

        if (!IsInside(position))
        {
            throw new ArgumentOutOfRangeException(nameof(position), "The position is outside this room.");
        }

        return _tiles[position.Row, position.Column];
    }

    public int CountSafeTiles()
    {
        var safeTiles = 0;

        foreach (var position in Positions())
        {
            if (TileAt(position).Type == TileType.Safe)
            {
                safeTiles++;
            }
        }

        return safeTiles;
    }

    public IEnumerable<GridPosition> Positions()
    {
        for (var row = 0; row < GameConstants.RoomRows; row++)
        {
            for (var column = 0; column < GameConstants.RoomColumns; column++)
            {
                yield return new GridPosition(row, column);
            }
        }
    }

    private void Crack(GridPosition position, TimeSpan now)
    {
        if (position != Entrance && position != Door)
        {
            TileAt(position).Crack(now);
        }
    }

    private void ConvertExpiredCracks(Player player, TimeSpan now)
    {
        foreach (var position in Positions())
        {
            var tile = TileAt(position);
            if (tile.HasCrackedFor(now, GameConstants.CrackDuration) && CanTurnToLava(player, position))
            {
                tile.TurnToLava();
            }
        }
    }

    private void SpreadLava(Player player, TimeSpan now)
    {
        if (now - _lastSpreadAt < GameConstants.LavaSpreadDelay)
        {
            return;
        }

        _lastSpreadAt = now;
        var lavaTiles = Positions().Where(position => TileAt(position).Type == TileType.Lava).ToArray();

        foreach (var lavaTile in lavaTiles)
        {
            foreach (var direction in CardinalDirections)
            {
                var neighbor = lavaTile.Move(direction.Row, direction.Column);
                if (IsInside(neighbor) &&
                    TileAt(neighbor).HasCrackedFor(now, GameConstants.LavaSpreadDelay) &&
                    CanTurnToLava(player, neighbor))
                {
                    TileAt(neighbor).TurnToLava();
                }
            }
        }
    }

    private bool CanTurnToLava(Player player, GridPosition candidate) =>
        candidate != player.Position && candidate != Entrance && candidate != Door;

    private string? ForceRevealIfNeeded(Player player)
    {
        if (CipherPuzzle is not null || IsKeyRevealed || player.HasKey || CountSafeTiles() > GameConstants.ForceRevealSafeTiles)
        {
            return null;
        }

        var forcedLocation = FindKeyLocation(player, chooseFarthest: false);
        if (!forcedLocation.HasValue)
        {
            return null;
        }

        IsKeyRevealed = true;
        RevealedKey = forcedLocation;
        return "A key flashes nearby!";
    }

    private string? RevealKey(Player player, bool fromPressurePlate)
    {
        var location = FindKeyLocation(player, chooseFarthest: true);
        if (!location.HasValue)
        {
            return null;
        }

        IsKeyRevealed = true;
        RevealedKey = location;
        return fromPressurePlate
            ? "The pressure plate reveals a key in the dungeon."
            : "A hidden mechanism reveals a key across the room!";
    }

    private GridPosition? CurrentObjective(Player player)
    {
        if (player.HasKey)
        {
            return Door;
        }

        if (IsKeyRevealed)
        {
            return RevealedKey;
        }

        return CipherPuzzle is null ? HiddenKey : FindPressurePlate();
    }

    private string? ActivateCipher()
    {
        if (IsCipherActive)
        {
            return null;
        }

        IsCipherActive = true;
        CipherProgress = 0;
        return $"RUNES: {CipherPuzzle!.EncodedRoute}\n{CipherPuzzle.Hint}";
    }

    private GridPosition FindPressurePlate() =>
        Positions().Single(position => TileAt(position).IsPressurePlate);

    private static Facing? DirectionFrom(GridPosition from, GridPosition to) => (to.Row - from.Row, to.Column - from.Column) switch
    {
        (-1, 0) => Facing.Up,
        (1, 0) => Facing.Down,
        (0, -1) => Facing.Left,
        (0, 1) => Facing.Right,
        _ => null,
    };

    private GridPosition? FindKeyLocation(Player player, bool chooseFarthest)
    {
        var distances = PathFinder.Distances(this, player.Position);
        GridPosition? selection = null;
        var selectedDistance = chooseFarthest ? -1 : int.MaxValue;

        foreach (var candidate in KeyCandidates())
        {
            if (candidate == player.Position || candidate == Entrance || candidate == Door || TileAt(candidate).Type == TileType.Lava)
            {
                continue;
            }

            var distance = distances[candidate.Row, candidate.Column];
            if (distance < 0)
            {
                continue;
            }

            if ((chooseFarthest && distance > selectedDistance) || (!chooseFarthest && distance < selectedDistance))
            {
                selection = candidate;
                selectedDistance = distance;
            }
        }

        return selection;
    }

    private IEnumerable<GridPosition> KeyCandidates() =>
        Positions().Where(candidate => candidate != Entrance && candidate != Door && candidate.ManhattanDistanceTo(Entrance) > 1);
}

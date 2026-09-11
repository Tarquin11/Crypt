using Crypt.Core.Entities;
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

    public Room(GridPosition entrance, GridPosition door, GridPosition hiddenKey, GridPosition pressurePlate)
    {
        Entrance = entrance;
        Door = door;
        HiddenKey = hiddenKey;

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

    public string? Enter(Player player, TimeSpan now)
    {
        var position = player.Position;
        var tile = TileAt(position);
        string? gameEvent = null;

        if (!IsKeyRevealed && position == HiddenKey)
        {
            gameEvent = RevealKey(player, fromPressurePlate: false);
        }
        else if (!IsKeyRevealed && tile.IsPressurePlate)
        {
            gameEvent = RevealKey(player, fromPressurePlate: true);
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
        if (IsKeyRevealed || player.HasKey || CountSafeTiles() > GameConstants.ForceRevealSafeTiles)
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

        return IsKeyRevealed ? RevealedKey : HiddenKey;
    }

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
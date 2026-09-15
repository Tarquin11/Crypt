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
        GridPosition? pressurePlate,
        ITextCipherPuzzle? cipherPuzzle = null,
        bool lavaEnabled = true,
        bool allowsExplorationKeyReveal = true,
        SpecialMovePattern? specialMovePattern = null,
        GridPosition? specialMoveStart = null,
        GridPosition? specialMovePaper = null,
        bool triggersSequenceCheatTrap = false,
        AnswerSigilPuzzle? answerSigilPuzzle = null,
        GridPosition? libraryBook = null,
        IReadOnlyList<EchoDecoySymbol>? echoDecoys = null)
    {
        Entrance = entrance;
        Door = door;
        HiddenKey = hiddenKey;
        CipherPuzzle = cipherPuzzle;
        LavaEnabled = lavaEnabled;
        AllowsExplorationKeyReveal = allowsExplorationKeyReveal;
        SpecialMovePattern = specialMovePattern;
        SpecialMoveStart = specialMoveStart;
        SpecialMovePaper = specialMovePaper;
        HasSpecialMovePaper = specialMovePaper.HasValue;
        TriggersSequenceCheatTrap = triggersSequenceCheatTrap;
        AnswerSigilPuzzle = answerSigilPuzzle;
        LibraryBook = libraryBook;
        HasLibraryBook = libraryBook.HasValue;
        EchoDecoys = echoDecoys ?? Array.Empty<EchoDecoySymbol>();

        if (SpecialMovePattern is not null && (!SpecialMoveStart.HasValue || !SpecialMovePaper.HasValue))
        {
            throw new ArgumentException("A special-move puzzle needs both a start tile and a paper tile.");
        }

        for (var row = 0; row < GameConstants.RoomRows; row++)
        {
            for (var column = 0; column < GameConstants.RoomColumns; column++)
            {
                _tiles[row, column] = new Tile();
            }
        }

        if (pressurePlate is { } tablet)
        {
            TileAt(tablet).IsPressurePlate = true;
        }

        if (!LavaEnabled)
        {
            foreach (var position in Positions())
            {
                TileAt(position).Crack(TimeSpan.Zero);
            }
        }
    }

    public GridPosition Entrance { get; }

    public GridPosition Door { get; }

    public GridPosition HiddenKey { get; }

    public GridPosition? RevealedKey { get; private set; }

    public bool IsKeyRevealed { get; private set; }

    public ITextCipherPuzzle? CipherPuzzle { get; }

    public bool LavaEnabled { get; }

    public bool AllowsExplorationKeyReveal { get; }

    public SpecialMovePattern? SpecialMovePattern { get; }

    public GridPosition? SpecialMoveStart { get; }

    public GridPosition? SpecialMovePaper { get; }

    public bool HasSpecialMovePaper { get; private set; }

    public bool IsSpecialMovePaperCollected => SpecialMovePaper.HasValue && !HasSpecialMovePaper;

    /// <summary>One-in-four completed level-two sequences trigger a playful anti-screenshot trap.</summary>
    public bool TriggersSequenceCheatTrap { get; }

    public AnswerSigilPuzzle? AnswerSigilPuzzle { get; }

    /// <summary>A one-time book that reveals the keyword for the library cipher.</summary>
    public GridPosition? LibraryBook { get; }

    public bool HasLibraryBook { get; private set; }

    public bool IsLibraryBookCollected => LibraryBook.HasValue && !HasLibraryBook;

    public IReadOnlyList<EchoDecoySymbol> EchoDecoys { get; }

    public bool IsEchoRoom => EchoDecoys.Count > 0;

    public bool IsCipherActive { get; private set; }

    public string? Enter(Player player, TimeSpan now)
    {
        var position = player.Position;
        var tile = TileAt(position);
        string? gameEvent = null;

        if (HasLibraryBook && position == LibraryBook)
        {
            HasLibraryBook = false;
            gameEvent = "You found a keyword book.";
        }
        else if (HasSpecialMovePaper && position == SpecialMovePaper)
        {
            HasSpecialMovePaper = false;
            gameEvent = "You found a folded paper.";
        }
        else if (!IsKeyRevealed && AllowsExplorationKeyReveal && CipherPuzzle is null && position == HiddenKey)
        {
            gameEvent = RevealKey(player, fromPressurePlate: false);
        }
        else if (!IsKeyRevealed && tile.IsPressurePlate)
        {
            gameEvent = CipherPuzzle is null
                ? RevealKey(player, fromPressurePlate: true)
                : CanActivateCipher()
                    ? ActivateCipher()
                    : "The rune lectern is sealed. Find its keyword book.";
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

    public string SolveCipher(Player player)
    {
        if (CipherPuzzle is null || !IsCipherActive)
        {
            throw new InvalidOperationException("There is no active cipher to solve.");
        }

        IsCipherActive = false;
        return RevealKey(player, fromPressurePlate: true) ?? "The runes answer, but the key has nowhere to appear.";
    }

    public void RejectCipher() => IsCipherActive = false;

    public string RevealKeyFromSpecialMove(Player player)
    {
        if (IsKeyRevealed)
        {
            return "The hidden key is already revealed.";
        }

        return RevealKey(player, fromPressurePlate: false) ?? "The floor shifts, but no key appears.";
    }

    public string RevealKeyFromMinefield(Player player)
    {
        if (IsKeyRevealed)
        {
            return "The minefield has already revealed the key.";
        }

        return RevealKey(player, fromPressurePlate: false) ?? "The minefield unlocks, but no key appears.";
    }

    public string? Update(Player player, TimeSpan now)
    {
        if (!LavaEnabled)
        {
            return null;
        }

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
        // The rune tablet is a permanent landmark. Letting it crack during the
        // dialogue made it turn to lava as soon as the player started the route.
        if (LavaEnabled && position != Entrance && position != Door && !IsCipherTablet(position))
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
        if (!AllowsExplorationKeyReveal || CipherPuzzle is not null || IsKeyRevealed || player.HasKey || CountSafeTiles() > GameConstants.ForceRevealSafeTiles)
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
        if (fromPressurePlate && LibraryBook.HasValue)
        {
            return "The lectern opens a hidden compartment. A key appears in the library.";
        }

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

        // The player already knows the next direction from the decoded clue.
        // During that short sequence, reachability to the tablet is irrelevant
        // and can be false after the player deliberately walks away from it.
        if (IsCipherActive)
        {
            return player.Position;
        }

        if (!AllowsExplorationKeyReveal)
        {
            return player.Position;
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
        return "A cipher panel unfolds from the rune tablet.";
    }

    private bool CanActivateCipher() => !LibraryBook.HasValue || !HasLibraryBook;

    private GridPosition FindPressurePlate() =>
        Positions().Single(position => TileAt(position).IsPressurePlate);

    private bool IsCipherTablet(GridPosition position) =>
        CipherPuzzle is not null && TileAt(position).IsPressurePlate;

    private GridPosition? FindKeyLocation(Player player, bool chooseFarthest)
    {
        var distances = PathFinder.Distances(this, player.Position);
        GridPosition? selection = null;
        var selectedDistance = chooseFarthest ? -1 : int.MaxValue;

        foreach (var candidate in KeyCandidates())
        {
            if (candidate == player.Position ||
                candidate == Entrance ||
                candidate == Door ||
                candidate == LibraryBook ||
                (CipherPuzzle is not null && TileAt(candidate).IsPressurePlate) ||
                TileAt(candidate).Type == TileType.Lava)
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

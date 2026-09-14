using Crypt.Core.Utility;
using Crypt.Core.World;

namespace Crypt.Core.Puzzles;

/// <summary>
/// A small, forgiving Minesweeper board. Mines are placed after the first reveal,
/// so the opening cell and all of its neighbors are always safe.
/// </summary>
public sealed class MinesweeperPuzzle
{
    private readonly Random _random;
    private readonly MineCell[,] _cells;
    private bool _minesPlaced;
    private int _safeCellsRemaining;

    public MinesweeperPuzzle(Random? random = null)
        : this(GameConstants.RoomRows, GameConstants.RoomColumns, 8, random)
    {
    }

    public MinesweeperPuzzle(int rows, int columns, int mineCount, Random? random = null)
    {
        if (rows < 1 || columns < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(rows), "A minefield needs at least one row and column.");
        }

        if (mineCount < 0 || mineCount >= rows * columns)
        {
            throw new ArgumentOutOfRangeException(nameof(mineCount), "Mine count must leave at least one safe cell.");
        }

        Rows = rows;
        Columns = columns;
        MineCount = mineCount;
        _random = random ?? new Random();
        _cells = new MineCell[rows, columns];
        _safeCellsRemaining = rows * columns - mineCount;
    }

    public int Rows { get; }

    public int Columns { get; }

    public int MineCount { get; }

    public bool IsGenerated => _minesPlaced;

    public bool IsComplete { get; private set; }

    public bool IsExploded { get; private set; }

    public int FlagsPlaced => Positions().Count(position => _cells[position.Row, position.Column].IsFlagged);

    public int SafeCellsRemaining => _safeCellsRemaining;

    public MineCellView GetCell(GridPosition position)
    {
        EnsureInside(position);
        var cell = _cells[position.Row, position.Column];
        return new MineCellView(cell.IsRevealed, cell.IsFlagged, cell.IsMine, cell.AdjacentMines);
    }

    public MineActionResult Reveal(GridPosition position)
    {
        EnsureInside(position);

        if (IsComplete || IsExploded)
        {
            return MineActionResult.Ignored;
        }

        if (!_minesPlaced)
        {
            PlaceMines(position);
        }

        ref var cell = ref _cells[position.Row, position.Column];
        if (cell.IsRevealed || cell.IsFlagged)
        {
            return MineActionResult.Ignored;
        }

        if (cell.IsMine)
        {
            cell.IsRevealed = true;
            IsExploded = true;
            return MineActionResult.MineHit;
        }

        RevealSafeArea(position);
        if (_safeCellsRemaining == 0)
        {
            IsComplete = true;
            return MineActionResult.Cleared;
        }

        return MineActionResult.Revealed;
    }

    public MineActionResult ToggleFlag(GridPosition position)
    {
        EnsureInside(position);

        if (IsComplete || IsExploded)
        {
            return MineActionResult.Ignored;
        }

        ref var cell = ref _cells[position.Row, position.Column];
        if (cell.IsRevealed)
        {
            return MineActionResult.Ignored;
        }

        cell.IsFlagged = !cell.IsFlagged;
        return cell.IsFlagged ? MineActionResult.Flagged : MineActionResult.Unflagged;
    }

    private void PlaceMines(GridPosition firstReveal)
    {
        var candidates = Positions()
            .Where(position => Math.Abs(position.Row - firstReveal.Row) > 1 ||
                Math.Abs(position.Column - firstReveal.Column) > 1)
            .ToList();

        // A corner can only exclude four cells. Keep the configured board valid
        // even if a future level asks for more mines than that safe opening allows.
        if (candidates.Count < MineCount)
        {
            candidates = Positions().Where(position => position != firstReveal).ToList();
        }

        for (var index = candidates.Count - 1; index > 0; index--)
        {
            var swapIndex = _random.Next(index + 1);
            (candidates[index], candidates[swapIndex]) = (candidates[swapIndex], candidates[index]);
        }

        foreach (var position in candidates.Take(MineCount))
        {
            _cells[position.Row, position.Column].IsMine = true;
        }

        foreach (var position in Positions())
        {
            _cells[position.Row, position.Column].AdjacentMines = Neighbors(position)
                .Count(neighbor => _cells[neighbor.Row, neighbor.Column].IsMine);
        }

        _minesPlaced = true;
    }

    private void RevealSafeArea(GridPosition start)
    {
        var pending = new Queue<GridPosition>();
        pending.Enqueue(start);

        while (pending.Count > 0)
        {
            var position = pending.Dequeue();
            ref var cell = ref _cells[position.Row, position.Column];
            if (cell.IsRevealed || cell.IsFlagged || cell.IsMine)
            {
                continue;
            }

            cell.IsRevealed = true;
            _safeCellsRemaining--;

            if (cell.AdjacentMines != 0)
            {
                continue;
            }

            foreach (var neighbor in Neighbors(position))
            {
                if (!_cells[neighbor.Row, neighbor.Column].IsRevealed && !_cells[neighbor.Row, neighbor.Column].IsMine)
                {
                    pending.Enqueue(neighbor);
                }
            }
        }
    }

    private IEnumerable<GridPosition> Positions()
    {
        for (var row = 0; row < Rows; row++)
        {
            for (var column = 0; column < Columns; column++)
            {
                yield return new GridPosition(row, column);
            }
        }
    }

    private IEnumerable<GridPosition> Neighbors(GridPosition position)
    {
        for (var rowOffset = -1; rowOffset <= 1; rowOffset++)
        {
            for (var columnOffset = -1; columnOffset <= 1; columnOffset++)
            {
                if (rowOffset == 0 && columnOffset == 0)
                {
                    continue;
                }

                var neighbor = position.Move(rowOffset, columnOffset);
                if (neighbor.Row >= 0 && neighbor.Row < Rows && neighbor.Column >= 0 && neighbor.Column < Columns)
                {
                    yield return neighbor;
                }
            }
        }
    }

    private void EnsureInside(GridPosition position)
    {
        if (position.Row < 0 || position.Row >= Rows || position.Column < 0 || position.Column >= Columns)
        {
            throw new ArgumentOutOfRangeException(nameof(position), "The cell is outside the minefield.");
        }
    }

    private struct MineCell
    {
        public bool IsMine;
        public bool IsRevealed;
        public bool IsFlagged;
        public int AdjacentMines;
    }
}

public readonly record struct MineCellView(bool IsRevealed, bool IsFlagged, bool IsMine, int AdjacentMines);

public enum MineActionResult
{
    Ignored,
    Revealed,
    Flagged,
    Unflagged,
    MineHit,
    Cleared,
}

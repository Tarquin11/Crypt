using Crypt.Core.Puzzles;
using Crypt.Core.Utility;
using Crypt.Core.World;

namespace Crypt.Core.Tests;

public sealed class MinesweeperPuzzleTests
{
    [Fact]
    public void First_reveal_and_its_neighbors_are_always_safe()
    {
        var puzzle = new MinesweeperPuzzle(new Random(42));
        var firstCell = new GridPosition(4, 5);

        var result = puzzle.Reveal(firstCell);

        Assert.NotEqual(MineActionResult.MineHit, result);
        Assert.True(puzzle.IsGenerated);
        Assert.True(puzzle.GetCell(firstCell).IsRevealed);
        Assert.Equal(0, puzzle.GetCell(firstCell).AdjacentMines);

        for (var row = firstCell.Row - 1; row <= firstCell.Row + 1; row++)
        {
            for (var column = firstCell.Column - 1; column <= firstCell.Column + 1; column++)
            {
                Assert.False(puzzle.GetCell(new GridPosition(row, column)).IsMine);
            }
        }
    }

    [Fact]
    public void Standard_level_has_only_eight_mines()
    {
        var puzzle = new MinesweeperPuzzle(new Random(42));

        puzzle.Reveal(new GridPosition(0, 0));

        var mines = Enumerable.Range(0, puzzle.Rows)
            .SelectMany(row => Enumerable.Range(0, puzzle.Columns)
                .Select(column => puzzle.GetCell(new GridPosition(row, column))))
            .Count(cell => cell.IsMine);

        Assert.Equal(8, mines);
    }

    [Fact]
    public void Right_click_flag_can_be_added_and_removed_before_a_reveal()
    {
        var puzzle = new MinesweeperPuzzle(new Random(42));
        var position = new GridPosition(1, 1);

        Assert.Equal(MineActionResult.Flagged, puzzle.ToggleFlag(position));
        Assert.True(puzzle.GetCell(position).IsFlagged);
        Assert.Equal(1, puzzle.FlagsPlaced);

        Assert.Equal(MineActionResult.Unflagged, puzzle.ToggleFlag(position));
        Assert.False(puzzle.GetCell(position).IsFlagged);
        Assert.Equal(0, puzzle.FlagsPlaced);
    }

    [Fact]
    public void Clearing_every_safe_cell_completes_the_board()
    {
        var puzzle = new MinesweeperPuzzle(2, 2, 0, new Random(42));

        var result = puzzle.Reveal(new GridPosition(0, 0));

        Assert.Equal(MineActionResult.Cleared, result);
        Assert.True(puzzle.IsComplete);
        Assert.Equal(0, puzzle.SafeCellsRemaining);
    }
}

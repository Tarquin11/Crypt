using Crypt.Core.Entities;
using Crypt.Core.Puzzles;
using Crypt.Core.World;

namespace Crypt.Core.Tests;

public sealed class MoveSequencePuzzleTests
{
    [Fact]
    public void Level_two_sequence_solves_after_the_tenth_move()
    {
        var puzzle = new MoveSequencePuzzle(
        [
            Facing.Up,
            Facing.Up,
            Facing.Down,
            Facing.Down,
            Facing.Left,
            Facing.Right,
            Facing.Left,
            Facing.Right,
            Facing.Left,
            Facing.Left,
        ], new GridPosition(4, 3));

        foreach (var move in puzzle.Sequence.Take(puzzle.Sequence.Count - 1))
        {
            Assert.Equal(MoveSequenceResult.CorrectStep, puzzle.Register(puzzle.StartPosition, move));
        }

        Assert.Equal(MoveSequenceResult.Solved, puzzle.Register(puzzle.StartPosition, Facing.Left));
        Assert.True(puzzle.IsSolved);
    }

    [Fact]
    public void Pattern_only_starts_at_its_strict_coordinate()
    {
        var puzzle = new MoveSequencePuzzle([Facing.Up, Facing.Up, Facing.Down], new GridPosition(4, 3));

        Assert.Equal(MoveSequenceResult.NotAtStart, puzzle.Register(new GridPosition(4, 2), Facing.Up));
        Assert.Equal(MoveSequenceResult.CorrectStep, puzzle.Register(new GridPosition(4, 3), Facing.Up));
        Assert.Equal(MoveSequenceResult.Reset, puzzle.Register(new GridPosition(3, 3), Facing.Left));
        Assert.Equal(0, puzzle.Progress);
    }

    [Fact]
    public void Catalog_has_multiple_patterns_to_choose_from()
    {
        var names = Enumerable.Range(0, 30)
            .Select(seed => SpecialMovePatternCatalog.Pick(new Random(seed)).Name)
            .Distinct()
            .ToArray();

        Assert.True(SpecialMovePatternCatalog.Count >= 6);
        Assert.True(names.Length >= 3);
    }
}

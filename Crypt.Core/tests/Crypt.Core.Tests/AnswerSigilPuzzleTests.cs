using Crypt.Core.Puzzles;
using Crypt.Core.World;

namespace Crypt.Core.Tests;

public sealed class AnswerSigilPuzzleTests
{
    private static readonly MultipleChoiceChallenge Challenge = new(
        3,
        ChallengeTopic.Math,
        "What is 7 x 8?",
        ["48", "54", "56", "64"],
        CorrectAnswerIndex: 2,
        MaximumAttempts: 1);

    [Fact]
    public void Correct_sigil_solves_the_trial()
    {
        var positions = FourPositions();
        var puzzle = new AnswerSigilPuzzle(Challenge, positions);

        var result = puzzle.Choose(positions[2]);

        Assert.Equal(AnswerSigilResult.Correct, result);
        Assert.True(puzzle.IsSolved);
        Assert.True(puzzle.IsComplete);
    }

    [Fact]
    public void Wrong_sigil_exhausts_the_one_allowed_attempt()
    {
        var positions = FourPositions();
        var puzzle = new AnswerSigilPuzzle(Challenge, positions);

        var result = puzzle.Choose(positions[0]);

        Assert.Equal(AnswerSigilResult.Exhausted, result);
        Assert.False(puzzle.IsSolved);
        Assert.True(puzzle.IsComplete);
    }

    private static GridPosition[] FourPositions() =>
    [
        new GridPosition(1, 1),
        new GridPosition(1, 3),
        new GridPosition(3, 1),
        new GridPosition(3, 3),
    ];
}

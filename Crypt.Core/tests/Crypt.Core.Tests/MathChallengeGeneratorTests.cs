using Crypt.Core.Puzzles;

namespace Crypt.Core.Tests;

public sealed class MathChallengeGeneratorTests
{
    [Fact]
    public void Generates_square_root_linear_and_bracket_equation_trials()
    {
        var questions = Enumerable.Range(0, 80)
            .Select(seed => MathChallengeGenerator.Create(new Random(seed)))
            .ToArray();

        Assert.Contains(questions, question => question.Question.StartsWith("SQRT(", StringComparison.Ordinal));
        Assert.Contains(questions, question => question.Question.Contains("X +", StringComparison.Ordinal));
        Assert.Contains(questions, question => question.Question.Contains("(X +", StringComparison.Ordinal));
    }

    [Fact]
    public void Time_limit_gets_shorter_for_more_complex_equations()
    {
        var limits = Enumerable.Range(0, 80)
            .Select(seed => MathChallengeGenerator.Create(new Random(seed)).TimeLimit)
            .Distinct()
            .ToArray();

        Assert.Contains(TimeSpan.FromSeconds(10), limits);
        Assert.Contains(TimeSpan.FromSeconds(8), limits);
        Assert.Contains(TimeSpan.FromSeconds(6), limits);
    }
}

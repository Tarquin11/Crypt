namespace Crypt.Core.Puzzles;

/// <summary>Creates fresh level-three math trials with a timer matched to their complexity.</summary>
public static class MathChallengeGenerator
{
    public static MultipleChoiceChallenge Create(Random random)
    {
        ArgumentNullException.ThrowIfNull(random);

        return random.Next(3) switch
        {
            0 => CreateSquareRoot(random),
            1 => CreateLinearEquation(random),
            _ => CreateBracketEquation(random),
        };
    }

    private static MultipleChoiceChallenge CreateSquareRoot(Random random)
    {
        var root = random.Next(3, 13);
        return CreateChallenge(
            $"SQRT({root * root}) = ?",
            root,
            TimeSpan.FromSeconds(10),
            random);
    }

    private static MultipleChoiceChallenge CreateLinearEquation(Random random)
    {
        var coefficient = random.Next(2, 6);
        var solution = random.Next(-5, 6);
        var constant = random.Next(-10, 11);
        var total = (coefficient * solution) + constant;
        var constantText = constant < 0 ? $"- {Math.Abs(constant)}" : $"+ {constant}";

        return CreateChallenge(
            $"{coefficient}X {constantText} = {total}. X = ?",
            solution,
            TimeSpan.FromSeconds(8),
            random);
    }

    private static MultipleChoiceChallenge CreateBracketEquation(Random random)
    {
        var coefficient = random.Next(2, 5);
        var solution = random.Next(-3, 9);
        var offset = random.Next(2, 7);
        var total = coefficient * (solution + offset);

        return CreateChallenge(
            $"{coefficient}(X + {offset}) = {total}. X = ?",
            solution,
            TimeSpan.FromSeconds(6),
            random);
    }

    private static MultipleChoiceChallenge CreateChallenge(string question, int answer, TimeSpan timeLimit, Random random)
    {
        var choices = new HashSet<int> { answer };
        while (choices.Count < 4)
        {
            var distance = random.Next(1, 6);
            choices.Add(answer + (random.Next(2) == 0 ? -distance : distance));
        }

        var answers = choices.Select(value => value.ToString()).ToList();
        for (var index = answers.Count - 1; index > 0; index--)
        {
            var swapIndex = random.Next(index + 1);
            (answers[index], answers[swapIndex]) = (answers[swapIndex], answers[index]);
        }

        return new MultipleChoiceChallenge(
            3,
            ChallengeTopic.Math,
            question,
            answers,
            answers.IndexOf(answer.ToString()),
            MaximumAttempts: 1,
            TimeLimit: timeLimit);
    }
}

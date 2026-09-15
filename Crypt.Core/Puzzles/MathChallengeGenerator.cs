using Crypt.Core.Utility;

namespace Crypt.Core.Puzzles;

public static class MathChallengeGenerator
{
    public static MultipleChoiceChallenge Create(Random random) =>
        Create(random, GameDifficulty.Difficult);

    public static MultipleChoiceChallenge Create(
        Random random,
        GameDifficulty difficulty)
    {
        ArgumentNullException.ThrowIfNull(random);

        return difficulty switch
        {
            GameDifficulty.Noob => CreateNoobChallenge(random),
            GameDifficulty.Extreme => CreateExtremeChallenge(random),
            _ => CreateDifficultChallenge(random),
        };
    }

    private static MultipleChoiceChallenge CreateNoobChallenge(Random random)
    {
        if (random.Next(2) == 0)
        {
            var root = random.Next(2, 10);

            return CreateChallenge(
                $"SQRT({root * root}) = ?",
                root,
                TimeSpan.FromSeconds(15),
                random,
                choiceRadius: 3);
        }

        var solution = random.Next(1, 10);
        var constant = random.Next(1, 6);
        var total = (2 * solution) + constant;

        return CreateChallenge(
            $"2X + {constant} = {total}. X = ?",
            solution,
            TimeSpan.FromSeconds(12),
            random,
            choiceRadius: 3);
    }

    private static MultipleChoiceChallenge CreateDifficultChallenge(Random random)
    {
        return random.Next(3) switch
        {
            0 => CreateSquareRoot(random),
            1 => CreateLinearEquation(random),
            _ => CreateBracketEquation(random),
        };
    }

    private static MultipleChoiceChallenge CreateExtremeChallenge(Random random)
    {
        if (random.Next(2) == 0)
        {
            var coefficient = random.Next(4, 9);
            var solution = random.Next(-8, 10);
            var constant = random.Next(-18, 19);
            var total = (coefficient * solution) + constant;
            var constantText = constant < 0
                ? $"- {Math.Abs(constant)}"
                : $"+ {constant}";

            return CreateChallenge(
                $"{coefficient}X {constantText} = {total}. X = ?",
                solution,
                TimeSpan.FromSeconds(5),
                random,
                choiceRadius: 8);
        }

        var multiplier = random.Next(3, 8);
        var answer = random.Next(-7, 12);
        var offset = random.Next(3, 10);
        var result = multiplier * (answer + offset);

        return CreateChallenge(
            $"{multiplier}(X + {offset}) = {result}. X = ?",
            answer,
            TimeSpan.FromSeconds(4),
            random,
            choiceRadius: 8);
    }

    private static MultipleChoiceChallenge CreateSquareRoot(Random random)
    {
        var root = random.Next(3, 13);

        return CreateChallenge(
            $"SQRT({root * root}) = ?",
            root,
            TimeSpan.FromSeconds(10),
            random,
            choiceRadius: 5);
    }

    private static MultipleChoiceChallenge CreateLinearEquation(Random random)
    {
        var coefficient = random.Next(2, 6);
        var solution = random.Next(-5, 6);
        var constant = random.Next(-10, 11);
        var total = (coefficient * solution) + constant;
        var constantText = constant < 0
            ? $"- {Math.Abs(constant)}"
            : $"+ {constant}";

        return CreateChallenge(
            $"{coefficient}X {constantText} = {total}. X = ?",
            solution,
            TimeSpan.FromSeconds(8),
            random,
            choiceRadius: 5);
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
            random,
            choiceRadius: 5);
    }

    private static MultipleChoiceChallenge CreateChallenge(
        string question,
        int answer,
        TimeSpan timeLimit,
        Random random,
        int choiceRadius)
    {
        var choices = new HashSet<int> { answer };

        while (choices.Count < 4)
        {
            var distance = random.Next(1, choiceRadius + 1);
            choices.Add(answer + (random.Next(2) == 0 ? -distance : distance));
        }

        var answers = choices.Select(value => value.ToString()).ToList();

        for (var index = answers.Count - 1; index > 0; index--)
        {
            var swapIndex = random.Next(index + 1);
            (answers[index], answers[swapIndex]) =
                (answers[swapIndex], answers[index]);
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

using Crypt.Core.World;

namespace Crypt.Core.Puzzles;

/// <summary>Maps a multiple-choice challenge's answers to four physical floor sigils.</summary>
public sealed class AnswerSigilPuzzle
{
    private readonly GridPosition[] _positions;

    public AnswerSigilPuzzle(MultipleChoiceChallenge challenge, IEnumerable<GridPosition> positions)
    {
        Challenge = challenge ?? throw new ArgumentNullException(nameof(challenge));
        _positions = positions?.ToArray() ?? throw new ArgumentNullException(nameof(positions));

        if (_positions.Length != Challenge.Answers.Count)
        {
            throw new ArgumentException("Each answer needs exactly one floor sigil.", nameof(positions));
        }

        if (_positions.Distinct().Count() != _positions.Length)
        {
            throw new ArgumentException("Answer sigils must occupy distinct tiles.", nameof(positions));
        }
    }

    public MultipleChoiceChallenge Challenge { get; }

    public IReadOnlyList<GridPosition> Positions => _positions;

    public int AttemptsUsed { get; private set; }

    public bool IsSolved { get; private set; }

    public bool IsComplete => IsSolved || AttemptsUsed >= Challenge.MaximumAttempts;

    public bool IsAnswerPosition(GridPosition position) => Array.IndexOf(_positions, position) >= 0;

    public string AnswerAt(GridPosition position)
    {
        var index = Array.IndexOf(_positions, position);
        if (index < 0)
        {
            throw new ArgumentException("This tile is not an answer sigil.", nameof(position));
        }

        return Challenge.Answers[index];
    }

    public AnswerSigilResult Choose(GridPosition position)
    {
        if (IsComplete)
        {
            return AnswerSigilResult.AlreadyComplete;
        }

        var index = Array.IndexOf(_positions, position);
        if (index < 0)
        {
            return AnswerSigilResult.NotAnAnswer;
        }

        if (Challenge.IsCorrectAnswer(index))
        {
            IsSolved = true;
            return AnswerSigilResult.Correct;
        }

        AttemptsUsed++;
        return AttemptsUsed >= Challenge.MaximumAttempts
            ? AnswerSigilResult.Exhausted
            : AnswerSigilResult.Incorrect;
    }
}

public enum AnswerSigilResult
{
    NotAnAnswer,
    Incorrect,
    Exhausted,
    Correct,
    AlreadyComplete,
}

using Crypt.Core.Entities;
using Crypt.Core.World;

namespace Crypt.Core.Puzzles;

/// <summary>A directional input sequence that can be entered independently of board movement.</summary>
public sealed class MoveSequencePuzzle
{
    private readonly Facing[] _sequence;

    public MoveSequencePuzzle(IEnumerable<Facing> sequence, GridPosition startPosition)
    {
        ArgumentNullException.ThrowIfNull(sequence);
        _sequence = sequence.ToArray();
        if (_sequence.Length == 0)
        {
            throw new ArgumentException("A move sequence needs at least one direction.", nameof(sequence));
        }

        StartPosition = startPosition;
    }

    public IReadOnlyList<Facing> Sequence => _sequence;

    public GridPosition StartPosition { get; }

    public int Progress { get; private set; }

    public bool IsSolved => Progress == _sequence.Length;

    public MoveSequenceResult Register(GridPosition playerPosition, Facing move)
    {
        if (IsSolved)
        {
            return MoveSequenceResult.AlreadySolved;
        }

        if (Progress == 0 && playerPosition != StartPosition)
        {
            return MoveSequenceResult.NotAtStart;
        }

        if (move == _sequence[Progress])
        {
            Progress++;
            return IsSolved ? MoveSequenceResult.Solved : MoveSequenceResult.CorrectStep;
        }

        Progress = 0;
        return MoveSequenceResult.Reset;
    }
}

public enum MoveSequenceResult
{
    NotAtStart,
    CorrectStep,
    Reset,
    Solved,
    AlreadySolved,
}

using Crypt.Core.Puzzles;
using Crypt.Core.Utility;

namespace Crypt.Core.World;

/// <summary>level one room layouts.</summary>
public sealed class RoomGenerator
{
    private readonly Random _random;
    private readonly Queue<bool> _levelTwoTrapDeck = new();

    public RoomGenerator()
        : this(new Random())
    {
    }

    public RoomGenerator(Random random)
    {
        ArgumentNullException.ThrowIfNull(random);
        _random = random;
    }

    public Room GenerateLevelOne()
    {
        var entrance = new GridPosition(5, 0);
        var door = new GridPosition(2, GameConstants.RoomColumns - 1);
        var candidates = KeyCandidates(entrance, door).ToArray();
        var hiddenKey = candidates[_random.Next(candidates.Length)];
        var plateCandidates = candidates
            .Where(position =>
                position != hiddenKey &&
                position.ManhattanDistanceTo(entrance) >= 4 &&
                position.Row >= 1 &&
                position.Column <= GameConstants.RoomColumns - 3 &&
                position.Move(-1, 2) != door)
            .ToArray();
        var pressurePlate = plateCandidates[_random.Next(plateCandidates.Length)];

        return new Room(entrance, door, hiddenKey, pressurePlate, CaesarRunePuzzle.CreateRandom(_random));
    }

    public Room GenerateLevelTwo()
    {
        var entrance = new GridPosition(5, 0);
        var door = new GridPosition(2, GameConstants.RoomColumns - 1);
        var candidates = KeyCandidates(entrance, door).ToArray();
        var hiddenKey = candidates[_random.Next(candidates.Length)];
        var pattern = SpecialMovePatternCatalog.Pick(_random);
        var startCandidates = Positions()
            .Where(position => position != entrance && position != door && CanPerformPatternFrom(position, pattern, door))
            .ToArray();
        var start = startCandidates[_random.Next(startCandidates.Length)];
        var paperCandidates = Positions()
            .Where(position => position != entrance && position != door && position != hiddenKey && position != start)
            .ToArray();
        var paper = paperCandidates[_random.Next(paperCandidates.Length)];

        return new Room(
            entrance,
            door,
            hiddenKey,
            pressurePlate: null,
            cipherPuzzle: null,
            lavaEnabled: false,
            allowsExplorationKeyReveal: false,
            specialMovePattern: pattern,
            specialMoveStart: start,
            specialMovePaper: paper,
            triggersSequenceCheatTrap: DrawLevelTwoTrapOutcome());
    }

    public Room GenerateLevelThree()
    {
        var challenge = LevelChallengeCatalog.CreateLevelThreeMathChallenge(_random);

        var entrance = new GridPosition(5, 0);
        var door = new GridPosition(2, GameConstants.RoomColumns - 1);
        var positions = Positions()
            .Where(position => position != entrance && position != door)
            .ToList();
        Shuffle(positions);
        var sigilPositions = positions.Take(challenge.Answers.Count).ToArray();
        var hiddenKey = positions[challenge.Answers.Count];

        return new Room(
            entrance,
            door,
            hiddenKey,
            pressurePlate: null,
            lavaEnabled: false,
            allowsExplorationKeyReveal: false,
            answerSigilPuzzle: new AnswerSigilPuzzle(challenge, sigilPositions));
    }

    public Room GenerateLevelFour()
    {
        var entrance = new GridPosition(5, 0);
        var door = new GridPosition(2, GameConstants.RoomColumns - 1);
        var positions = KeyCandidates(entrance, door).ToList();
        Shuffle(positions);

        var hiddenKey = positions[0];
        var libraryBook = positions[1];
        var lectern = positions[2];

        return new Room(
            entrance,
            door,
            hiddenKey,
            pressurePlate: lectern,
            cipherPuzzle: VigenereRunePuzzle.CreateRandom(_random),
            lavaEnabled: false,
            allowsExplorationKeyReveal: false,
            libraryBook: libraryBook);
    }

    public Room GenerateLevelFive()
    {
        var entrance = new GridPosition(5, 0);
        var door = new GridPosition(2, GameConstants.RoomColumns - 1);
        var candidates = KeyCandidates(entrance, door).ToArray();
        var hiddenKey = candidates[_random.Next(candidates.Length)];

        return new Room(
            entrance,
            door,
            hiddenKey,
            pressurePlate: null,
            lavaEnabled: false,
            allowsExplorationKeyReveal: false);
    }

    private bool DrawLevelTwoTrapOutcome()
    {
        if (_levelTwoTrapDeck.Count == 0)
        {
            var outcomes = new[] { true, false, false, false };
            for (var index = outcomes.Length - 1; index > 0; index--)
            {
                var swapIndex = _random.Next(index + 1);
                (outcomes[index], outcomes[swapIndex]) = (outcomes[swapIndex], outcomes[index]);
            }

            foreach (var outcome in outcomes)
            {
                _levelTwoTrapDeck.Enqueue(outcome);
            }
        }

        return _levelTwoTrapDeck.Dequeue();
    }

    private static bool CanPerformPatternFrom(GridPosition start, SpecialMovePattern pattern, GridPosition door)
    {
        var current = start;

        foreach (var move in pattern.Sequence)
        {
            current = move switch
            {
                Crypt.Core.Entities.Facing.Up => current.Move(-1, 0),
                Crypt.Core.Entities.Facing.Down => current.Move(1, 0),
                Crypt.Core.Entities.Facing.Left => current.Move(0, -1),
                Crypt.Core.Entities.Facing.Right => current.Move(0, 1),
                _ => throw new ArgumentOutOfRangeException(nameof(move), move, null),
            };

            if (current.Row < 0 || current.Row >= GameConstants.RoomRows ||
                current.Column < 0 || current.Column >= GameConstants.RoomColumns ||
                current == door)
            {
                return false;
            }
        }

        return true;
    }

    private static IEnumerable<GridPosition> Positions()
    {
        for (var row = 0; row < GameConstants.RoomRows; row++)
        {
            for (var column = 0; column < GameConstants.RoomColumns; column++)
            {
                yield return new GridPosition(row, column);
            }
        }
    }

    private void Shuffle<T>(IList<T> values)
    {
        for (var index = values.Count - 1; index > 0; index--)
        {
            var swapIndex = _random.Next(index + 1);
            (values[index], values[swapIndex]) = (values[swapIndex], values[index]);
        }
    }

    private static IEnumerable<GridPosition> KeyCandidates(GridPosition entrance, GridPosition door)
    {
        for (var row = 0; row < GameConstants.RoomRows; row++)
        {
            for (var column = 0; column < GameConstants.RoomColumns; column++)
            {
                var position = new GridPosition(row, column);
                if (position != entrance && position != door && position.ManhattanDistanceTo(entrance) > 1)
                {
                    yield return position;
                }
            }
        }
    }
}

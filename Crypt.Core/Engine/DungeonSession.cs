using Crypt.Core.Entities;
using Crypt.Core.Puzzles;
using Crypt.Core.Utility;
using Crypt.Core.World;

namespace Crypt.Core.Engine;

/// <summary>Coordinates input, world updates, and game-state transitions for level one.</summary>
public sealed class DungeonSession
{
    public const string ThirdDeathRoast = "Maybe I made this game a bit too difficult, but not THAT difficult. You are THAT bad.";

    private readonly RoomGenerator _roomGenerator;
    private TimeSpan _now;
    private TimeSpan _deathStartedAt;
    private CipherAttemptState? _cipherAttempt;
    private MoveSequencePuzzle? _moveSequencePuzzle;
    private bool _hasReadSpecialMovePaper;
    private bool _hasReadLibraryBook;
    private bool _cheatDeathPending;
    private bool _cheatTrapTriggered;
    private TimeSpan? _answerSigilStartedAt;
    private MinesweeperPuzzle? _minesweeperPuzzle;
    private bool _showDeathRoast;

    public DungeonSession(RoomGenerator? roomGenerator = null, int initialLevel = 1)
    {
        if (initialLevel < 1 || initialLevel > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(initialLevel), "The initial level must be available.");
        }

        _roomGenerator = roomGenerator ?? new RoomGenerator();
        StartLevel(initialLevel, TimeSpan.Zero);
    }

    public Room Room { get; private set; } = null!;

    public Player Player { get; private set; } = null!;

    public DialogueState Dialogue { get; } = new();

    public GameState State { get; private set; } = GameState.Dialogue;

    public string Message { get; private set; } = string.Empty;

    public string DeathReason { get; private set; } = string.Empty;

    public int CurrentLevel { get; private set; }

    /// <summary>Deaths accumulated during this game session, across every level.</summary>
    public int TotalDeaths { get; private set; }

    /// <summary>Deaths accumulated on the current level, including retries.</summary>
    public int DeathsOnCurrentLevel { get; private set; }

    public DeathKind DeathKind { get; private set; } = DeathKind.Lava;

    public CipherAttemptState? CipherAttempt => _cipherAttempt;

    public MoveSequencePuzzle? MoveSequence => _moveSequencePuzzle;

    public MinesweeperPuzzle? MinesweeperPuzzle => _minesweeperPuzzle;

    public TimeSpan GetAnswerSigilTimeRemaining(TimeSpan now)
    {
        var timeLimit = Room?.AnswerSigilPuzzle?.Challenge.TimeLimit;
        if (!timeLimit.HasValue || !_answerSigilStartedAt.HasValue)
        {
            return TimeSpan.Zero;
        }

        return TimeSpan.FromTicks(Math.Max(0, (timeLimit.Value - (now - _answerSigilStartedAt.Value)).Ticks));
    }

    public float GetDeathProgress(TimeSpan now) =>
        State == GameState.Dying
            ? Math.Clamp((float)(now - _deathStartedAt).TotalSeconds / (float)GameConstants.DeathSequenceDuration.TotalSeconds, 0f, 1f)
            : 0f;

    public void Update(TimeSpan now)
    {
        _now = now;

        if (State == GameState.Dialogue)
        {
            Dialogue.Update(now);
        }

        if (State == GameState.Dying && now - _deathStartedAt >= GameConstants.DeathSequenceDuration)
        {
            State = _showDeathRoast ? GameState.DeathRoast : GameState.GameOver;
        }

        if (State != GameState.Playing)
        {
            return;
        }

        if (HasAnswerSigilTimedOut(now))
        {
            BeginDeath("Time runs out beneath the sigils.", DeathKind.Sinkhole);
            return;
        }

        if (Player.UpdateMovement(now))
        {
            FinishMove();
            CheckForTrap();
            return;
        }

        if (Player.UpdateAttack(now) || !Player.CanAcceptInput)
        {
            return;
        }

        var worldMessage = Room.Update(Player, now);
        if (!Room.HasPathToObjective(Player))
        {
            BeginDeath("You trapped yourself.", DeathKind.Lava);
            return;
        }

        if (worldMessage is not null)
        {
            Message = worldMessage;
            ShowDialogue("DUNGEON", worldMessage);
        }
    }

    public void Handle(GameCommand command, TimeSpan now)
    {
        _now = now;

        if (command == GameCommand.Restart && !_cheatDeathPending && State != GameState.DeathRoast)
        {
            StartLevel(CurrentLevel, now);
            return;
        }

        if (State == GameState.GameOver)
        {
            if (command == GameCommand.Confirm)
            {
                StartLevel(CurrentLevel, now);
            }

            return;
        }

        if (State == GameState.DeathRoast)
        {
            if (command == GameCommand.Confirm)
            {
                State = GameState.GameOver;
            }

            return;
        }

        if (State == GameState.Victory)
        {
            if (command == GameCommand.NextLevel && CurrentLevel is 1 or 2 or 3 or 4)
            {
                StartLevel(CurrentLevel + 1, now);
            }

            return;
        }

        if (State == GameState.Dialogue)
        {
            if (command == GameCommand.Confirm)
            {
                Dialogue.Advance();
                if (!Dialogue.IsVisible)
                {
                    if (_cheatDeathPending)
                    {
                        _cheatDeathPending = false;
                        BeginDeath("The dungeon does not forgive cheaters.", DeathKind.Sinkhole);
                    }
                    else
                    {
                        if (CurrentLevel == 5 && _minesweeperPuzzle is { IsComplete: false })
                        {
                            State = GameState.Minesweeper;
                        }
                        else
                        {
                            State = GameState.Playing;
                            StartAnswerSigilTimer(now);
                        }
                    }
                }
            }

            return;
        }

        if (State == GameState.PaperReading)
        {
            if (command == GameCommand.Confirm)
            {
                State = GameState.Playing;
            }

            return;
        }

        if (State == GameState.LibraryReading)
        {
            if (command == GameCommand.Confirm)
            {
                State = GameState.Playing;
            }

            return;
        }

        if (State == GameState.CipherPuzzle)
        {
            HandleCipherCommand(command);
            return;
        }

        if (State == GameState.Minesweeper)
        {
            return;
        }

        if (State != GameState.Playing)
        {
            return;
        }

        switch (command)
        {
            case GameCommand.MoveUp:
                Move(Facing.Up);
                break;
            case GameCommand.MoveDown:
                Move(Facing.Down);
                break;
            case GameCommand.MoveLeft:
                Move(Facing.Left);
                break;
            case GameCommand.MoveRight:
                Move(Facing.Right);
                break;
            case GameCommand.Attack:
                Attack();
                break;
        }
    }

    public void AppendCipherText(string characters)
    {
        if (State == GameState.CipherPuzzle)
        {
            _cipherAttempt?.Append(characters);
        }
    }

    public void HandleMinesweeperClick(GridPosition position, bool flag, TimeSpan now)
    {
        _now = now;

        if (State != GameState.Minesweeper || _minesweeperPuzzle is null)
        {
            return;
        }

        var result = flag
            ? _minesweeperPuzzle.ToggleFlag(position)
            : _minesweeperPuzzle.Reveal(position);

        switch (result)
        {
            case MineActionResult.MineHit:
                BeginDeath("A hidden mine detonates beneath you.", DeathKind.Sinkhole);
                break;
            case MineActionResult.Cleared:
                var message = Room.RevealKeyFromMinefield(Player);
                Message = message;
                ShowDialogue("MINEFIELD", message);
                break;
        }
    }

    private void Move(Facing direction)
    {
        if (!Player.CanAcceptInput)
        {
            return;
        }

        var sequenceMessage = RegisterLevelTwoMove(direction);
        Player.Face(direction);
        var next = direction switch
        {
            Facing.Up => Player.Position.Move(-1, 0),
            Facing.Down => Player.Position.Move(1, 0),
            Facing.Left => Player.Position.Move(0, -1),
            Facing.Right => Player.Position.Move(0, 1),
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
        };

        if (!Room.IsInside(next))
        {
            Message = sequenceMessage ?? "Cold stone blocks your way.";
            return;
        }

        if (Room.IsDoor(next) && !Player.HasKey)
        {
            Message = "The exit is locked. Find the key.";
            return;
        }

        if (Room.IsLava(next))
        {
            Message = sequenceMessage ?? "The lava is impassable.";
            return;
        }

        if (sequenceMessage is not null)
        {
            Message = sequenceMessage;
        }

        Player.BeginMove(next, _now);
    }

    private void FinishMove()
    {
        if (Room.IsDoor(Player.Position))
        {
            State = GameState.Victory;
            Message = CurrentLevel == 1 ? "The first gate opens." : "The dungeon gate opens.";
            Dialogue.Clear();
            return;
        }

        var worldMessage = Room.Enter(Player, _now);
        if (Room.AnswerSigilPuzzle is { } sigilPuzzle && sigilPuzzle.IsAnswerPosition(Player.Position))
        {
            HandleAnswerSigil(sigilPuzzle);
            return;
        }

        if (CurrentLevel == 4 && Room.IsLibraryBookCollected && !_hasReadLibraryBook)
        {
            _hasReadLibraryBook = true;
            ShowLibraryBookReading();
            return;
        }

        if (Room.IsCipherActive && Room.CipherPuzzle is not null)
        {
            _cipherAttempt = new CipherAttemptState(Room.CipherPuzzle);
            State = GameState.CipherPuzzle;
            return;
        }

        if (_cheatTrapTriggered)
        {
            _cheatTrapTriggered = false;
            ShowCheatAccusation();
            return;
        }

        if (worldMessage is not null)
        {
            if (CurrentLevel == 2 && Room.IsSpecialMovePaperCollected && !_hasReadSpecialMovePaper)
            {
                _hasReadSpecialMovePaper = true;
                ShowSpecialMovePaper();
                return;
            }

            Message = worldMessage;
            ShowDialogue("DUNGEON", worldMessage);
        }
    }

    private void Attack()
    {
        if (Player.BeginAttack(_now))
        {
            Message = "Slash!";
        }
    }

    private void HandleCipherCommand(GameCommand command)
    {
        if (_cipherAttempt is null)
        {
            return;
        }

        switch (command)
        {
            case GameCommand.CipherBackspace:
                _cipherAttempt.Backspace();
                break;
            case GameCommand.CipherHint:
                _cipherAttempt.ToggleHint();
                break;
            case GameCommand.CipherSubmit:
                SubmitCipherAnswer();
                break;
        }
    }

    private void SubmitCipherAnswer()
    {
        var attempt = _cipherAttempt;
        if (attempt is null)
        {
            return;
        }

        switch (attempt.Submit())
        {
            case CipherSubmission.Correct:
                var message = Room.SolveCipher(Player);
                _cipherAttempt = null;
                Message = message;
                ShowDialogue("RUNES", message);
                break;
            case CipherSubmission.Exhausted:
                Room.RejectCipher();
                _cipherAttempt = null;
                BeginDeath("The runes reject you.", DeathKind.Sinkhole);
                break;
        }
    }

    private void CheckForTrap()
    {
        if (State == GameState.Playing && !Room.HasPathToObjective(Player))
        {
            BeginDeath("You trapped yourself.", DeathKind.Lava);
        }
    }

    private void HandleAnswerSigil(AnswerSigilPuzzle puzzle)
    {
        switch (puzzle.Choose(Player.Position))
        {
            case AnswerSigilResult.Correct:
                var message = Room.RevealKeyFromSpecialMove(Player);
                Message = message;
                ShowDialogue("SIGILS", message);
                break;
            case AnswerSigilResult.Exhausted:
                BeginDeath("The false sigil breaks beneath you.", DeathKind.Sinkhole);
                break;
            case AnswerSigilResult.Incorrect:
                Message = "The sigil rejects your answer.";
                break;
        }
    }

    private bool HasAnswerSigilTimedOut(TimeSpan now)
    {
        var puzzle = Room.AnswerSigilPuzzle;
        return puzzle?.Challenge.TimeLimit is not null &&
            _answerSigilStartedAt.HasValue &&
            !puzzle.IsComplete &&
            GetAnswerSigilTimeRemaining(now) == TimeSpan.Zero;
    }

    private void StartAnswerSigilTimer(TimeSpan now)
    {
        if (Room.AnswerSigilPuzzle?.Challenge.TimeLimit is not null && !_answerSigilStartedAt.HasValue)
        {
            _answerSigilStartedAt = now;
        }
    }

    private string? RegisterLevelTwoMove(Facing direction)
    {
        if (CurrentLevel != 2 || _moveSequencePuzzle is null)
        {
            return null;
        }

        if (!_hasReadSpecialMovePaper)
        {
            return null;
        }

        var result = _moveSequencePuzzle.Register(Player.Position, direction);
        if (result == MoveSequenceResult.Solved && Room.TriggersSequenceCheatTrap)
        {
            _cheatTrapTriggered = true;
            return "The final move wakes something beneath the stone.";
        }

        return result switch
        {
            MoveSequenceResult.Solved => Room.RevealKeyFromSpecialMove(Player),
            MoveSequenceResult.Reset => "The pattern slips away.",
            _ => null,
        };
    }

    private void BeginDeath(string reason, DeathKind deathKind)
    {
        if (State is GameState.Dying or GameState.GameOver or GameState.Victory)
        {
            return;
        }

        DeathReason = reason;
        TotalDeaths++;
        DeathsOnCurrentLevel++;
        _showDeathRoast = DeathsOnCurrentLevel == 3;
        _deathStartedAt = _now;
        Message = reason;
        DeathKind = deathKind;
        Player.Die();
        _cipherAttempt = null;
        Dialogue.Clear();
        State = GameState.Dying;
    }

    private void StartLevel(int level, TimeSpan now)
    {
        _now = now;
        var isNewLevel = level != CurrentLevel;
        CurrentLevel = level;
        if (isNewLevel)
        {
            DeathsOnCurrentLevel = 0;
        }
        Room = level switch
        {
            1 => _roomGenerator.GenerateLevelOne(),
            2 => _roomGenerator.GenerateLevelTwo(),
            3 => _roomGenerator.GenerateLevelThree(),
            4 => _roomGenerator.GenerateLevelFour(),
            5 => _roomGenerator.GenerateLevelFive(),
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, "This level is not available yet."),
        };
        Player = new Player(Room.Entrance);
        _moveSequencePuzzle = Room.SpecialMovePattern is not null && Room.SpecialMoveStart is { } start
            ? new MoveSequencePuzzle(Room.SpecialMovePattern.Sequence, start)
            : null;
        _hasReadSpecialMovePaper = false;
        _hasReadLibraryBook = false;
        _cheatDeathPending = false;
        _cheatTrapTriggered = false;
        _answerSigilStartedAt = null;
        _minesweeperPuzzle = level == 5 ? new MinesweeperPuzzle() : null;
        _showDeathRoast = false;
        Message = level switch
        {
            1 => "Find the hidden key. Every step leaves a crack.",
            2 => "Find the special moves that uncover the key.",
            3 => "Choose the one sigil that answers the trial.",
            4 => "Find the book that unlocks the Vigenere lectern.",
            5 => "Clear the minefield to reveal the key.",
            _ => string.Empty,
        };
        DeathReason = string.Empty;
        DeathKind = DeathKind.Lava;
        _cipherAttempt = null;
        Dialogue.Clear();

        if (level == 1)
        {
            ShowDialogue(
                "DUNGEON",
                "The dungeon remembers every step you take.",
                "Find the rune tablet. Decode its route to reveal the key.",
                "Then reach the sealed exit.");
            return;
        }

        if (level == 3)
        {
            ShowDialogue(
                "SIGILS",
                "Only one answer opens the way.",
                "Step onto one numbered sigil. You have one attempt.");
            return;
        }

        if (level == 4)
        {
            ShowDialogue(
                "LIBRARY",
                "The Vigenere Library keeps its locks in books.",
                "Find the keyword book. Carry its word to the rune lectern.",
                "The keyword repeats across the encrypted letters.");
            return;
        }

        if (level == 5)
        {
            ShowDialogue(
                "MINEFIELD",
                "A silent field seals the final gate.",
                "Reveal every safe cell to uncover the key.",
                "Left click reveals. Right click places a flag.",
                "The first reveal is always safe. Only eight mines are hidden.");
            return;
        }

        ShowDialogue(
            "DUNGEON",
            "There are special moves you need to do to uncover the key. Good luck.");
    }

    private void ShowSpecialMovePaper()
    {
        Message = "You memorize the paper before it crumbles to dust.";
        State = GameState.PaperReading;
    }

    private void ShowLibraryBookReading()
    {
        Message = "You memorize the keyword before the book locks itself.";
        State = GameState.LibraryReading;
    }

    private void ShowCheatAccusation()
    {
        _cheatDeathPending = true;
        ShowDialogue(
            "DUNGEON",
            "You completed those moves suspiciously fast.",
            "You thought the room would not notice you photographing its secrets.",
            "The floor opens beneath you.");
    }

    private void ShowDialogue(string speaker, params string[] pages)
    {
        Dialogue.Show(speaker, pages);
        State = GameState.Dialogue;
    }
}

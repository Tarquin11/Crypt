using Crypt.Core.Entities;
using Crypt.Core.Utility;
using Crypt.Core.World;

namespace Crypt.Core.Engine;

/// <summary>Coordinates input, world updates, and game-state transitions for level one.</summary>
public sealed class DungeonSession
{
    private readonly RoomGenerator _roomGenerator;
    private TimeSpan _now;
    private TimeSpan _deathStartedAt;

    public DungeonSession(RoomGenerator? roomGenerator = null)
    {
        _roomGenerator = roomGenerator ?? new RoomGenerator();
        Restart(TimeSpan.Zero);
    }

    public Room Room { get; private set; } = null!;

    public Player Player { get; private set; } = null!;

    public DialogueState Dialogue { get; } = new();

    public GameState State { get; private set; } = GameState.Dialogue;

    public string Message { get; private set; } = string.Empty;

    public string DeathReason { get; private set; } = string.Empty;

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
            State = GameState.GameOver;
        }

        if (State != GameState.Playing)
        {
            return;
        }

        if (Player.UpdateMovement(now))
        {
            FinishMove();
            return;
        }

        if (Player.UpdateAttack(now) || !Player.CanAcceptInput)
        {
            return;
        }

        var worldMessage = Room.Update(Player, now);
        if (worldMessage is not null)
        {
            Message = worldMessage;
            ShowDialogue("DUNGEON", worldMessage);
        }

        if (!Room.HasPathToObjective(Player))
        {
            BeginDeath("You blocked yourself! Nowhere to escape.");
        }
    }

    public void Handle(GameCommand command, TimeSpan now)
    {
        _now = now;

        if (command == GameCommand.Restart)
        {
            Restart(now);
            return;
        }

        if (State == GameState.GameOver)
        {
            if (command == GameCommand.Confirm)
            {
                Restart(now);
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
                    State = GameState.Playing;
                }
            }

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

    private void Move(Facing direction)
    {
        if (!Player.CanAcceptInput)
        {
            return;
        }

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
            Message = "Cold stone blocks your way.";
            return;
        }

        if (Room.IsDoor(next) && !Player.HasKey)
        {
            Message = "The exit is locked. Find the key.";
            return;
        }

        if (Room.IsLava(next))
        {
            Message = "The lava is impassable.";
            return;
        }

        Player.BeginMove(next, _now);
    }

    private void FinishMove()
    {
        if (Room.IsDoor(Player.Position))
        {
            State = GameState.Victory;
            Message = "The dungeon gate opens.";
            Dialogue.Clear();
            return;
        }

        var cipherWasActive = Room.IsCipherActive;
        var worldMessage = Room.Enter(Player, _now);
        var cipherMessage = cipherWasActive ? Room.FollowCipherMove(Player.MoveStart, Player) : null;
        if (cipherMessage is not null)
        {
            Message = cipherMessage;

            if (!Room.IsCipherActive && Room.IsKeyRevealed)
            {
                ShowDialogue("RUNES", cipherMessage);
                return;
            }
        }

        if (worldMessage is not null)
        {
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

    private void BeginDeath(string reason)
    {
        if (State != GameState.Playing)
        {
            return;
        }

        DeathReason = reason;
        _deathStartedAt = _now;
        Message = reason;
        Player.Die();
        Dialogue.Clear();
        State = GameState.Dying;
    }

    private void Restart(TimeSpan now)
    {
        _now = now;
        Room = _roomGenerator.GenerateLevelOne();
        Player = new Player(Room.Entrance);
        Message = "Find the hidden key. Every step leaves a crack.";
        DeathReason = string.Empty;
        Dialogue.Clear();
        ShowDialogue(
            "DUNGEON",
            "The dungeon remembers every step you take.",
            "Find the hidden key. Then reach the sealed exit.");
    }

    private void ShowDialogue(string speaker, params string[] pages)
    {
        Dialogue.Show(speaker, pages);
        State = GameState.Dialogue;
    }
}

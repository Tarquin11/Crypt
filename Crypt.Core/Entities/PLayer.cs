using Crypt.Core.Combat;
using Crypt.Core.Utility;
using Crypt.Core.World;

namespace Crypt.Core.Entities;

/// <summary>The player character, including movement, health, inventory, and sword state.</summary>
public sealed class Player : Entity
{
    public const int MaxHealth = 3;

    private readonly Sword _sword = new();
    private TimeSpan _movementStartedAt;

    public Player(GridPosition position)
        : base(position)
    {
        MoveStart = position;
        MoveTarget = position;
    }

    public Facing Facing { get; private set; } = Facing.Right;

    public PlayerState State { get; private set; } = PlayerState.Idle;

    public GridPosition MoveStart { get; private set; }

    public GridPosition MoveTarget { get; private set; }

    public bool HasKey { get; private set; }

    public int Health { get; private set; } = MaxHealth;

    public Sword Sword => _sword;

    public bool CanAcceptInput => State == PlayerState.Idle;

    public void BeginMove(GridPosition target, TimeSpan now)
    {
        if (!CanAcceptInput)
        {
            return;
        }

        MoveStart = Position;
        MoveTarget = target;
        _movementStartedAt = now;
        State = PlayerState.Moving;
    }

    public bool UpdateMovement(TimeSpan now)
    {
        if (State != PlayerState.Moving || now - _movementStartedAt < GameConstants.MoveDuration)
        {
            return false;
        }

        MoveTo(MoveTarget);
        State = PlayerState.Idle;
        return true;
    }

    public float GetMoveProgress(TimeSpan now)
    {
        if (State != PlayerState.Moving)
        {
            return 1f;
        }

        return Math.Min(1f, (float)((now - _movementStartedAt).TotalSeconds / GameConstants.MoveDuration.TotalSeconds));
    }

    public void Face(Facing facing) => Facing = facing;

    public void CollectKey() => HasKey = true;

    public bool TakeDamage(int amount)
    {
        if (amount <= 0 || State == PlayerState.Dead)
        {
            return false;
        }

        Health = Math.Max(0, Health - amount);
        if (Health != 0)
        {
            return false;
        }

        State = PlayerState.Dead;
        return true;
    }

    public void Die()
    {
        Health = 0;
        State = PlayerState.Dead;
    }

    public bool BeginAttack(TimeSpan now)
    {
        if (!CanAcceptInput || !_sword.Swing(now))
        {
            return false;
        }

        State = PlayerState.Attacking;
        return true;
    }

    public bool UpdateAttack(TimeSpan now)
    {
        if (State != PlayerState.Attacking || _sword.IsSwinging(now))
        {
            return false;
        }

        State = PlayerState.Idle;
        return true;
    }
}
using Crypt.Core.Utility;
using Crypt.Core.World;

namespace Crypt.Core.Entities;

/// <summary>Base contract for future enemies.</summary>
public abstract class Enemy : Entity
{
    private TimeSpan _hitFlashUntil;

    protected Enemy(GridPosition position, int maxHealth)
        : base(position)
    {
        if (maxHealth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxHealth), "Enemy health must be positive.");
        }

        MaxHealth = maxHealth;
        Health = maxHealth;
    }

    public int MaxHealth { get; }

    public int Health { get; private set; }

    public bool IsAlive => Health > 0;

    public bool IsFlashing(TimeSpan now) => now < _hitFlashUntil;

    /// <summary>Returns true only when this hit defeats the enemy.</summary>
    public bool TakeDamage(int amount, TimeSpan now)
    {
        if (amount <= 0 || !IsAlive)
        {
            return false;
        }

        Health = Math.Max(0, Health - amount);
        _hitFlashUntil = now + GameConstants.EnemyHitFlashDuration;
        return Health == 0;
    }

    public abstract void Update(Room room, Player player, TimeSpan now);
}
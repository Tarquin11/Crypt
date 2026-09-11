using Crypt.Core.Utility;

namespace Crypt.Core.Combat;

/// <summary>Timing state for a single sword swing and its one damage window.</summary>
public sealed class Sword
{
    private TimeSpan _lastSwingAt;
    private bool _hasSwung;
    private bool _hitDelivered;

    public bool Swing(TimeSpan now)
    {
        if (_hasSwung && now - _lastSwingAt < GameConstants.SwordCooldown)
        {
            return false;
        }

        _lastSwingAt = now;
        _hasSwung = true;
        _hitDelivered = false;
        return true;
    }

    public bool IsSwinging(TimeSpan now) => _hasSwung && now - _lastSwingAt < GameConstants.SwordSwingDuration;

    public bool IsHitActive(TimeSpan now)
    {
        if (!_hasSwung)
        {
            return false;
        }

        var elapsed = now - _lastSwingAt;
        return elapsed >= GameConstants.SwordHitStart && elapsed < GameConstants.SwordHitEnd;
    }

    public float GetSwingProgress(TimeSpan now)
    {
        if (!_hasSwung)
        {
            return 1f;
        }

        return Math.Clamp((float)((now - _lastSwingAt).TotalSeconds / GameConstants.SwordSwingDuration.TotalSeconds), 0f, 1f);
    }

    /// <summary>Returns true exactly once while the active hit window is open.</summary>
    public bool ConsumeHitWindow(TimeSpan now)
    {
        if (!IsHitActive(now) || _hitDelivered)
        {
            return false;
        }

        _hitDelivered = true;
        return true;
    }
}
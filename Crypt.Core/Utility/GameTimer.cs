namespace Crypt.Core.Utility;

/// <summary>Small reusable timer for future entity and animation behavior.</summary>
public sealed class GameTimer
{
    private TimeSpan _startedAt;
    public void Start(TimeSpan now) => _startedAt = now;
    public bool HasElapsed(TimeSpan now, TimeSpan duration) => now - _startedAt >= duration;
}
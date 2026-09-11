using Crypt.Core.Combat;
using Crypt.Core.Utility;

namespace Crypt.Core.Tests;

public sealed class SwordTests
{
    [Fact]
    public void Swing_has_one_short_damage_window()
    {
        var sword = new Sword();

        Assert.True(sword.Swing(TimeSpan.Zero));
        Assert.True(sword.IsSwinging(TimeSpan.Zero));
        Assert.False(sword.IsHitActive(TimeSpan.Zero));

        Assert.True(sword.IsHitActive(GameConstants.SwordHitStart));
        Assert.False(sword.IsHitActive(GameConstants.SwordHitEnd));
        Assert.False(sword.IsSwinging(GameConstants.SwordSwingDuration));
    }

    [Fact]
    public void Hit_window_can_only_be_consumed_once_per_swing()
    {
        var sword = new Sword();

        sword.Swing(TimeSpan.Zero);

        Assert.True(sword.ConsumeHitWindow(GameConstants.SwordHitStart));
        Assert.False(sword.ConsumeHitWindow(GameConstants.SwordHitStart));
    }
}
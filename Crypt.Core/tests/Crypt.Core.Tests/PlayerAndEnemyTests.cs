using Crypt.Core.Entities;
using Crypt.Core.Utility;
using Crypt.Core.World;

namespace Crypt.Core.Tests;

public sealed class PlayerAndEnemyTests
{
    [Fact]
    public void Player_cannot_move_during_a_sword_swing()
    {
        var player = new Player(new GridPosition(2, 2));

        Assert.True(player.BeginAttack(TimeSpan.Zero));
        Assert.Equal(PlayerState.Attacking, player.State);
        Assert.False(player.CanAcceptInput);
        Assert.False(player.UpdateAttack(GameConstants.SwordSwingDuration - TimeSpan.FromTicks(1)));
        Assert.True(player.UpdateAttack(GameConstants.SwordSwingDuration));
        Assert.Equal(PlayerState.Idle, player.State);
        Assert.True(player.CanAcceptInput);
    }

    [Fact]
    public void Slime_takes_two_sword_hits_to_defeat()
    {
        var slime = new Slime(new GridPosition(3, 4));

        Assert.Equal(2, slime.Health);
        Assert.True(slime.IsAlive);
        Assert.False(slime.TakeDamage(1, TimeSpan.FromMilliseconds(100)));
        Assert.Equal(1, slime.Health);
        Assert.True(slime.IsAlive);
        Assert.True(slime.IsFlashing(TimeSpan.FromMilliseconds(100)));
        Assert.True(slime.TakeDamage(1, TimeSpan.FromMilliseconds(200)));
        Assert.Equal(0, slime.Health);
        Assert.False(slime.IsAlive);
    }
}
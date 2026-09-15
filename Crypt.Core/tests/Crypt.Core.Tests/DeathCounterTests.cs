using Crypt.Core.Engine;
using Crypt.Core.Utility;
using Crypt.Core.World;

namespace Crypt.Core.Tests;

public sealed class DeathCounterTests
{
    [Fact]
    public void Third_death_on_one_stage_shows_the_secret_roast_and_tracks_totals()
    {
        var session = new DungeonSession(
            new RoomGenerator(new Random(42)),
            initialLevel: 5,
            startAtMenu: false);
        var now = TimeSpan.Zero;

        for (var death = 1; death <= 3; death++)
        {
            DismissOpeningDialogue(session, now);
            TriggerMine(session, now);

            Assert.Equal(death, session.TotalDeaths);
            Assert.Equal(death, session.DeathsOnCurrentLevel);
            Assert.Equal(GameState.Dying, session.State);

            now += GameConstants.DeathSequenceDuration + TimeSpan.FromMilliseconds(1);
            session.Update(now);

            if (death < 3)
            {
                Assert.Equal(GameState.GameOver, session.State);
                session.Handle(GameCommand.Confirm, now);
            }
        }

        Assert.Equal(GameState.DeathRoast, session.State);
        Assert.Equal(DungeonSession.ThirdDeathRoast, "Maybe I made this game a bit too difficult, but not THAT difficult. You are THAT bad.");

        session.Handle(GameCommand.Confirm, now);
        Assert.Equal(GameState.GameOver, session.State);
    }

    private static void DismissOpeningDialogue(DungeonSession session, TimeSpan now)
    {
        while (session.State == GameState.Dialogue)
        {
            session.Handle(GameCommand.Confirm, now);
        }

        Assert.Equal(GameState.Minesweeper, session.State);
    }

    private static void TriggerMine(DungeonSession session, TimeSpan now)
    {
        var puzzle = Assert.IsType<Crypt.Core.Puzzles.MinesweeperPuzzle>(session.MinesweeperPuzzle);
        session.HandleMinesweeperClick(new GridPosition(0, 0), flag: false, now);
        var mine = Enumerable.Range(0, puzzle.Rows)
            .SelectMany(row => Enumerable.Range(0, puzzle.Columns)
                .Select(column => new GridPosition(row, column)))
            .First(position => puzzle.GetCell(position).IsMine);

        session.HandleMinesweeperClick(mine, flag: false, now);
    }
}

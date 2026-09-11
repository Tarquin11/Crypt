using Crypt.Core.World;

namespace Crypt.Core.Entities;

///<summary> two-hit enemy placeholder </summary>


public sealed class Slime : Enemy
{
    public const int StartingHealth = 2;
    public Slime(GridPosition position)
        : base(position, StartingHealth)
    {
    }
    public override void Update(Room room, Player player, TimeSpan now)
    {
        throw new NotImplementedException();
    }
}
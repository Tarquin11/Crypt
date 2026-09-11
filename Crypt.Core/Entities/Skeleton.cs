using Crypt.Core.Entities;
using Crypt.Core.World;

namespace Crypt.core.Entities;

/// level 4 monster placeholder
public sealed class Skeleton : Enemy
{
    public const int StartingHealth = 5;
    public Skeleton(GridPosition position)
        : base(position, StartingHealth)
{
}

    public override void Update(Room room, Player player, TimeSpan now)
    {
        throw new NotImplementedException();
    }
}

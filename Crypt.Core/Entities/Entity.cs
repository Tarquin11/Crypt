using Crypt.Core.World;

namespace Crypt.Core.Entities;

/// <summary>Base type for actors that occupy a grid position.</summary>
public abstract class Entity
{
    protected Entity(GridPosition position) => Position = position;

    public GridPosition Position { get; private set; }

    public void MoveTo(GridPosition position) => Position = position;
}
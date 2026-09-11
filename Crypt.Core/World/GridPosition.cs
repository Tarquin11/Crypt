namespace Crypt.Core.World;

 /// <summary> immutable row/column position in a dungeon </summary>

public readonly record struct GridPosition (int Row, int Column)
{
    public GridPosition Move(int RowDelta, int ColumnDelta) => new (Row + RowDelta , Column + ColumnDelta);
    public int ManhattanDistanceTo(GridPosition other) => Math.Abs(Row - other.Row) + Math.Abs(Column - other.Column);
}
using System;

namespace GCCC.BoardGame.Core.Model
{
    public readonly struct GridPosition : IEquatable<GridPosition>
    {
        public GridPosition(int column, int row)
        {
            Column = column;
            Row = row;
        }

        public int Column { get; }

        public int Row { get; }

        public bool Equals(GridPosition other)
        {
            return Column == other.Column && Row == other.Row;
        }

        public override bool Equals(object obj)
        {
            return obj is GridPosition other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Column * 397) ^ Row;
            }
        }

        public override string ToString()
        {
            return $"({Column}, {Row})";
        }

        public static GridPosition operator +(GridPosition left, GridPosition right)
        {
            return new GridPosition(left.Column + right.Column, left.Row + right.Row);
        }

        public static bool operator ==(GridPosition left, GridPosition right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GridPosition left, GridPosition right)
        {
            return !left.Equals(right);
        }
    }
    public static class GridPositionExtensions
{
    public static bool IsAdjacentTo(this GridPosition a, GridPosition b)
    {
        int dCol = Math.Abs(a.Column - b.Column);
        int dRow = Math.Abs(a.Row - b.Row);
        return dCol + dRow == 1;
    }
}
}

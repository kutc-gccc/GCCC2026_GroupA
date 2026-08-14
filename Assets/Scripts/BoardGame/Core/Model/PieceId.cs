using System;

namespace GCCC.BoardGame.Core.Model
{
    public readonly struct PieceId : IEquatable<PieceId>
    {
        public PieceId(int value)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            Value = value;
        }

        public int Value { get; }

        public bool Equals(PieceId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is PieceId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public override string ToString()
        {
            return $"Piece {Value}";
        }

        public static bool operator ==(PieceId left, PieceId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PieceId left, PieceId right)
        {
            return !left.Equals(right);
        }
    }
}

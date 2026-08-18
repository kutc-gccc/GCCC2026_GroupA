using System;

namespace GCCC.BoardGame.Core.Model
{
    public readonly struct MovementProfileId : IEquatable<MovementProfileId>
    {
        public MovementProfileId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Movement profile ID must not be empty.", nameof(value));
            }

            Value = value;
        }

        public string Value { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(MovementProfileId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is MovementProfileId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        }

        public override string ToString()
        {
            return Value ?? string.Empty;
        }

        public static bool operator ==(MovementProfileId left, MovementProfileId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MovementProfileId left, MovementProfileId right)
        {
            return !left.Equals(right);
        }
    }
}

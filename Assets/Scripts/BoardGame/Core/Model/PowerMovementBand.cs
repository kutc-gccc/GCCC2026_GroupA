using System;

namespace GCCC.BoardGame.Core.Model
{
    public sealed class PowerMovementBand
    {
        public PowerMovementBand(
            int minCombatPower,
            int maxCombatPower,
            MoveDirections directions)
        {
            if (minCombatPower <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minCombatPower));
            }

            if (maxCombatPower < minCombatPower)
            {
                throw new ArgumentOutOfRangeException(nameof(maxCombatPower));
            }

            if ((directions & ~MoveDirections.All) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(directions));
            }

            MinCombatPower = minCombatPower;
            MaxCombatPower = maxCombatPower;
            Directions = directions;
        }

        public int MinCombatPower { get; }

        public int MaxCombatPower { get; }

        public MoveDirections Directions { get; }

        public bool Contains(int combatPower)
        {
            return combatPower >= MinCombatPower && combatPower <= MaxCombatPower;
        }
    }
}

using GCCC.BoardGame.Core.Model;

namespace GCCC.BoardGame.Core.Events
{
    public sealed class CombatResolved : GameEvent
    {
        public CombatResolved(
            PieceId attackerId,
            PieceId defenderId,
            int attackerPowerBefore,
            int defenderPowerBefore,
            int attackerPowerAfter,
            int defenderPowerAfter)
        {
            AttackerId = attackerId;
            DefenderId = defenderId;
            AttackerPowerBefore = attackerPowerBefore;
            DefenderPowerBefore = defenderPowerBefore;
            AttackerPowerAfter = attackerPowerAfter;
            DefenderPowerAfter = defenderPowerAfter;
        }

        public PieceId AttackerId { get; }

        public PieceId DefenderId { get; }

        public int AttackerPowerBefore { get; }

        public int DefenderPowerBefore { get; }

        public int AttackerPowerAfter { get; }

        public int DefenderPowerAfter { get; }
    }
}

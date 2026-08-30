using System;
using GCCC.BoardGame.Core.Model;

namespace GCCC.BoardGame.Core.Rules.CellEffects
{
    public sealed class CombatPowerBoostCellEffectHandler : ICellEffectHandler
    {
        private readonly int amount;

        public CombatPowerBoostCellEffectHandler(string effectId, int amount)
        {
            if (string.IsNullOrWhiteSpace(effectId))
            {
                throw new ArgumentException(
                    "Cell effect ID must not be empty.", nameof(effectId));
            }

            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            EffectId = effectId;
            this.amount = amount;
        }

        public string EffectId { get; }

        public bool BlocksPowerRandomization => true;

        public CellEffectResult Apply(CellEffectContext context)
        {
            PieceState piece =
                context.Definition.Lifetime == CellEffectLifetime.WhileOccupied
                    ? context.Piece.WithActiveEffect(EffectId, amount)
                    : context.Piece.WithCombatPower(
                        context.Piece.CombatPower + amount);
            return new CellEffectResult(piece);
        }
    }
}

using System;
using GCCC.BoardGame.Core.Model;

namespace GCCC.BoardGame.Core.Rules.CellEffects
{
    public sealed class ReservePieceGrantCellEffectHandler : ICellEffectHandler
    {
        private readonly int combatPower;
        private readonly MovementProfileId movementProfileId;

        public ReservePieceGrantCellEffectHandler(
            string effectId,
            int combatPower,
            MovementProfileId movementProfileId)
        {
            if (string.IsNullOrWhiteSpace(effectId))
            {
                throw new ArgumentException(
                    "Cell effect ID must not be empty.", nameof(effectId));
            }

            if (combatPower <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(combatPower));
            }

            if (!movementProfileId.IsValid)
            {
                throw new ArgumentException(
                    "Movement profile ID is invalid.", nameof(movementProfileId));
            }

            EffectId = effectId;
            this.combatPower = combatPower;
            this.movementProfileId = movementProfileId;
        }

        public string EffectId { get; }

        public bool BlocksPowerRandomization => false;

        public CellEffectResult Apply(CellEffectContext context)
        {
            if (context.Definition.Lifetime !=
                CellEffectLifetime.PermanentOncePerPiece)
            {
                throw new InvalidOperationException(
                    "Reserve piece grants must be permanent once-per-piece effects.");
            }

            return new CellEffectResult(
                context.Piece,
                reservePieceGrants: new[]
                {
                    new ReservePieceGrant(
                        context.Piece.Owner,
                        combatPower,
                        movementProfileId)
                });
        }
    }
}

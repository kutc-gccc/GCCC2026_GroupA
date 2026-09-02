using System;
using GCCC.BoardGame.Core.Model;
using GCCC.BoardGame.Core.Rules.CellEffects;
using UnityEngine;

namespace GCCC.BoardGame.Presentation.Config
{
    [CreateAssetMenu(
        menuName = "GCCC/Cell Effects/Reserve Piece Grant",
        fileName = "ReservePieceGrantEffect")]
    public sealed class ReservePieceGrantEffectConfig : CellEffectConfig
    {
        [SerializeField, Min(1)] private int combatPower = 1;
        [SerializeField] private string movementProfileId =
            PowerMovementProfile.StandardIdValue;

        public override ICellEffectHandler CreateHandler()
        {
            if (Lifetime == CellEffectLifetime.WhileOccupied)
            {
                throw new InvalidOperationException(
                    "Reserve piece grants must use PermanentOncePerPiece or EveryStop.");
            }

            return new ReservePieceGrantCellEffectHandler(
                EffectId,
                combatPower,
                new MovementProfileId(movementProfileId));
        }
    }
}

using System;
using GCCC.BoardGame.Core.Model;

namespace GCCC.BoardGame.Core.Rules.CellEffects
{
    public sealed class ReservePieceGrant
    {
        public ReservePieceGrant(
            PlayerId owner,
            int combatPower,
            MovementProfileId movementProfileId)
        {
            if (combatPower <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(combatPower));
            }

            if (!movementProfileId.IsValid)
            {
                throw new ArgumentException(
                    "Movement profile ID is invalid.", nameof(movementProfileId));
            }

            Owner = owner;
            CombatPower = combatPower;
            MovementProfileId = movementProfileId;
        }

        public PlayerId Owner { get; }

        public int CombatPower { get; }

        public MovementProfileId MovementProfileId { get; }
    }
}

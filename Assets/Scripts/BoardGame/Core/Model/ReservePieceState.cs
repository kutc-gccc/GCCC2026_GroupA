using System;

namespace GCCC.BoardGame.Core.Model
{
    public sealed class ReservePieceState
    {
        public ReservePieceState(
            PieceId id,
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

            Id = id;
            Owner = owner;
            CombatPower = combatPower;
            MovementProfileId = movementProfileId;
        }

        public PieceId Id { get; }

        public PlayerId Owner { get; }

        public int CombatPower { get; }

        public MovementProfileId MovementProfileId { get; }
    }
}

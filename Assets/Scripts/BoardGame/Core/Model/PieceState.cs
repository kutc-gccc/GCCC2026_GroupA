using System;

namespace GCCC.BoardGame.Core.Model
{
    public sealed class PieceState
    {
        public PieceState(
            PieceId id,
            PlayerId owner,
            GridPosition position,
            int combatPower,
            MovementProfileId movementProfileId)
        {
            if (combatPower <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(combatPower),
                    "Combat power must be greater than zero.");
            }

            if (!movementProfileId.IsValid)
            {
                throw new ArgumentException(
                    "Movement profile ID is invalid.", nameof(movementProfileId));
            }

            Id = id;
            Owner = owner;
            Position = position;
            CombatPower = combatPower;
            MovementProfileId = movementProfileId;
        }

        public PieceId Id { get; }

        public PlayerId Owner { get; }

        public GridPosition Position { get; }

        public int CombatPower { get; }

        public MovementProfileId MovementProfileId { get; }

        public PieceState WithPosition(GridPosition position)
        {
            return new PieceState(Id, Owner, position, CombatPower, MovementProfileId);
        }

        public PieceState WithCombatPower(int combatPower)
        {
            return new PieceState(Id, Owner, Position, combatPower, MovementProfileId);
        }

        public PieceState WithMovementProfile(MovementProfileId movementProfileId)
        {
            return new PieceState(Id, Owner, Position, CombatPower, movementProfileId);
        }

        public PieceState WithAttributes(
            int combatPower,
            MovementProfileId movementProfileId)
        {
            return new PieceState(Id, Owner, Position, combatPower, movementProfileId);
        }
    }
}

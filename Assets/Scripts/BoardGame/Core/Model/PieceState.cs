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
            MoveDirections moveDirections)
        {
            if (combatPower <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(combatPower),
                    "Combat power must be greater than zero.");
            }

            Id = id;
            Owner = owner;
            Position = position;
            CombatPower = combatPower;
            MoveDirections = moveDirections;
        }

        public PieceId Id { get; }

        public PlayerId Owner { get; }

        public GridPosition Position { get; }

        public int CombatPower { get; }

        public MoveDirections MoveDirections { get; }

        public PieceState WithPosition(GridPosition position)
        {
            return new PieceState(Id, Owner, position, CombatPower, MoveDirections);
        }

        public PieceState WithCombatPower(int combatPower)
        {
            return new PieceState(Id, Owner, Position, combatPower, MoveDirections);
        }

        public PieceState WithAttributes(int combatPower, MoveDirections moveDirections)
        {
            return new PieceState(Id, Owner, Position, combatPower, moveDirections);
        }
    }
}

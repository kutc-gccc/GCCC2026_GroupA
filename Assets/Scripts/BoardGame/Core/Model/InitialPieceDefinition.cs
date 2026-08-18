namespace GCCC.BoardGame.Core.Model
{
    public sealed class InitialPieceDefinition
    {
        public InitialPieceDefinition(
            PieceId id,
            PlayerId owner,
            GridPosition position,
            int combatPower,
            MovementProfileId movementProfileId)
        {
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

        public PieceState CreateState()
        {
            return new PieceState(Id, Owner, Position, CombatPower, MovementProfileId);
        }
    }
}

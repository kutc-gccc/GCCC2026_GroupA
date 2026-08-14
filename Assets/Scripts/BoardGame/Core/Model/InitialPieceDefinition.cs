namespace GCCC.BoardGame.Core.Model
{
    public sealed class InitialPieceDefinition
    {
        public InitialPieceDefinition(
            PieceId id,
            PlayerId owner,
            GridPosition position,
            int combatPower,
            MoveDirections moveDirections)
        {
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

        public PieceState CreateState()
        {
            return new PieceState(Id, Owner, Position, CombatPower, MoveDirections);
        }
    }
}

using GCCC.BoardGame.Core.Model;

namespace GCCC.BoardGame.Core.Events
{
    public sealed class ReservePieceDeployed : GameEvent
    {
        public ReservePieceDeployed(PieceId pieceId, PlayerId owner, GridPosition position)
        {
            PieceId = pieceId;
            Owner = owner;
            Position = position;
        }

        public PieceId PieceId { get; }

        public PlayerId Owner { get; }

        public GridPosition Position { get; }
    }
}

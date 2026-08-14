using GCCC.BoardGame.Core.Model;

namespace GCCC.BoardGame.Core.Events
{
    public sealed class PieceDestroyed : GameEvent
    {
        public PieceDestroyed(PieceId pieceId, GridPosition position)
        {
            PieceId = pieceId;
            Position = position;
        }

        public PieceId PieceId { get; }

        public GridPosition Position { get; }
    }
}

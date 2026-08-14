using GCCC.BoardGame.Core.Model;

namespace GCCC.BoardGame.Core.Events
{
    public sealed class PieceMoved : GameEvent
    {
        public PieceMoved(PieceId pieceId, GridPosition from, GridPosition to)
        {
            PieceId = pieceId;
            From = from;
            To = to;
        }

        public PieceId PieceId { get; }

        public GridPosition From { get; }

        public GridPosition To { get; }
    }
}

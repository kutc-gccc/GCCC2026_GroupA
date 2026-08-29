using GCCC.BoardGame.Core.Model;

namespace GCCC.BoardGame.Core.Events
{
    public sealed class ReservePieceAdded : GameEvent
    {
        public ReservePieceAdded(ReservePieceState piece)
        {
            Piece = piece;
        }

        public ReservePieceState Piece { get; }
    }
}

using GCCC.BoardGame.Core.Model;

namespace GCCC.BoardGame.Core.Events
{
    public sealed class PiecePowerChanged : GameEvent
    {
        public PiecePowerChanged(PieceId pieceId, int previousPower, int currentPower)
        {
            PieceId = pieceId;
            PreviousPower = previousPower;
            CurrentPower = currentPower;
        }

        public PieceId PieceId { get; }

        public int PreviousPower { get; }

        public int CurrentPower { get; }
    }
}

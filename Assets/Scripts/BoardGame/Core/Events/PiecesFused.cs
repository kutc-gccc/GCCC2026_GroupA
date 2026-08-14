using GCCC.BoardGame.Core.Model;

namespace GCCC.BoardGame.Core.Events
{
    public sealed class PiecesFused : GameEvent
    {
        public PiecesFused(PieceId firstPieceId, PieceId secondPieceId, PieceId resultingPieceId)
        {
            FirstPieceId = firstPieceId;
            SecondPieceId = secondPieceId;
            ResultingPieceId = resultingPieceId;
        }

        public PieceId FirstPieceId { get; }

        public PieceId SecondPieceId { get; }

        public PieceId ResultingPieceId { get; }
    }
}

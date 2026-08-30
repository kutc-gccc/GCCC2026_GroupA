using GCCC.BoardGame.Core.Model;

namespace GCCC.BoardGame.Core.Events
{
    public sealed class PiecesFused : GameEvent
    {
        public PiecesFused(
            PieceId firstPieceId, PieceId secondPieceId, PieceId resultingPieceId, int bonus)
        {
            FirstPieceId = firstPieceId;
            SecondPieceId = secondPieceId;
            ResultingPieceId = resultingPieceId;
            Bonus = bonus;
        }

        public PieceId FirstPieceId { get; }

        public PieceId SecondPieceId { get; }

        public PieceId ResultingPieceId { get; }

        /// <summary>合体成功時に上乗せされた戦闘力（成功=1、大成功=2）。</summary>
        public int Bonus { get; }
    }
}
using GCCC.BoardGame.Core.Model;

namespace GCCC.BoardGame.Core.Events
{
    /// <summary>
    /// 合体の試み自体は正当だったが、判定（乱数）に失敗して不成立に終わったことを表す。
    /// 両方の駒はそのまま残る。
    /// </summary>
    public sealed class FusionAttemptFailed : GameEvent
    {
        public FusionAttemptFailed(PieceId firstPieceId, PieceId secondPieceId)
        {
            FirstPieceId = firstPieceId;
            SecondPieceId = secondPieceId;
        }

        public PieceId FirstPieceId { get; }

        public PieceId SecondPieceId { get; }
    }
}
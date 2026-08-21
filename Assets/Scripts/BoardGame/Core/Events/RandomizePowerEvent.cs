using GCCC.BoardGame.Core.Events; // ★ 必要に応じて追加
using GCCC.BoardGame.Core.Model;

namespace GCCC.BoardGame.Core.Events
{
    /// <summary>
    /// 駒の戦闘力がランダムに変更されたことを表すイベント
    /// </summary>
    public sealed class RandomizePowerEvent : GameEvent
    {
        public PieceId PieceId { get; }
        public int PreviousPower { get; }
        public int NewPower { get; }

        public RandomizePowerEvent(PieceId pieceId, int previousPower, int newPower)
        {
            PieceId = pieceId;
            PreviousPower = previousPower;
            NewPower = newPower;
        }
    }
}
using GCCC.BoardGame.Core.Model;

namespace GCCC.BoardGame.Core.Events
{
    /// <summary>
    /// 駒の戦闘力がランダムに変更されたことを表すイベント。
    /// </summary>
    public sealed class RandomizePowerEvent : GameEvent
    {
        public RandomizePowerEvent(PieceId pieceId, int previousPower, int newPower)
        {
            PieceId = pieceId;
            PreviousPower = previousPower;
            NewPower = newPower;
        }

        public PieceId PieceId { get; }

        public int PreviousPower { get; }

        public int NewPower { get; }
    }
}

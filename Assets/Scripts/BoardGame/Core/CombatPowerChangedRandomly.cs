using GCCC.BoardGame.Core.Model;

namespace GCCC.BoardGame.Core.Events
{
    public sealed class CombatPowerChangedRandomly : GameEvent
    {
        public PieceId PieceId { get; }
        public int OldPower { get; }
        public int NewPower { get; }

        public CombatPowerChangedRandomly(PieceId pieceId, int oldPower, int newPower)
        {
            PieceId = pieceId;
            OldPower = oldPower;
            NewPower = newPower;
        }
    }
}
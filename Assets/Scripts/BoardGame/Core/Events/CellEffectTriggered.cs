using GCCC.BoardGame.Core.Model;

namespace GCCC.BoardGame.Core.Events
{
    public sealed class CellEffectTriggered : GameEvent
    {
        public CellEffectTriggered(string effectId, PieceId pieceId, GridPosition position)
        {
            EffectId = effectId;
            PieceId = pieceId;
            Position = position;
        }

        public string EffectId { get; }

        public PieceId PieceId { get; }

        public GridPosition Position { get; }
    }
}

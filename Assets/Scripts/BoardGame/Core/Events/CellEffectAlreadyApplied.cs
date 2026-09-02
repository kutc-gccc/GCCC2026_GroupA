using GCCC.BoardGame.Core.Model;

namespace GCCC.BoardGame.Core.Events
{
    /// <summary>A cell effect was skipped because it had already applied to this piece.</summary>
    public sealed class CellEffectAlreadyApplied : GameEvent
    {
        public CellEffectAlreadyApplied(string effectId, PieceId pieceId, GridPosition position)
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

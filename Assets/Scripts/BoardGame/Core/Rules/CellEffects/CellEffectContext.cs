using GCCC.BoardGame.Core.Model;

namespace GCCC.BoardGame.Core.Rules.CellEffects
{
    public sealed class CellEffectContext
    {
        public CellEffectContext(
            GameSnapshot snapshot,
            PieceState piece,
            CellDefinition cell,
            CellEffectDefinition definition)
        {
            Snapshot = snapshot;
            Piece = piece;
            Cell = cell;
            Definition = definition;
        }

        public GameSnapshot Snapshot { get; }

        public PieceState Piece { get; }

        public CellDefinition Cell { get; }

        public CellEffectDefinition Definition { get; }
    }
}

using GCCC.BoardGame.Core.Model;

namespace GCCC.BoardGame.Core.Rules.Fusion
{
    public sealed class FusionResolution
    {
        public FusionResolution(PieceState resultingPiece)
        {
            ResultingPiece = resultingPiece;
        }

        public PieceState ResultingPiece { get; }
    }
}

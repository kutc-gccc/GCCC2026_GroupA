using System.Collections.Generic;
using GCCC.BoardGame.Core.Model;

namespace GCCC.BoardGame.Core.Rules.Fusion
{
    public interface IFusionResolver
    {
        bool IsEnabled { get; }

        IReadOnlyList<FusionPair> GetLegalFusions(GameSnapshot snapshot, PlayerId player);

        bool TryResolve(PieceState first, PieceState second, out FusionResolution resolution);
    }

    public readonly struct FusionPair
    {
        public FusionPair(PieceId firstPieceId, PieceId secondPieceId)
        {
            FirstPieceId = firstPieceId;
            SecondPieceId = secondPieceId;
        }

        public PieceId FirstPieceId { get; }

        public PieceId SecondPieceId { get; }
    }
}

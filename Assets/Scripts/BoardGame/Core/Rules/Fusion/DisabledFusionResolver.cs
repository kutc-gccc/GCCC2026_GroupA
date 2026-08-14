using System;
using System.Collections.Generic;
using GCCC.BoardGame.Core.Model;

namespace GCCC.BoardGame.Core.Rules.Fusion
{
    public sealed class DisabledFusionResolver : IFusionResolver
    {
        public bool IsEnabled => false;

        public IReadOnlyList<FusionPair> GetLegalFusions(GameSnapshot snapshot, PlayerId player)
        {
            return Array.Empty<FusionPair>();
        }

        public bool TryResolve(PieceState first, PieceState second, out FusionResolution resolution)
        {
            resolution = null;
            return false;
        }
    }
}

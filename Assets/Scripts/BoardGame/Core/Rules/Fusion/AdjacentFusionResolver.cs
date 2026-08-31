using System;
using System.Collections.Generic;
using System.Linq;
using GCCC.BoardGame.Core.Model;
using GCCC.BoardGame.Core.Rules.Random;

namespace GCCC.BoardGame.Core.Rules.Fusion
{
    internal sealed class AdjacentFusionResolver : IFusionResolver
    {
        private readonly IRandomSource random;

        public AdjacentFusionResolver() : this(new SystemRandomSource())
        {
        }

        public AdjacentFusionResolver(IRandomSource random)
        {
            this.random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public bool IsEnabled => true;

        public IReadOnlyList<FusionPair> GetLegalFusions(GameSnapshot snapshot, PlayerId player)
        {
            var ownPieces = snapshot.Pieces
                .Where(p => p.Owner == player && !p.HasFused)
                .ToList();
            var pairs = new List<FusionPair>();

            for (int i = 0; i < ownPieces.Count; i++)
            {
                for (int j = i + 1; j < ownPieces.Count; j++)
                {
                    if (ownPieces[i].Position.IsAdjacentTo(ownPieces[j].Position))
                    {
                        pairs.Add(new FusionPair(ownPieces[i].Id, ownPieces[j].Id));
                    }
                }
            }

            return pairs;
        }

        public bool TryResolve(PieceState first, PieceState second, out FusionResolution resolution)
        {
            resolution = null;

            if (first.Owner != second.Owner)
            {
                return false;
            }

            if (first.HasFused || second.HasFused)
            {
                // 既に一度「成功」した駒は、二度と合体を試みることができない
                return false;
            }

            if (!first.Position.IsAdjacentTo(second.Position))
            {
                return false;
            }

            // 1/2:成功(+1) 1/4:大成功(+2) 1/4:失敗(合体不成立だが、試み自体は成立=ターンを消費する)
            int? bonus = RollFusionBonus();
            if (!bonus.HasValue)
            {
                resolution = FusionResolution.Attempted();
                return true;
            }

            PieceState mergedPiece = first
                .MergeWith(second, bonus.Value)
                .WithFusedFlag(true);

            resolution = FusionResolution.Success(mergedPiece, bonus.Value);
            return true;
        }

        private int? RollFusionBonus()
        {
            double roll = random.NextDouble();

            if (roll < 0.25d)
            {
                return 2; // 大成功: 1/4
            }

            if (roll < 0.75d)
            {
                return 1; // 成功: 1/2
            }

            return null; // 失敗: 1/4 → 合体不成立
        }
    }
}

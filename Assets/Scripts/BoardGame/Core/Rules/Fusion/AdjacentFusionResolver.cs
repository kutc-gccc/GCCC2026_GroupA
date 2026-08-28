using System;
using System.Collections.Generic;
using System.Linq;
using GCCC.BoardGame.Core.Model;

namespace GCCC.BoardGame.Core.Rules.Fusion
{
    internal sealed class AdjacentFusionResolver : IFusionResolver
    {
        private readonly Random random;

        public AdjacentFusionResolver() : this(new Random())
        {
        }

        // テストなどで乱数を固定したい場合に使う
        public AdjacentFusionResolver(Random random)
        {
            this.random = random ?? new Random();
        }

        public bool IsEnabled => true;

        public IReadOnlyList<FusionPair> GetLegalFusions(GameSnapshot snapshot, PlayerId player)
        {
            var ownPieces = snapshot.Pieces.Where(p => p.Owner == player).ToList();
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

            int combinedCombatPower = first.CombatPower + second.CombatPower + bonus.Value;
            PieceState mergedPiece = first.WithCombatPower(combinedCombatPower);

            resolution = FusionResolution.Success(mergedPiece, bonus.Value);
            return true;
        }

        private int? RollFusionBonus()
        {
            double roll = random.NextDouble(); // [0.0, 1.0)

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
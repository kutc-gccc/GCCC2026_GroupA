using System;
using System.Collections.Generic;
using System.Linq;
using GCCC.BoardGame.Core;
using GCCC.BoardGame.Core.Commands;
using GCCC.BoardGame.Core.Events;
using GCCC.BoardGame.Core.Model;
using GCCC.BoardGame.Core.Rules.CellEffects;
using GCCC.BoardGame.Core.Rules.Random;
using NUnit.Framework;

namespace GCCC.BoardGame.Tests
{
    public sealed partial class GameSessionTests
    {
        private GameSession session;

        [SetUp]
        public void SetUp()
        {
            session = new GameSession(GameDefinition.CreateStandard());
        }


        private static GameSession CreateSession(
            PlayerId firstPlayer,
            params InitialPieceDefinition[] pieces)
        {
            return GameSessionTestBuilder.CreateSession(firstPlayer, pieces);
        }

        private static GameDefinition CreateDefinition(
            PlayerId firstPlayer,
            IDictionary<GridPosition, string[]> cellEffects,
            params InitialPieceDefinition[] pieces)
        {
            return GameSessionTestBuilder.CreateDefinition(
                firstPlayer, cellEffects, pieces);
        }

        private static GameDefinition CreateDefinitionWithProfiles(
            PlayerId firstPlayer,
            IEnumerable<PowerMovementProfile> movementProfiles,
            IDictionary<GridPosition, string[]> cellEffects,
            params InitialPieceDefinition[] pieces)
        {
            return GameSessionTestBuilder.CreateDefinitionWithProfiles(
                firstPlayer, movementProfiles, cellEffects, pieces);
        }

        private static GameDefinition CreateDefinitionWithEffects(
            PlayerId firstPlayer,
            IDictionary<GridPosition, string[]> cellEffects,
            IEnumerable<CellEffectDefinition> effectDefinitions,
            params InitialPieceDefinition[] pieces)
        {
            return GameSessionTestBuilder.CreateDefinitionWithEffects(
                firstPlayer, cellEffects, effectDefinitions, pieces);
        }

        private static GameDefinition CreateDefinitionWithEffectsAndLimits(
            PlayerId firstPlayer,
            IDictionary<GridPosition, string[]> cellEffects,
            IEnumerable<CellEffectDefinition> effectDefinitions,
            int maxPiecesPerPlayer,
            int reserveDeploymentDepth,
            params InitialPieceDefinition[] pieces)
        {
            return GameSessionTestBuilder.CreateDefinitionWithEffectsAndLimits(
                firstPlayer,
                cellEffects,
                effectDefinitions,
                maxPiecesPerPlayer,
                reserveDeploymentDepth,
                pieces);
        }

        private static InitialPieceDefinition InitialPiece(
            int id,
            int column,
            int row,
            PlayerId owner,
            int power = 1,
            string movementProfileId = PowerMovementProfile.StandardIdValue)
        {
            return GameSessionTestBuilder.InitialPiece(
                id, column, row, owner, power, movementProfileId);
        }

        private static PieceState GetPiece(GameSnapshot snapshot, GridPosition position)
        {
            Assert.That(snapshot.TryGetPiece(position, out PieceState piece), Is.True);
            return piece;
        }

        private static void AssertPiece(
            GameSnapshot snapshot,
            GridPosition position,
            PlayerId owner,
            int combatPower)
        {
            PieceState piece = GetPiece(snapshot, position);
            Assert.That(piece.Owner, Is.EqualTo(owner));
            Assert.That(piece.CombatPower, Is.EqualTo(combatPower));
        }

        private static void AssertEffectivePower(
            GameSnapshot snapshot,
            GridPosition position,
            int effectiveCombatPower)
        {
            Assert.That(GetPiece(snapshot, position).EffectiveCombatPower,
                Is.EqualTo(effectiveCombatPower));
        }

        private sealed class RecordingPowerEffect : ICellEffectHandler
        {
            private readonly IList<string> order;

            public RecordingPowerEffect(string effectId, IList<string> order)
            {
                EffectId = effectId;
                this.order = order;
            }

            public string EffectId { get; }

            public bool BlocksPowerRandomization => true;

            public CellEffectResult Apply(CellEffectContext context)
            {
                order.Add(EffectId);
                return new CellEffectResult(
                    context.Piece.WithCombatPower(context.Piece.CombatPower + 1));
            }
        }

        private sealed class FixedRandomSource : IRandomSource
        {
            private readonly int nextInt;
            private readonly double nextDouble;

            public FixedRandomSource(int nextInt, double nextDouble)
            {
                this.nextInt = nextInt;
                this.nextDouble = nextDouble;
            }

            public int NextInt(int minInclusive, int maxExclusive)
            {
                Assert.That(nextInt, Is.InRange(minInclusive, maxExclusive - 1));
                return nextInt;
            }

            public double NextDouble()
            {
                return nextDouble;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using GCCC.BoardGame.Core;
using GCCC.BoardGame.Core.Commands;
using GCCC.BoardGame.Core.Events;
using GCCC.BoardGame.Core.Model;
using GCCC.BoardGame.Core.Rules.CellEffects;
using NUnit.Framework;

namespace GCCC.BoardGame.Tests
{
    public sealed partial class GameSessionTests
    {
        [TestCase(1)]
        [TestCase(2)]
        public void DestroyedAttackerDoesNotTriggerDestinationEffect(int defenderPower)
        {
            const string id = "destination";
            var target = new GridPosition(3, 2);
            var custom = new GameSession(CreateDefinitionWithEffectsAndLimits(
                PlayerId.Player1,
                new Dictionary<GridPosition, string[]> { [target] = new[] { id } },
                new[] { new CellEffectDefinition(id, CellEffectLifetime.EveryStop) }, 6, 2,
                InitialPiece(1, 2, 2, PlayerId.Player1),
                InitialPiece(2, 3, 2, PlayerId.Player2, defenderPower)),
                cellEffectHandlers: new[] { new ReservePieceGrantCellEffectHandler(id, 1, PowerMovementProfile.StandardId) });
            CommandResult result = custom.Execute(new MovePieceCommand(PlayerId.Player1, new PieceId(1), target));
            Assert.That(result.Success, Is.True);
            Assert.That(result.Events.OfType<CombatResolved>().Single().AttackerPowerAfter, Is.Zero);
            Assert.That(result.Events.OfType<CellEffectTriggered>(), Is.Empty);
            Assert.That(result.Events.OfType<CellEffectAlreadyApplied>(), Is.Empty);
            Assert.That(result.Events.OfType<ReservePieceAdded>(), Is.Empty);
            Assert.That(result.Events.OfType<ReservePieceGrantBlockedByLimit>(), Is.Empty);
        }

        [TestCase(PlayerId.Player1, 2)]
        [TestCase(PlayerId.Player1, 3)]
        [TestCase(PlayerId.Player2, 2)]
        public void FeedbackPreservesEffectOrderAndCountsPartialGrants(PlayerId actor, int limit)
        {
            string[] ids = { "first", "second", "third" };
            var target = new GridPosition(2, 3);
            GameDefinition definition = CreateDefinitionWithEffectsAndLimits(
                actor, new Dictionary<GridPosition, string[]> { [target] = ids },
                ids.Select(id => new CellEffectDefinition(id, CellEffectLifetime.EveryStop)),
                limit, 2,
                InitialPiece(1, 2, 2, actor), InitialPiece(2, 5, 8, actor.Other()));
            var custom = new GameSession(definition, cellEffectHandlers: ids.Select(id =>
                new ReservePieceGrantCellEffectHandler(id, 1, PowerMovementProfile.StandardId)));
            CommandResult result = custom.Execute(new MovePieceCommand(actor, new PieceId(1), target));
            Assert.That(result.Success, Is.True);
            Assert.That(custom.Snapshot.GetOwnedPieceCount(actor), Is.EqualTo(limit));
            Assert.That(custom.Snapshot.CurrentPlayer, Is.EqualTo(actor.Other()));
            Assert.That(result.Events.OfType<ReservePieceAdded>().Count(), Is.EqualTo(limit - 1));
            Assert.That(result.Events.OfType<ReservePieceGrantBlockedByLimit>().Count(), Is.EqualTo(4 - limit));
            Assert.That(result.Events.OfType<ReservePieceGrantBlockedByLimit>().All(e =>
                e.Owner == actor && e.OwnedPieceCount == limit && e.MaxPiecesPerPlayer == limit), Is.True);
            // Every trigger is immediately followed by its actual grant outcome, before the next effect.
            GameEvent[] effects = result.Events.Skip(1).Take(6).ToArray();
            for (int i = 0; i < ids.Length; i++)
            {
                Assert.That(((CellEffectTriggered)effects[i * 2]).EffectId, Is.EqualTo(ids[i]));
                Assert.That(effects[i * 2 + 1], i < limit - 1
                    ? Is.TypeOf<ReservePieceAdded>() : Is.TypeOf<ReservePieceGrantBlockedByLimit>());
            }
            Assert.That(result.Events.Last(), Is.TypeOf<TurnChanged>());
            Assert.That(custom.Snapshot.Pieces.Single(p => p.Id == new PieceId(1))
                .AppliedPermanentEffectIds, Is.Empty);
        }

        [Test]
        public void BlockedEveryStopGrantCanSucceedAfterFusionAndReentry()
        {
            const string id = "repeated";
            var custom = new GameSession(CreateDefinitionWithEffectsAndLimits(
                PlayerId.Player1,
                new Dictionary<GridPosition, string[]> { [new GridPosition(2, 3)] = new[] { id } },
                new[] { new CellEffectDefinition(id, CellEffectLifetime.EveryStop) }, 2, 2,
                InitialPiece(1, 2, 2, PlayerId.Player1),
                InitialPiece(2, 1, 2, PlayerId.Player1),
                InitialPiece(3, 5, 8, PlayerId.Player2)),
                cellEffectHandlers: new[] { new ReservePieceGrantCellEffectHandler(id, 1, PowerMovementProfile.StandardId) },
                randomSource: new FixedRandomSource(1, 0.5d));
            CommandResult blocked = custom.Execute(new MovePieceCommand(PlayerId.Player1, new PieceId(1), new GridPosition(2, 3)));
            Assert.That(blocked.Events.OfType<ReservePieceGrantBlockedByLimit>().Count(), Is.EqualTo(1));
            custom.Execute(new RandomizePowerCommand(PlayerId.Player2, new PieceId(3)));
            custom.Execute(new MovePieceCommand(PlayerId.Player1, new PieceId(1), new GridPosition(2, 2)));
            custom.Execute(new RandomizePowerCommand(PlayerId.Player2, new PieceId(3)));
            Assert.That(custom.Execute(new FusePiecesCommand(PlayerId.Player1, new PieceId(1), new PieceId(2))).Success, Is.True);
            custom.Execute(new RandomizePowerCommand(PlayerId.Player2, new PieceId(3)));
            CommandResult reentry = custom.Execute(new MovePieceCommand(PlayerId.Player1, new PieceId(1), new GridPosition(2, 3)));
            Assert.That(reentry.Success, Is.True);
            Assert.That(reentry.Events.OfType<ReservePieceAdded>().Count(), Is.EqualTo(1));
            Assert.That(reentry.Events.OfType<CellEffectAlreadyApplied>(), Is.Empty);
            Assert.That(reentry.Events.OfType<ReservePieceGrantBlockedByLimit>(), Is.Empty);
        }
    }
}

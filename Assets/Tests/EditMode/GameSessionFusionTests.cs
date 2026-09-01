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
        [Test]
        public void AdjacentFriendlyPiecesCanAttemptFusion()
        {
            PieceState first = GetPiece(session.Snapshot, new GridPosition(0, 1));
            PieceState second = GetPiece(session.Snapshot, new GridPosition(1, 1));

            CommandResult result = session.Execute(new FusePiecesCommand(
                PlayerId.Player1, first.Id, second.Id));

            Assert.That(result.Success, Is.True);
            Assert.That(result.FailureReason, Is.EqualTo(CommandFailureReason.None));
            Assert.That(
                result.Events.Any(gameEvent =>
                    gameEvent is PiecesFused || gameEvent is FusionAttemptFailed),
                Is.True);
            Assert.That(session.Snapshot.CurrentPlayer, Is.EqualTo(PlayerId.Player2));
        }

        [TestCase(0.1d, 2)]
        [TestCase(0.5d, 1)]
        public void InjectedRandomControlsSuccessfulFusion(double roll, int expectedBonus)
        {
            GameSession custom = new GameSession(
                GameDefinition.CreateStandard(),
                randomSource: new FixedRandomSource(1, roll));
            PieceState first = GetPiece(
                custom.Snapshot, new GridPosition(0, 1));
            PieceState second = GetPiece(
                custom.Snapshot, new GridPosition(1, 1));

            CommandResult result = custom.Execute(new FusePiecesCommand(
                PlayerId.Player1, first.Id, second.Id));

            Assert.That(result.Events.OfType<PiecesFused>().Single().Bonus,
                Is.EqualTo(expectedBonus));
            AssertPiece(
                custom.Snapshot,
                first.Position,
                PlayerId.Player1,
                2 + expectedBonus);
        }

        [Test]
        public void InjectedRandomControlsFailedFusion()
        {
            GameSession custom = new GameSession(
                GameDefinition.CreateStandard(),
                randomSource: new FixedRandomSource(1, 0.9d));
            PieceState first = GetPiece(
                custom.Snapshot, new GridPosition(0, 1));
            PieceState second = GetPiece(
                custom.Snapshot, new GridPosition(1, 1));

            CommandResult result = custom.Execute(new FusePiecesCommand(
                PlayerId.Player1, first.Id, second.Id));

            Assert.That(result.Events.OfType<FusionAttemptFailed>().Count(),
                Is.EqualTo(1));
            Assert.That(custom.Snapshot.Pieces.Count, Is.EqualTo(12));
        }


        [Test]
        public void FusionStateUnionsPermanentHistoryAndKeepsOnlyFirstTemporaryPower()
        {
            PieceState first = new PieceState(
                new PieceId(1),
                PlayerId.Player1,
                new GridPosition(2, 2),
                2,
                PowerMovementProfile.StandardId,
                new[] { "first-permanent" },
                new[] { new ActiveCellEffectState("first-temporary", 1) });
            PieceState second = new PieceState(
                new PieceId(2),
                PlayerId.Player1,
                new GridPosition(3, 2),
                3,
                PowerMovementProfile.StandardId,
                new[] { "second-permanent" },
                new[] { new ActiveCellEffectState("second-temporary", 4) });

            PieceState merged = first.MergeWith(second, 1);

            Assert.That(merged.CombatPower, Is.EqualTo(6));
            Assert.That(merged.TemporaryCombatPower, Is.EqualTo(1));
            Assert.That(merged.AppliedPermanentEffectIds,
                Is.EquivalentTo(new[] { "first-permanent", "second-permanent" }));
            Assert.That(merged.HasActiveEffect("second-temporary"), Is.False);
        }
    }
}

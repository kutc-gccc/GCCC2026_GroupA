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
        public void CellEffectsRunInDefinitionOrder()
        {
            List<string> order = new List<string>();
            GameDefinition definition = CreateDefinition(
                PlayerId.Player1,
                new Dictionary<GridPosition, string[]>
                {
                    [new GridPosition(2, 3)] = new[] { "first", "second" }
                },
                InitialPiece(1, 2, 2, PlayerId.Player1),
                InitialPiece(2, 5, 8, PlayerId.Player2));
            GameSession custom = new GameSession(
                definition,
                cellEffectHandlers: new ICellEffectHandler[]
                {
                    new RecordingPowerEffect("first", order),
                    new RecordingPowerEffect("second", order)
                });

            CommandResult result = custom.Execute(new MovePieceCommand(
                PlayerId.Player1, new PieceId(1), new GridPosition(2, 3)));

            Assert.That(order, Is.EqualTo(new[] { "first", "second" }));
            Assert.That(result.Events.OfType<CellEffectTriggered>()
                .Select(gameEvent => gameEvent.EffectId),
                Is.EqualTo(new[] { "first", "second" }));
            AssertPiece(custom.Snapshot, new GridPosition(2, 3), PlayerId.Player1, 3);
        }

        [Test]
        public void RandomizePowerUsesInjectedSourceAndConsumesTurn()
        {
            GameSession custom = new GameSession(
                GameDefinition.CreateStandard(),
                randomSource: new FixedRandomSource(3, 0.5d));
            PieceState piece = GetPiece(
                custom.Snapshot, new GridPosition(0, 1));

            CommandResult result = custom.Execute(new RandomizePowerCommand(
                PlayerId.Player1, piece.Id));

            Assert.That(result.Success, Is.True);
            AssertPiece(custom.Snapshot, new GridPosition(0, 1), PlayerId.Player1, 3);
            Assert.That(result.Events.OfType<RandomizePowerEvent>().Single().NewPower,
                Is.EqualTo(3));
            Assert.That(custom.Snapshot.CurrentPlayer, Is.EqualTo(PlayerId.Player2));
        }

        [Test]
        public void WhileOccupiedPowerExpiresAndRechargesOnlyAfterReentry()
        {
            const string effectId = "temporary-power";
            GameDefinition definition = CreateDefinitionWithEffects(
                PlayerId.Player1,
                new Dictionary<GridPosition, string[]>
                {
                    [new GridPosition(2, 3)] = new[] { effectId }
                },
                new[]
                {
                    new CellEffectDefinition(
                        effectId, CellEffectLifetime.WhileOccupied)
                },
                InitialPiece(1, 2, 2, PlayerId.Player1),
                InitialPiece(2, 5, 8, PlayerId.Player2));
            GameSession custom = new GameSession(
                definition,
                cellEffectHandlers: new ICellEffectHandler[]
                {
                    new CombatPowerBoostCellEffectHandler(effectId, 2)
                },
                randomSource: new FixedRandomSource(1, 0.5d));

            custom.Execute(new MovePieceCommand(
                PlayerId.Player1, new PieceId(1), new GridPosition(2, 3)));
            AssertEffectivePower(custom.Snapshot, new GridPosition(2, 3), 3);

            custom.Execute(new RandomizePowerCommand(
                PlayerId.Player2, new PieceId(2)));
            CommandResult exit = custom.Execute(new MovePieceCommand(
                PlayerId.Player1, new PieceId(1), new GridPosition(2, 4)));
            Assert.That(exit.Events.OfType<CellEffectExpired>()
                .Select(gameEvent => gameEvent.EffectId),
                Does.Contain(effectId));
            AssertEffectivePower(custom.Snapshot, new GridPosition(2, 4), 1);

            custom.Execute(new RandomizePowerCommand(
                PlayerId.Player2, new PieceId(2)));
            custom.Execute(new MovePieceCommand(
                PlayerId.Player1, new PieceId(1), new GridPosition(2, 3)));
            AssertEffectivePower(custom.Snapshot, new GridPosition(2, 3), 3);
        }

        [Test]
        public void TemporaryPowerAbsorbsDamageWithoutRechargingWhileOccupied()
        {
            const string effectId = "temporary-shield";
            GameDefinition definition = CreateDefinitionWithEffects(
                PlayerId.Player1,
                new Dictionary<GridPosition, string[]>
                {
                    [new GridPosition(2, 3)] = new[] { effectId }
                },
                new[]
                {
                    new CellEffectDefinition(
                        effectId, CellEffectLifetime.WhileOccupied)
                },
                InitialPiece(1, 2, 2, PlayerId.Player1),
                InitialPiece(2, 3, 3, PlayerId.Player2, 2));
            GameSession custom = new GameSession(
                definition,
                cellEffectHandlers: new ICellEffectHandler[]
                {
                    new CombatPowerBoostCellEffectHandler(effectId, 2)
                });

            custom.Execute(new MovePieceCommand(
                PlayerId.Player1, new PieceId(1), new GridPosition(2, 3)));
            custom.Execute(new MovePieceCommand(
                PlayerId.Player2, new PieceId(2), new GridPosition(2, 3)));

            PieceState defender = GetPiece(
                custom.Snapshot, new GridPosition(2, 3));
            Assert.That(defender.Owner, Is.EqualTo(PlayerId.Player1));
            Assert.That(defender.CombatPower, Is.EqualTo(1));
            Assert.That(defender.TemporaryCombatPower, Is.Zero);
            Assert.That(defender.HasActiveEffect(effectId), Is.True);
            Assert.That(defender.EffectiveCombatPower, Is.EqualTo(1));
        }

        [Test]
        public void PermanentPowerAppliesOnceAndBlocksRandomizeCommand()
        {
            const string effectId = "permanent-power";
            GameDefinition definition = CreateDefinitionWithEffects(
                PlayerId.Player1,
                new Dictionary<GridPosition, string[]>
                {
                    [new GridPosition(2, 3)] = new[] { effectId }
                },
                new[]
                {
                    new CellEffectDefinition(
                        effectId, CellEffectLifetime.PermanentOncePerPiece)
                },
                InitialPiece(1, 2, 2, PlayerId.Player1),
                InitialPiece(2, 5, 8, PlayerId.Player2));
            GameSession custom = new GameSession(
                definition,
                cellEffectHandlers: new ICellEffectHandler[]
                {
                    new CombatPowerBoostCellEffectHandler(effectId, 2)
                });

            custom.Execute(new MovePieceCommand(
                PlayerId.Player1, new PieceId(1), new GridPosition(2, 3)));
            custom.Execute(new RandomizePowerCommand(
                PlayerId.Player2, new PieceId(2)));
            Assert.That(custom.GetLegalCommands(PlayerId.Player1)
                .OfType<RandomizePowerCommand>()
                .Any(command => command.PieceId == new PieceId(1)), Is.False);
            Assert.That(custom.Execute(new RandomizePowerCommand(
                    PlayerId.Player1, new PieceId(1))).FailureReason,
                Is.EqualTo(CommandFailureReason.IllegalMove));
            custom.Execute(new MovePieceCommand(
                PlayerId.Player1, new PieceId(1), new GridPosition(2, 4)));
            custom.Execute(new RandomizePowerCommand(
                PlayerId.Player2, new PieceId(2)));
            custom.Execute(new MovePieceCommand(
                PlayerId.Player1, new PieceId(1), new GridPosition(2, 3)));

            AssertPiece(custom.Snapshot, new GridPosition(2, 3), PlayerId.Player1, 3);
        }

        [Test]
        public void CellRejectsMixedEffectLifetimes()
        {
            Assert.Throws<ArgumentException>(() => CreateDefinitionWithEffects(
                PlayerId.Player1,
                new Dictionary<GridPosition, string[]>
                {
                    [new GridPosition(2, 3)] = new[] { "temporary", "permanent" }
                },
                new[]
                {
                    new CellEffectDefinition(
                        "temporary", CellEffectLifetime.WhileOccupied),
                    new CellEffectDefinition(
                        "permanent", CellEffectLifetime.PermanentOncePerPiece)
                },
                InitialPiece(1, 2, 2, PlayerId.Player1)));
        }
    }
}

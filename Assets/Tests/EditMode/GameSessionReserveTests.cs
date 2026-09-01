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
        public void ReservePieceGrantIsRecordedOnceAndResetClearsReserve()
        {
            const string effectId = "reserve-piece";
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
                    new ReservePieceGrantCellEffectHandler(
                        effectId, 2, PowerMovementProfile.StandardId)
                });

            CommandResult firstEntry = custom.Execute(new MovePieceCommand(
                PlayerId.Player1, new PieceId(1), new GridPosition(2, 3)));
            Assert.That(firstEntry.Events.OfType<ReservePieceAdded>().Count(),
                Is.EqualTo(1));
            Assert.That(custom.Snapshot.GetPlayer(PlayerId.Player1)
                .ReservePieces.Count, Is.EqualTo(1));

            custom.Execute(new RandomizePowerCommand(
                PlayerId.Player2, new PieceId(2)));
            custom.Execute(new MovePieceCommand(
                PlayerId.Player1, new PieceId(1), new GridPosition(2, 4)));
            custom.Execute(new RandomizePowerCommand(
                PlayerId.Player2, new PieceId(2)));
            CommandResult secondEntry = custom.Execute(new MovePieceCommand(
                PlayerId.Player1, new PieceId(1), new GridPosition(2, 3)));
            Assert.That(secondEntry.Events.OfType<ReservePieceAdded>(), Is.Empty);
            Assert.That(custom.Snapshot.GetPlayer(PlayerId.Player1)
                .ReservePieces.Count, Is.EqualTo(1));

            custom.Reset();
            Assert.That(custom.Snapshot.GetPlayer(PlayerId.Player1)
                .ReservePieces, Is.Empty);
        }

        [Test]
        public void SharedReserveGrantCellsWorkForBothPlayersAndOnlyOncePerPiece()
        {
            const string effectId = "reserve-piece-grant";
            GameDefinition definition = CreateDefinitionWithEffects(
                PlayerId.Player1,
                new Dictionary<GridPosition, string[]>
                {
                    [new GridPosition(1, 4)] = new[] { effectId },
                    [new GridPosition(4, 5)] = new[] { effectId }
                },
                new[]
                {
                    new CellEffectDefinition(
                        effectId, CellEffectLifetime.PermanentOncePerPiece)
                },
                InitialPiece(1, 1, 3, PlayerId.Player1),
                InitialPiece(2, 4, 6, PlayerId.Player2));
            GameSession custom = new GameSession(
                definition,
                cellEffectHandlers: new ICellEffectHandler[]
                {
                    new ReservePieceGrantCellEffectHandler(
                        effectId, 1, PowerMovementProfile.StandardId)
                });

            custom.Execute(new MovePieceCommand(
                PlayerId.Player1, new PieceId(1), new GridPosition(1, 4)));
            custom.Execute(new MovePieceCommand(
                PlayerId.Player2, new PieceId(2), new GridPosition(4, 5)));

            Assert.That(custom.Snapshot.GetPlayer(PlayerId.Player1)
                .ReservePieces.Single().CombatPower, Is.EqualTo(1));
            Assert.That(custom.Snapshot.GetPlayer(PlayerId.Player2)
                .ReservePieces.Single().CombatPower, Is.EqualTo(1));
            Assert.That(custom.Snapshot.GetPlayer(PlayerId.Player1)
                .ReservePieces.Single().MovementProfileId,
                Is.EqualTo(PowerMovementProfile.StandardId));

            custom.Execute(new MovePieceCommand(
                PlayerId.Player1, new PieceId(1), new GridPosition(2, 5)));
            custom.Execute(new MovePieceCommand(
                PlayerId.Player2, new PieceId(2), new GridPosition(5, 4)));
            custom.Execute(new MovePieceCommand(
                PlayerId.Player1, new PieceId(1), new GridPosition(3, 5)));
            custom.Execute(new RandomizePowerCommand(
                PlayerId.Player2, new PieceId(2)));
            CommandResult secondCell = custom.Execute(new MovePieceCommand(
                PlayerId.Player1, new PieceId(1), new GridPosition(4, 5)));

            Assert.That(secondCell.Success, Is.True);
            Assert.That(secondCell.Events.OfType<ReservePieceAdded>(), Is.Empty);
            Assert.That(custom.Snapshot.GetPlayer(PlayerId.Player1)
                .ReservePieces.Count, Is.EqualTo(1));
        }

        [Test]
        public void ReserveGrantCannotExceedSixOwnedPieces()
        {
            const string effectId = "reserve-piece-limit";
            GameDefinition definition = CreateDefinitionWithEffectsAndLimits(
                PlayerId.Player1,
                new Dictionary<GridPosition, string[]>
                {
                    [new GridPosition(0, 3)] = new[] { effectId }
                },
                new[]
                {
                    new CellEffectDefinition(
                        effectId, CellEffectLifetime.PermanentOncePerPiece)
                },
                6,
                2,
                InitialPiece(1, 0, 2, PlayerId.Player1),
                InitialPiece(2, 1, 2, PlayerId.Player1),
                InitialPiece(3, 2, 2, PlayerId.Player1),
                InitialPiece(4, 3, 2, PlayerId.Player1),
                InitialPiece(5, 4, 2, PlayerId.Player1),
                InitialPiece(6, 5, 2, PlayerId.Player1),
                InitialPiece(7, 5, 8, PlayerId.Player2));
            GameSession custom = new GameSession(
                definition,
                cellEffectHandlers: new ICellEffectHandler[]
                {
                    new ReservePieceGrantCellEffectHandler(
                        effectId, 2, PowerMovementProfile.StandardId)
                });

            CommandResult result = custom.Execute(new MovePieceCommand(
                PlayerId.Player1, new PieceId(1), new GridPosition(0, 3)));

            Assert.That(result.Success, Is.True);
            Assert.That(result.Events.OfType<ReservePieceAdded>(), Is.Empty);
            Assert.That(custom.Snapshot.GetOwnedPieceCount(PlayerId.Player1),
                Is.EqualTo(6));
            Assert.That(custom.Snapshot.GetPlayer(PlayerId.Player1).ReservePieces,
                Is.Empty);
        }

        [TestCase(PlayerId.Player1, 2, 3, 1, 2, 1, 0)]
        [TestCase(PlayerId.Player2, 7, 6, 7, 8, 8, 9)]
        public void ReserveDeploymentExcludesTerritoryAndUsesTwoForwardRows(
            PlayerId owner,
            int startRow,
            int effectRow,
            int minimumDeploymentRow,
            int maximumDeploymentRow,
            int validDeploymentRow,
            int territoryRow)
        {
            const string effectId = "reserve-deployment";
            PlayerId opponent = owner == PlayerId.Player1
                ? PlayerId.Player2
                : PlayerId.Player1;
            int opponentRow = owner == PlayerId.Player1 ? 8 : 1;
            GameDefinition definition = CreateDefinitionWithEffectsAndLimits(
                owner,
                new Dictionary<GridPosition, string[]>
                {
                    [new GridPosition(2, effectRow)] = new[] { effectId }
                },
                new[]
                {
                    new CellEffectDefinition(
                        effectId, CellEffectLifetime.PermanentOncePerPiece)
                },
                6,
                2,
                InitialPiece(1, 2, startRow, owner),
                InitialPiece(2, 5, opponentRow, opponent));
            GameSession custom = new GameSession(
                definition,
                cellEffectHandlers: new ICellEffectHandler[]
                {
                    new ReservePieceGrantCellEffectHandler(
                        effectId, 2, PowerMovementProfile.StandardId)
                });

            custom.Execute(new MovePieceCommand(
                owner, new PieceId(1), new GridPosition(2, effectRow)));
            custom.Execute(new RandomizePowerCommand(opponent, new PieceId(2)));

            ReservePieceState reserve = custom.Snapshot.GetPlayer(owner)
                .ReservePieces.Single();
            DeployReservePieceCommand[] deployments =
                custom.GetLegalCommands(owner)
                    .OfType<DeployReservePieceCommand>()
                    .Where(command => command.ReservePieceId == reserve.Id)
                    .ToArray();
            Assert.That(deployments, Is.Not.Empty);
            Assert.That(deployments.All(command =>
                    command.Destination.Row >= minimumDeploymentRow &&
                    command.Destination.Row <= maximumDeploymentRow),
                Is.True);

            CommandResult territory = custom.Execute(new DeployReservePieceCommand(
                owner, reserve.Id, new GridPosition(0, territoryRow)));
            Assert.That(territory.Success, Is.False);
            Assert.That(territory.FailureReason,
                Is.EqualTo(CommandFailureReason.InvalidDeploymentPosition));

            int beyondDeploymentRows = owner == PlayerId.Player1 ? 3 : 6;
            CommandResult tooFar = custom.Execute(new DeployReservePieceCommand(
                owner, reserve.Id, new GridPosition(0, beyondDeploymentRows)));
            Assert.That(tooFar.Success, Is.False);
            Assert.That(tooFar.FailureReason,
                Is.EqualTo(CommandFailureReason.InvalidDeploymentPosition));

            GridPosition validDestination =
                new GridPosition(0, validDeploymentRow);
            CommandResult valid = custom.Execute(new DeployReservePieceCommand(
                owner, reserve.Id, validDestination));
            Assert.That(valid.Success, Is.True);
            Assert.That(valid.Events.OfType<ReservePieceDeployed>().Single().PieceId,
                Is.EqualTo(reserve.Id));
            AssertPiece(custom.Snapshot, validDestination, owner, 2);
            Assert.That(custom.Snapshot.GetPlayer(owner).ReservePieces, Is.Empty);
            Assert.That(custom.Snapshot.GetOwnedPieceCount(owner), Is.EqualTo(2));
        }

        [Test]
        public void DefinitionRejectsMoreThanSixInitialPiecesForOnePlayer()
        {
            Assert.Throws<ArgumentException>(() => CreateDefinitionWithEffectsAndLimits(
                PlayerId.Player1,
                new Dictionary<GridPosition, string[]>(),
                Array.Empty<CellEffectDefinition>(),
                6,
                2,
                InitialPiece(1, 0, 1, PlayerId.Player1),
                InitialPiece(2, 1, 1, PlayerId.Player1),
                InitialPiece(3, 2, 1, PlayerId.Player1),
                InitialPiece(4, 3, 1, PlayerId.Player1),
                InitialPiece(5, 4, 1, PlayerId.Player1),
                InitialPiece(6, 5, 1, PlayerId.Player1),
                InitialPiece(7, 0, 2, PlayerId.Player1)));
        }
    }
}

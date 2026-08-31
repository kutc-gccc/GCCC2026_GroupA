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
    public sealed class GameSessionTests
    {
        private GameSession session;

        [SetUp]
        public void SetUp()
        {
            session = new GameSession(GameDefinition.CreateStandard());
        }

        [Test]
        public void StandardGameStartsWithTwelveDirectionalPiecesOutsideTerritories()
        {
            GameSnapshot snapshot = session.Snapshot;
            Assert.That(snapshot.Columns, Is.EqualTo(6));
            Assert.That(snapshot.Rows, Is.EqualTo(10));
            Assert.That(snapshot.Pieces.Count, Is.EqualTo(12));
            Assert.That(snapshot.GetPieceCount(PlayerId.Player1), Is.EqualTo(6));
            Assert.That(snapshot.GetPieceCount(PlayerId.Player2), Is.EqualTo(6));
            Assert.That(snapshot.CurrentPlayer, Is.EqualTo(PlayerId.Player1));

            for (int column = 0; column < 6; column++)
            {
                AssertPiece(snapshot, new GridPosition(column, 1), PlayerId.Player1, 1);
                AssertPiece(snapshot, new GridPosition(column, 8), PlayerId.Player2, 1);
                Assert.That(snapshot.TryGetPiece(new GridPosition(column, 0), out _), Is.False);
                Assert.That(snapshot.TryGetPiece(new GridPosition(column, 9), out _), Is.False);
            }

            Assert.That(snapshot.Pieces.All(piece =>
                    piece.MovementProfileId == PowerMovementProfile.StandardId),
                Is.True);
        }

        [Test]
        public void CombatPowerTwoExcludesNorthEastFromLegalCommands()
        {
            GameSession custom = CreateSession(PlayerId.Player1,
                InitialPiece(1, 2, 2, PlayerId.Player1, 2),
                InitialPiece(2, 5, 8, PlayerId.Player2));

            MovePieceCommand[] moves = custom.GetLegalCommands(PlayerId.Player1)
                .OfType<MovePieceCommand>()
                .Where(move => move.PieceId == new PieceId(1))
                .ToArray();

            Assert.That(moves.Select(move => move.Destination),
                Is.EquivalentTo(new[]
                {
                    new GridPosition(2, 3),
                    new GridPosition(3, 2),
                    new GridPosition(3, 1),
                    new GridPosition(2, 1),
                    new GridPosition(1, 1),
                    new GridPosition(1, 2),
                    new GridPosition(1, 3)
                }));
            Assert.That(moves.Select(move => move.Destination),
                Has.None.EqualTo(new GridPosition(3, 3)));
        }

        [Test]
        public void PiecesCanUseDifferentMovementProfilesAtTheSamePower()
        {
            PowerMovementProfile northOnly = new PowerMovementProfile(
                new MovementProfileId("north-only"),
                new[]
                {
                    new PowerMovementBand(
                        1,
                        int.MaxValue,
                        MoveDirections.North)
                });
            GameDefinition definition = CreateDefinitionWithProfiles(
                PlayerId.Player1,
                new[] { PowerMovementProfile.CreateStandard(), northOnly },
                null,
                InitialPiece(1, 2, 2, PlayerId.Player1),
                InitialPiece(2, 4, 2, PlayerId.Player1, 1, "north-only"),
                InitialPiece(3, 5, 8, PlayerId.Player2));
            GameSession custom = new GameSession(definition);

            MovePieceCommand[] northOnlyMoves = custom.GetLegalCommands(PlayerId.Player1)
                .OfType<MovePieceCommand>()
                .Where(move => move.PieceId == new PieceId(2))
                .ToArray();

            Assert.That(northOnlyMoves.Select(move => move.Destination),
                Is.EquivalentTo(new[] { new GridPosition(4, 3) }));
        }

        [Test]
        public void StandardProfileMapsPowerOneThroughSevenAndFallback()
        {
            PowerMovementProfile profile = PowerMovementProfile.CreateStandard();

            // 制限は累積する。上の段は下の段で失った方向をすべて引き継ぐ。
            MoveDirections power2 = MoveDirections.All & ~MoveDirections.NorthEast;
            MoveDirections power3 = power2 & ~MoveDirections.SouthEast;
            MoveDirections power4 = power3 & ~MoveDirections.NorthWest;
            MoveDirections power5 = power4 & ~MoveDirections.SouthWest;
            MoveDirections power6 = power5 & ~MoveDirections.West;
            MoveDirections power7 = power6 & ~MoveDirections.East;

            Assert.That(profile.GetDirections(1), Is.EqualTo(MoveDirections.All));
            Assert.That(profile.GetDirections(2), Is.EqualTo(power2));
            Assert.That(profile.GetDirections(3), Is.EqualTo(power3));
            Assert.That(profile.GetDirections(4), Is.EqualTo(power4));
            Assert.That(profile.GetDirections(5), Is.EqualTo(power5));
            Assert.That(profile.GetDirections(6), Is.EqualTo(power6));
            Assert.That(profile.GetDirections(7), Is.EqualTo(power7));
            Assert.That(profile.GetDirections(8), Is.EqualTo(power7));
            Assert.That(profile.GetDirections(100), Is.EqualTo(power7));

            // 累積の結果を絶対値でも固定する。
            Assert.That(power6, Is.EqualTo(
                MoveDirections.North | MoveDirections.East | MoveDirections.South));
            Assert.That(power7, Is.EqualTo(
                MoveDirections.North | MoveDirections.South));
        }

        [Test]
        public void MovementProfileRejectsPowerRangeGaps()
        {
            Assert.Throws<ArgumentException>(() => new PowerMovementProfile(
                new MovementProfileId("invalid"),
                new[]
                {
                    new PowerMovementBand(1, 1, MoveDirections.All),
                    new PowerMovementBand(3, int.MaxValue, MoveDirections.All)
                }));
        }

        [Test]
        public void CombatPowerChangeImmediatelyChangesLegalDirections()
        {
            GameDefinition definition = CreateDefinition(
                PlayerId.Player1,
                new Dictionary<GridPosition, string[]>
                {
                    [new GridPosition(2, 3)] = new[] { "power-up" }
                },
                InitialPiece(1, 2, 2, PlayerId.Player1));
            GameSession custom = new GameSession(
                definition,
                cellEffectHandlers: new ICellEffectHandler[]
                {
                    new RecordingPowerEffect("power-up", new List<string>())
                });

            custom.Execute(new MovePieceCommand(
                PlayerId.Player1, new PieceId(1), new GridPosition(2, 3)));

            PieceState poweredPiece = GetPiece(
                custom.Snapshot, new GridPosition(2, 3));
            MovePieceCommand[] legalMoves = custom.GetLegalCommands(PlayerId.Player1)
                .OfType<MovePieceCommand>()
                .Where(move => move.PieceId == poweredPiece.Id)
                .ToArray();

            Assert.That(poweredPiece.CombatPower, Is.EqualTo(2));
            Assert.That(legalMoves.Select(move => move.Destination),
                Has.None.EqualTo(new GridPosition(3, 4)));
            Assert.That(legalMoves.Select(move => move.Destination),
                Does.Contain(new GridPosition(2, 4)));
        }

        [Test]
        public void ValidMoveChangesPositionAndSwitchesTurn()
        {
            PieceState piece = GetPiece(session.Snapshot, new GridPosition(0, 1));
            CommandResult result = session.Execute(new MovePieceCommand(
                PlayerId.Player1, piece.Id, new GridPosition(0, 2)));

            Assert.That(result.Success, Is.True);
            Assert.That(result.Events.OfType<PieceMoved>().Count(), Is.EqualTo(1));
            AssertPiece(session.Snapshot, new GridPosition(0, 2), PlayerId.Player1, 1);
            Assert.That(session.Snapshot.CurrentPlayer, Is.EqualTo(PlayerId.Player2));
        }

        [Test]
        public void InvalidPlayerAndDestinationAreRejectedWithoutChangingSnapshot()
        {
            GameSnapshot before = session.Snapshot;
            PieceState piece = GetPiece(before, new GridPosition(0, 1));

            CommandResult wrongPlayer = session.Execute(new MovePieceCommand(
                PlayerId.Player2, piece.Id, new GridPosition(0, 2)));
            CommandResult tooFar = session.Execute(new MovePieceCommand(
                PlayerId.Player1, piece.Id, new GridPosition(0, 3)));

            Assert.That(wrongPlayer.FailureReason, Is.EqualTo(CommandFailureReason.NotPlayersTurn));
            Assert.That(tooFar.FailureReason, Is.EqualTo(CommandFailureReason.IllegalMove));
            AssertPiece(session.Snapshot, new GridPosition(0, 1), PlayerId.Player1, 1);
            Assert.That(session.Snapshot.CurrentPlayer, Is.EqualTo(PlayerId.Player1));
        }

        [Test]
        public void PlayersCannotMoveIntoTheirOwnTerritory()
        {
            GameSession custom = CreateSession(PlayerId.Player1,
                InitialPiece(1, 2, 1, PlayerId.Player1),
                InitialPiece(2, 5, 8, PlayerId.Player2));

            CommandResult result = custom.Execute(new MovePieceCommand(
                PlayerId.Player1, new PieceId(1), new GridPosition(2, 0)));

            Assert.That(result.FailureReason, Is.EqualTo(CommandFailureReason.IllegalMove));
        }

        [Test]
        public void EqualCombatPowerDestroysBothPieces()
        {
            GameSession custom = CreateSession(PlayerId.Player1,
                InitialPiece(1, 2, 2, PlayerId.Player1),
                InitialPiece(2, 3, 3, PlayerId.Player2),
                InitialPiece(3, 5, 8, PlayerId.Player2));

            CommandResult result = custom.Execute(new MovePieceCommand(
                PlayerId.Player1, new PieceId(1), new GridPosition(3, 3)));

            Assert.That(result.Success, Is.True);
            Assert.That(result.Events.OfType<CombatResolved>().Count(), Is.EqualTo(1));
            Assert.That(result.Events.OfType<PieceDestroyed>().Count(), Is.EqualTo(2));
            Assert.That(custom.Snapshot.TryGetPiece(new PieceId(1), out _), Is.False);
            Assert.That(custom.Snapshot.TryGetPiece(new PieceId(2), out _), Is.False);
        }

        [Test]
        public void StrongerAttackerMovesWithRemainingPower()
        {
            // 戦闘力5は累積制限で斜め4方向を失うため、東（右）へ攻撃する。
            GameSession custom = CreateSession(PlayerId.Player1,
                InitialPiece(1, 2, 2, PlayerId.Player1, 5),
                InitialPiece(2, 3, 2, PlayerId.Player2, 2),
                InitialPiece(3, 5, 8, PlayerId.Player2));

            custom.Execute(new MovePieceCommand(
                PlayerId.Player1, new PieceId(1), new GridPosition(3, 2)));

            AssertPiece(custom.Snapshot, new GridPosition(3, 2), PlayerId.Player1, 3);
            Assert.That(custom.Snapshot.TryGetPiece(new PieceId(2), out _), Is.False);
        }

        [Test]
        public void StrongerDefenderStaysWithRemainingPower()
        {
            GameSession custom = CreateSession(PlayerId.Player1,
                InitialPiece(1, 2, 2, PlayerId.Player1, 2),
                InitialPiece(2, 3, 2, PlayerId.Player2, 5));

            custom.Execute(new MovePieceCommand(
                PlayerId.Player1, new PieceId(1), new GridPosition(3, 2)));

            AssertPiece(custom.Snapshot, new GridPosition(3, 2), PlayerId.Player2, 3);
            Assert.That(custom.Snapshot.TryGetPiece(new PieceId(1), out _), Is.False);
        }

        [Test]
        public void ReachingOpponentTerritoryWinsAndLocksCommands()
        {
            GameSession custom = CreateSession(PlayerId.Player1,
                InitialPiece(1, 2, 8, PlayerId.Player1),
                InitialPiece(2, 5, 8, PlayerId.Player2));

            CommandResult winningMove = custom.Execute(new MovePieceCommand(
                PlayerId.Player1, new PieceId(1), new GridPosition(2, 9)));
            CommandResult afterWin = custom.Execute(new MovePieceCommand(
                PlayerId.Player2, new PieceId(2), new GridPosition(5, 7)));

            Assert.That(winningMove.Events.OfType<GameEnded>().Single().Winner,
                Is.EqualTo(PlayerId.Player1));
            Assert.That(custom.Snapshot.Winner, Is.EqualTo(PlayerId.Player1));
            Assert.That(afterWin.FailureReason, Is.EqualTo(CommandFailureReason.GameOver));
        }

        [Test]
        public void DefeatingEveryOpponentDoesNotWinAndPassesTurnBack()
        {
            GameSession custom = CreateSession(PlayerId.Player1,
                InitialPiece(1, 2, 2, PlayerId.Player1, 2),
                InitialPiece(2, 3, 2, PlayerId.Player2, 1));

            custom.Execute(new MovePieceCommand(
                PlayerId.Player1, new PieceId(1), new GridPosition(3, 2)));

            Assert.That(custom.Snapshot.GetPieceCount(PlayerId.Player2), Is.Zero);
            Assert.That(custom.Snapshot.Winner, Is.Null);
            Assert.That(custom.Snapshot.IsGameOver, Is.False);
            Assert.That(custom.Snapshot.CurrentPlayer, Is.EqualTo(PlayerId.Player1));
        }

        [Test]
        public void NoLegalActionsForEitherPlayerIsDraw()
        {
            GameSession custom = CreateSession(PlayerId.Player1);

            Assert.That(custom.Snapshot.IsDraw, Is.True);
            Assert.That(custom.Snapshot.IsGameOver, Is.True);
        }

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

        [Test]
        public void OldSnapshotDoesNotChangeAfterExecutingACommand()
        {
            GameSnapshot before = session.Snapshot;
            PieceState beforePiece = GetPiece(before, new GridPosition(0, 1));

            session.Execute(new MovePieceCommand(
                PlayerId.Player1, beforePiece.Id, new GridPosition(0, 2)));

            Assert.That(before.TryGetPiece(new GridPosition(0, 1), out _), Is.True);
            Assert.That(before.TryGetPiece(new GridPosition(0, 2), out _), Is.False);
        }

        [Test]
        public void ResetRestoresStandardPositionAndFirstTurn()
        {
            PieceState piece = GetPiece(session.Snapshot, new GridPosition(0, 1));
            session.Execute(new MovePieceCommand(
                PlayerId.Player1, piece.Id, new GridPosition(0, 2)));

            session.Reset();

            Assert.That(session.Snapshot.Pieces.Count, Is.EqualTo(12));
            Assert.That(session.Snapshot.CurrentPlayer, Is.EqualTo(PlayerId.Player1));
            AssertPiece(session.Snapshot, new GridPosition(0, 1), PlayerId.Player1, 1);
        }

        private static GameSession CreateSession(
            PlayerId firstPlayer,
            params InitialPieceDefinition[] pieces)
        {
            return new GameSession(CreateDefinition(firstPlayer, null, pieces));
        }

        private static GameDefinition CreateDefinition(
            PlayerId firstPlayer,
            IDictionary<GridPosition, string[]> cellEffects,
            params InitialPieceDefinition[] pieces)
        {
            return CreateDefinitionWithProfiles(
                firstPlayer,
                new[] { PowerMovementProfile.CreateStandard() },
                cellEffects,
                pieces);
        }

        private static GameDefinition CreateDefinitionWithProfiles(
            PlayerId firstPlayer,
            IEnumerable<PowerMovementProfile> movementProfiles,
            IDictionary<GridPosition, string[]> cellEffects,
            params InitialPieceDefinition[] pieces)
        {
            List<CellDefinition> cells = new List<CellDefinition>(60);
            for (int row = 0; row < 10; row++)
            {
                for (int column = 0; column < 6; column++)
                {
                    GridPosition position = new GridPosition(column, row);
                    PlayerId? territoryOwner = row == 0
                        ? PlayerId.Player1
                        : row == 9 ? PlayerId.Player2 : (PlayerId?)null;
                    string[] effects = null;
                    if (cellEffects != null)
                    {
                        cellEffects.TryGetValue(position, out effects);
                    }
                    cells.Add(new CellDefinition(position, territoryOwner, effects));
                }
            }

            CellEffectDefinition[] effectDefinitions = (cellEffects ??
                    new Dictionary<GridPosition, string[]>())
                .Values
                .SelectMany(effectIds => effectIds ?? Array.Empty<string>())
                .Distinct(StringComparer.Ordinal)
                .Select(effectId => new CellEffectDefinition(
                    effectId, CellEffectLifetime.PermanentOncePerPiece))
                .ToArray();
            return new GameDefinition(
                6,
                10,
                cells,
                pieces,
                firstPlayer,
                movementProfiles,
                effectDefinitions);
        }

        private static GameDefinition CreateDefinitionWithEffects(
            PlayerId firstPlayer,
            IDictionary<GridPosition, string[]> cellEffects,
            IEnumerable<CellEffectDefinition> effectDefinitions,
            params InitialPieceDefinition[] pieces)
        {
            return CreateDefinitionWithEffectsAndLimits(
                firstPlayer,
                cellEffects,
                effectDefinitions,
                GameDefinition.StandardMaxPiecesPerPlayer,
                GameDefinition.StandardReserveDeploymentDepth,
                pieces);
        }

        private static GameDefinition CreateDefinitionWithEffectsAndLimits(
            PlayerId firstPlayer,
            IDictionary<GridPosition, string[]> cellEffects,
            IEnumerable<CellEffectDefinition> effectDefinitions,
            int maxPiecesPerPlayer,
            int reserveDeploymentDepth,
            params InitialPieceDefinition[] pieces)
        {
            List<CellDefinition> cells = new List<CellDefinition>(60);
            for (int row = 0; row < 10; row++)
            {
                for (int column = 0; column < 6; column++)
                {
                    GridPosition position = new GridPosition(column, row);
                    PlayerId? territoryOwner = row == 0
                        ? PlayerId.Player1
                        : row == 9 ? PlayerId.Player2 : (PlayerId?)null;
                    cellEffects.TryGetValue(position, out string[] effects);
                    cells.Add(new CellDefinition(position, territoryOwner, effects));
                }
            }

            return new GameDefinition(
                6,
                10,
                cells,
                pieces,
                firstPlayer,
                new[] { PowerMovementProfile.CreateStandard() },
                effectDefinitions,
                maxPiecesPerPlayer,
                reserveDeploymentDepth);
        }

        private static InitialPieceDefinition InitialPiece(
            int id,
            int column,
            int row,
            PlayerId owner,
            int power = 1,
            string movementProfileId = PowerMovementProfile.StandardIdValue)
        {
            return new InitialPieceDefinition(
                new PieceId(id),
                owner,
                new GridPosition(column, row),
                power,
                new MovementProfileId(movementProfileId));
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

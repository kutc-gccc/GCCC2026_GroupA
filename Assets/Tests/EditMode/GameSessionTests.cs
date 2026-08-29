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

            Assert.That(profile.GetDirections(1), Is.EqualTo(MoveDirections.All));
            Assert.That(profile.GetDirections(2), Is.EqualTo(
                MoveDirections.All & ~MoveDirections.NorthEast));
            Assert.That(profile.GetDirections(3), Is.EqualTo(
                MoveDirections.All & ~MoveDirections.SouthEast));
            Assert.That(profile.GetDirections(4), Is.EqualTo(
                MoveDirections.All & ~MoveDirections.NorthWest));
            Assert.That(profile.GetDirections(5), Is.EqualTo(
                MoveDirections.All & ~MoveDirections.SouthWest));
            Assert.That(profile.GetDirections(6), Is.EqualTo(
                MoveDirections.All & ~MoveDirections.West));
            Assert.That(profile.GetDirections(7), Is.EqualTo(
                MoveDirections.All & ~MoveDirections.East));
            Assert.That(profile.GetDirections(8), Is.EqualTo(MoveDirections.All));
            Assert.That(profile.GetDirections(100), Is.EqualTo(MoveDirections.All));
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
            GameSession custom = CreateSession(PlayerId.Player1,
                InitialPiece(1, 2, 2, PlayerId.Player1, 5),
                InitialPiece(2, 3, 3, PlayerId.Player2, 2),
                InitialPiece(3, 5, 8, PlayerId.Player2));

            custom.Execute(new MovePieceCommand(
                PlayerId.Player1, new PieceId(1), new GridPosition(3, 3)));

            AssertPiece(custom.Snapshot, new GridPosition(3, 3), PlayerId.Player1, 3);
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

            return new GameDefinition(
                6,
                10,
                cells,
                pieces,
                firstPlayer,
                movementProfiles);
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

        private sealed class RecordingPowerEffect : ICellEffectHandler
        {
            private readonly IList<string> order;

            public RecordingPowerEffect(string effectId, IList<string> order)
            {
                EffectId = effectId;
                this.order = order;
            }

            public string EffectId { get; }

            public CellEffectResult Apply(CellEffectContext context)
            {
                order.Add(EffectId);
                return new CellEffectResult(
                    context.Piece.WithCombatPower(context.Piece.CombatPower + 1));
            }
        }
    }
}

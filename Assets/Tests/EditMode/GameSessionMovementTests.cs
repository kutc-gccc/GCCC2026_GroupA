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

    }
}

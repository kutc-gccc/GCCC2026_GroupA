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

    }
}

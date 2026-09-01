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
    }
}

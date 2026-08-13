using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GCCC.BoardGame.Tests
{
    public sealed class BoardStateTests
    {
        private BoardState board;

        [SetUp]
        public void SetUp()
        {
            board = new BoardState(6, 10);
        }

        [Test]
        public void StandardGameStartsWithTwelvePiecesOutsideTheTerritories()
        {
            Assert.That(board.Columns, Is.EqualTo(6));
            Assert.That(board.Rows, Is.EqualTo(10));
            Assert.That(board.PieceCount, Is.EqualTo(12));
            Assert.That(board.GetPieceCount(PlayerId.Player1), Is.EqualTo(6));
            Assert.That(board.GetPieceCount(PlayerId.Player2), Is.EqualTo(6));
            Assert.That(board.CurrentPlayer, Is.EqualTo(PlayerId.Player1));

            for (int column = 0; column < 6; column++)
            {
                AssertOwner(board, new Vector2Int(column, 1), PlayerId.Player1);
                AssertOwner(board, new Vector2Int(column, 8), PlayerId.Player2);
                AssertCombatPower(board, new Vector2Int(column, 1), 1);
                AssertCombatPower(board, new Vector2Int(column, 8), 1);
                Assert.That(board.HasPiece(new Vector2Int(column, 0)), Is.False);
                Assert.That(board.HasPiece(new Vector2Int(column, 9)), Is.False);
            }
        }

        [Test]
        public void PiecesCanHaveIndividualCombatPowerAndKeepItWhenMoved()
        {
            BoardState custom = new BoardState(
                6,
                10,
                new[]
                {
                    PoweredPiece(2, 2, PlayerId.Player1, 4),
                    PoweredPiece(5, 8, PlayerId.Player2, 2)
                },
                PlayerId.Player1);

            AssertCombatPower(custom, new Vector2Int(2, 2), 4);
            AssertCombatPower(custom, new Vector2Int(5, 8), 2);
            Assert.That(custom.TryMove(new Vector2Int(2, 2), new Vector2Int(3, 3)), Is.True);
            Assert.That(custom.TryGetCombatPower(new Vector2Int(2, 2), out _), Is.False);
            AssertCombatPower(custom, new Vector2Int(3, 3), 4);
        }

        [Test]
        public void CombatPowerMustBeGreaterThanZero()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new BoardPiece(PlayerId.Player1, 0));
        }

        [Test]
        public void TerritoryOwnershipMatchesTheTopAndBottomRows()
        {
            Assert.That(board.IsOwnTerritory(PlayerId.Player1, new Vector2Int(3, 0)), Is.True);
            Assert.That(board.IsOpponentTerritory(PlayerId.Player1, new Vector2Int(3, 9)), Is.True);
            Assert.That(board.IsOwnTerritory(PlayerId.Player2, new Vector2Int(3, 9)), Is.True);
            Assert.That(board.IsOpponentTerritory(PlayerId.Player2, new Vector2Int(3, 0)), Is.True);
            Assert.That(board.IsOwnTerritory(PlayerId.Player1, new Vector2Int(3, 1)), Is.False);
        }

        [Test]
        public void InitialPieceHasOnlyThreeForwardLegalMoves()
        {
            IReadOnlyList<Vector2Int> moves = board.GetLegalMoves(new Vector2Int(2, 1));

            Assert.That(moves, Is.EquivalentTo(new[]
            {
                new Vector2Int(1, 2),
                new Vector2Int(2, 2),
                new Vector2Int(3, 2)
            }));
        }

        [Test]
        public void OrthogonalAndDiagonalOneStepMovesAreAllowedAndSwitchTurn()
        {
            Assert.That(board.TryMove(new Vector2Int(0, 1), new Vector2Int(0, 2)), Is.True);
            Assert.That(board.CurrentPlayer, Is.EqualTo(PlayerId.Player2));

            Assert.That(board.TryMove(new Vector2Int(5, 8), new Vector2Int(4, 7)), Is.True);
            Assert.That(board.CurrentPlayer, Is.EqualTo(PlayerId.Player1));
        }

        [Test]
        public void InvalidMovesLeaveThePositionAndTurnUnchanged()
        {
            BoardState custom = CreateState(PlayerId.Player1,
                Piece(2, 2, PlayerId.Player1),
                Piece(3, 3, PlayerId.Player1),
                Piece(5, 8, PlayerId.Player2));

            Assert.That(custom.TryMove(new Vector2Int(2, 2), new Vector2Int(2, 2)), Is.False);
            Assert.That(custom.TryMove(new Vector2Int(2, 2), new Vector2Int(4, 4)), Is.False);
            Assert.That(custom.TryMove(new Vector2Int(2, 2), new Vector2Int(3, 3)), Is.False);
            Assert.That(custom.TryMove(new Vector2Int(2, 2), new Vector2Int(-1, 2)), Is.False);
            Assert.That(custom.TryMove(new Vector2Int(5, 8), new Vector2Int(5, 7)), Is.False);
            Assert.That(custom.CurrentPlayer, Is.EqualTo(PlayerId.Player1));
            AssertOwner(custom, new Vector2Int(2, 2), PlayerId.Player1);
            AssertOwner(custom, new Vector2Int(3, 3), PlayerId.Player1);
        }

        [Test]
        public void PlayersCannotMoveIntoTheirOwnTerritories()
        {
            BoardState player1Turn = CreateState(PlayerId.Player1,
                Piece(2, 1, PlayerId.Player1),
                Piece(5, 8, PlayerId.Player2));
            Assert.That(player1Turn.TryMove(new Vector2Int(2, 1), new Vector2Int(2, 0)), Is.False);

            BoardState player2Turn = CreateState(PlayerId.Player2,
                Piece(0, 1, PlayerId.Player1),
                Piece(2, 8, PlayerId.Player2));
            Assert.That(player2Turn.TryMove(new Vector2Int(2, 8), new Vector2Int(2, 9)), Is.False);
        }

        [Test]
        public void MovingOntoAnOpponentCapturesItWithoutEndingTheGame()
        {
            BoardState custom = CreateState(PlayerId.Player1,
                Piece(2, 2, PlayerId.Player1),
                Piece(3, 3, PlayerId.Player2),
                Piece(5, 8, PlayerId.Player2));

            Assert.That(custom.TryMove(new Vector2Int(2, 2), new Vector2Int(3, 3)), Is.True);
            AssertOwner(custom, new Vector2Int(3, 3), PlayerId.Player1);
            Assert.That(custom.GetPieceCount(PlayerId.Player2), Is.EqualTo(1));
            Assert.That(custom.Winner, Is.Null);
            Assert.That(custom.CurrentPlayer, Is.EqualTo(PlayerId.Player2));
        }

        [Test]
        public void EnteringTheOpponentTerritoryWinsAndLocksTheGame()
        {
            BoardState custom = CreateState(PlayerId.Player1,
                Piece(2, 8, PlayerId.Player1),
                Piece(5, 8, PlayerId.Player2));

            Assert.That(custom.TryMove(new Vector2Int(2, 8), new Vector2Int(2, 9)), Is.True);
            Assert.That(custom.Winner, Is.EqualTo(PlayerId.Player1));
            Assert.That(custom.IsGameOver, Is.True);
            Assert.That(custom.TryMove(new Vector2Int(5, 8), new Vector2Int(5, 7)), Is.False);
        }

        [Test]
        public void CapturingEveryOpponentPieceAutomaticallyPassesBackWithoutWinning()
        {
            BoardState custom = CreateState(PlayerId.Player1,
                Piece(2, 2, PlayerId.Player1),
                Piece(3, 3, PlayerId.Player2));

            Assert.That(custom.TryMove(new Vector2Int(2, 2), new Vector2Int(3, 3)), Is.True);
            Assert.That(custom.GetPieceCount(PlayerId.Player2), Is.Zero);
            Assert.That(custom.Winner, Is.Null);
            Assert.That(custom.IsGameOver, Is.False);
            Assert.That(custom.CurrentPlayer, Is.EqualTo(PlayerId.Player1));
        }

        [Test]
        public void PositionWithNoMovesForEitherPlayerIsADraw()
        {
            BoardState custom = new BoardState(
                6,
                10,
                Array.Empty<KeyValuePair<Vector2Int, PlayerId>>(),
                PlayerId.Player1);

            Assert.That(custom.IsDraw, Is.True);
            Assert.That(custom.IsGameOver, Is.True);
        }

        [Test]
        public void ResetRestoresTheStandardPositionAndFirstTurn()
        {
            Assert.That(board.TryMove(new Vector2Int(0, 1), new Vector2Int(0, 2)), Is.True);

            board.ResetGame();

            Assert.That(board.CurrentPlayer, Is.EqualTo(PlayerId.Player1));
            Assert.That(board.PieceCount, Is.EqualTo(12));
            Assert.That(board.Winner, Is.Null);
            Assert.That(board.IsDraw, Is.False);
            AssertOwner(board, new Vector2Int(0, 1), PlayerId.Player1);
            AssertOwner(board, new Vector2Int(0, 8), PlayerId.Player2);
        }

        private static BoardState CreateState(
            PlayerId currentPlayer,
            params KeyValuePair<Vector2Int, PlayerId>[] pieces)
        {
            return new BoardState(6, 10, pieces, currentPlayer);
        }

        private static KeyValuePair<Vector2Int, PlayerId> Piece(
            int column,
            int row,
            PlayerId owner)
        {
            return new KeyValuePair<Vector2Int, PlayerId>(
                new Vector2Int(column, row), owner);
        }

        private static KeyValuePair<Vector2Int, BoardPiece> PoweredPiece(
            int column,
            int row,
            PlayerId owner,
            int combatPower)
        {
            return new KeyValuePair<Vector2Int, BoardPiece>(
                new Vector2Int(column, row),
                new BoardPiece(owner, combatPower));
        }

        private static void AssertOwner(
            BoardState state,
            Vector2Int position,
            PlayerId expectedOwner)
        {
            Assert.That(state.TryGetOwner(position, out PlayerId actualOwner), Is.True);
            Assert.That(actualOwner, Is.EqualTo(expectedOwner));
        }

        private static void AssertCombatPower(
            BoardState state,
            Vector2Int position,
            int expectedCombatPower)
        {
            Assert.That(state.TryGetPiece(position, out BoardPiece piece), Is.True);
            Assert.That(piece.CombatPower, Is.EqualTo(expectedCombatPower));
        }
    }
}

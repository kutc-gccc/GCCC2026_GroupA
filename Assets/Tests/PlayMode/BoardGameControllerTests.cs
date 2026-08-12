using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace GCCC.BoardGame.Tests
{
    public sealed class BoardGameControllerTests
    {
        private GameObject controllerObject;
        private BoardGameController controller;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            controllerObject = new GameObject("Board Game Controller Test");
            controller = controllerObject.AddComponent<BoardGameController>();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (controllerObject != null)
            {
                Object.Destroy(controllerObject);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator AwakeBuildsBoardPiecesTerritoriesAndUi()
        {
            Assert.That(controller.GeneratedCellCount, Is.EqualTo(60));
            Assert.That(controller.PieceViewCount, Is.EqualTo(12));
            Assert.That(controller.State.CurrentPlayer, Is.EqualTo(PlayerId.Player1));
            Assert.That(controller.StatusText, Does.Contain("プレイヤー1"));
            Assert.That(GameObject.Find("Reset Button"), Is.Not.Null);
            Assert.That(GameObject.Find("Turn Status"), Is.Not.Null);
            Assert.That(GameObject.Find("Player 1 Territory Border"), Is.Not.Null);
            Assert.That(GameObject.Find("Player 2 Territory Border"), Is.Not.Null);
            Assert.That(GameObject.Find("Player 1 Territory Label"), Is.Not.Null);
            Assert.That(GameObject.Find("Player 2 Territory Label"), Is.Not.Null);

            SpriteRenderer territoryCell = GameObject.Find("Cell (0, 0)")
                .GetComponent<SpriteRenderer>();
            SpriteRenderer normalCell = GameObject.Find("Cell (0, 2)")
                .GetComponent<SpriteRenderer>();
            Assert.That(territoryCell.color, Is.EqualTo(normalCell.color));
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerPiecesUseBlueAndRedColors()
        {
            SpriteRenderer player1Piece = GameObject.Find("Player1 Piece (0, 1)")
                .GetComponent<SpriteRenderer>();
            SpriteRenderer player2Piece = GameObject.Find("Player2 Piece (0, 8)")
                .GetComponent<SpriteRenderer>();

            Assert.That(player1Piece.color.b, Is.GreaterThan(player1Piece.color.r));
            Assert.That(player2Piece.color.r, Is.GreaterThan(player2Piece.color.b));
            Assert.That(player1Piece.color, Is.Not.EqualTo(player2Piece.color));
            yield return null;
        }

        [UnityTest]
        public IEnumerator OnlyCurrentPlayerCanSelectAndLegalMovesAreHighlighted()
        {
            controller.HandleCellClick(new Vector2Int(2, 8));
            Assert.That(controller.SelectedCell, Is.Null);
            Assert.That(controller.MoveIndicatorCount, Is.Zero);

            controller.HandleCellClick(new Vector2Int(2, 1));
            Assert.That(controller.SelectedCell, Is.EqualTo(new Vector2Int(2, 1)));
            Assert.That(controller.MoveIndicatorCount, Is.EqualTo(3));

            controller.HandleCellClick(new Vector2Int(2, 1));
            Assert.That(controller.SelectedCell, Is.Null);
            Assert.That(controller.MoveIndicatorCount, Is.Zero);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ValidMoveUpdatesTheViewAndSwitchesTurn()
        {
            Move(new Vector2Int(2, 1), new Vector2Int(2, 2));

            AssertOwner(new Vector2Int(2, 2), PlayerId.Player1);
            Assert.That(controller.State.HasPiece(new Vector2Int(2, 1)), Is.False);
            Assert.That(controller.State.CurrentPlayer, Is.EqualTo(PlayerId.Player2));
            Assert.That(controller.StatusText, Does.Contain("プレイヤー2"));
            Assert.That(controller.SelectedCell, Is.Null);
            Assert.That(controller.MoveIndicatorCount, Is.Zero);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AdjacentOpponentCanBeCaptured()
        {
            Move(new Vector2Int(0, 1), new Vector2Int(0, 2));
            Move(new Vector2Int(0, 8), new Vector2Int(0, 7));
            Move(new Vector2Int(0, 2), new Vector2Int(0, 3));
            Move(new Vector2Int(0, 7), new Vector2Int(0, 6));
            Move(new Vector2Int(0, 3), new Vector2Int(0, 4));
            Move(new Vector2Int(0, 6), new Vector2Int(0, 5));
            Move(new Vector2Int(0, 4), new Vector2Int(0, 5));

            AssertOwner(new Vector2Int(0, 5), PlayerId.Player1);
            Assert.That(controller.State.GetPieceCount(PlayerId.Player2), Is.EqualTo(5));
            Assert.That(controller.PieceViewCount, Is.EqualTo(11));
            Assert.That(controller.State.Winner, Is.Null);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ReachingTheOpponentTerritoryWinsAndLocksInput()
        {
            Move(new Vector2Int(0, 1), new Vector2Int(0, 2));
            Move(new Vector2Int(0, 8), new Vector2Int(1, 7));
            Move(new Vector2Int(0, 2), new Vector2Int(0, 3));
            Move(new Vector2Int(5, 8), new Vector2Int(5, 7));
            Move(new Vector2Int(0, 3), new Vector2Int(0, 4));
            Move(new Vector2Int(5, 7), new Vector2Int(5, 6));
            Move(new Vector2Int(0, 4), new Vector2Int(0, 5));
            Move(new Vector2Int(5, 6), new Vector2Int(5, 5));
            Move(new Vector2Int(0, 5), new Vector2Int(0, 6));
            Move(new Vector2Int(5, 5), new Vector2Int(5, 4));
            Move(new Vector2Int(0, 6), new Vector2Int(0, 7));
            Move(new Vector2Int(5, 4), new Vector2Int(5, 3));
            Move(new Vector2Int(0, 7), new Vector2Int(0, 8));
            Move(new Vector2Int(5, 3), new Vector2Int(5, 2));
            Move(new Vector2Int(0, 8), new Vector2Int(0, 9));

            Assert.That(controller.State.Winner, Is.EqualTo(PlayerId.Player1));
            Assert.That(controller.StatusText, Does.Contain("勝利"));

            controller.HandleCellClick(new Vector2Int(1, 7));
            Assert.That(controller.SelectedCell, Is.Null);
            Assert.That(controller.State.CurrentPlayer, Is.EqualTo(PlayerId.Player1));
            yield return null;
        }

        [UnityTest]
        public IEnumerator ResetButtonRestoresInitialPositionAndFirstTurn()
        {
            Move(new Vector2Int(0, 1), new Vector2Int(0, 2));

            Button resetButton = GameObject.Find("Reset Button").GetComponent<Button>();
            resetButton.onClick.Invoke();

            Assert.That(controller.State.PieceCount, Is.EqualTo(12));
            Assert.That(controller.PieceViewCount, Is.EqualTo(12));
            Assert.That(controller.State.CurrentPlayer, Is.EqualTo(PlayerId.Player1));
            Assert.That(controller.SelectedCell, Is.Null);
            Assert.That(controller.MoveIndicatorCount, Is.Zero);
            Assert.That(controller.State.HasPiece(new Vector2Int(0, 2)), Is.False);
            AssertOwner(new Vector2Int(0, 1), PlayerId.Player1);
            AssertOwner(new Vector2Int(0, 8), PlayerId.Player2);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SampleSceneLoadsWithAWorkingTerritoryGame()
        {
            SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
            yield return null;

            BoardGameController sceneController =
                Object.FindFirstObjectByType<BoardGameController>();
            Assert.That(sceneController, Is.Not.Null);
            Assert.That(sceneController.GeneratedCellCount, Is.EqualTo(60));
            Assert.That(sceneController.State.PieceCount, Is.EqualTo(12));
            Assert.That(sceneController.State.CurrentPlayer, Is.EqualTo(PlayerId.Player1));
            Assert.That(GameObject.Find("Reset Button"), Is.Not.Null);
            Assert.That(GameObject.Find("Player 1 Territory Border"), Is.Not.Null);
            Assert.That(GameObject.Find("Player 2 Territory Border"), Is.Not.Null);

            controllerObject = null;
        }

        private void Move(Vector2Int from, Vector2Int to)
        {
            controller.HandleCellClick(from);
            Assert.That(controller.SelectedCell, Is.EqualTo(from));
            controller.HandleCellClick(to);
        }

        private void AssertOwner(Vector2Int position, PlayerId expectedOwner)
        {
            Assert.That(controller.State.TryGetOwner(position, out PlayerId owner), Is.True);
            Assert.That(owner, Is.EqualTo(expectedOwner));
        }
    }
}

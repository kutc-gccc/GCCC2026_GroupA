using System.Collections;
using GCCC.BoardGame.Core.Model;
using GCCC.BoardGame.Presentation.Bootstrap;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace GCCC.BoardGame.Tests
{
    public sealed class BoardGameBootstrapTests
    {
        private GameObject bootstrapObject;
        private BoardGameBootstrap bootstrap;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            bootstrapObject = new GameObject("Board Game Bootstrap Test");
            bootstrap = bootstrapObject.AddComponent<BoardGameBootstrap>();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (bootstrapObject != null)
            {
                Object.Destroy(bootstrapObject);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator AwakeBuildsSeparatedBoardPiecesTerritoriesAndHud()
        {
            Assert.That(bootstrap.GeneratedCellCount, Is.EqualTo(60));
            Assert.That(bootstrap.PieceViewCount, Is.EqualTo(12));
            Assert.That(bootstrap.Snapshot.CurrentPlayer, Is.EqualTo(PlayerId.Player1));
            Assert.That(bootstrap.StatusText, Does.Contain("プレイヤー1"));
            Assert.That(GameObject.Find("Board View"), Is.Not.Null);
            Assert.That(GameObject.Find("Piece Views"), Is.Not.Null);
            Assert.That(GameObject.Find("Game HUD"), Is.Not.Null);
            Assert.That(GameObject.Find("Board Input"), Is.Not.Null);
            Assert.That(GameObject.Find("Reset Button"), Is.Not.Null);
            Assert.That(GameObject.Find("Player 1 Territory Border"), Is.Not.Null);
            Assert.That(GameObject.Find("Player 2 Territory Border"), Is.Not.Null);

            SpriteRenderer territoryCell = GameObject.Find("Cell (0, 0)")
                .GetComponent<SpriteRenderer>();
            SpriteRenderer normalCell = GameObject.Find("Cell (0, 2)")
                .GetComponent<SpriteRenderer>();
            Assert.That(territoryCell.color, Is.EqualTo(normalCell.color));
            yield return null;
        }

        [UnityTest]
        public IEnumerator PieceViewsRenderOwnersAndCombatPower()
        {
            GameObject player1 = GameObject.Find("Player1 Piece (0, 1)");
            GameObject player2 = GameObject.Find("Player2 Piece (0, 8)");
            SpriteRenderer player1Renderer = player1.GetComponent<SpriteRenderer>();
            SpriteRenderer player2Renderer = player2.GetComponent<SpriteRenderer>();

            Assert.That(player1Renderer.color.b, Is.GreaterThan(player1Renderer.color.r));
            Assert.That(player2Renderer.color.r, Is.GreaterThan(player2Renderer.color.b));
            Assert.That(player1.transform.Find("Combat Power").GetComponent<TextMesh>().text,
                Is.EqualTo("1"));
            Assert.That(player2.transform.Find("Combat Power").GetComponent<TextMesh>().text,
                Is.EqualTo("1"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator OnlyCurrentPlayerCanSelectAndLegalMovesAreHighlighted()
        {
            bootstrap.HandleCellClick(new GridPosition(2, 8));
            Assert.That(bootstrap.SelectedCell, Is.Null);
            Assert.That(bootstrap.MoveIndicatorCount, Is.Zero);

            bootstrap.HandleCellClick(new GridPosition(2, 1));
            Assert.That(bootstrap.SelectedCell, Is.EqualTo(new GridPosition(2, 1)));
            Assert.That(bootstrap.MoveIndicatorCount, Is.EqualTo(3));

            bootstrap.HandleCellClick(new GridPosition(2, 1));
            Assert.That(bootstrap.SelectedCell, Is.Null);
            Assert.That(bootstrap.MoveIndicatorCount, Is.Zero);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ValidInputExecutesOneCommandAndUpdatesViews()
        {
            Move(new GridPosition(2, 1), new GridPosition(2, 2));

            AssertPiece(new GridPosition(2, 2), PlayerId.Player1);
            Assert.That(bootstrap.Snapshot.TryGetPiece(new GridPosition(2, 1), out _), Is.False);
            Assert.That(bootstrap.Coordinator.ExecutedCommandCount, Is.EqualTo(1));
            Assert.That(bootstrap.Snapshot.CurrentPlayer, Is.EqualTo(PlayerId.Player2));
            Assert.That(bootstrap.StatusText, Does.Contain("プレイヤー2"));
            Assert.That(bootstrap.SelectedCell, Is.Null);
            Assert.That(bootstrap.MoveIndicatorCount, Is.Zero);
            Assert.That(GameObject.Find("Player1 Piece (2, 2)"), Is.Not.Null);
            yield return null;
        }

        [UnityTest]
        public IEnumerator EqualCombatPowerCollisionRemovesBothPieceViews()
        {
            Move(new GridPosition(0, 1), new GridPosition(0, 2));
            Move(new GridPosition(0, 8), new GridPosition(0, 7));
            Move(new GridPosition(0, 2), new GridPosition(0, 3));
            Move(new GridPosition(0, 7), new GridPosition(0, 6));
            Move(new GridPosition(0, 3), new GridPosition(0, 4));
            Move(new GridPosition(0, 6), new GridPosition(0, 5));
            Move(new GridPosition(0, 4), new GridPosition(0, 5));
            yield return null;

            Assert.That(bootstrap.Snapshot.TryGetPiece(new GridPosition(0, 4), out _), Is.False);
            Assert.That(bootstrap.Snapshot.TryGetPiece(new GridPosition(0, 5), out _), Is.False);
            Assert.That(GameObject.Find("Player1 Piece (0, 4)"), Is.Null);
            Assert.That(GameObject.Find("Player2 Piece (0, 5)"), Is.Null);
            Assert.That(bootstrap.PieceViewCount, Is.EqualTo(10));
            Assert.That(bootstrap.Snapshot.Winner, Is.Null);
        }

        [UnityTest]
        public IEnumerator ReachingOpponentTerritoryWinsAndLocksInput()
        {
            Move(new GridPosition(0, 1), new GridPosition(0, 2));
            Move(new GridPosition(0, 8), new GridPosition(1, 7));
            Move(new GridPosition(0, 2), new GridPosition(0, 3));
            Move(new GridPosition(5, 8), new GridPosition(5, 7));
            Move(new GridPosition(0, 3), new GridPosition(0, 4));
            Move(new GridPosition(5, 7), new GridPosition(5, 6));
            Move(new GridPosition(0, 4), new GridPosition(0, 5));
            Move(new GridPosition(5, 6), new GridPosition(5, 5));
            Move(new GridPosition(0, 5), new GridPosition(0, 6));
            Move(new GridPosition(5, 5), new GridPosition(5, 4));
            Move(new GridPosition(0, 6), new GridPosition(0, 7));
            Move(new GridPosition(5, 4), new GridPosition(5, 3));
            Move(new GridPosition(0, 7), new GridPosition(0, 8));
            Move(new GridPosition(5, 3), new GridPosition(5, 2));
            Move(new GridPosition(0, 8), new GridPosition(0, 9));

            Assert.That(bootstrap.Snapshot.Winner, Is.EqualTo(PlayerId.Player1));
            Assert.That(bootstrap.StatusText, Does.Contain("勝利"));
            bootstrap.HandleCellClick(new GridPosition(1, 7));
            Assert.That(bootstrap.SelectedCell, Is.Null);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ResetButtonRestoresInitialStateAndViews()
        {
            Move(new GridPosition(0, 1), new GridPosition(0, 2));
            Button resetButton = GameObject.Find("Reset Button").GetComponent<Button>();
            resetButton.onClick.Invoke();
            yield return null;

            Assert.That(bootstrap.Snapshot.Pieces.Count, Is.EqualTo(12));
            Assert.That(bootstrap.PieceViewCount, Is.EqualTo(12));
            Assert.That(bootstrap.Snapshot.CurrentPlayer, Is.EqualTo(PlayerId.Player1));
            Assert.That(bootstrap.SelectedCell, Is.Null);
            Assert.That(bootstrap.MoveIndicatorCount, Is.Zero);
            AssertPiece(new GridPosition(0, 1), PlayerId.Player1);
            AssertPiece(new GridPosition(0, 8), PlayerId.Player2);
        }

        [UnityTest]
        public IEnumerator SampleSceneLoadsWithBootstrapOnlyCompositionRoot()
        {
            SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
            yield return null;

            BoardGameBootstrap sceneBootstrap =
                Object.FindFirstObjectByType<BoardGameBootstrap>();
            Assert.That(sceneBootstrap, Is.Not.Null);
            Assert.That(sceneBootstrap.GeneratedCellCount, Is.EqualTo(60));
            Assert.That(sceneBootstrap.Snapshot.Pieces.Count, Is.EqualTo(12));
            Assert.That(GameObject.Find("Board View"), Is.Not.Null);
            Assert.That(GameObject.Find("Game HUD"), Is.Not.Null);

            bootstrapObject = null;
        }

        private void Move(GridPosition from, GridPosition to)
        {
            bootstrap.HandleCellClick(from);
            Assert.That(bootstrap.SelectedCell, Is.EqualTo(from));
            bootstrap.HandleCellClick(to);
        }

        private void AssertPiece(GridPosition position, PlayerId owner)
        {
            Assert.That(bootstrap.Snapshot.TryGetPiece(position, out PieceState piece), Is.True);
            Assert.That(piece.Owner, Is.EqualTo(owner));
        }
    }
}

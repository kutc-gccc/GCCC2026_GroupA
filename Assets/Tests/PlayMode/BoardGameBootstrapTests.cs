using System.Collections;
using System.Collections.Generic;
using GCCC.BoardGame.Core.Events;
using GCCC.BoardGame.Core.Model;
using GCCC.BoardGame.Presentation;
using GCCC.BoardGame.Presentation.Audio;
using GCCC.BoardGame.Presentation.Bootstrap;
using GCCC.BoardGame.Presentation.Views;
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
        private GameObject auxiliaryObject;
        private RuntimeSpriteFactory auxiliarySprites;

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

            if (auxiliaryObject != null)
            {
                Object.Destroy(auxiliaryObject);
            }

            auxiliarySprites?.Dispose();
            yield return null;
        }

        [UnityTest]
        public IEnumerator AwakeBuildsSeparatedBoardPiecesTerritoriesAndHud()
        {
            Assert.That(bootstrap.GeneratedCellCount, Is.EqualTo(60));
            Assert.That(bootstrap.PieceViewCount, Is.EqualTo(12));
            Assert.That(bootstrap.Snapshot.CurrentPlayer, Is.EqualTo(PlayerId.Player1));
            Assert.That(bootstrap.StatusText, Does.Contain("プレイヤー1"));
            Assert.That(bootstrap.IsResultVisible, Is.False);
            Assert.That(bootstrap.ResultText, Is.Empty);
            Assert.That(GameObject.Find("Board View"), Is.Not.Null);
            Assert.That(GameObject.Find("Piece Views"), Is.Not.Null);
            Assert.That(GameObject.Find("Game HUD"), Is.Not.Null);
            Assert.That(GameObject.Find("Board Input"), Is.Not.Null);
            Assert.That(GameObject.Find("Reset Button"), Is.Not.Null);
            Assert.That(GameObject.Find("Reserve Deploy Button"), Is.Not.Null);
            Assert.That(GameObject.Find("Reserve Deploy Button")
                .GetComponent<Button>().interactable, Is.False);
            Assert.That(GameObject.Find("Audio Volume Controls"), Is.Not.Null);
            Assert.That(GameObject.Find("BGM Slider"), Is.Not.Null);
            Assert.That(GameObject.Find("SFX Slider"), Is.Not.Null);
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
        public IEnumerator EffectCellsLegendAndReserveCountsRenderFromSnapshot()
        {
            const string effectId = "temporary-power";
            List<CellDefinition> cells = new List<CellDefinition>();
            for (int row = 0; row < 10; row++)
            {
                for (int column = 0; column < 6; column++)
                {
                    GridPosition position = new GridPosition(column, row);
                    cells.Add(new CellDefinition(
                        position,
                        row == 0
                            ? PlayerId.Player1
                            : row == 9 ? PlayerId.Player2 : (PlayerId?)null,
                        position == new GridPosition(2, 3)
                            ? new[] { effectId }
                            : null));
                }
            }

            GameSnapshot snapshot = new GameSnapshot(
                6,
                10,
                new PieceState[0],
                cells,
                PlayerId.Player1,
                null,
                false,
                effectDefinitions: new[]
                {
                    new CellEffectDefinition(
                        effectId, CellEffectLifetime.WhileOccupied)
                },
                players: new[]
                {
                    new PlayerState(
                        PlayerId.Player1,
                        new[]
                        {
                            new ReservePieceState(
                                new PieceId(100),
                                PlayerId.Player1,
                                2,
                                PowerMovementProfile.StandardId)
                        }),
                    new PlayerState(PlayerId.Player2)
                });

            auxiliaryObject = new GameObject("Cell Effect Presentation Test");
            auxiliarySprites = new RuntimeSpriteFactory();
            BoardView board = auxiliaryObject.AddComponent<BoardView>();
            board.Initialize(Camera.main, auxiliarySprites.SquareSprite, snapshot);
            GameHudView hud = auxiliaryObject.AddComponent<GameHudView>();
            hud.Initialize();
            hud.Render(snapshot);
            yield return null;

            Assert.That(board.EffectOverlayCount, Is.EqualTo(1));
            Assert.That(hud.IsEffectLegendVisible, Is.True);
            Assert.That(hud.ReserveText, Does.Contain("青: 1"));
            Assert.That(hud.ReserveText, Does.Contain("赤: 0"));
        }

        [UnityTest]
        public IEnumerator ReserveDeploymentCandidatesAndPieceViewAreRendered()
        {
            GameSnapshot standard = bootstrap.Snapshot;
            ReservePieceState reserve = new ReservePieceState(
                new PieceId(100),
                PlayerId.Player1,
                2,
                PowerMovementProfile.StandardId);
            GameSnapshot before = new GameSnapshot(
                standard.Columns,
                standard.Rows,
                new PieceState[0],
                standard.Cells,
                PlayerId.Player1,
                null,
                false,
                players: new[]
                {
                    new PlayerState(PlayerId.Player1, new[] { reserve }),
                    new PlayerState(PlayerId.Player2)
                });

            auxiliaryObject = new GameObject("Reserve Deployment Presentation Test");
            auxiliarySprites = new RuntimeSpriteFactory();
            BoardView board = auxiliaryObject.AddComponent<BoardView>();
            board.Initialize(Camera.main, auxiliarySprites.SquareSprite, before);
            PieceViewManager pieces = auxiliaryObject.AddComponent<PieceViewManager>();
            pieces.Initialize(auxiliarySprites.CircleSprite, before);
            GameHudView hud = auxiliaryObject.AddComponent<GameHudView>();
            hud.Initialize();
            hud.Render(before);
            hud.SetReserveDeployButtonInteractable(true);

            GridPosition destination = new GridPosition(0, 1);
            board.ShowSelection(
                null,
                new[] { destination },
                new GridPosition[0],
                before);
            PieceState deployed = new PieceState(
                reserve.Id,
                reserve.Owner,
                destination,
                reserve.CombatPower,
                reserve.MovementProfileId);
            GameSnapshot after = new GameSnapshot(
                standard.Columns,
                standard.Rows,
                new[] { deployed },
                standard.Cells,
                PlayerId.Player2,
                null,
                false);
            pieces.ApplyEvents(
                new GameEvent[]
                {
                    new ReservePieceDeployed(
                        deployed.Id, deployed.Owner, deployed.Position)
                },
                after);
            yield return null;

            Assert.That(board.MoveIndicatorCount, Is.EqualTo(1));
            Assert.That(pieces.PieceViewCount, Is.EqualTo(1));
            Assert.That(hud.ReserveDeployButton.interactable, Is.True);
        }

        [UnityTest]
        public IEnumerator OnlyCurrentPlayerCanSelectAndLegalMovesAreHighlighted()
        {
            bootstrap.HandleCellClick(new GridPosition(2, 8));
            Assert.That(bootstrap.SelectedCell, Is.Null);
            Assert.That(bootstrap.MoveIndicatorCount, Is.Zero);
            Assert.That(GameObject.Find("Randomize Power Button")
                .GetComponent<Button>().interactable, Is.False);

            bootstrap.HandleCellClick(new GridPosition(2, 1));
            Assert.That(bootstrap.SelectedCell, Is.EqualTo(new GridPosition(2, 1)));
            Assert.That(bootstrap.MoveIndicatorCount, Is.EqualTo(3));
            Assert.That(GameObject.Find("Randomize Power Button")
                .GetComponent<Button>().interactable, Is.True);

            bootstrap.HandleCellClick(new GridPosition(2, 1));
            Assert.That(bootstrap.SelectedCell, Is.Null);
            Assert.That(bootstrap.MoveIndicatorCount, Is.Zero);
            Assert.That(GameObject.Find("Randomize Power Button")
                .GetComponent<Button>().interactable, Is.False);
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
        public IEnumerator ReachingOpponentTerritoryShowsResultLocksInputAndReturnsToTitle()
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
            Assert.That(bootstrap.IsResultVisible, Is.True);
            Assert.That(bootstrap.ResultText, Is.EqualTo("プレイヤー1（青）の勝利"));
            bootstrap.HandleCellClick(new GridPosition(1, 7));
            Assert.That(bootstrap.SelectedCell, Is.Null);

            Button resetButton = GameObject.Find("Reset Button").GetComponent<Button>();
            Button randomizeButton = GameObject.Find("Randomize Power Button")
                .GetComponent<Button>();
            Button fuseButton = GameObject.Find("Fuse Button").GetComponent<Button>();
            Slider bgmSlider = GameObject.Find("BGM Slider").GetComponent<Slider>();
            Slider sfxSlider = GameObject.Find("SFX Slider").GetComponent<Slider>();
            Assert.That(resetButton.interactable, Is.False);
            Assert.That(randomizeButton.interactable, Is.False);
            Assert.That(fuseButton.interactable, Is.False);
            Assert.That(bgmSlider.interactable, Is.False);
            Assert.That(sfxSlider.interactable, Is.False);

            resetButton.onClick.Invoke();
            randomizeButton.onClick.Invoke();
            fuseButton.onClick.Invoke();
            Assert.That(bootstrap.Snapshot.Winner, Is.EqualTo(PlayerId.Player1));
            Assert.That(bootstrap.IsResultVisible, Is.True);

            Button returnButton = GameObject.Find("Return To Title Button")
                .GetComponent<Button>();
            Assert.That(returnButton.interactable, Is.True);
            Assert.That(
                returnButton.transform.Find("Label").GetComponent<Text>().text,
                Is.EqualTo("スタート画面に戻る"));
            returnButton.onClick.Invoke();
            yield return null;

            Assert.That(SceneManager.GetActiveScene().name,
                Is.EqualTo(BoardGameSceneNames.Title));
            Assert.That(GameObject.Find("Title Text"), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<BoardGameAudioManager>(), Is.Null);
            Assert.That(GameObject.Find("BGM Source"), Is.Null);
            bootstrapObject = null;
            bootstrap = null;
        }

        [UnityTest]
        public IEnumerator ResultOverlayRendersSecondPlayerAndDraw()
        {
            GameObject hudObject = new GameObject("Result HUD Test");
            GameHudView hud = hudObject.AddComponent<GameHudView>();
            hud.Initialize();

            GameSnapshot initial = bootstrap.Snapshot;
            GameSnapshot player2Win = new GameSnapshot(
                initial.Columns,
                initial.Rows,
                initial.Pieces,
                initial.Cells,
                initial.CurrentPlayer,
                PlayerId.Player2,
                false);
            hud.Render(player2Win);

            Assert.That(hud.IsResultVisible, Is.True);
            Assert.That(hud.ResultText, Is.EqualTo("プレイヤー2（赤）の勝利"));
            Assert.That(hud.IsPointerOverControl(new Vector2(100f, 100f)), Is.True);

            GameSnapshot draw = new GameSnapshot(
                initial.Columns,
                initial.Rows,
                initial.Pieces,
                initial.Cells,
                initial.CurrentPlayer,
                null,
                true);
            hud.Render(draw);

            Assert.That(hud.IsResultVisible, Is.True);
            Assert.That(hud.ResultText, Is.EqualTo("引き分け"));

            hud.Render(initial);
            Assert.That(hud.IsResultVisible, Is.False);
            Assert.That(hud.ResultText, Is.Empty);

            Object.Destroy(hudObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TitleSceneStartsFreshGame()
        {
            Assert.That(SceneUtility.GetScenePathByBuildIndex(0),
                Is.EqualTo("Assets/Scenes/TitleScene.unity"));

            SceneManager.LoadScene(BoardGameSceneNames.Title, LoadSceneMode.Single);
            yield return null;

            Assert.That(SceneManager.GetActiveScene().name,
                Is.EqualTo(BoardGameSceneNames.Title));
            Assert.That(
                GameObject.Find("Title Text").GetComponent<Text>().text,
                Is.EqualTo("Number War"));
            Assert.That(
                GameObject.Find("Background").GetComponent<Image>().sprite,
                Is.Not.Null);

            Button startButton = GameObject.Find("Game Start Button").GetComponent<Button>();
            Assert.That(startButton.interactable, Is.True);
            Assert.That(
                startButton.transform.Find("Label").GetComponent<Text>().text,
                Is.EqualTo("ゲーム開始"));

            startButton.onClick.Invoke();
            yield return null;

            Assert.That(SceneManager.GetActiveScene().name,
                Is.EqualTo(BoardGameSceneNames.Game));
            BoardGameBootstrap sceneBootstrap =
                Object.FindFirstObjectByType<BoardGameBootstrap>();
            Assert.That(sceneBootstrap, Is.Not.Null);
            Assert.That(sceneBootstrap.Snapshot.Pieces.Count, Is.EqualTo(12));
            Assert.That(sceneBootstrap.Snapshot.CurrentPlayer, Is.EqualTo(PlayerId.Player1));
            Assert.That(sceneBootstrap.Snapshot.IsGameOver, Is.False);
            Assert.That(Object.FindFirstObjectByType<BoardGameAudioManager>(), Is.Not.Null);
            Assert.That(GameObject.Find("BGM Source"), Is.Not.Null);

            bootstrapObject = sceneBootstrap.gameObject;
            bootstrap = sceneBootstrap;
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
        public IEnumerator AudioControlsUpdateSingleManagerAndSources()
        {
            SceneManager.LoadScene(BoardGameSceneNames.Game, LoadSceneMode.Single);
            yield return null;

            BoardGameAudioManager[] managers =
                Object.FindObjectsByType<BoardGameAudioManager>(FindObjectsSortMode.None);
            Assert.That(managers, Has.Length.EqualTo(1));

            Slider bgmSlider = GameObject.Find("BGM Slider").GetComponent<Slider>();
            Slider sfxSlider = GameObject.Find("SFX Slider").GetComponent<Slider>();
            bgmSlider.value = 0.4f;
            sfxSlider.value = 0.35f;
            yield return null;

            Assert.That(managers[0].BgmVolume, Is.EqualTo(0.4f).Within(0.001f));
            Assert.That(managers[0].SfxVolume, Is.EqualTo(0.35f).Within(0.001f));

            AudioSource bgmSource = GameObject.Find("BGM Source").GetComponent<AudioSource>();
            AudioSource sfxSource = GameObject.Find("SFX Source").GetComponent<AudioSource>();
            Assert.That(bgmSource.loop, Is.True);
            Assert.That(bgmSource.volume, Is.EqualTo(0.04f).Within(0.001f));
            Assert.That(sfxSource.loop, Is.False);
            Assert.That(sfxSource.volume, Is.EqualTo(0.35f).Within(0.001f));

            bootstrapObject = Object.FindFirstObjectByType<BoardGameBootstrap>().gameObject;
            bootstrap = bootstrapObject.GetComponent<BoardGameBootstrap>();
        }

        [UnityTest]
        public IEnumerator SampleSceneLoadsCompositionRootEventSystemAndBgm()
        {
            SceneManager.LoadScene(BoardGameSceneNames.Game, LoadSceneMode.Single);
            yield return null;

            BoardGameBootstrap sceneBootstrap =
                Object.FindFirstObjectByType<BoardGameBootstrap>();
            Assert.That(sceneBootstrap, Is.Not.Null);
            Assert.That(sceneBootstrap.GeneratedCellCount, Is.EqualTo(60));
            Assert.That(sceneBootstrap.Snapshot.Pieces.Count, Is.EqualTo(12));
            Assert.That(GameObject.Find("Board View"), Is.Not.Null);
            Assert.That(GameObject.Find("Game HUD"), Is.Not.Null);
            Assert.That(GameObject.Find("EventSystem"), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<BoardGameAudioManager>(), Is.Not.Null);
            Assert.That(GameObject.Find("BGM Source"), Is.Not.Null);

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

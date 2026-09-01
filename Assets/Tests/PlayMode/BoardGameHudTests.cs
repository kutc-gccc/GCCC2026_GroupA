using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GCCC.BoardGame.Core;
using GCCC.BoardGame.Core.Commands;
using GCCC.BoardGame.Core.Events;
using GCCC.BoardGame.Core.Model;
using GCCC.BoardGame.Core.Rules.CellEffects;
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
    public sealed partial class BoardGameBootstrapTests
    {
        [UnityTest]
        public IEnumerator HudInitializeTwiceDoesNotDuplicateHierarchyOrListeners()
        {
            GameHudView hud = CreateHudView(null);
            int childCount = hud.transform.childCount;
            hud.Initialize();
            hud.Initialize();

            int clickCount = 0;
            hud.OnRandomizePowerButtonClicked += () => clickCount++;
            hud.RandomizePowerButton.onClick.Invoke();
            yield return null;

            Assert.That(hud.transform.childCount, Is.EqualTo(childCount));
            Assert.That(clickCount, Is.EqualTo(1));
            Object.Destroy(hud.gameObject);
        }


        [UnityTest]
        public IEnumerator ReserveCardsRenderDetailsSelectionAndPointerBlocking()
        {
            GameSnapshot standard = bootstrap.Snapshot;
            ReservePieceState firstPlayer1Reserve = new ReservePieceState(
                new PieceId(100),
                PlayerId.Player1,
                2,
                PowerMovementProfile.StandardId);
            ReservePieceState secondPlayer1Reserve = new ReservePieceState(
                new PieceId(101),
                PlayerId.Player1,
                4,
                new MovementProfileId("scout"));
            ReservePieceState player2Reserve = new ReservePieceState(
                new PieceId(200),
                PlayerId.Player2,
                3,
                PowerMovementProfile.StandardId);
            GameSnapshot snapshot = new GameSnapshot(
                standard.Columns,
                standard.Rows,
                new PieceState[0],
                standard.Cells,
                PlayerId.Player1,
                null,
                false,
                players: new[]
                {
                    new PlayerState(
                        PlayerId.Player1,
                        new[] { firstPlayer1Reserve, secondPlayer1Reserve }),
                    new PlayerState(PlayerId.Player2, new[] { player2Reserve })
                });

            auxiliaryObject = new GameObject("Reserve Card Presentation Test");
            auxiliarySprites = new RuntimeSpriteFactory();
            GameHudView hud = CreateHudView(auxiliaryObject.transform);
            hud.Initialize(
                null,
                auxiliarySprites.CircleSprite,
                auxiliarySprites.SquareSprite);
            hud.Render(snapshot);
            hud.SetDeployableReservePieces(
                new[]
                {
                    firstPlayer1Reserve.Id,
                    secondPlayer1Reserve.Id,
                    player2Reserve.Id
                });
            yield return null;

            ReservePieceCardView firstCard = hud.GetReserveCard(firstPlayer1Reserve.Id);
            ReservePieceCardView secondCard = hud.GetReserveCard(secondPlayer1Reserve.Id);
            ReservePieceCardView opponentCard = hud.GetReserveCard(player2Reserve.Id);
            Assert.That(hud.ReserveCardCount, Is.EqualTo(3));
            Assert.That(firstCard.CombatPowerText, Is.EqualTo("2"));
            Assert.That(firstCard.MovementProfileText, Is.EqualTo("standard"));
            Assert.That(firstCard.PieceSprite, Is.SameAs(auxiliarySprites.CircleSprite));
            Assert.That(secondCard.CombatPowerText, Is.EqualTo("4"));
            Assert.That(secondCard.MovementProfileText, Is.EqualTo("scout"));
            Assert.That(firstCard.IsInteractable, Is.True);
            Assert.That(secondCard.IsInteractable, Is.True);
            Assert.That(opponentCard.IsInteractable, Is.False);
            Assert.That(opponentCard.PieceSprite, Is.SameAs(auxiliarySprites.SquareSprite));

            PieceId? clickedId = null;
            hud.ReservePieceSelected += id => clickedId = id;
            secondCard.GetComponent<Button>().onClick.Invoke();
            Assert.That(clickedId, Is.EqualTo(secondPlayer1Reserve.Id));

            hud.SetSelectedReservePiece(secondPlayer1Reserve.Id);
            Assert.That(secondCard.GetComponent<Outline>().enabled, Is.True);
            Assert.That(firstCard.GetComponent<Outline>().enabled, Is.False);

            Canvas.ForceUpdateCanvases();
            RectTransform panelRect = GameObject.Find("Player 2 Reserve Panel")
                .GetComponent<RectTransform>();
            Vector2 panelCenter = RectTransformUtility.WorldToScreenPoint(
                null, panelRect.TransformPoint(panelRect.rect.center));
            Assert.That(hud.IsPointerOverControl(panelCenter), Is.True);

            GameSnapshot gameOver = new GameSnapshot(
                snapshot.Columns,
                snapshot.Rows,
                snapshot.Pieces,
                snapshot.Cells,
                snapshot.CurrentPlayer,
                PlayerId.Player1,
                false,
                players: snapshot.Players);
            hud.Render(gameOver);
            Assert.That(firstCard.IsInteractable, Is.False);
            Assert.That(secondCard.IsInteractable, Is.False);
            Assert.That(opponentCard.IsInteractable, Is.False);
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
            Assert.That(bootstrap.ResultText, Is.EqualTo("プレイヤー1の勝利"));
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
            GameHudView hud = CreateHudView(null);
            GameObject hudObject = hud.gameObject;
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
            Assert.That(hud.ResultText, Is.EqualTo("プレイヤー2の勝利"));
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

    }
}

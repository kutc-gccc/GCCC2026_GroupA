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
            GameObject boardUi = GameObject.Find("Board UI");
            GameObject resetButton = GameObject.Find("Reset Button");
            Assert.That(resetButton, Is.Not.Null);
            Assert.That(resetButton.transform.parent, Is.EqualTo(boardUi.transform));
            Assert.That(boardUi.transform.Find("Operation Bar/Reset Button"), Is.Null);
            RectTransform resetRect = resetButton.GetComponent<RectTransform>();
            Assert.That(resetRect.anchorMin, Is.EqualTo(new Vector2(1f, 0f)));
            Assert.That(resetRect.anchorMax, Is.EqualTo(new Vector2(1f, 0f)));
            Assert.That(resetButton.GetComponent<Image>().color.a, Is.LessThan(0.1f));
            Assert.That(resetButton.transform.Find("Top Border"), Is.Not.Null);
            Assert.That(
                (Color32)resetButton.transform.Find("Label").GetComponent<Text>().color,
                Is.EqualTo(new Color32(198, 40, 40, 255)));
            Assert.That(GameObject.Find("Reserve Deploy Button"), Is.Not.Null);
            Assert.That(GameObject.Find("Reserve Deploy Button")
                .GetComponent<Button>().interactable, Is.False);
            Assert.That(GameObject.Find("Player 1 Reserve Panel"), Is.Not.Null);
            Assert.That(GameObject.Find("Player 2 Reserve Panel"), Is.Not.Null);
            Assert.That(
                GameObject.Find("Player 1 Reserve Panel")
                    .transform.Find("Header").GetComponent<Text>().text,
                Does.Contain("駒 6 / 6"));
            Assert.That(
                GameObject.Find("Player 2 Reserve Panel")
                    .transform.Find("Header").GetComponent<Text>().text,
                Does.Contain("駒 6 / 6"));
            Assert.That(bootstrapObject.GetComponentInChildren<GameHudView>()
                .ReserveCardCount, Is.Zero);
            Assert.That(GameObject.Find("Audio Volume Controls"), Is.Not.Null);
            Assert.That(GameObject.Find("BGM Slider"), Is.Not.Null);
            Assert.That(GameObject.Find("SFX Slider"), Is.Not.Null);
            RectTransform legendRect = boardUi.transform.Find("Cell Effect Legend")
                .GetComponent<RectTransform>();
            Assert.That(legendRect.anchorMin, Is.EqualTo(new Vector2(0f, 0.5f)));
            Assert.That(legendRect.childCount, Is.EqualTo(6));
            Assert.That(legendRect.Find("Selected Legend Row"), Is.Not.Null);
            Assert.That(legendRect.Find("Movable Legend Row"), Is.Not.Null);
            Assert.That(legendRect.Find("Combat Legend Row"), Is.Not.Null);
            Assert.That(legendRect.Find("Fusion Legend Row"), Is.Not.Null);
            Assert.That(legendRect.Find("Permanent Legend Row"), Is.Not.Null);
            Assert.That(legendRect.Find("While Occupied Legend Row"), Is.Not.Null);
            RectTransform audioRect = GameObject.Find("Audio Volume Controls")
                .GetComponent<RectTransform>();
            Assert.That(audioRect.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(audioRect.GetComponent<Image>(), Is.Null);
            Assert.That(
                GameObject.Find("BGM Slider").transform.Find("Background")
                    .GetComponent<RectTransform>().sizeDelta.y,
                Is.EqualTo(8f));

            GameHudView hud = bootstrapObject.GetComponentInChildren<GameHudView>();
            Button reserveDeployButton = GameObject.Find("Reserve Deploy Button")
                .GetComponent<Button>();
            hud.SetReserveDeployModeActive(true);
            Assert.That(
                (Color32)reserveDeployButton.GetComponent<Image>().color,
                Is.EqualTo(new Color32(38, 54, 77, 255)));
            Assert.That(
                reserveDeployButton.transform.Find("Label").GetComponent<Text>().color,
                Is.EqualTo(Color.white));
            hud.SetReserveDeployModeActive(false);
            Assert.That(
                (Color32)reserveDeployButton.transform.Find("Label").GetComponent<Text>().color,
                Is.EqualTo(new Color32(35, 41, 52, 255)));
            Assert.That(GameObject.Find("Player 1 Territory Border"), Is.Not.Null);
            Assert.That(GameObject.Find("Player 2 Territory Border"), Is.Not.Null);

            // 陣地は通常マスと別の地形として描き、所有者を ▲▼ で示す。
            SpriteRenderer territoryCell = GameObject.Find("Cell (0, 0)")
                .GetComponent<SpriteRenderer>();
            SpriteRenderer normalCell = GameObject.Find("Cell (0, 2)")
                .GetComponent<SpriteRenderer>();
            Assert.That(territoryCell.color, Is.Not.EqualTo(normalCell.color));
            Assert.That(GameObject.Find("Territory Marker (0, 0)"), Is.Not.Null);
            Assert.That(GameObject.Find("Territory Marker (0, 9)"), Is.Not.Null);
            Assert.That(
                GameObject.Find("Player 1 Territory Label Text")
                    .GetComponent<Text>().text,
                Is.EqualTo("▲ プレイヤー1の陣地"));
            Assert.That(
                GameObject.Find("Player 2 Territory Label Text")
                    .GetComponent<Text>().text,
                Is.EqualTo("▼ プレイヤー2の陣地"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator PieceViewsRenderOwnersAndCombatPower()
        {
            PieceView player1 = FindPieceView(
                bootstrap,
                new GridPosition(0, 1),
                PlayerId.Player1);
            PieceView player2 = FindPieceView(
                bootstrap,
                new GridPosition(0, 8),
                PlayerId.Player2);

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
            board.Initialize(Camera.main, auxiliarySprites, snapshot);
            GameHudView hud = CreateHudView(auxiliaryObject.transform);
            hud.Initialize();
            hud.Render(snapshot);
            yield return null;

            Assert.That(board.EffectOverlayCount, Is.EqualTo(1));
            Assert.That(hud.IsEffectLegendVisible, Is.True);
            Assert.That(hud.ReserveText, Does.Contain("プレイヤー1: 1"));
            Assert.That(hud.ReserveText, Does.Contain("プレイヤー2: 0"));
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
            board.Initialize(Camera.main, auxiliarySprites, before);
            PieceViewManager pieces = auxiliaryObject.AddComponent<PieceViewManager>();
            pieces.Initialize(
                auxiliarySprites.CircleSprite,
                auxiliarySprites.CircleSprite,
                before);
            GameHudView hud = CreateHudView(auxiliaryObject.transform);
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

    }
}

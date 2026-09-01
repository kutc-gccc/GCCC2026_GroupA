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
        public IEnumerator SampleSceneLoadsCompositionRootEventSystemAndBgm()
        {
            SceneManager.LoadScene(BoardGameSceneNames.Game, LoadSceneMode.Single);
            yield return null;

            BoardGameBootstrap sceneBootstrap =
                Object.FindFirstObjectByType<BoardGameBootstrap>();
            Assert.That(sceneBootstrap, Is.Not.Null);
            Assert.That(sceneBootstrap.GeneratedCellCount, Is.EqualTo(60));
            Assert.That(sceneBootstrap.Snapshot.Pieces.Count, Is.EqualTo(12));
            PieceView player1 = FindPieceView(
                sceneBootstrap,
                new GridPosition(0, 1),
                PlayerId.Player1);
            PieceView player2 = FindPieceView(
                sceneBootstrap,
                new GridPosition(0, 8),
                PlayerId.Player2);
            SpriteRenderer player1Renderer =
                player1.GetComponent<SpriteRenderer>();
            SpriteRenderer player2Renderer =
                player2.GetComponent<SpriteRenderer>();
            Assert.That(player1Renderer.sprite, Is.Not.Null);
            Assert.That(player2Renderer.sprite, Is.Not.Null);
            Assert.That(
                player1Renderer.sprite,
                Is.Not.SameAs(player2Renderer.sprite));

            Assert.That(sceneBootstrap.EffectOverlayCount, Is.EqualTo(2));
            Assert.That(sceneBootstrap.IsEffectLegendVisible, Is.True);
            Assert.That(
                sceneBootstrap.Snapshot.TryGetCell(
                    new GridPosition(1, 4), out CellDefinition player1EffectCell),
                Is.True);
            Assert.That(player1EffectCell.EffectIds,
                Is.EquivalentTo(new[] { "reserve-piece-grant" }));
            Assert.That(
                sceneBootstrap.Snapshot.TryGetCell(
                    new GridPosition(4, 5), out CellDefinition player2EffectCell),
                Is.True);
            Assert.That(player2EffectCell.EffectIds,
                Is.EquivalentTo(new[] { "reserve-piece-grant" }));
            Assert.That(
                sceneBootstrap.Snapshot.TryGetCellEffectDefinition(
                    "reserve-piece-grant", out CellEffectDefinition effectDefinition),
                Is.True);
            Assert.That(effectDefinition.Lifetime,
                Is.EqualTo(CellEffectLifetime.PermanentOncePerPiece));
            int configuredEffectCellCount = 0;
            foreach (CellDefinition cell in sceneBootstrap.Snapshot.Cells)
            {
                if (cell.EffectIds.Count > 0)
                {
                    configuredEffectCellCount++;
                }
            }
            Assert.That(configuredEffectCellCount, Is.EqualTo(2));
            Assert.That(GameObject.Find("Board View"), Is.Not.Null);
            Assert.That(GameObject.Find("Game HUD"), Is.Not.Null);
            Assert.That(GameObject.Find("EventSystem"), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<BoardGameAudioManager>(), Is.Not.Null);
            Assert.That(GameObject.Find("BGM Source"), Is.Not.Null);

            bootstrapObject = null;
        }

    }
}

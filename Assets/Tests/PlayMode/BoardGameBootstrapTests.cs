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
        private GameObject bootstrapObject;
        private BoardGameBootstrap bootstrap;
        private GameObject auxiliaryObject;
        private RuntimeSpriteFactory auxiliarySprites;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return SceneManager.LoadSceneAsync(
                BoardGameSceneNames.Game,
                LoadSceneMode.Single);
            yield return null;
            bootstrap = Object.FindFirstObjectByType<BoardGameBootstrap>();
            Assert.That(bootstrap, Is.Not.Null);
            bootstrapObject = bootstrap.gameObject;
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

        private void Move(GridPosition from, GridPosition to)
        {
            bootstrap.HandleCellClick(from);
            Assert.That(bootstrap.SelectedCell, Is.EqualTo(from));
            bootstrap.HandleCellClick(to);
        }

        private GameHudView CreateHudView(Transform parent)
        {
            GameHudView hud = Object.Instantiate(bootstrap.HudView, parent);
            hud.gameObject.name = "Game HUD Test Instance";
            return hud;
        }

        private static GameDefinition CreateReserveSelectionDefinition(string effectId)
        {
            const int columns = 4;
            const int rows = 6;
            List<CellDefinition> cells = new List<CellDefinition>();
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    GridPosition position = new GridPosition(column, row);
                    PlayerId? territoryOwner = row == 0
                        ? PlayerId.Player1
                        : row == rows - 1 ? PlayerId.Player2 : (PlayerId?)null;
                    bool grantsReserve =
                        position == new GridPosition(1, 1) ||
                        position == new GridPosition(1, 2);
                    cells.Add(new CellDefinition(
                        position,
                        territoryOwner,
                        grantsReserve ? new[] { effectId } : null));
                }
            }

            return new GameDefinition(
                columns,
                rows,
                cells,
                new[]
                {
                    new InitialPieceDefinition(
                        new PieceId(1), PlayerId.Player1,
                        new GridPosition(0, 1), 1,
                        PowerMovementProfile.StandardId),
                    new InitialPieceDefinition(
                        new PieceId(2), PlayerId.Player1,
                        new GridPosition(2, 1), 1,
                        PowerMovementProfile.StandardId),
                    new InitialPieceDefinition(
                        new PieceId(3), PlayerId.Player2,
                        new GridPosition(3, 4), 1,
                        PowerMovementProfile.StandardId)
                },
                movementProfiles: new[] { PowerMovementProfile.CreateStandard() },
                cellEffectDefinitions: new[]
                {
                    new CellEffectDefinition(
                        effectId, CellEffectLifetime.PermanentOncePerPiece)
                });
        }

        private static void ExecuteMove(
            GameSession session,
            PieceId pieceId,
            GridPosition destination)
        {
            MovePieceCommand command = session
                .GetLegalCommands(session.Snapshot.CurrentPlayer)
                .OfType<MovePieceCommand>()
                .First(candidate =>
                    candidate.PieceId == pieceId &&
                    candidate.Destination == destination);
            Assert.That(session.Execute(command).Success, Is.True);
        }

        private static PieceView FindPieceView(
            BoardGameBootstrap targetBootstrap,
            GridPosition position,
            PlayerId owner)
        {
            Assert.That(
                targetBootstrap.Snapshot.TryGetPiece(
                    position,
                    out PieceState piece),
                Is.True);
            Assert.That(piece.Owner, Is.EqualTo(owner));

            PieceView[] views =
                targetBootstrap.PieceViews.GetComponentsInChildren<PieceView>(
                    true);
            foreach (PieceView view in views)
            {
                if (view.PieceId.Equals(piece.Id))
                {
                    return view;
                }
            }

            Assert.Fail(
                $"PieceView for {piece.Id} at {position} was not found.");
            return null;
        }

        private void AssertPiece(GridPosition position, PlayerId owner)
        {
            Assert.That(bootstrap.Snapshot.TryGetPiece(position, out PieceState piece), Is.True);
            Assert.That(piece.Owner, Is.EqualTo(owner));
        }
    }
}

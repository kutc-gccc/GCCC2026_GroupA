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
        public IEnumerator SelectingNonFirstReserveDeploysTheChosenPiece()
        {
            const string effectId = "reserve-test";
            GameDefinition definition = CreateReserveSelectionDefinition(effectId);
            GameSession session = new GameSession(
                definition,
                cellEffectHandlers: new ICellEffectHandler[]
                {
                    new ReservePieceGrantCellEffectHandler(
                        effectId, 1, PowerMovementProfile.StandardId)
                });
            ExecuteMove(session, new PieceId(1), new GridPosition(1, 2));
            ExecuteMove(session, new PieceId(3), new GridPosition(3, 3));
            ExecuteMove(session, new PieceId(2), new GridPosition(1, 1));
            ExecuteMove(session, new PieceId(3), new GridPosition(3, 2));

            IReadOnlyList<ReservePieceState> reserves =
                session.Snapshot.GetPlayer(PlayerId.Player1).ReservePieces;
            Assert.That(reserves, Has.Count.EqualTo(2));
            PieceId firstReserveId = reserves[0].Id;
            PieceId secondReserveId = reserves[1].Id;

            auxiliaryObject = new GameObject("Reserve Selection Coordinator Test");
            auxiliarySprites = new RuntimeSpriteFactory();
            BoardView board = auxiliaryObject.AddComponent<BoardView>();
            board.Initialize(Camera.main, auxiliarySprites.SquareSprite, session.Snapshot);
            PieceViewManager pieces = auxiliaryObject.AddComponent<PieceViewManager>();
            pieces.Initialize(
                auxiliarySprites.CircleSprite,
                auxiliarySprites.SquareSprite,
                session.Snapshot);
            GameHudView hud = CreateHudView(auxiliaryObject.transform);
            hud.Initialize(
                null,
                auxiliarySprites.CircleSprite,
                auxiliarySprites.SquareSprite);
            GameCoordinator coordinator = new GameCoordinator(
                session, board, pieces, hud);
            hud.ReservePieceSelected += coordinator.ToggleReservePieceSelection;
            yield return null;

            ReservePieceCardView firstCard = hud.GetReserveCard(firstReserveId);
            ReservePieceCardView secondCard = hud.GetReserveCard(secondReserveId);
            Assert.That(firstCard.IsInteractable, Is.True);
            Assert.That(secondCard.IsInteractable, Is.True);

            secondCard.GetComponent<Button>().onClick.Invoke();
            Assert.That(coordinator.SelectedReservePieceId, Is.EqualTo(secondReserveId));
            Assert.That(board.MoveIndicatorCount, Is.GreaterThan(0));

            secondCard.GetComponent<Button>().onClick.Invoke();
            Assert.That(coordinator.SelectedReservePieceId, Is.Null);
            Assert.That(board.MoveIndicatorCount, Is.Zero);

            firstCard.GetComponent<Button>().onClick.Invoke();
            Assert.That(coordinator.SelectedReservePieceId, Is.EqualTo(firstReserveId));
            secondCard.GetComponent<Button>().onClick.Invoke();
            Assert.That(coordinator.SelectedReservePieceId, Is.EqualTo(secondReserveId));

            DeployReservePieceCommand deployment =
                session.GetLegalCommands(PlayerId.Player1)
                    .OfType<DeployReservePieceCommand>()
                    .First(command => command.ReservePieceId == secondReserveId);
            coordinator.HandleCellClick(deployment.Destination);
            yield return null;

            Assert.That(
                session.Snapshot.TryGetPiece(deployment.Destination, out PieceState deployed),
                Is.True);
            Assert.That(deployed.Id, Is.EqualTo(secondReserveId));
            Assert.That(
                session.Snapshot.GetPlayer(PlayerId.Player1).ReservePieces
                    .Select(piece => piece.Id),
                Is.EquivalentTo(new[] { firstReserveId }));
            Assert.That(hud.GetReserveCard(secondReserveId), Is.Null);
            Assert.That(hud.GetReserveCard(firstReserveId), Is.Not.Null);

            hud.ReservePieceSelected -= coordinator.ToggleReservePieceSelection;
            coordinator.Dispose();
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
            yield return null;

            Assert.That(
                FindPieceView(
                    bootstrap,
                    new GridPosition(2, 2),
                    PlayerId.Player1),
                Is.Not.Null);
        }

    }
}

using System.Collections.Generic;
using System.Linq;
using GCCC.BoardGame.Core;
using GCCC.BoardGame.Core.Commands;
using GCCC.BoardGame.Core.Events;
using GCCC.BoardGame.Core.Model;
using GCCC.BoardGame.Core.Players;
using GCCC.BoardGame.Presentation.Views;
using UnityEngine; // Debug.Log g—p‚Ì‚½‚ß’Ç‰Á

namespace GCCC.BoardGame.Presentation
{
    public sealed class GameCoordinator
    {
        private readonly GameSession session;
        private readonly BoardView boardView;
        private readonly PieceViewManager pieceViews;
        private readonly GameHudView hudView;
        private readonly Dictionary<PlayerId, IPlayerAgent> agents;
        private PieceId? selectedPieceId;
        private bool isFusionModeActive;

        public GameCoordinator(
            GameSession session,
            BoardView boardView,
            PieceViewManager pieceViews,
            GameHudView hudView,
            IPlayerAgent player1Agent = null,
            IPlayerAgent player2Agent = null)
        {
            this.session = session;
            this.boardView = boardView;
            this.pieceViews = pieceViews;
            this.hudView = hudView;
            agents = new Dictionary<PlayerId, IPlayerAgent>
            {
                [PlayerId.Player1] = player1Agent ?? new HumanPlayerAgent(PlayerId.Player1),
                [PlayerId.Player2] = player2Agent ?? new HumanPlayerAgent(PlayerId.Player2)
            };

            if (this.hudView != null)
            {
                this.hudView.OnRandomizePowerButtonClicked += HandleRandomizePowerButtonClicked;
            }

            hudView.Render(session.Snapshot);
            BeginCurrentTurn();
        }

        private void HandleRandomizePowerButtonClicked()
        {
            Debug.Log("[Coordinator] ƒ{ƒ^ƒ“‰Ÿ‰ºƒCƒxƒ“ƒg‚ğóM‚µ‚Ü‚µ‚½B");

            GameSnapshot snapshot = session.Snapshot;
            if (snapshot.IsGameOver)
            {
                Debug.LogWarning("[Coordinator] ƒQ[ƒ€I—¹Ï‚İ‚Ì‚½‚ßˆ—‚ğƒXƒLƒbƒv‚µ‚Ü‚µ‚½B");
                return;
            }

            if (!selectedPieceId.HasValue)
            {
                Debug.LogWarning("[Coordinator] ‹î‚ª‘I‘ğ‚³‚ê‚Ä‚¢‚Ü‚¹‚ñI ‹î‚ğƒNƒŠƒbƒN‚µ‚Ä‘I‘ğ‚µ‚Ä‚©‚çƒ{ƒ^ƒ“‚ğ‰Ÿ‚µ‚Ä‚­‚¾‚³‚¢B");
                return;
            }

            Debug.Log($"[Coordinator] ‘I‘ğ’†‚Ì‹î(ID: {selectedPieceId.Value})‚É‘Î‚µ‚Ä RandomizePowerCommand ‚ğ‘—M‚µ‚Ü‚·B");

            var command = new RandomizePowerCommand(snapshot.CurrentPlayer, selectedPieceId.Value);
            if (agents[snapshot.CurrentPlayer] is HumanPlayerAgent humanAgent)
            {
                humanAgent.TrySubmit(command);
            }
            else
            {
                Debug.LogError("[Coordinator] Œ»İ‚ÌƒvƒŒƒCƒ„[ Agent ‚ª HumanPlayerAgent ‚Å‚Í‚ ‚è‚Ü‚¹‚ñB");
            }
        }

        public GridPosition? SelectedCell
        {
            get
            {
                if (!selectedPieceId.HasValue ||
                    !session.Snapshot.TryGetPiece(selectedPieceId.Value, out PieceState selected))
                {
                    return null;
                }

                return selected.Position;
            }
        }

        public int ExecutedCommandCount { get; private set; }

        public void HandleCellClick(GridPosition cell)
        {
            GameSnapshot snapshot = session.Snapshot;
            if (!snapshot.IsInside(cell) || snapshot.IsGameOver)
            {
                return;
            }

            if (isFusionModeActive)
            {
                HandleFusionModeClick(cell, snapshot);
                return;
            }

            if (snapshot.TryGetPiece(cell, out PieceState clickedPiece) &&
                clickedPiece.Owner == snapshot.CurrentPlayer)
            {
                selectedPieceId = selectedPieceId == clickedPiece.Id
                    ? (PieceId?)null
                    : clickedPiece.Id;
                
                Debug.Log($"[Coordinator] ‹î‚ª‘I‘ğ‚³‚ê‚Ü‚µ‚½: ID={clickedPiece.Id}");
                RenderSelection();
                return;
            }

            if (!selectedPieceId.HasValue)
            {
                return;
            }

            MovePieceCommand move = session.GetLegalCommands(snapshot.CurrentPlayer)
                .OfType<MovePieceCommand>()
                .FirstOrDefault(command =>
                    command.PieceId == selectedPieceId.Value && command.Destination == cell);
            if (move == null || !(agents[snapshot.CurrentPlayer] is HumanPlayerAgent humanAgent))
            {
                return;
            }

            humanAgent.TrySubmit(move);
        }

        /// <summary>
        /// ã€Œåˆä½“ã€ãƒœã‚¿ãƒ³ã‹ã‚‰å‘¼ã³å‡ºã•ã‚Œã‚‹ã€‚é§’ãŒé¸æŠã•ã‚Œã¦ã„ã‚‹é–“ã ã‘åˆä½“ãƒ¢ãƒ¼ãƒ‰ã‚’ON/OFFã™ã‚‹ã€‚
        /// </summary>
        public void ToggleFusionMode()
        {
            if (!selectedPieceId.HasValue)
            {
                return;
            }

            isFusionModeActive = !isFusionModeActive;
            RenderSelection();
        }

        private void HandleFusionModeClick(GridPosition cell, GameSnapshot snapshot)
        {
            if (!selectedPieceId.HasValue)
            {
                isFusionModeActive = false;
                return;
            }

            if (snapshot.TryGetPiece(cell, out PieceState clickedPiece) &&
                clickedPiece.Id != selectedPieceId.Value &&
                clickedPiece.Owner == snapshot.CurrentPlayer &&
                agents[snapshot.CurrentPlayer] is HumanPlayerAgent humanAgent)
            {
                var fusion = new FusePiecesCommand(
                    snapshot.CurrentPlayer, selectedPieceId.Value, clickedPiece.Id);
                isFusionModeActive = false;
                humanAgent.TrySubmit(fusion);
                RenderSelection();
                return;
            }

            // åˆä½“å¯¾è±¡ã«ãªã‚‰ãªã„ãƒã‚¹ã‚’ã‚¯ãƒªãƒƒã‚¯ã—ãŸã‚‰ã€é¸æŠã¯æ®‹ã—ãŸã¾ã¾åˆä½“ãƒ¢ãƒ¼ãƒ‰ã ã‘æŠœã‘ã‚‹
            isFusionModeActive = false;
            RenderSelection();
        }

        public void Reset()
        {
            foreach (IPlayerAgent agent in agents.Values)
            {
                agent.EndTurn();
            }

            session.Reset();
            selectedPieceId = null;
            isFusionModeActive = false;
            pieceViews.Rebuild(session.Snapshot);
            boardView.ShowSelection(
                null, new List<GridPosition>(), new List<GridPosition>(), session.Snapshot);
            hudView.Render(session.Snapshot);
            hudView.ShowMessage(string.Empty);
            BeginCurrentTurn();
        }

        private void ExecuteSubmittedCommand(GameCommand command)
        {
            Debug.Log($"[Coordinator] ƒRƒ}ƒ“ƒhÀsŠJn: {command.GetType().Name}");

            agents[command.Player].EndTurn();
            CommandResult result = session.Execute(command);
            if (!result.Success)
            {
                Debug.LogError($"[Coordinator] ƒRƒ}ƒ“ƒh‚ÌÀs‚É¸”s‚µ‚Ü‚µ‚½B ——R: {result.FailureReason}");
                BeginCurrentTurn();
                return;
            }

            Debug.Log("ƒ^[ƒ“‚ğXV‚µ‚Ü‚·B");

            ExecutedCommandCount++;
            selectedPieceId = null;
            GameSnapshot snapshot = session.Snapshot;
            pieceViews.ApplyEvents(result.Events, snapshot);
            boardView.ShowSelection(
                null, new List<GridPosition>(), new List<GridPosition>(), snapshot);
            hudView.Render(snapshot);
            ShowFusionResultMessage(result.Events);
            if (!snapshot.IsGameOver)
            {
                BeginCurrentTurn();
            }
        }

        private void ShowFusionResultMessage(IReadOnlyList<GameEvent> events)
        {
            foreach (GameEvent gameEvent in events)
            {
                if (gameEvent is PiecesFused fused)
                {
                    hudView.ShowMessage(fused.Bonus >= 2
                        ? "å¤§æˆåŠŸï¼ æˆ¦é—˜åŠ›+2ã§åˆä½“ã—ã¾ã—ãŸ"
                        : "åˆä½“æˆåŠŸï¼ æˆ¦é—˜åŠ›+1ã§åˆä½“ã—ã¾ã—ãŸ");
                    return;
                }

                if (gameEvent is FusionAttemptFailed)
                {
                    hudView.ShowMessage("åˆä½“å¤±æ•—â€¦ã€€é§’ã¯ãã®ã¾ã¾æ®‹ã‚Šã¾ã—ãŸ");
                    return;
                }
            }

            hudView.ShowMessage(string.Empty);
        }

        private void BeginCurrentTurn()
        {
            GameSnapshot snapshot = session.Snapshot;
            if (snapshot.IsGameOver)
            {
                return;
            }

            IReadOnlyList<GameCommand> legalCommands =
                session.GetLegalCommands(snapshot.CurrentPlayer);

            GameSnapshot snapshotWithLegalCommands = new GameSnapshot(
                snapshot.Columns,
                snapshot.Rows,
                snapshot.Pieces,
                snapshot.Cells,
                snapshot.CurrentPlayer,
                snapshot.Winner,
                snapshot.IsDraw,
                legalCommands);

            agents[snapshot.CurrentPlayer].BeginTurn(
                snapshotWithLegalCommands, legalCommands, ExecuteSubmittedCommand);
        }

        private void RenderSelection()
        {
            GameSnapshot snapshot = session.Snapshot;
            if (!selectedPieceId.HasValue ||
                !snapshot.TryGetPiece(selectedPieceId.Value, out PieceState selectedPiece))
            {
                isFusionModeActive = false;
                hudView.SetFuseButtonInteractable(false);
                boardView.ShowSelection(
                    null, new List<GridPosition>(), new List<GridPosition>(), snapshot);
                return;
            }

            IReadOnlyList<GameCommand> legalCommands =
                session.GetLegalCommands(snapshot.CurrentPlayer);

            List<GridPosition> fusionTargets = legalCommands
                .OfType<FusePiecesCommand>()
                .Where(command =>
                    command.FirstPieceId == selectedPiece.Id ||
                    command.SecondPieceId == selectedPiece.Id)
                .Select(command =>
                {
                    PieceId otherId = command.FirstPieceId == selectedPiece.Id
                        ? command.SecondPieceId
                        : command.FirstPieceId;
                    snapshot.TryGetPiece(otherId, out PieceState other);
                    return other.Position;
                })
                .ToList();

            hudView.SetFuseButtonInteractable(fusionTargets.Count > 0);

            if (isFusionModeActive)
            {
                // åˆä½“ãƒ¢ãƒ¼ãƒ‰ä¸­ã¯ç§»å‹•å…ˆãƒã‚¤ãƒ©ã‚¤ãƒˆã‚’éš ã—ã€åˆä½“å¯¾è±¡ã ã‘ã‚’è¦‹ã›ã‚‹
                boardView.ShowSelection(
                    selectedPiece.Position, new List<GridPosition>(), fusionTargets, snapshot);
                return;
            }

            List<GridPosition> destinations = legalCommands
                .OfType<MovePieceCommand>()
                .Where(command => command.PieceId == selectedPiece.Id)
                .Select(command => command.Destination)
                .ToList();

            boardView.ShowSelection(
                selectedPiece.Position, destinations, new List<GridPosition>(), snapshot);
        }
    }
}
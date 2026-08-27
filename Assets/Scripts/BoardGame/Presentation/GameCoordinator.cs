using System.Collections.Generic;
using System.Linq;
using GCCC.BoardGame.Core;
using GCCC.BoardGame.Core.Commands;
using GCCC.BoardGame.Core.Events;
using GCCC.BoardGame.Core.Model;
using GCCC.BoardGame.Core.Players;
using GCCC.BoardGame.Presentation.Views;

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

            hudView.Render(session.Snapshot);
            BeginCurrentTurn();
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
        /// 「合体」ボタンから呼び出される。駒が選択されている間だけ合体モードをON/OFFする。
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

            // 合体対象にならないマスをクリックしたら、選択は残したまま合体モードだけ抜ける
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
            agents[command.Player].EndTurn();
            CommandResult result = session.Execute(command);
            if (!result.Success)
            {
                BeginCurrentTurn();
                return;
            }

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
                        ? "大成功！ 戦闘力+2で合体しました"
                        : "合体成功！ 戦闘力+1で合体しました");
                    return;
                }

                if (gameEvent is FusionAttemptFailed)
                {
                    hudView.ShowMessage("合体失敗…　駒はそのまま残りました");
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
            agents[snapshot.CurrentPlayer].BeginTurn(
                snapshot, legalCommands, ExecuteSubmittedCommand);
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
                // 合体モード中は移動先ハイライトを隠し、合体対象だけを見せる
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
using System.Collections.Generic;
using System.Linq;
using GCCC.BoardGame.Core;
using GCCC.BoardGame.Core.Commands;
using GCCC.BoardGame.Core.Model;
using GCCC.BoardGame.Core.Players;
using GCCC.BoardGame.Presentation.Views;
using UnityEngine; // Debug.Log 使用のため追加

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
            Debug.Log("[Coordinator] ボタン押下イベントを受信しました。");

            GameSnapshot snapshot = session.Snapshot;
            if (snapshot.IsGameOver)
            {
                Debug.LogWarning("[Coordinator] ゲーム終了済みのため処理をスキップしました。");
                return;
            }

            if (!selectedPieceId.HasValue)
            {
                Debug.LogWarning("[Coordinator] 駒が選択されていません！ 駒をクリックして選択してからボタンを押してください。");
                return;
            }

            Debug.Log($"[Coordinator] 選択中の駒(ID: {selectedPieceId.Value})に対して RandomizePowerCommand を送信します。");

            var command = new RandomizePowerCommand(snapshot.CurrentPlayer, selectedPieceId.Value);
            if (agents[snapshot.CurrentPlayer] is HumanPlayerAgent humanAgent)
            {
                humanAgent.TrySubmit(command);
            }
            else
            {
                Debug.LogError("[Coordinator] 現在のプレイヤー Agent が HumanPlayerAgent ではありません。");
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

            if (snapshot.TryGetPiece(cell, out PieceState clickedPiece) &&
                clickedPiece.Owner == snapshot.CurrentPlayer)
            {
                selectedPieceId = selectedPieceId == clickedPiece.Id
                    ? (PieceId?)null
                    : clickedPiece.Id;
                
                Debug.Log($"[Coordinator] 駒が選択されました: ID={clickedPiece.Id}");
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

        public void Reset()
        {
            foreach (IPlayerAgent agent in agents.Values)
            {
                agent.EndTurn();
            }

            session.Reset();
            selectedPieceId = null;
            pieceViews.Rebuild(session.Snapshot);
            boardView.ShowSelection(null, new List<GridPosition>(), session.Snapshot);
            hudView.Render(session.Snapshot);
            BeginCurrentTurn();
        }

        private void ExecuteSubmittedCommand(GameCommand command)
        {
            Debug.Log($"[Coordinator] コマンド実行開始: {command.GetType().Name}");

            agents[command.Player].EndTurn();
            CommandResult result = session.Execute(command);
            if (!result.Success)
            {
                Debug.LogError($"[Coordinator] コマンドの実行に失敗しました。 理由: {result.FailureReason}");
                BeginCurrentTurn();
                return;
            }

            Debug.Log("ターンを更新します。");

            ExecutedCommandCount++;
            selectedPieceId = null;
            GameSnapshot snapshot = session.Snapshot;
            pieceViews.ApplyEvents(result.Events, snapshot);
            boardView.ShowSelection(null, new List<GridPosition>(), snapshot);
            hudView.Render(snapshot);
            if (!snapshot.IsGameOver)
            {
                BeginCurrentTurn();
            }
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
                boardView.ShowSelection(null, new List<GridPosition>(), snapshot);
                return;
            }

            List<GridPosition> destinations = session.GetLegalCommands(snapshot.CurrentPlayer)
                .OfType<MovePieceCommand>()
                .Where(command => command.PieceId == selectedPiece.Id)
                .Select(command => command.Destination)
                .ToList();
            boardView.ShowSelection(selectedPiece.Position, destinations, snapshot);
        }
    }
}
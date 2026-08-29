using System.Collections.Generic;
using System.Linq;
using GCCC.BoardGame.Core;
using GCCC.BoardGame.Core.Commands;
using GCCC.BoardGame.Core.Model;
using GCCC.BoardGame.Core.Players;
using GCCC.BoardGame.Presentation.Audio;
using GCCC.BoardGame.Presentation.Views;

namespace GCCC.BoardGame.Presentation
{
    public sealed class GameCoordinator
    {
        private readonly GameSession session;
        private readonly BoardView boardView;
        private readonly PieceViewManager pieceViews;
        private readonly GameHudView hudView;
        private readonly BoardGameAudioManager audioManager;
        private readonly Dictionary<PlayerId, IPlayerAgent> agents;
        private PieceId? selectedPieceId;

        public GameCoordinator(
            GameSession session,
            BoardView boardView,
            PieceViewManager pieceViews,
            GameHudView hudView,
            IPlayerAgent player1Agent = null,
            IPlayerAgent player2Agent = null,
            BoardGameAudioManager audioManager = null)
        {
            this.session = session;
            this.boardView = boardView;
            this.pieceViews = pieceViews;
            this.hudView = hudView;
            this.audioManager = audioManager;
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
            audioManager?.PlayEvents(result.Events);
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
            agents[snapshot.CurrentPlayer].BeginTurn(
                snapshot, legalCommands, ExecuteSubmittedCommand);
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

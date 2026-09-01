using System;
using System.Collections.Generic;
using System.Linq;
using GCCC.BoardGame.Core;
using GCCC.BoardGame.Core.Commands;
using GCCC.BoardGame.Core.Events;
using GCCC.BoardGame.Core.Model;
using GCCC.BoardGame.Core.Players;
using GCCC.BoardGame.Presentation.Audio;
using GCCC.BoardGame.Presentation.Views;

namespace GCCC.BoardGame.Presentation
{
    public sealed class GameCoordinator
    {
        private static readonly IReadOnlyList<GridPosition> NoCells =
            Array.Empty<GridPosition>();

        private readonly GameSession session;
        private readonly IBoardGameBoardView boardView;
        private readonly IPieceViewCollection pieceViews;
        private readonly IGameHud hudView;
        private readonly IGameAudio audioManager;
        private readonly Dictionary<PlayerId, IPlayerAgent> agents;
        private InteractionState interactionStateValue = InteractionState.None;

        /// <summary>
        /// モードは11箇所から書き換わるので、代入経路を1本にまとめて
        /// ボタンの押し込み表示を必ず追随させる。
        /// </summary>
        private InteractionState interactionState
        {
            get => interactionStateValue;
            set
            {
                interactionStateValue = value;
                hudView.SetFuseModeActive(
                    value.Mode == InteractionMode.Fusion);
                hudView.SetReserveDeployModeActive(
                    value.Mode == InteractionMode.ReserveDeployment);
            }
        }

        public GameCoordinator(
            GameSession session,
            BoardView boardView,
            PieceViewManager pieceViews,
            GameHudView hudView,
            IPlayerAgent player1Agent = null,
            IPlayerAgent player2Agent = null,
            BoardGameAudioManager audioManager = null)
            : this(
                session,
                (IBoardGameBoardView)boardView,
                (IPieceViewCollection)pieceViews,
                (IGameHud)hudView,
                player1Agent,
                player2Agent,
                audioManager)
        {
        }

        internal GameCoordinator(
            GameSession session,
            IBoardGameBoardView boardView,
            IPieceViewCollection pieceViews,
            IGameHud hudView,
            IPlayerAgent player1Agent = null,
            IPlayerAgent player2Agent = null,
            IGameAudio audioManager = null)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.boardView = boardView ?? throw new ArgumentNullException(nameof(boardView));
            this.pieceViews = pieceViews ?? throw new ArgumentNullException(nameof(pieceViews));
            this.hudView = hudView ?? throw new ArgumentNullException(nameof(hudView));
            this.audioManager = audioManager;
            agents = new Dictionary<PlayerId, IPlayerAgent>
            {
                [PlayerId.Player1] = player1Agent ?? new HumanPlayerAgent(PlayerId.Player1),
                [PlayerId.Player2] = player2Agent ?? new HumanPlayerAgent(PlayerId.Player2)
            };

            hudView.OnRandomizePowerButtonClicked += HandleRandomizePowerButtonClicked;
            hudView.Render(session.Snapshot);
            BeginCurrentTurn();
        }

        public GridPosition? SelectedCell
        {
            get
            {
                if (!interactionState.SelectedPieceId.HasValue ||
                    !session.Snapshot.TryGetPiece(
                        interactionState.SelectedPieceId.Value,
                        out PieceState selected))
                {
                    return null;
                }

                return selected.Position;
            }
        }

        public int ExecutedCommandCount { get; private set; }

        public PieceId? SelectedReservePieceId =>
            interactionState.SelectedReservePieceId;

        public void HandleCellClick(GridPosition cell)
        {
            GameSnapshot snapshot = session.Snapshot;
            if (!snapshot.IsInside(cell) || snapshot.IsGameOver)
            {
                return;
            }

            if (interactionState.Mode == InteractionMode.Fusion)
            {
                HandleFusionModeClick(cell, snapshot);
                return;
            }

            if (interactionState.Mode == InteractionMode.ReserveDeployment)
            {
                HandleReserveDeploymentClick(cell, snapshot);
                return;
            }

            if (snapshot.TryGetPiece(cell, out PieceState clickedPiece) &&
                clickedPiece.Owner == snapshot.CurrentPlayer)
            {
                PieceId? selectedPieceId = interactionState.SelectedPieceId;
                interactionState = selectedPieceId == clickedPiece.Id
                    ? InteractionState.None
                    : InteractionState.PieceSelected(clickedPiece.Id);
                hudView.SetSelectedReservePiece(null);
                RenderSelection();
                return;
            }

            if (!interactionState.SelectedPieceId.HasValue)
            {
                return;
            }

            MovePieceCommand move = session.GetLegalCommands(snapshot.CurrentPlayer)
                .OfType<MovePieceCommand>()
                .FirstOrDefault(command =>
                    command.PieceId == interactionState.SelectedPieceId.Value &&
                    command.Destination == cell);
            if (move == null || !(agents[snapshot.CurrentPlayer] is HumanPlayerAgent humanAgent))
            {
                return;
            }

            humanAgent.TrySubmit(move);
        }

        public void ToggleFusionMode()
        {
            if (!interactionState.SelectedPieceId.HasValue)
            {
                return;
            }

            PieceId pieceId = interactionState.SelectedPieceId.Value;
            interactionState = interactionState.Mode == InteractionMode.Fusion
                ? InteractionState.PieceSelected(pieceId)
                : InteractionState.Fusion(pieceId);
            hudView.SetSelectedReservePiece(null);
            RenderSelection();
        }

        public void ToggleReserveDeployMode()
        {
            GameSnapshot snapshot = session.Snapshot;
            if (snapshot.IsGameOver)
            {
                return;
            }

            if (interactionState.Mode == InteractionMode.ReserveDeployment)
            {
                CancelReserveDeployment();
                RenderSelection();
                hudView.ShowMessage(string.Empty);
                return;
            }

            DeployReservePieceCommand firstDeployment =
                session.GetLegalCommands(snapshot.CurrentPlayer)
                    .OfType<DeployReservePieceCommand>()
                    .FirstOrDefault();
            if (firstDeployment == null)
            {
                return;
            }

            ToggleReservePieceSelection(firstDeployment.ReservePieceId);
        }

        public void ToggleReservePieceSelection(PieceId reservePieceId)
        {
            GameSnapshot snapshot = session.Snapshot;
            if (snapshot.IsGameOver)
            {
                return;
            }

            if (interactionState.Mode == InteractionMode.ReserveDeployment &&
                interactionState.SelectedReservePieceId == reservePieceId)
            {
                CancelReserveDeployment();
                RenderSelection();
                hudView.ShowMessage(string.Empty);
                return;
            }

            bool canDeploy = session.GetLegalCommands(snapshot.CurrentPlayer)
                .OfType<DeployReservePieceCommand>()
                .Any(command => command.ReservePieceId == reservePieceId);
            if (!canDeploy)
            {
                return;
            }

            interactionState = InteractionState.ReserveDeployment(reservePieceId);
            hudView.SetSelectedReservePiece(reservePieceId);
            RenderReserveDeployment();
            hudView.ShowMessage("リザーブを配置するマスを選んでください");
        }

        public void Reset()
        {
            foreach (IPlayerAgent agent in agents.Values)
            {
                agent.EndTurn();
            }

            session.Reset();
            interactionState = InteractionState.None;
            CancelReserveDeployment();
            pieceViews.Rebuild(session.Snapshot);
            boardView.ShowSelection(null, NoCells, NoCells, session.Snapshot);
            hudView.Render(session.Snapshot);
            hudView.ShowMessage(string.Empty);
            hudView.SetRandomizeButtonInteractable(false);
            hudView.SetReserveDeployButtonInteractable(false);
            hudView.SetDeployableReservePieces(Array.Empty<PieceId>());
            BeginCurrentTurn();
        }

        public void Dispose()
        {
            hudView.OnRandomizePowerButtonClicked -= HandleRandomizePowerButtonClicked;
        }

        private void HandleRandomizePowerButtonClicked()
        {
            GameSnapshot snapshot = session.Snapshot;
            if (snapshot.IsGameOver ||
                !interactionState.SelectedPieceId.HasValue)
            {
                return;
            }

            if (agents[snapshot.CurrentPlayer] is HumanPlayerAgent humanAgent)
            {
                RandomizePowerCommand command =
                    session.GetLegalCommands(snapshot.CurrentPlayer)
                        .OfType<RandomizePowerCommand>()
                        .FirstOrDefault(candidate =>
                            candidate.PieceId == interactionState.SelectedPieceId.Value);
                if (command != null)
                {
                    humanAgent.TrySubmit(command);
                }
            }
        }

        private void HandleFusionModeClick(GridPosition cell, GameSnapshot snapshot)
        {
            if (!interactionState.SelectedPieceId.HasValue)
            {
                interactionState = InteractionState.None;
                return;
            }

            if (snapshot.TryGetPiece(cell, out PieceState clickedPiece) &&
                clickedPiece.Id != interactionState.SelectedPieceId.Value &&
                clickedPiece.Owner == snapshot.CurrentPlayer &&
                agents[snapshot.CurrentPlayer] is HumanPlayerAgent humanAgent)
            {
                FusePiecesCommand fusion = new FusePiecesCommand(
                    snapshot.CurrentPlayer,
                    interactionState.SelectedPieceId.Value,
                    clickedPiece.Id);
                interactionState = InteractionState.None;
                humanAgent.TrySubmit(fusion);
                RenderSelection();
                return;
            }

            interactionState = InteractionState.PieceSelected(
                interactionState.SelectedPieceId.Value);
            RenderSelection();
        }

        private void HandleReserveDeploymentClick(
            GridPosition cell,
            GameSnapshot snapshot)
        {
            if (!interactionState.SelectedReservePieceId.HasValue ||
                !(agents[snapshot.CurrentPlayer] is HumanPlayerAgent humanAgent))
            {
                CancelReserveDeployment();
                RenderSelection();
                return;
            }

            DeployReservePieceCommand deployment =
                session.GetLegalCommands(snapshot.CurrentPlayer)
                    .OfType<DeployReservePieceCommand>()
                    .FirstOrDefault(command =>
                        command.ReservePieceId ==
                        interactionState.SelectedReservePieceId.Value &&
                        command.Destination == cell);
            if (deployment != null)
            {
                humanAgent.TrySubmit(deployment);
            }
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
            interactionState = InteractionState.None;
            hudView.SetSelectedReservePiece(null);
            GameSnapshot snapshot = session.Snapshot;
            pieceViews.ApplyEvents(result.Events, snapshot);
            audioManager?.PlayEvents(result.Events);
            boardView.ShowSelection(null, NoCells, NoCells, snapshot);
            hudView.Render(snapshot);
            hudView.SetRandomizeButtonInteractable(false);
            hudView.SetReserveDeployButtonInteractable(false);
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
            UpdateReserveControls(legalCommands);
            GameSnapshot snapshotWithLegalCommands =
                snapshot.WithLegalCommands(legalCommands);

            agents[snapshot.CurrentPlayer].BeginTurn(
                snapshotWithLegalCommands, legalCommands, ExecuteSubmittedCommand);
        }

        private void RenderSelection()
        {
            GameSnapshot snapshot = session.Snapshot;
            if (!interactionState.SelectedPieceId.HasValue ||
                !snapshot.TryGetPiece(
                    interactionState.SelectedPieceId.Value,
                    out PieceState selectedPiece))
            {
                interactionState = InteractionState.None;
                hudView.SetFuseButtonInteractable(false);
                hudView.SetRandomizeButtonInteractable(false);
                RefreshReserveDeployButton(snapshot);
                boardView.ShowSelection(null, NoCells, NoCells, snapshot);
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
            bool canRandomize = legalCommands
                .OfType<RandomizePowerCommand>()
                .Any(command => command.PieceId == selectedPiece.Id);
            hudView.SetRandomizeButtonInteractable(
                canRandomize && interactionState.Mode != InteractionMode.Fusion);
            UpdateReserveControls(
                legalCommands,
                interactionState.Mode != InteractionMode.Fusion);
            if (interactionState.Mode == InteractionMode.Fusion)
            {
                boardView.ShowSelection(
                    selectedPiece.Position, NoCells, fusionTargets, snapshot);
                return;
            }

            List<GridPosition> destinations = legalCommands
                .OfType<MovePieceCommand>()
                .Where(command => command.PieceId == selectedPiece.Id)
                .Select(command => command.Destination)
                .ToList();
            boardView.ShowSelection(
                selectedPiece.Position, destinations, NoCells, snapshot);
        }

        private void RenderReserveDeployment()
        {
            GameSnapshot snapshot = session.Snapshot;
            if (!interactionState.SelectedReservePieceId.HasValue)
            {
                CancelReserveDeployment();
                RenderSelection();
                return;
            }

            List<GridPosition> destinations =
                session.GetLegalCommands(snapshot.CurrentPlayer)
                    .OfType<DeployReservePieceCommand>()
                    .Where(command =>
                        command.ReservePieceId ==
                        interactionState.SelectedReservePieceId.Value)
                    .Select(command => command.Destination)
                    .ToList();
            if (destinations.Count == 0)
            {
                CancelReserveDeployment();
                RenderSelection();
                return;
            }

            hudView.SetFuseButtonInteractable(false);
            hudView.SetRandomizeButtonInteractable(false);
            UpdateReserveControls(
                session.GetLegalCommands(snapshot.CurrentPlayer));
            boardView.ShowSelection(
                null, destinations, NoCells, snapshot);
        }

        private void RefreshReserveDeployButton(GameSnapshot snapshot)
        {
            IReadOnlyList<GameCommand> legalCommands = snapshot.IsGameOver
                ? Array.Empty<GameCommand>()
                : session.GetLegalCommands(snapshot.CurrentPlayer);
            UpdateReserveControls(legalCommands);
        }

        private void UpdateReserveControls(
            IEnumerable<GameCommand> legalCommands,
            bool buttonAllowed = true)
        {
            PieceId[] deployablePieceIds = legalCommands
                .OfType<DeployReservePieceCommand>()
                .Select(command => command.ReservePieceId)
                .Distinct()
                .ToArray();
            hudView.SetReserveDeployButtonInteractable(
                buttonAllowed && deployablePieceIds.Length > 0);
            hudView.SetDeployableReservePieces(deployablePieceIds);
        }

        private void CancelReserveDeployment()
        {
            if (interactionState.Mode == InteractionMode.ReserveDeployment)
            {
                interactionState = InteractionState.None;
            }

            hudView.SetSelectedReservePiece(null);
        }
    }
}

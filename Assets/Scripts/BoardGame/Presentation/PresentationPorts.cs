using System;
using System.Collections.Generic;
using GCCC.BoardGame.Core.Events;
using GCCC.BoardGame.Core.Model;

namespace GCCC.BoardGame.Presentation
{
    internal interface IBoardGameBoardView
    {
        void ShowSelection(
            GridPosition? selectedCell,
            IReadOnlyList<GridPosition> legalDestinations,
            IReadOnlyList<GridPosition> fusionTargets,
            GameSnapshot snapshot);
    }

    internal interface IPieceViewCollection
    {
        void Rebuild(GameSnapshot snapshot);

        void ApplyEvents(IReadOnlyList<GameEvent> events, GameSnapshot snapshot);
    }

    internal interface IGameHud
    {
        event Action OnRandomizePowerButtonClicked;

        void Render(GameSnapshot snapshot);

        void ShowMessage(string text);

        void SetFuseButtonInteractable(bool interactable);

        void SetRandomizeButtonInteractable(bool interactable);

        void SetReserveDeployButtonInteractable(bool interactable);

        void SetFuseModeActive(bool active);

        void SetReserveDeployModeActive(bool active);

        void SetDeployableReservePieces(IEnumerable<PieceId> pieceIds);

        void SetSelectedReservePiece(PieceId? pieceId);
    }

    internal interface IGameAudio
    {
        void PlayEvents(IReadOnlyList<GameEvent> events);
    }
}

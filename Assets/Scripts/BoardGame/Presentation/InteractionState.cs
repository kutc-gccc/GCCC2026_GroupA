using System;
using GCCC.BoardGame.Core.Model;

namespace GCCC.BoardGame.Presentation
{
    internal enum InteractionMode
    {
        None,
        PieceSelected,
        Fusion,
        ReserveDeployment
    }

    internal readonly struct InteractionState
    {
        private InteractionState(
            InteractionMode mode,
            PieceId? pieceId,
            PieceId? reservePieceId)
        {
            Mode = mode;
            SelectedPieceId = pieceId;
            SelectedReservePieceId = reservePieceId;
        }

        public InteractionMode Mode { get; }

        public PieceId? SelectedPieceId { get; }

        public PieceId? SelectedReservePieceId { get; }

        public static InteractionState None =>
            new InteractionState(InteractionMode.None, null, null);

        public static InteractionState PieceSelected(PieceId pieceId) =>
            new InteractionState(InteractionMode.PieceSelected, pieceId, null);

        public static InteractionState Fusion(PieceId pieceId) =>
            new InteractionState(InteractionMode.Fusion, pieceId, null);

        public static InteractionState ReserveDeployment(PieceId reservePieceId) =>
            new InteractionState(
                InteractionMode.ReserveDeployment, null, reservePieceId);
    }
}

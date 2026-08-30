namespace GCCC.BoardGame.Core.Commands
{
    public enum CommandFailureReason
    {
        None,
        GameOver,
        NotPlayersTurn,
        PieceNotFound,
        NotPieceOwner,
        IllegalMove,
        FusionDisabled,
        ReservePieceNotFound,
        PieceLimitReached,
        InvalidDeploymentPosition,
        InvalidCommand
    }
}

using GCCC.BoardGame.Core.Model;

namespace GCCC.BoardGame.Core.Commands
{
    public sealed class DeployReservePieceCommand : GameCommand
    {
        public DeployReservePieceCommand(
            PlayerId player,
            PieceId reservePieceId,
            GridPosition destination)
            : base(player)
        {
            ReservePieceId = reservePieceId;
            Destination = destination;
        }

        public PieceId ReservePieceId { get; }

        public GridPosition Destination { get; }
    }
}

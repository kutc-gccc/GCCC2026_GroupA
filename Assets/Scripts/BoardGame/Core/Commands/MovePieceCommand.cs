using GCCC.BoardGame.Core.Model;

namespace GCCC.BoardGame.Core.Commands
{
    public sealed class MovePieceCommand : GameCommand
    {
        public MovePieceCommand(PlayerId player, PieceId pieceId, GridPosition destination)
            : base(player)
        {
            PieceId = pieceId;
            Destination = destination;
        }

        public PieceId PieceId { get; }

        public GridPosition Destination { get; }
    }
}

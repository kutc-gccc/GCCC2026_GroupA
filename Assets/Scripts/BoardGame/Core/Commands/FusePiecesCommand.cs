using GCCC.BoardGame.Core.Model;

namespace GCCC.BoardGame.Core.Commands
{
    public sealed class FusePiecesCommand : GameCommand
    {
        public FusePiecesCommand(PlayerId player, PieceId firstPieceId, PieceId secondPieceId)
            : base(player)
        {
            FirstPieceId = firstPieceId;
            SecondPieceId = secondPieceId;
        }

        public PieceId FirstPieceId { get; }

        public PieceId SecondPieceId { get; }
    }
}

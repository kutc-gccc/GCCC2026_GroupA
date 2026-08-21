using GCCC.BoardGame.Core.Model;

namespace GCCC.BoardGame.Core.Commands
{
    public sealed class ChangeCombatPowerRandomlyCommand : GameCommand
    {
        public PieceId TargetPieceId { get; }

        public ChangeCombatPowerRandomlyCommand(PlayerId player, PieceId targetPieceId) 
            : base(player)
        {
            TargetPieceId = targetPieceId;
        }
    }
}
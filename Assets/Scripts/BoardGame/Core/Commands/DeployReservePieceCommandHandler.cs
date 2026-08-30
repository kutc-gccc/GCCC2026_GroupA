using System;

namespace GCCC.BoardGame.Core.Commands
{
    internal sealed class DeployReservePieceCommandHandler : IGameCommandHandler
    {
        public Type CommandType => typeof(DeployReservePieceCommand);

        public CommandResult Execute(GameSession session, GameCommand command)
        {
            return session.ExecuteDeployReservePiece(
                (DeployReservePieceCommand)command);
        }
    }
}

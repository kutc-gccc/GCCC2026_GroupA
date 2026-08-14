using System;
using GCCC.BoardGame.Core.Commands;

namespace GCCC.BoardGame.Core.Rules.Movement
{
    internal sealed class MovePieceCommandHandler : IGameCommandHandler
    {
        public Type CommandType => typeof(MovePieceCommand);

        public CommandResult Execute(GameSession session, GameCommand command)
        {
            return session.ExecuteMove((MovePieceCommand)command);
        }
    }
}

using System;

namespace GCCC.BoardGame.Core.Commands
{
    internal interface IGameCommandHandler
    {
        Type CommandType { get; }

        CommandResult Execute(GameSession session, GameCommand command);
    }
}

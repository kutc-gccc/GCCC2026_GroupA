using System;
using GCCC.BoardGame.Core.Commands;

namespace GCCC.BoardGame.Core.Rules.Fusion
{
    internal sealed class FusePiecesCommandHandler : IGameCommandHandler
    {
        public Type CommandType => typeof(FusePiecesCommand);

        public CommandResult Execute(GameSession session, GameCommand command)
        {
            return session.ExecuteFusion((FusePiecesCommand)command);
        }
    }
}

using System;

namespace GCCC.BoardGame.Core.Commands
{
    internal sealed class RandomizePowerCommandHandler : IGameCommandHandler
    {
        public Type CommandType => typeof(RandomizePowerCommand);

        public CommandResult Execute(GameSession session, GameCommand command)
        {
            return session.ExecuteRandomizePower((RandomizePowerCommand)command);
        }
    }
}

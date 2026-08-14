using System;
using System.Collections.Generic;
using GCCC.BoardGame.Core.Commands;
using GCCC.BoardGame.Core.Model;

namespace GCCC.BoardGame.Core.Players
{
    public sealed class HumanPlayerAgent : IPlayerAgent
    {
        private Action<GameCommand> submitCommand;

        public HumanPlayerAgent(PlayerId player)
        {
            Player = player;
        }

        public PlayerId Player { get; }

        public void BeginTurn(
            GameSnapshot snapshot,
            IReadOnlyList<GameCommand> legalCommands,
            Action<GameCommand> submit)
        {
            submitCommand = submit;
        }

        public bool TrySubmit(GameCommand command)
        {
            if (submitCommand == null || command.Player != Player)
            {
                return false;
            }

            submitCommand(command);
            return true;
        }

        public void EndTurn()
        {
            submitCommand = null;
        }
    }
}

using System;
using System.Collections.Generic;
using GCCC.BoardGame.Core.Commands;
using GCCC.BoardGame.Core.Model;

namespace GCCC.BoardGame.Core.Players
{
    public interface IPlayerAgent
    {
        PlayerId Player { get; }

        void BeginTurn(
            GameSnapshot snapshot,
            IReadOnlyList<GameCommand> legalCommands,
            Action<GameCommand> submitCommand);

        void EndTurn();
    }
}

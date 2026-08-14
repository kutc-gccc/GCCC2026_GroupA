using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GCCC.BoardGame.Core.Events;

namespace GCCC.BoardGame.Core.Commands
{
    public sealed class CommandResult
    {
        private CommandResult(
            bool success,
            CommandFailureReason failureReason,
            IEnumerable<GameEvent> events)
        {
            Success = success;
            FailureReason = failureReason;
            Events = new ReadOnlyCollection<GameEvent>((events ?? Array.Empty<GameEvent>()).ToArray());
        }

        public bool Success { get; }

        public CommandFailureReason FailureReason { get; }

        public IReadOnlyList<GameEvent> Events { get; }

        public static CommandResult Succeeded(IEnumerable<GameEvent> events)
        {
            return new CommandResult(true, CommandFailureReason.None, events);
        }

        public static CommandResult Failed(CommandFailureReason reason)
        {
            return new CommandResult(false, reason, Array.Empty<GameEvent>());
        }
    }
}

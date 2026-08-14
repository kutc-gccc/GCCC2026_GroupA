using GCCC.BoardGame.Core.Model;

namespace GCCC.BoardGame.Core.Commands
{
    public abstract class GameCommand
    {
        protected GameCommand(PlayerId player)
        {
            Player = player;
        }

        public PlayerId Player { get; }
    }
}

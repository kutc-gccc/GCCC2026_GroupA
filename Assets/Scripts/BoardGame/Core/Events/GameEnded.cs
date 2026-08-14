using GCCC.BoardGame.Core.Model;

namespace GCCC.BoardGame.Core.Events
{
    public sealed class GameEnded : GameEvent
    {
        public GameEnded(PlayerId? winner, bool isDraw)
        {
            Winner = winner;
            IsDraw = isDraw;
        }

        public PlayerId? Winner { get; }

        public bool IsDraw { get; }
    }
}

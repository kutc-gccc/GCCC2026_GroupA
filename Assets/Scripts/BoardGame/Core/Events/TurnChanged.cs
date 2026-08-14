using GCCC.BoardGame.Core.Model;

namespace GCCC.BoardGame.Core.Events
{
    public sealed class TurnChanged : GameEvent
    {
        public TurnChanged(PlayerId previousPlayer, PlayerId currentPlayer, bool turnWasPassed)
        {
            PreviousPlayer = previousPlayer;
            CurrentPlayer = currentPlayer;
            TurnWasPassed = turnWasPassed;
        }

        public PlayerId PreviousPlayer { get; }

        public PlayerId CurrentPlayer { get; }

        public bool TurnWasPassed { get; }
    }
}

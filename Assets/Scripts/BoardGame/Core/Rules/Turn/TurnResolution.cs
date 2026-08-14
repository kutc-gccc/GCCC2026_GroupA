using GCCC.BoardGame.Core.Model;

namespace GCCC.BoardGame.Core.Rules.Turn
{
    public readonly struct TurnResolution
    {
        public TurnResolution(PlayerId currentPlayer, bool turnWasPassed, bool isDraw)
        {
            CurrentPlayer = currentPlayer;
            TurnWasPassed = turnWasPassed;
            IsDraw = isDraw;
        }

        public PlayerId CurrentPlayer { get; }

        public bool TurnWasPassed { get; }

        public bool IsDraw { get; }
    }
}

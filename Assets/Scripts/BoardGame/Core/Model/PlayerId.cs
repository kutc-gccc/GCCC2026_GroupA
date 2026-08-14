namespace GCCC.BoardGame.Core.Model
{
    public enum PlayerId
    {
        Player1,
        Player2
    }

    public static class PlayerIdExtensions
    {
        public static PlayerId Other(this PlayerId player)
        {
            return player == PlayerId.Player1 ? PlayerId.Player2 : PlayerId.Player1;
        }
    }
}

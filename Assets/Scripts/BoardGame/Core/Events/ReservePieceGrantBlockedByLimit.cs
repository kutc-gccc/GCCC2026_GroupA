using GCCC.BoardGame.Core.Model;

namespace GCCC.BoardGame.Core.Events
{
    /// <summary>A reserve grant was attempted, but the owner's piece limit prevented it.</summary>
    public sealed class ReservePieceGrantBlockedByLimit : GameEvent
    {
        public ReservePieceGrantBlockedByLimit(
            PlayerId owner, int ownedPieceCount, int maxPiecesPerPlayer)
        {
            Owner = owner;
            OwnedPieceCount = ownedPieceCount;
            MaxPiecesPerPlayer = maxPiecesPerPlayer;
        }

        public PlayerId Owner { get; }
        public int OwnedPieceCount { get; }
        public int MaxPiecesPerPlayer { get; }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace GCCC.BoardGame.Core.Model
{
    public sealed class PlayerState
    {
        public PlayerState(PlayerId player, IEnumerable<ReservePieceState> reservePieces = null)
        {
            Player = player;
            ReservePieces = new ReadOnlyCollection<ReservePieceState>(
                (reservePieces ?? Array.Empty<ReservePieceState>())
                .Select(piece => new ReservePieceState(
                    piece.Id,
                    piece.Owner,
                    piece.CombatPower,
                    piece.MovementProfileId))
                .ToArray());

            if (ReservePieces.Any(piece => piece.Owner != player))
            {
                throw new ArgumentException(
                    "Every reserve piece must belong to the player.",
                    nameof(reservePieces));
            }
        }

        public PlayerId Player { get; }

        public IReadOnlyList<ReservePieceState> ReservePieces { get; }
    }
}

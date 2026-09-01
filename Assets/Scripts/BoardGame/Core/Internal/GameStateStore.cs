using System;
using System.Collections.Generic;
using System.Linq;
using GCCC.BoardGame.Core.Model;

namespace GCCC.BoardGame.Core.Internal
{
    internal sealed class GameStateStore
    {
        private readonly Action stateChanged;
        private readonly Dictionary<PieceId, PieceState> piecesById =
            new Dictionary<PieceId, PieceState>();
        private readonly Dictionary<GridPosition, PieceId> pieceIdsByPosition =
            new Dictionary<GridPosition, PieceId>();
        private readonly Dictionary<PlayerId, List<ReservePieceState>> reservesByPlayer =
            new Dictionary<PlayerId, List<ReservePieceState>>();

        public GameStateStore(Action stateChanged)
        {
            this.stateChanged = stateChanged ?? throw new ArgumentNullException(
                nameof(stateChanged));
            Reset();
        }

        public IEnumerable<PieceState> Pieces => piecesById.Values;

        public IReadOnlyList<ReservePieceState> GetReserves(PlayerId player) =>
            reservesByPlayer[player];

        public void Reset()
        {
            piecesById.Clear();
            pieceIdsByPosition.Clear();
            reservesByPlayer.Clear();
            reservesByPlayer[PlayerId.Player1] = new List<ReservePieceState>();
            reservesByPlayer[PlayerId.Player2] = new List<ReservePieceState>();
            stateChanged();
        }

        public bool TryGetPiece(PieceId id, out PieceState piece) =>
            piecesById.TryGetValue(id, out piece);

        public bool TryGetPiece(GridPosition position, out PieceState piece)
        {
            if (pieceIdsByPosition.TryGetValue(position, out PieceId id))
            {
                piece = piecesById[id];
                return true;
            }

            piece = null;
            return false;
        }

        public bool ContainsPiece(PieceId id) => piecesById.ContainsKey(id);

        public bool IsOccupied(GridPosition position) =>
            pieceIdsByPosition.ContainsKey(position);

        public void AddPiece(PieceState piece)
        {
            piecesById.Add(piece.Id, piece);
            pieceIdsByPosition.Add(piece.Position, piece.Id);
            stateChanged();
        }

        public void RemovePiece(PieceId id)
        {
            if (!piecesById.TryGetValue(id, out PieceState piece))
            {
                return;
            }

            piecesById.Remove(id);
            pieceIdsByPosition.Remove(piece.Position);
            stateChanged();
        }

        public void SetPiece(PieceState piece)
        {
            if (piecesById.TryGetValue(piece.Id, out PieceState previous) &&
                previous.Position != piece.Position)
            {
                pieceIdsByPosition.Remove(previous.Position);
                pieceIdsByPosition[piece.Position] = piece.Id;
            }

            piecesById[piece.Id] = piece;
            stateChanged();
        }

        public int GetBoardPieceCount(PlayerId player) =>
            piecesById.Values.Count(piece => piece.Owner == player);

        public int GetOwnedPieceCount(PlayerId player) =>
            GetBoardPieceCount(player) + reservesByPlayer[player].Count;

        public void AddReserve(ReservePieceState reservePiece)
        {
            reservesByPlayer[reservePiece.Owner].Add(reservePiece);
            stateChanged();
        }

        public void RemoveReserve(PlayerId player, ReservePieceState reservePiece)
        {
            reservesByPlayer[player].Remove(reservePiece);
            stateChanged();
        }

        public bool ReserveBelongsToOpponent(PlayerId player, PieceId reservePieceId) =>
            reservesByPlayer
                .Where(pair => pair.Key != player)
                .SelectMany(pair => pair.Value)
                .Any(piece => piece.Id == reservePieceId);
    }
}

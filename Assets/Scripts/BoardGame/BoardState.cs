using System;
using System.Collections.Generic;
using UnityEngine;

namespace GCCC.BoardGame
{
    public enum PlayerId
    {
        Player1,
        Player2
    }

    public readonly struct BoardPiece
    {
        public BoardPiece(PlayerId owner, int combatPower)
        {
            if (combatPower <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(combatPower),
                    "Combat power must be greater than zero.");
            }

            Owner = owner;
            CombatPower = combatPower;
        }

        public PlayerId Owner { get; }

        public int CombatPower { get; }
    }

    /// <summary>
    /// Owns the complete rules state for the two-player territory game.
    /// Coordinates start at (0, 0) in the bottom-left corner.
    /// </summary>
    public sealed class BoardState
    {
        public const int InitialCombatPower = 1;

        private readonly Dictionary<Vector2Int, BoardPiece> pieces =
            new Dictionary<Vector2Int, BoardPiece>();

        public BoardState(int columns, int rows)
        {
            ValidateDimensions(columns, rows);
            Columns = columns;
            Rows = rows;
            ResetGame();
        }

        public BoardState(
            int columns,
            int rows,
            IEnumerable<KeyValuePair<Vector2Int, PlayerId>> initialPieces,
            PlayerId currentPlayer)
            : this(columns, rows, AddDefaultCombatPower(initialPieces), currentPlayer)
        {
        }

        public BoardState(
            int columns,
            int rows,
            IEnumerable<KeyValuePair<Vector2Int, BoardPiece>> initialPieces,
            PlayerId currentPlayer)
        {
            ValidateDimensions(columns, rows);
            Columns = columns;
            Rows = rows;
            CurrentPlayer = currentPlayer;

            foreach (KeyValuePair<Vector2Int, BoardPiece> piece in initialPieces)
            {
                if (!IsInside(piece.Key) || IsOwnTerritory(piece.Value.Owner, piece.Key))
                {
                    throw new ArgumentException("The supplied position contains an invalid piece placement.",
                        nameof(initialPieces));
                }

                if (!pieces.TryAdd(piece.Key, piece.Value))
                {
                    throw new ArgumentException("The supplied position contains duplicate coordinates.",
                        nameof(initialPieces));
                }

                if (IsOpponentTerritory(piece.Value.Owner, piece.Key))
                {
                    Winner = piece.Value.Owner;
                }
            }

            if (!Winner.HasValue)
            {
                ResolveTurnAvailability();
            }
        }

        public int Columns { get; }

        public int Rows { get; }

        public int PieceCount => pieces.Count;

        public IReadOnlyDictionary<Vector2Int, BoardPiece> Pieces => pieces;

        public PlayerId CurrentPlayer { get; private set; }

        public PlayerId? Winner { get; private set; }

        public bool IsDraw { get; private set; }

        public bool IsGameOver => Winner.HasValue || IsDraw;

        public bool IsInside(Vector2Int position)
        {
            return position.x >= 0 && position.x < Columns &&
                   position.y >= 0 && position.y < Rows;
        }

        public bool IsOwnTerritory(PlayerId player, Vector2Int position)
        {
            if (!IsInside(position))
            {
                return false;
            }

            return player == PlayerId.Player1 ? position.y == 0 : position.y == Rows - 1;
        }

        public bool IsOpponentTerritory(PlayerId player, Vector2Int position)
        {
            if (!IsInside(position))
            {
                return false;
            }

            return player == PlayerId.Player1 ? position.y == Rows - 1 : position.y == 0;
        }

        public bool HasPiece(Vector2Int position)
        {
            return pieces.ContainsKey(position);
        }

        public bool TryGetOwner(Vector2Int position, out PlayerId owner)
        {
            if (pieces.TryGetValue(position, out BoardPiece piece))
            {
                owner = piece.Owner;
                return true;
            }

            owner = default;
            return false;
        }

        public bool TryGetPiece(Vector2Int position, out BoardPiece piece)
        {
            return pieces.TryGetValue(position, out piece);
        }

        public bool TryGetCombatPower(Vector2Int position, out int combatPower)
        {
            if (pieces.TryGetValue(position, out BoardPiece piece))
            {
                combatPower = piece.CombatPower;
                return true;
            }

            combatPower = default;
            return false;
        }

        public int GetPieceCount(PlayerId player)
        {
            int count = 0;
            foreach (BoardPiece piece in pieces.Values)
            {
                if (piece.Owner == player)
                {
                    count++;
                }
            }

            return count;
        }

        public IReadOnlyList<Vector2Int> GetLegalMoves(Vector2Int from)
        {
            if (IsGameOver || !pieces.TryGetValue(from, out BoardPiece piece) ||
                piece.Owner != CurrentPlayer)
            {
                return Array.Empty<Vector2Int>();
            }

            List<Vector2Int> legalMoves = new List<Vector2Int>(8);
            for (int yOffset = -1; yOffset <= 1; yOffset++)
            {
                for (int xOffset = -1; xOffset <= 1; xOffset++)
                {
                    if (xOffset == 0 && yOffset == 0)
                    {
                        continue;
                    }

                    Vector2Int destination = from + new Vector2Int(xOffset, yOffset);
                    if (IsLegalDestination(piece.Owner, destination))
                    {
                        legalMoves.Add(destination);
                    }
                }
            }

            return legalMoves;
        }

        public bool TryMove(Vector2Int from, Vector2Int to)
        {
            if (IsGameOver || !pieces.TryGetValue(from, out BoardPiece movingPiece) ||
                movingPiece.Owner != CurrentPlayer)
            {
                return false;
            }

            Vector2Int difference = to - from;
            if ((difference.x == 0 && difference.y == 0) ||
                Mathf.Abs(difference.x) > 1 || Mathf.Abs(difference.y) > 1 ||
                !IsLegalDestination(movingPiece.Owner, to))
            {
                return false;
            }

            pieces.Remove(from);
            pieces[to] = movingPiece;

            if (IsOpponentTerritory(movingPiece.Owner, to))
            {
                Winner = movingPiece.Owner;
                return true;
            }

            AdvanceTurn(movingPiece.Owner);
            return true;
        }

        public void ResetGame()
        {
            pieces.Clear();
            Winner = null;
            IsDraw = false;
            CurrentPlayer = PlayerId.Player1;

            int player1Row = 1;
            int player2Row = Rows - 2;
            for (int column = 0; column < Columns; column++)
            {
                pieces.Add(new Vector2Int(column, player1Row),
                    new BoardPiece(PlayerId.Player1, InitialCombatPower));
                pieces.Add(new Vector2Int(column, player2Row),
                    new BoardPiece(PlayerId.Player2, InitialCombatPower));
            }
        }

        private bool IsLegalDestination(PlayerId player, Vector2Int destination)
        {
            if (!IsInside(destination) || IsOwnTerritory(player, destination))
            {
                return false;
            }

            return !pieces.TryGetValue(destination, out BoardPiece destinationPiece) ||
                   destinationPiece.Owner != player;
        }

        private bool HasAnyLegalMove(PlayerId player)
        {
            foreach (KeyValuePair<Vector2Int, BoardPiece> piece in pieces)
            {
                if (piece.Value.Owner != player)
                {
                    continue;
                }

                for (int yOffset = -1; yOffset <= 1; yOffset++)
                {
                    for (int xOffset = -1; xOffset <= 1; xOffset++)
                    {
                        if ((xOffset != 0 || yOffset != 0) &&
                            IsLegalDestination(player,
                                piece.Key + new Vector2Int(xOffset, yOffset)))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private void AdvanceTurn(PlayerId playerWhoMoved)
        {
            CurrentPlayer = Other(playerWhoMoved);
            ResolveTurnAvailability();
        }

        private void ResolveTurnAvailability()
        {
            if (HasAnyLegalMove(CurrentPlayer))
            {
                return;
            }

            PlayerId otherPlayer = Other(CurrentPlayer);
            if (HasAnyLegalMove(otherPlayer))
            {
                CurrentPlayer = otherPlayer;
                return;
            }

            IsDraw = true;
        }

        private static PlayerId Other(PlayerId player)
        {
            return player == PlayerId.Player1 ? PlayerId.Player2 : PlayerId.Player1;
        }

        private static IEnumerable<KeyValuePair<Vector2Int, BoardPiece>> AddDefaultCombatPower(
            IEnumerable<KeyValuePair<Vector2Int, PlayerId>> initialPieces)
        {
            foreach (KeyValuePair<Vector2Int, PlayerId> piece in initialPieces)
            {
                yield return new KeyValuePair<Vector2Int, BoardPiece>(
                    piece.Key,
                    new BoardPiece(piece.Value, InitialCombatPower));
            }
        }

        private static void ValidateDimensions(int columns, int rows)
        {
            if (columns <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(columns));
            }

            if (rows < 4)
            {
                throw new ArgumentOutOfRangeException(nameof(rows),
                    "The territory game requires at least four rows.");
            }
        }
    }
}

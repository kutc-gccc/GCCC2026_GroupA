using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GCCC.BoardGame.Core.Commands;

namespace GCCC.BoardGame.Core.Model
{
    public sealed class GameSnapshot
    {
        private readonly IReadOnlyDictionary<PieceId, PieceState> piecesById;
        private readonly IReadOnlyDictionary<GridPosition, PieceState> piecesByPosition;
        private readonly IReadOnlyDictionary<GridPosition, CellDefinition> cellsByPosition;

        // ★ ４．２修正: アクセシビリティを public に変更し、8つ目の引数 legalCommands を追加
        public GameSnapshot(
            int columns,
            int rows,
            IEnumerable<PieceState> pieces,
            IEnumerable<CellDefinition> cells,
            PlayerId currentPlayer,
            PlayerId? winner,
            bool isDraw,
            IReadOnlyList<GameCommand> legalCommands = null)
        {
            Columns = columns;
            Rows = rows;
            CurrentPlayer = currentPlayer;
            Winner = winner;
            IsDraw = isDraw;

            // ★ ４．２追加: 実行可能コマンドのプロパティを初期化（nullの場合は空配列）
            LegalCommands = legalCommands ?? Array.Empty<GameCommand>();

            PieceState[] pieceCopies = pieces
                .Select(piece => new PieceState(piece.Id, piece.Owner, piece.Position,
                    piece.CombatPower, piece.MovementProfileId))
                .ToArray();
            Pieces = new ReadOnlyCollection<PieceState>(pieceCopies);
            piecesById = new ReadOnlyDictionary<PieceId, PieceState>(
                pieceCopies.ToDictionary(piece => piece.Id));
            piecesByPosition = new ReadOnlyDictionary<GridPosition, PieceState>(
                pieceCopies.ToDictionary(piece => piece.Position));

            CellDefinition[] cellCopies = cells
                .Select(cell => new CellDefinition(cell.Position, cell.TerritoryOwner, cell.EffectIds))
                .ToArray();
            Cells = new ReadOnlyCollection<CellDefinition>(cellCopies);
            cellsByPosition = new ReadOnlyDictionary<GridPosition, CellDefinition>(
                cellCopies.ToDictionary(cell => cell.Position));
        }

        public int Columns { get; }

        public int Rows { get; }

        public IReadOnlyList<PieceState> Pieces { get; }

        public IReadOnlyList<CellDefinition> Cells { get; }

        public PlayerId CurrentPlayer { get; }

        public PlayerId? Winner { get; }

        public bool IsDraw { get; }

        // ★ ４．２追加: 実行可能なコマンド一覧を保持するプロパティ
        public IReadOnlyList<GameCommand> LegalCommands { get; }

        public bool IsGameOver => Winner.HasValue || IsDraw;

        public bool IsInside(GridPosition position)
        {
            return position.Column >= 0 && position.Column < Columns &&
                   position.Row >= 0 && position.Row < Rows;
        }

        public bool TryGetPiece(PieceId id, out PieceState piece)
        {
            return piecesById.TryGetValue(id, out piece);
        }

        public bool TryGetPiece(GridPosition position, out PieceState piece)
        {
            return piecesByPosition.TryGetValue(position, out piece);
        }

        public bool TryGetCell(GridPosition position, out CellDefinition cell)
        {
            return cellsByPosition.TryGetValue(position, out cell);
        }

        public int GetPieceCount(PlayerId player)
        {
            return Pieces.Count(piece => piece.Owner == player);
        }
    }
}
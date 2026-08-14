using System;
using System.Collections.Generic;
using System.Linq;
using GCCC.BoardGame.Core.Model;
using UnityEngine;

namespace GCCC.BoardGame.Presentation.Config
{
    [CreateAssetMenu(menuName = "GCCC/Board Game Config", fileName = "BoardGameConfig")]
    public sealed class BoardGameConfig : ScriptableObject
    {
        [SerializeField, Min(1)] private int columns = 6;
        [SerializeField, Min(3)] private int rows = 10;
        [SerializeField] private PlayerId firstPlayer = PlayerId.Player1;
        [SerializeField] private int player1TerritoryRow;
        [SerializeField] private int player2TerritoryRow = 9;
        [SerializeField] private int player1StartRow = 1;
        [SerializeField] private int player2StartRow = 8;
        [SerializeField, Min(1)] private int initialCombatPower = 1;
        [SerializeField] private MoveDirections initialMoveDirections = MoveDirections.All;
        [SerializeField] private List<CellEffectEntry> cellEffects = new List<CellEffectEntry>();

        public GameDefinition CreateDefinition()
        {
            ValidateRows();

            Dictionary<GridPosition, string[]> effectsByPosition = cellEffects
                .ToDictionary(entry => entry.Position, entry => entry.EffectIds);
            List<CellDefinition> cells = new List<CellDefinition>(columns * rows);
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    GridPosition position = new GridPosition(column, row);
                    PlayerId? territoryOwner = row == player1TerritoryRow
                        ? PlayerId.Player1
                        : row == player2TerritoryRow ? PlayerId.Player2 : (PlayerId?)null;
                    effectsByPosition.TryGetValue(position, out string[] effectIds);
                    cells.Add(new CellDefinition(position, territoryOwner, effectIds));
                }
            }

            List<InitialPieceDefinition> pieces = new List<InitialPieceDefinition>(columns * 2);
            int nextPieceId = 1;
            AddStartingRow(pieces, PlayerId.Player1, player1StartRow, ref nextPieceId);
            AddStartingRow(pieces, PlayerId.Player2, player2StartRow, ref nextPieceId);
            return new GameDefinition(columns, rows, cells, pieces, firstPlayer);
        }

        private void AddStartingRow(
            ICollection<InitialPieceDefinition> pieces,
            PlayerId owner,
            int row,
            ref int nextPieceId)
        {
            for (int column = 0; column < columns; column++)
            {
                pieces.Add(new InitialPieceDefinition(
                    new PieceId(nextPieceId++),
                    owner,
                    new GridPosition(column, row),
                    initialCombatPower,
                    initialMoveDirections));
            }
        }

        private void ValidateRows()
        {
            int[] configuredRows =
            {
                player1TerritoryRow,
                player2TerritoryRow,
                player1StartRow,
                player2StartRow
            };
            if (configuredRows.Any(row => row < 0 || row >= rows) ||
                player1TerritoryRow == player2TerritoryRow ||
                player1StartRow == player1TerritoryRow ||
                player2StartRow == player2TerritoryRow ||
                player1StartRow == player2StartRow)
            {
                throw new InvalidOperationException(
                    "Territory and starting rows in BoardGameConfig are invalid.");
            }
        }

        [Serializable]
        private sealed class CellEffectEntry
        {
            [SerializeField] private Vector2Int position;
            [SerializeField] private string[] effectIds = Array.Empty<string>();

            public GridPosition Position => new GridPosition(position.x, position.y);

            public string[] EffectIds => effectIds ?? Array.Empty<string>();
        }
    }
}

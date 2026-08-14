using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace GCCC.BoardGame.Core.Model
{
    public sealed class GameDefinition
    {
        public const int StandardColumns = 6;
        public const int StandardRows = 10;
        public const int StandardInitialCombatPower = 1;

        public GameDefinition(
            int columns,
            int rows,
            IEnumerable<CellDefinition> cells,
            IEnumerable<InitialPieceDefinition> initialPieces,
            PlayerId firstPlayer = PlayerId.Player1)
        {
            if (columns <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(columns));
            }

            if (rows < 4)
            {
                throw new ArgumentOutOfRangeException(nameof(rows));
            }

            Columns = columns;
            Rows = rows;
            Cells = new ReadOnlyCollection<CellDefinition>(cells.ToArray());
            InitialPieces = new ReadOnlyCollection<InitialPieceDefinition>(initialPieces.ToArray());
            FirstPlayer = firstPlayer;
        }

        public int Columns { get; }

        public int Rows { get; }

        public IReadOnlyList<CellDefinition> Cells { get; }

        public IReadOnlyList<InitialPieceDefinition> InitialPieces { get; }

        public PlayerId FirstPlayer { get; }

        public static GameDefinition CreateStandard(int initialCombatPower = StandardInitialCombatPower)
        {
            List<CellDefinition> cells = new List<CellDefinition>(StandardColumns * StandardRows);
            for (int row = 0; row < StandardRows; row++)
            {
                for (int column = 0; column < StandardColumns; column++)
                {
                    PlayerId? territoryOwner = row == 0
                        ? PlayerId.Player1
                        : row == StandardRows - 1
                            ? PlayerId.Player2
                            : (PlayerId?)null;
                    cells.Add(new CellDefinition(new GridPosition(column, row), territoryOwner));
                }
            }

            List<InitialPieceDefinition> pieces = new List<InitialPieceDefinition>(12);
            int nextId = 1;
            for (int column = 0; column < StandardColumns; column++)
            {
                pieces.Add(new InitialPieceDefinition(
                    new PieceId(nextId++), PlayerId.Player1,
                    new GridPosition(column, 1), initialCombatPower, MoveDirections.All));
                pieces.Add(new InitialPieceDefinition(
                    new PieceId(nextId++), PlayerId.Player2,
                    new GridPosition(column, StandardRows - 2), initialCombatPower, MoveDirections.All));
            }

            return new GameDefinition(StandardColumns, StandardRows, cells, pieces);
        }
    }
}

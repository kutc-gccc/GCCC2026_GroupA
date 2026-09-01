using System;
using System.Collections.Generic;
using System.Linq;
using GCCC.BoardGame.Core.Model;

namespace GCCC.BoardGame.Core.Internal
{
    internal sealed class ReserveDeploymentRules
    {
        private readonly int columns;
        private readonly int rows;
        private readonly int depth;
        private readonly Dictionary<PlayerId, int> originRows =
            new Dictionary<PlayerId, int>();
        private readonly Dictionary<PlayerId, int> directions =
            new Dictionary<PlayerId, int>();

        public ReserveDeploymentRules(
            int columns,
            int rows,
            int depth,
            IEnumerable<CellDefinition> cells)
        {
            this.columns = columns;
            this.rows = rows;
            this.depth = depth;
            CellDefinition[] copiedCells = cells.ToArray();

            foreach (PlayerId player in new[] { PlayerId.Player1, PlayerId.Player2 })
            {
                int[] ownRows = copiedCells
                    .Where(cell => cell.TerritoryOwner == player)
                    .Select(cell => cell.Position.Row)
                    .Distinct()
                    .ToArray();
                int[] opponentRows = copiedCells
                    .Where(cell => cell.TerritoryOwner.HasValue &&
                                   cell.TerritoryOwner.Value != player)
                    .Select(cell => cell.Position.Row)
                    .Distinct()
                    .ToArray();
                if (ownRows.Length != 1 || opponentRows.Length != 1)
                {
                    continue;
                }

                int direction = Math.Sign(opponentRows[0] - ownRows[0]);
                if (direction != 0)
                {
                    originRows[player] = ownRows[0];
                    directions[player] = direction;
                }
            }
        }

        public IEnumerable<GridPosition> GetLegalPositions(
            PlayerId player,
            Func<GridPosition, bool> isOccupied,
            Func<PlayerId, GridPosition, bool> isOpponentTerritory)
        {
            if (!originRows.TryGetValue(player, out int originRow) ||
                !directions.TryGetValue(player, out int direction))
            {
                yield break;
            }

            for (int distance = 1; distance <= depth; distance++)
            {
                int row = originRow + direction * distance;
                for (int column = 0; column < columns; column++)
                {
                    GridPosition position = new GridPosition(column, row);
                    if (position.Row >= 0 && position.Row < rows &&
                        !isOccupied(position) &&
                        !isOpponentTerritory(player, position))
                    {
                        yield return position;
                    }
                }
            }
        }
    }
}

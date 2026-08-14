using GCCC.BoardGame.Core.Model;
using UnityEngine;

namespace GCCC.BoardGame.Presentation.Views
{
    public static class BoardGeometry
    {
        public const float CellSpacing = 1f;

        public static Vector3 CellToLocalPosition(
            GridPosition cell,
            int columns,
            int rows)
        {
            float x = (cell.Column - (columns - 1) * 0.5f) * CellSpacing;
            float y = (cell.Row - (rows - 1) * 0.5f) * CellSpacing;
            return new Vector3(x, y, 0f);
        }
    }
}

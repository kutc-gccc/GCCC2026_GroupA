using System.Collections.Generic;
using GCCC.BoardGame.Core.Model;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GCCC.BoardGame.Presentation.Views
{
    public sealed class BoardView : MonoBehaviour
    {
        private const float CellScale = 0.9f;
        private const float TerritoryBorderThickness = 0.09f;

        private static readonly Color BoardColor = new Color32(42, 47, 57, 255);
        private static readonly Color CellColor = new Color32(224, 228, 235, 255);
        private static readonly Color SelectionColor = new Color32(255, 193, 7, 175);
        private static readonly Color LegalMoveColor = new Color32(76, 175, 80, 150);
        private static readonly Color CombatMoveColor = new Color32(255, 152, 0, 175);
        private static readonly Color TerritoryBorderColor = new Color32(255, 255, 255, 235);
        private static readonly Color FusionCandidateColor = new Color32(33, 150, 243, 175);
        private static readonly Color WhileOccupiedEffectColor =
            new Color32(0, 188, 212, 125);
        private static readonly Color PermanentEffectColor =
            new Color32(156, 39, 176, 125);

        private readonly List<SpriteRenderer> moveIndicators = new List<SpriteRenderer>();
        private readonly List<SpriteRenderer> fusionIndicators = new List<SpriteRenderer>();
        private Camera boardCamera;
        private Sprite squareSprite;
        private Transform indicatorsRoot;
        private SpriteRenderer selectionIndicator;
        private int columns;
        private int rows;

        public int GeneratedCellCount { get; private set; }

        public int MoveIndicatorCount => moveIndicators.Count;
        public int FusionIndicatorCount => fusionIndicators.Count;
        public int EffectOverlayCount { get; private set; }

        public void Initialize(Camera camera, Sprite cellSprite, GameSnapshot snapshot)
        {
            boardCamera = camera;
            squareSprite = cellSprite;
            columns = snapshot.Columns;
            rows = snapshot.Rows;
            BuildBoard(snapshot);
        }

        public void ShowSelection(
            GridPosition? selectedCell,
            IReadOnlyList<GridPosition> legalDestinations,
            IReadOnlyList<GridPosition> fusionTargets,
            GameSnapshot snapshot)
        {
            ClearMoveIndicators();
            ClearFusionIndicators();

            if (!selectedCell.HasValue)
            {
                selectionIndicator.enabled = false;
                return;
            }

            selectionIndicator.transform.localPosition =
                BoardGeometry.CellToLocalPosition(selectedCell.Value, columns, rows);
            selectionIndicator.enabled = true;

            foreach (GridPosition destination in legalDestinations)
            {
                bool isCombat = snapshot.TryGetPiece(destination, out _);
                SpriteRenderer indicator = CreateSpriteRenderer(
                    $"{(isCombat ? "Combat" : "Move")} Candidate ({destination.Column}, {destination.Row})",
                    indicatorsRoot,
                    squareSprite,
                    isCombat ? CombatMoveColor : LegalMoveColor,
                    Vector3.one * 0.82f,
                    4);
                indicator.transform.localPosition =
                    BoardGeometry.CellToLocalPosition(destination, columns, rows);
                moveIndicators.Add(indicator);
            }

            foreach (GridPosition target in fusionTargets)
            {
                SpriteRenderer indicator = CreateSpriteRenderer(
                    $"Fusion Candidate ({target.Column}, {target.Row})",
                    indicatorsRoot,
                    squareSprite,
                    FusionCandidateColor,
                    Vector3.one * 0.82f,
                    4);
                indicator.transform.localPosition =
                    BoardGeometry.CellToLocalPosition(target, columns, rows);
                fusionIndicators.Add(indicator);
            }
        }
        public bool TryScreenToCell(Vector2 screenPosition, out GridPosition cell)
        {
            Vector3 worldPosition = boardCamera.ScreenToWorldPoint(screenPosition);
            Vector3 localPosition = transform.InverseTransformPoint(worldPosition);
            int column = Mathf.FloorToInt(
                localPosition.x / BoardGeometry.CellSpacing + columns * 0.5f);
            int row = Mathf.FloorToInt(
                localPosition.y / BoardGeometry.CellSpacing + rows * 0.5f);
            cell = new GridPosition(column, row);
            return column >= 0 && column < columns && row >= 0 && row < rows;
        }

        private void BuildBoard(GameSnapshot snapshot)
        {
            SpriteRenderer background = CreateSpriteRenderer(
                "Board Background", transform, squareSprite, BoardColor,
                new Vector3(columns + 0.16f, rows + 0.16f, 1f), 0);
            background.transform.localPosition = Vector3.zero;

            Transform cellsRoot = new GameObject("Cells").transform;
            cellsRoot.SetParent(transform, false);
            GeneratedCellCount = 0;
            EffectOverlayCount = 0;
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    GridPosition cell = new GridPosition(column, row);
                    SpriteRenderer renderer = CreateSpriteRenderer(
                        $"Cell ({column}, {row})", cellsRoot, squareSprite, CellColor,
                        Vector3.one * CellScale, 1);
                    renderer.transform.localPosition =
                        BoardGeometry.CellToLocalPosition(cell, columns, rows);
                    if (snapshot.TryGetCell(cell, out CellDefinition definition) &&
                        definition.EffectIds.Count > 0 &&
                        snapshot.TryGetCellEffectDefinition(
                            definition.EffectIds[0],
                            out CellEffectDefinition effectDefinition))
                    {
                        Color effectColor =
                            effectDefinition.Lifetime ==
                            CellEffectLifetime.WhileOccupied
                                ? WhileOccupiedEffectColor
                                : PermanentEffectColor;
                        SpriteRenderer effectOverlay = CreateSpriteRenderer(
                            $"Effect ({column}, {row})",
                            cellsRoot,
                            squareSprite,
                            effectColor,
                            Vector3.one * 0.72f,
                            2);
                        effectOverlay.transform.localPosition =
                            BoardGeometry.CellToLocalPosition(
                                cell, columns, rows);
                        EffectOverlayCount++;
                    }

                    GeneratedCellCount++;
                }
            }

            BuildTerritoryBorder("Player 1 Territory Border", 0);
            BuildTerritoryBorder("Player 2 Territory Border", rows - 1);

            indicatorsRoot = new GameObject("Move Indicators").transform;
            indicatorsRoot.SetParent(transform, false);
            selectionIndicator = CreateSpriteRenderer(
                "Selection", transform, squareSprite, SelectionColor,
                Vector3.one * 0.84f, 4);
            selectionIndicator.enabled = false;
        }

        private void BuildTerritoryBorder(string objectName, int row)
        {
            Transform borderRoot = new GameObject(objectName).transform;
            borderRoot.SetParent(transform, false);
            float rowCenterY = BoardGeometry.CellToLocalPosition(
                new GridPosition(0, row), columns, rows).y;
            float halfWidth = columns * BoardGeometry.CellSpacing * 0.5f;
            float halfHeight = BoardGeometry.CellSpacing * 0.5f;

            CreateBorderSegment("Top", borderRoot,
                new Vector3(0f, rowCenterY + halfHeight, 0f),
                new Vector3(columns + TerritoryBorderThickness, TerritoryBorderThickness, 1f));
            CreateBorderSegment("Bottom", borderRoot,
                new Vector3(0f, rowCenterY - halfHeight, 0f),
                new Vector3(columns + TerritoryBorderThickness, TerritoryBorderThickness, 1f));
            CreateBorderSegment("Left", borderRoot,
                new Vector3(-halfWidth, rowCenterY, 0f),
                new Vector3(TerritoryBorderThickness, 1f, 1f));
            CreateBorderSegment("Right", borderRoot,
                new Vector3(halfWidth, rowCenterY, 0f),
                new Vector3(TerritoryBorderThickness, 1f, 1f));
        }

        private void CreateBorderSegment(
            string segmentName,
            Transform parent,
            Vector3 position,
            Vector3 scale)
        {
            SpriteRenderer border = CreateSpriteRenderer(
                segmentName, parent, squareSprite, TerritoryBorderColor, scale, 4);
            border.transform.localPosition = position;
        }

        private void ClearMoveIndicators()
        {
            foreach (SpriteRenderer indicator in moveIndicators)
            {
                DestroyGeneratedObject(indicator.gameObject);
            }

            moveIndicators.Clear();
        }
    
        private void ClearFusionIndicators()
        {
            foreach (SpriteRenderer indicator in fusionIndicators)
            {
                DestroyGeneratedObject(indicator.gameObject);
            }

            fusionIndicators.Clear();
        }
        
        private static SpriteRenderer CreateSpriteRenderer(
            string objectName,
            Transform parent,
            Sprite sprite,
            Color color,
            Vector3 scale,
            int sortingOrder)
        {
            GameObject spriteObject = new GameObject(objectName, typeof(SpriteRenderer));
            spriteObject.transform.SetParent(parent, false);
            spriteObject.transform.localScale = scale;
            SpriteRenderer renderer = spriteObject.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private static void DestroyGeneratedObject(Object generatedObject)
        {
            if (generatedObject == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(generatedObject);
            }
            else
            {
                Object.DestroyImmediate(generatedObject);
            }
        }
    }
}

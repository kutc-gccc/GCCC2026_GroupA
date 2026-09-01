using System.Collections.Generic;
using GCCC.BoardGame.Core.Model;
using GCCC.BoardGame.Presentation;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GCCC.BoardGame.Presentation.Views
{
    public sealed class BoardView : MonoBehaviour, IBoardGameBoardView
    {
        [SerializeField] private Sprite woodBoardSprite;
        [SerializeField] private Sprite boardSprite;

        private const float CellScale = 0.9f;
        private const float TerritoryBorderThickness = 0.09f;
        private const float GridLineThickness = 0.04f;

        private static readonly Color GridLineColor =
            Color.black;

        private static readonly Color BoardColor =
            new Color32(42, 47, 57, 255);

        // 木目を見せるため半透明
        private static readonly Color CellColor =
            new Color32(255, 255, 255, 35);

        private static readonly Color SelectionColor =
            new Color32(255, 193, 7, 175);

        private static readonly Color LegalMoveColor =
            new Color32(76, 175, 80, 150);

        private static readonly Color CombatMoveColor =
            new Color32(255, 152, 0, 175);

        // 陣地枠
        private static readonly Color TerritoryBorderColor =
            new Color32(255, 0, 0, 235);

        private static readonly Color FusionCandidateColor =
            new Color32(33, 150, 243, 175);

        // 凡例（GameHudView）と色を共有するため internal で公開する。
        internal static readonly Color WhileOccupiedEffectColor =
            new Color32(0, 188, 212, 125);

        internal static readonly Color PermanentEffectColor =
            new Color32(156, 39, 176, 125);

        private readonly List<SpriteRenderer> moveIndicators =
            new List<SpriteRenderer>();

        private readonly List<SpriteRenderer> fusionIndicators =
            new List<SpriteRenderer>();

        private Camera boardCamera;
        private Sprite squareSprite;
        private Transform indicatorsRoot;
        private SpriteRenderer selectionIndicator;

        private int columns;
        private int rows;

        public int GeneratedCellCount { get; private set; }

        public int MoveIndicatorCount =>
            moveIndicators.Count;

        public int FusionIndicatorCount =>
            fusionIndicators.Count;

        public int EffectOverlayCount { get; private set; }

        public void Initialize(
            Camera camera,
            Sprite cellSprite,
            GameSnapshot snapshot)
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
            }
            else
            {
                selectionIndicator.transform.localPosition =
                    BoardGeometry.CellToLocalPosition(
                        selectedCell.Value,
                        columns,
                        rows);

                selectionIndicator.enabled = true;
            }

            foreach (GridPosition destination in legalDestinations)
            {
                bool isCombat =
                    snapshot.TryGetPiece(
                        destination,
                        out _);

                SpriteRenderer indicator =
                    CreateSpriteRenderer(
                        isCombat
                            ? $"Combat Candidate ({destination.Column}, {destination.Row})"
                            : $"Move Candidate ({destination.Column}, {destination.Row})",
                        indicatorsRoot,
                        squareSprite,
                        isCombat
                            ? CombatMoveColor
                            : LegalMoveColor,
                        Vector3.one * 0.82f,
                        4);

                indicator.transform.localPosition =
                    BoardGeometry.CellToLocalPosition(
                        destination,
                        columns,
                        rows);

                moveIndicators.Add(indicator);
            }

            foreach (GridPosition target in fusionTargets)
            {
                SpriteRenderer indicator =
                    CreateSpriteRenderer(
                        $"Fusion Candidate ({target.Column}, {target.Row})",
                        indicatorsRoot,
                        squareSprite,
                        FusionCandidateColor,
                        Vector3.one * 0.82f,
                        4);

                indicator.transform.localPosition =
                    BoardGeometry.CellToLocalPosition(
                        target,
                        columns,
                        rows);

                fusionIndicators.Add(indicator);
            }
        }

        public bool TryScreenToCell(
            Vector2 screenPosition,
            out GridPosition cell)
        {
            Vector3 worldPosition =
                boardCamera.ScreenToWorldPoint(
                    screenPosition);

            Vector3 localPosition =
                transform.InverseTransformPoint(
                    worldPosition);

            int column =
                Mathf.FloorToInt(
                    localPosition.x /
                    BoardGeometry.CellSpacing +
                    columns * 0.5f);

            int row =
                Mathf.FloorToInt(
                    localPosition.y /
                    BoardGeometry.CellSpacing +
                    rows * 0.5f);

            cell =
                new GridPosition(
                    column,
                    row);

            return column >= 0 &&
                   column < columns &&
                   row >= 0 &&
                   row < rows;
        }

        private void BuildBoard(
            GameSnapshot snapshot)
        {
            SpriteRenderer background =
                CreateSpriteRenderer(
                    "Board Background",
                    transform,
                    boardSprite != null
                        ? boardSprite
                        : squareSprite,
                    Color.white,
                    Vector3.one,
                    0);

            background.transform.localPosition =
                Vector3.zero;

            if (boardSprite != null)
            {
                float boardWidth =
                    columns *
                    BoardGeometry.CellSpacing +
                    0.16f;

                float boardHeight =
                    rows *
                    BoardGeometry.CellSpacing +
                    0.16f;

                float imageWidth =
                    boardSprite.bounds.size.x;

                float imageHeight =
                    boardSprite.bounds.size.y;

                background.transform.localScale =
                    new Vector3(
                        boardWidth / imageWidth,
                        boardHeight / imageHeight,
                        1f);
            }

            Transform cellsRoot =
                new GameObject("Cells").transform;

            cellsRoot.SetParent(
                transform,
                false);

            GeneratedCellCount = 0;
            EffectOverlayCount = 0;

            for (int row = 0; row < rows; row++)
            {
                for (int column = 0;
                     column < columns;
                     column++)
                {
                    GridPosition cell =
                        new GridPosition(
                            column,
                            row);

                    SpriteRenderer renderer =
                        CreateSpriteRenderer(
                            $"Cell ({column}, {row})",
                            cellsRoot,
                            squareSprite,
                            CellColor,
                            Vector3.one * CellScale,
                            1);

                    renderer.transform.localPosition =
                        BoardGeometry.CellToLocalPosition(
                            cell,
                            columns,
                            rows);

                    // Cell Effect
                    if (snapshot.TryGetCell(
                            cell,
                            out CellDefinition definition) &&
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

                        SpriteRenderer effectOverlay =
                            CreateSpriteRenderer(
                                $"Effect ({column}, {row})",
                                cellsRoot,
                                squareSprite,
                                effectColor,
                                Vector3.one * 0.72f,
                                2);

                        effectOverlay.transform.localPosition =
                            BoardGeometry.CellToLocalPosition(
                                cell,
                                columns,
                                rows);

                        EffectOverlayCount++;
                    }

                    GeneratedCellCount++;
                }
            }

            BuildGridLines();

            BuildTerritoryBorder(
                "Player 1 Territory Border",
                PlayerId.Player1,
                snapshot);

            BuildTerritoryBorder(
                "Player 2 Territory Border",
                PlayerId.Player2,
                snapshot);

            indicatorsRoot =
                new GameObject(
                    "Move Indicators").transform;

            indicatorsRoot.SetParent(
                transform,
                false);

            selectionIndicator =
                CreateSpriteRenderer(
                    "Selection",
                    transform,
                    squareSprite,
                    SelectionColor,
                    Vector3.one * 0.84f,
                    4);

            selectionIndicator.enabled = false;
        }

        private void BuildGridLines()
        {
            Transform gridRoot =
                new GameObject(
                    "Grid Lines").transform;

            gridRoot.SetParent(
                transform,
                false);

            float boardWidth =
                columns *
                BoardGeometry.CellSpacing;

            float boardHeight =
                rows *
                BoardGeometry.CellSpacing;

            // 縦線
            for (int column = 0;
                 column <= columns;
                 column++)
            {
                float x =
                    -boardWidth * 0.5f +
                    column *
                    BoardGeometry.CellSpacing;

                SpriteRenderer line =
                    CreateSpriteRenderer(
                        $"Vertical Grid Line {column}",
                        gridRoot,
                        squareSprite,
                        GridLineColor,
                        new Vector3(
                            GridLineThickness,
                            boardHeight,
                            1f),
                        2);

                line.transform.localPosition =
                    new Vector3(
                        x,
                        0f,
                        0f);
            }

            // 横線
            for (int row = 0;
                 row <= rows;
                 row++)
            {
                float y =
                    -boardHeight * 0.5f +
                    row *
                    BoardGeometry.CellSpacing;

                SpriteRenderer line =
                    CreateSpriteRenderer(
                        $"Horizontal Grid Line {row}",
                        gridRoot,
                        squareSprite,
                        GridLineColor,
                        new Vector3(
                            boardWidth,
                            GridLineThickness,
                            1f),
                        2);

                line.transform.localPosition =
                    new Vector3(
                        0f,
                        y,
                        0f);
            }
        }

        private void BuildTerritoryBorder(
            string objectName,
            PlayerId owner,
            GameSnapshot snapshot)
        {
            Transform borderRoot =
                new GameObject(objectName).transform;

            borderRoot.SetParent(
                transform,
                false);

            foreach (CellDefinition territoryCell in snapshot.Cells)
            {
                if (territoryCell.TerritoryOwner != owner)
                {
                    continue;
                }

                GridPosition position = territoryCell.Position;
                Vector3 center = BoardGeometry.CellToLocalPosition(
                    position, columns, rows);
                float halfCell = BoardGeometry.CellSpacing * 0.5f;
                float segmentLength =
                    BoardGeometry.CellSpacing + TerritoryBorderThickness;

                CreateTerritoryEdgeIfExposed(
                    snapshot,
                    owner,
                    new GridPosition(position.Column, position.Row + 1),
                    $"Top ({position.Column}, {position.Row})",
                    borderRoot,
                    center + Vector3.up * halfCell,
                    new Vector3(segmentLength, TerritoryBorderThickness, 1f));
                CreateTerritoryEdgeIfExposed(
                    snapshot,
                    owner,
                    new GridPosition(position.Column, position.Row - 1),
                    $"Bottom ({position.Column}, {position.Row})",
                    borderRoot,
                    center + Vector3.down * halfCell,
                    new Vector3(segmentLength, TerritoryBorderThickness, 1f));
                CreateTerritoryEdgeIfExposed(
                    snapshot,
                    owner,
                    new GridPosition(position.Column - 1, position.Row),
                    $"Left ({position.Column}, {position.Row})",
                    borderRoot,
                    center + Vector3.left * halfCell,
                    new Vector3(TerritoryBorderThickness, segmentLength, 1f));
                CreateTerritoryEdgeIfExposed(
                    snapshot,
                    owner,
                    new GridPosition(position.Column + 1, position.Row),
                    $"Right ({position.Column}, {position.Row})",
                    borderRoot,
                    center + Vector3.right * halfCell,
                    new Vector3(TerritoryBorderThickness, segmentLength, 1f));
            }
        }

        private void CreateTerritoryEdgeIfExposed(
            GameSnapshot snapshot,
            PlayerId owner,
            GridPosition adjacentPosition,
            string segmentName,
            Transform parent,
            Vector3 position,
            Vector3 scale)
        {
            if (snapshot.TryGetCell(
                    adjacentPosition,
                    out CellDefinition adjacentCell) &&
                adjacentCell.TerritoryOwner == owner)
            {
                return;
            }

            CreateBorderSegment(segmentName, parent, position, scale);
        }

        private void CreateBorderSegment(
            string segmentName,
            Transform parent,
            Vector3 position,
            Vector3 scale)
        {
            SpriteRenderer border =
                CreateSpriteRenderer(
                    segmentName,
                    parent,
                    squareSprite,
                    TerritoryBorderColor,
                    scale,
                    4);

            border.transform.localPosition =
                position;
        }

        private void ClearMoveIndicators()
        {
            foreach (SpriteRenderer indicator
                     in moveIndicators)
            {
                DestroyGeneratedObject(
                    indicator.gameObject);
            }

            moveIndicators.Clear();
        }

        private void ClearFusionIndicators()
        {
            foreach (SpriteRenderer indicator
                     in fusionIndicators)
            {
                DestroyGeneratedObject(
                    indicator.gameObject);
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
            GameObject spriteObject =
                new GameObject(
                    objectName,
                    typeof(SpriteRenderer));

            spriteObject.transform.SetParent(
                parent,
                false);

            spriteObject.transform.localScale =
                scale;

            SpriteRenderer renderer =
                spriteObject.GetComponent<SpriteRenderer>();

            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;

            return renderer;
        }

        private static void DestroyGeneratedObject(
            Object generatedObject)
        {
            if (generatedObject == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(
                    generatedObject);
            }
            else
            {
                Object.DestroyImmediate(
                    generatedObject);
            }
        }
    }
}

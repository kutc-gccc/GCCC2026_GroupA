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
        private const float TerritoryMarkerScale = 0.34f;
        private const float GridLineThickness = 0.04f;

        private static readonly Color GridLineColor =
            Color.black;

        private static readonly Color BoardColor =
            new Color32(42, 47, 57, 255);

        // 木目を見せるため半透明
        private static readonly Color CellColor =
            new Color32(255, 255, 255, 35);

        // 選択中は塗りつぶしではなく枠線で示す。
        private static readonly Color SelectionColor =
            new Color32(224, 162, 27, 235);

        // 駒の濃緑と紛れないよう、移動候補は緑をやめて白い点にする。
        private static readonly Color LegalMoveColor =
            new Color32(255, 255, 255, 225);

        // 陣地から外した赤を、危険の意味が合う戦闘可能へ回す。
        private static readonly Color CombatMoveColor =
            new Color32(224, 58, 47, 235);

        // 陣地と盤面の境に引く区切り線。純赤はやめて無彩色にする。
        private static readonly Color TerritoryBorderColor =
            new Color32(34, 38, 44, 235);

        // 陣地は「入れない／勝てる」別空間なので、通常セルと違う無彩色の地形にする。
        private static readonly Color TerritoryCellColor =
            new Color32(90, 96, 105, 215);

        // 陣地の所有者を示す ▲▼。駒と同じ記号を使う。
        private static readonly Color TerritoryMarkerColor =
            new Color32(237, 233, 225, 205);

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
        private Sprite dotSprite;
        private Sprite frameSprite;
        private Sprite markerSprite;
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
            RuntimeSpriteFactory sprites,
            GameSnapshot snapshot)
        {
            boardCamera = camera;
            squareSprite = sprites.SquareSprite;
            dotSprite = sprites.CircleSprite;
            frameSprite = sprites.FrameSprite;
            markerSprite = sprites.TriangleSprite;

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

                // 敵駒は枠で囲み、空きマスは点で示す。形で役割を分ける。
                SpriteRenderer indicator =
                    CreateSpriteRenderer(
                        isCombat
                            ? $"Combat Candidate ({destination.Column}, {destination.Row})"
                            : $"Move Candidate ({destination.Column}, {destination.Row})",
                        indicatorsRoot,
                        isCombat
                            ? frameSprite
                            : dotSprite,
                        isCombat
                            ? CombatMoveColor
                            : LegalMoveColor,
                        isCombat
                            ? Vector3.one * 0.94f
                            : Vector3.one * 0.28f,
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
                        frameSprite,
                        FusionCandidateColor,
                        Vector3.one * 0.94f,
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

                    snapshot.TryGetCell(cell, out CellDefinition cellDefinition);

                    PlayerId? territoryOwner =
                        cellDefinition?.TerritoryOwner;

                    SpriteRenderer renderer =
                        CreateSpriteRenderer(
                            $"Cell ({column}, {row})",
                            cellsRoot,
                            squareSprite,
                            territoryOwner.HasValue
                                ? TerritoryCellColor
                                : CellColor,
                            Vector3.one * CellScale,
                            1);

                    renderer.transform.localPosition =
                        BoardGeometry.CellToLocalPosition(
                            cell,
                            columns,
                            rows);

                    if (territoryOwner.HasValue)
                    {
                        CreateTerritoryMarker(
                            cell,
                            territoryOwner.Value,
                            cellsRoot,
                            renderer.transform.localPosition);
                    }

                    // Cell Effect
                    if (cellDefinition != null &&
                        cellDefinition.EffectIds.Count > 0 &&
                        snapshot.TryGetCellEffectDefinition(
                            cellDefinition.EffectIds[0],
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
                                Vector3.one * CellScale,
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
                    frameSprite,
                    SelectionColor,
                    Vector3.one * 0.98f,
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

        /// <summary>
        /// 陣地のマスへ所有者の ▲▼ を敷く。駒と同じ記号を使うことで、
        /// 色を使わずに「誰の陣地か」を盤上で示す。
        /// </summary>
        private void CreateTerritoryMarker(
            GridPosition cell,
            PlayerId owner,
            Transform parent,
            Vector3 localPosition)
        {
            // プレイヤー1が上向き、プレイヤー2は縦を反転して下向きにする。
            float verticalDirection =
                owner == PlayerId.Player1
                    ? 1f
                    : -1f;

            SpriteRenderer marker =
                CreateSpriteRenderer(
                    $"Territory Marker ({cell.Column}, {cell.Row})",
                    parent,
                    markerSprite,
                    TerritoryMarkerColor,
                    new Vector3(
                        TerritoryMarkerScale,
                        TerritoryMarkerScale * verticalDirection,
                        1f),
                    2);

            marker.transform.localPosition = localPosition;
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

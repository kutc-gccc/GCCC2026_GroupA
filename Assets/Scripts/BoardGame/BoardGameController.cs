using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace GCCC.BoardGame
{
    public sealed class BoardGameController : MonoBehaviour
    {
        public const int ColumnCount = 6;
        public const int RowCount = 10;

        private const float CellSpacing = 1f;
        private const float CellScale = 0.9f;
        private const float PieceScale = 0.72f;
        private const float TerritoryBorderThickness = 0.09f;

        private static readonly Color BoardColor = new Color32(42, 47, 57, 255);
        private static readonly Color CellColor = new Color32(224, 228, 235, 255);
        private static readonly Color Player1Color = new Color32(42, 91, 153, 255);
        private static readonly Color Player2Color = new Color32(196, 61, 54, 255);
        private static readonly Color SelectionColor = new Color32(255, 193, 7, 175);
        private static readonly Color LegalMoveColor = new Color32(76, 175, 80, 150);
        private static readonly Color CombatMoveColor = new Color32(255, 152, 0, 175);
        private static readonly Color TerritoryBorderColor = new Color32(255, 255, 255, 235);

        private readonly Dictionary<Vector2Int, SpriteRenderer> pieceViews =
            new Dictionary<Vector2Int, SpriteRenderer>();
        private readonly List<SpriteRenderer> moveIndicators = new List<SpriteRenderer>();

        private BoardState state;
        private Camera boardCamera;
        private Transform boardRoot;
        private Transform piecesRoot;
        private Transform indicatorsRoot;
        private Sprite squareSprite;
        private Sprite circleSprite;
        private Texture2D squareTexture;
        private Texture2D circleTexture;
        private SpriteRenderer selectionIndicator;
        private RectTransform resetButtonRect;
        private Text statusLabel;
        private GameObject createdEventSystem;
        private Camera createdCamera;
        private Vector2Int? selectedCell;
        private int generatedCellCount;

        public BoardState State => state;

        public Vector2Int? SelectedCell => selectedCell;

        public int GeneratedCellCount => generatedCellCount;

        public int PieceViewCount => pieceViews.Count;

        public int MoveIndicatorCount => moveIndicators.Count;

        public string StatusText => statusLabel != null ? statusLabel.text : string.Empty;

        private void Awake()
        {
            state = new BoardState(ColumnCount, RowCount);
            ConfigureCamera();
            CreateRuntimeSprites();
            BuildBoard();
            BuildUi();
            CreateAllPieceViews();
            UpdateStatusLabel();
        }

        private void Update()
        {
            if (!TryGetPointerPress(out Vector2 screenPosition) || IsOverResetButton(screenPosition))
            {
                return;
            }

            if (TryScreenToCell(screenPosition, out Vector2Int cell))
            {
                HandleCellClick(cell);
            }
        }

        public void HandleCellClick(Vector2Int cell)
        {
            if (!state.IsInside(cell) || state.IsGameOver)
            {
                return;
            }

            if (state.TryGetOwner(cell, out PlayerId owner) && owner == state.CurrentPlayer)
            {
                selectedCell = selectedCell == cell ? null : cell;
                UpdateSelectionAndMoves();
                return;
            }

            if (!selectedCell.HasValue)
            {
                return;
            }

            Vector2Int from = selectedCell.Value;
            if (!IsLegalSelectedDestination(cell))
            {
                return;
            }

            if (!state.TryMove(from, cell, out MoveResult result))
            {
                return;
            }

            ApplyMoveResultToViews(from, cell, result);
            selectedCell = null;
            UpdateSelectionAndMoves();
            UpdateStatusLabel();
        }

        public void ResetGame()
        {
            state.ResetGame();
            selectedCell = null;
            UpdateSelectionAndMoves();

            foreach (SpriteRenderer pieceView in pieceViews.Values)
            {
                DestroyGeneratedObject(pieceView.gameObject);
            }

            pieceViews.Clear();
            CreateAllPieceViews();
            UpdateStatusLabel();
        }

        private void ConfigureCamera()
        {
            boardCamera = Camera.main;
            if (boardCamera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera", typeof(Camera));
                cameraObject.tag = "MainCamera";
                createdCamera = cameraObject.GetComponent<Camera>();
                boardCamera = createdCamera;
            }

            boardCamera.transform.SetPositionAndRotation(new Vector3(0f, 0f, -10f), Quaternion.identity);
            boardCamera.orthographic = true;
            boardCamera.clearFlags = CameraClearFlags.SolidColor;
            boardCamera.backgroundColor = new Color32(24, 27, 34, 255);

            float verticalSize = (RowCount + 2f) * 0.5f;
            float horizontalSize = (ColumnCount + 1f) * 0.5f /
                                   Mathf.Max(boardCamera.aspect, 0.01f);
            boardCamera.orthographicSize = Mathf.Max(verticalSize, horizontalSize);
        }

        private void CreateRuntimeSprites()
        {
            squareTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                name = "Board Square Texture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            squareTexture.SetPixel(0, 0, Color.white);
            squareTexture.Apply();
            squareSprite = Sprite.Create(squareTexture, new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f), 1f);
            squareSprite.name = "Board Square Sprite";
            squareSprite.hideFlags = HideFlags.DontSave;

            const int resolution = 64;
            circleTexture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false)
            {
                name = "Board Piece Texture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };

            Color[] pixels = new Color[resolution * resolution];
            Vector2 center = new Vector2((resolution - 1) * 0.5f, (resolution - 1) * 0.5f);
            float radius = resolution * 0.46f;
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(radius - distance + 0.5f);
                    pixels[y * resolution + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            circleTexture.SetPixels(pixels);
            circleTexture.Apply();
            circleSprite = Sprite.Create(circleTexture,
                new Rect(0f, 0f, resolution, resolution),
                new Vector2(0.5f, 0.5f), resolution);
            circleSprite.name = "Board Piece Sprite";
            circleSprite.hideFlags = HideFlags.DontSave;
        }

        private void BuildBoard()
        {
            boardRoot = new GameObject("Board").transform;
            boardRoot.SetParent(transform, false);

            SpriteRenderer background = CreateSpriteRenderer(
                "Board Background",
                boardRoot,
                squareSprite,
                BoardColor,
                new Vector3(ColumnCount + 0.16f, RowCount + 0.16f, 1f),
                0);
            background.transform.localPosition = Vector3.zero;

            Transform cellsRoot = new GameObject("Cells").transform;
            cellsRoot.SetParent(boardRoot, false);

            generatedCellCount = 0;
            for (int row = 0; row < RowCount; row++)
            {
                for (int column = 0; column < ColumnCount; column++)
                {
                    Vector2Int cell = new Vector2Int(column, row);
                    SpriteRenderer cellRenderer = CreateSpriteRenderer(
                        $"Cell ({column}, {row})",
                        cellsRoot,
                        squareSprite,
                        CellColor,
                        Vector3.one * CellScale,
                        1);
                    cellRenderer.transform.localPosition = CellToLocalPosition(cell);
                    generatedCellCount++;
                }
            }

            BuildTerritoryBorder("Player 1 Territory Border", 0);
            BuildTerritoryBorder("Player 2 Territory Border", RowCount - 1);

            indicatorsRoot = new GameObject("Move Indicators").transform;
            indicatorsRoot.SetParent(boardRoot, false);

            piecesRoot = new GameObject("Pieces").transform;
            piecesRoot.SetParent(boardRoot, false);

            selectionIndicator = CreateSpriteRenderer(
                "Selection",
                boardRoot,
                squareSprite,
                SelectionColor,
                Vector3.one * 0.84f,
                2);
            selectionIndicator.enabled = false;
        }

        private void BuildTerritoryBorder(string objectName, int row)
        {
            Transform borderRoot = new GameObject(objectName).transform;
            borderRoot.SetParent(boardRoot, false);

            float rowCenterY = CellToLocalPosition(new Vector2Int(0, row)).y;
            float halfWidth = ColumnCount * CellSpacing * 0.5f;
            float halfHeight = CellSpacing * 0.5f;

            CreateBorderSegment("Top", borderRoot,
                new Vector3(0f, rowCenterY + halfHeight, 0f),
                new Vector3(ColumnCount + TerritoryBorderThickness,
                    TerritoryBorderThickness, 1f));
            CreateBorderSegment("Bottom", borderRoot,
                new Vector3(0f, rowCenterY - halfHeight, 0f),
                new Vector3(ColumnCount + TerritoryBorderThickness,
                    TerritoryBorderThickness, 1f));
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
                segmentName,
                parent,
                squareSprite,
                TerritoryBorderColor,
                scale,
                2);
            border.transform.localPosition = position;
        }

        private void BuildUi()
        {
            GameObject canvasObject = new GameObject(
                "Board UI",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            Font font = CreateUiFont();
            statusLabel = CreateUiText(
                "Turn Status",
                canvasObject.transform,
                font,
                28,
                TextAnchor.MiddleLeft,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(24f, -24f),
                new Vector2(520f, 64f));

            Text player2TerritoryLabel = CreateUiText(
                "Player 2 Territory Label",
                canvasObject.transform,
                font,
                22,
                TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -12f),
                new Vector2(520f, 42f));
            player2TerritoryLabel.text = "プレイヤー2の陣地";

            Text player1TerritoryLabel = CreateUiText(
                "Player 1 Territory Label",
                canvasObject.transform,
                font,
                22,
                TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 12f),
                new Vector2(520f, 42f));
            player1TerritoryLabel.text = "プレイヤー1の陣地";

            GameObject buttonObject = new GameObject(
                "Reset Button",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            buttonObject.transform.SetParent(canvasObject.transform, false);

            resetButtonRect = buttonObject.GetComponent<RectTransform>();
            resetButtonRect.anchorMin = Vector2.one;
            resetButtonRect.anchorMax = Vector2.one;
            resetButtonRect.pivot = Vector2.one;
            resetButtonRect.sizeDelta = new Vector2(180f, 64f);
            resetButtonRect.anchoredPosition = new Vector2(-24f, -24f);

            Image buttonImage = buttonObject.GetComponent<Image>();
            buttonImage.color = new Color32(235, 238, 244, 255);

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = buttonImage;
            button.onClick.AddListener(ResetGame);

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color32(222, 228, 239, 255);
            colors.pressedColor = new Color32(198, 207, 224, 255);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;

            Text resetLabel = CreateUiText(
                "Label",
                buttonObject.transform,
                font,
                24,
                TextAnchor.MiddleCenter,
                Vector2.zero,
                Vector2.zero,
                Vector2.zero,
                Vector2.zero);
            RectTransform labelRect = resetLabel.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            resetLabel.text = "リセット";
            resetLabel.color = new Color32(35, 41, 52, 255);

            if (EventSystem.current == null)
            {
                createdEventSystem = new GameObject("EventSystem", typeof(EventSystem));
                InputSystemUIInputModule inputModule =
                    createdEventSystem.AddComponent<InputSystemUIInputModule>();
                inputModule.AssignDefaultActions();
            }
        }

        private static Text CreateUiText(
            string objectName,
            Transform parent,
            Font font,
            int fontSize,
            TextAnchor alignment,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            GameObject labelObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            labelObject.transform.SetParent(parent, false);

            RectTransform rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Text label = labelObject.GetComponent<Text>();
            label.font = font;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = Color.white;
            label.raycastTarget = false;
            return label;
        }

        private static Font CreateUiFont()
        {
            string[] preferredFonts =
            {
                "Yu Gothic UI",
                "Meiryo UI",
                "Hiragino Sans",
                "Noto Sans CJK JP",
                "Arial"
            };

            Font font = Font.CreateDynamicFontFromOSFont(preferredFonts, 24);
            return font != null
                ? font
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private SpriteRenderer CreateSpriteRenderer(
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

            SpriteRenderer spriteRenderer = spriteObject.GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = color;
            spriteRenderer.sortingOrder = sortingOrder;
            return spriteRenderer;
        }

        private void CreateAllPieceViews()
        {
            foreach (KeyValuePair<Vector2Int, BoardPiece> piece in state.Pieces)
            {
                CreatePieceView(piece.Key, piece.Value);
            }
        }

        private void CreatePieceView(Vector2Int cell, BoardPiece pieceState)
        {
            SpriteRenderer piece = CreateSpriteRenderer(
                $"{pieceState.Owner} Piece ({cell.x}, {cell.y})",
                piecesRoot,
                circleSprite,
                pieceState.Owner == PlayerId.Player1 ? Player1Color : Player2Color,
                Vector3.one * PieceScale,
                3);
            piece.transform.localPosition = CellToLocalPosition(cell);
            CreateCombatPowerLabel(piece.transform, pieceState.CombatPower);
            pieceViews.Add(cell, piece);
        }

        private void CreateCombatPowerLabel(Transform pieceTransform, int combatPower)
        {
            GameObject labelObject = new GameObject("Combat Power", typeof(TextMesh));
            labelObject.transform.SetParent(pieceTransform, false);
            labelObject.transform.localPosition = new Vector3(0f, 0f, -0.01f);
            labelObject.transform.localScale = Vector3.one / PieceScale;

            TextMesh label = labelObject.GetComponent<TextMesh>();
            label.text = combatPower.ToString();
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 64;
            label.characterSize = 0.1f;
            label.color = Color.white;

            MeshRenderer labelRenderer = labelObject.GetComponent<MeshRenderer>();
            labelRenderer.sortingOrder = 4;
        }

        private void MovePieceView(Vector2Int from, Vector2Int to)
        {
            if (!pieceViews.TryGetValue(from, out SpriteRenderer piece))
            {
                return;
            }

            pieceViews.Remove(from);
            pieceViews.Add(to, piece);
            state.TryGetOwner(to, out PlayerId owner);
            piece.name = $"{owner} Piece ({to.x}, {to.y})";
            piece.transform.localPosition = CellToLocalPosition(to);
        }

        private void ApplyMoveResultToViews(Vector2Int from, Vector2Int to, MoveResult result)
        {
            if (!result.CombatOccurred)
            {
                MovePieceView(from, to);
                return;
            }

            if (result.DefenderDestroyed)
            {
                DestroyPieceView(to);
            }

            if (result.AttackerDestroyed)
            {
                DestroyPieceView(from);
            }
            else if (result.AttackerMoved)
            {
                MovePieceView(from, to);
            }

            UpdateCombatPowerLabel(to);
        }

        private void DestroyPieceView(Vector2Int cell)
        {
            if (!pieceViews.TryGetValue(cell, out SpriteRenderer piece))
            {
                return;
            }

            pieceViews.Remove(cell);
            DestroyGeneratedObject(piece.gameObject);
        }

        private void UpdateCombatPowerLabel(Vector2Int cell)
        {
            if (!pieceViews.TryGetValue(cell, out SpriteRenderer pieceView) ||
                !state.TryGetCombatPower(cell, out int combatPower))
            {
                return;
            }

            Transform labelTransform = pieceView.transform.Find("Combat Power");
            if (labelTransform != null && labelTransform.TryGetComponent(out TextMesh label))
            {
                label.text = combatPower.ToString();
            }
        }

        private void UpdateSelectionAndMoves()
        {
            ClearMoveIndicators();

            if (!selectedCell.HasValue)
            {
                selectionIndicator.enabled = false;
                return;
            }

            Vector2Int selected = selectedCell.Value;
            selectionIndicator.transform.localPosition = CellToLocalPosition(selected);
            selectionIndicator.enabled = true;

            IReadOnlyList<Vector2Int> legalMoves = state.GetLegalMoves(selected);
            foreach (Vector2Int move in legalMoves)
            {
                bool isCombat = state.HasPiece(move);
                SpriteRenderer indicator = CreateSpriteRenderer(
                    $"{(isCombat ? "Combat" : "Move")} Candidate ({move.x}, {move.y})",
                    indicatorsRoot,
                    squareSprite,
                    isCombat ? CombatMoveColor : LegalMoveColor,
                    Vector3.one * 0.82f,
                    2);
                indicator.transform.localPosition = CellToLocalPosition(move);
                moveIndicators.Add(indicator);
            }
        }

        private void ClearMoveIndicators()
        {
            foreach (SpriteRenderer indicator in moveIndicators)
            {
                DestroyGeneratedObject(indicator.gameObject);
            }

            moveIndicators.Clear();
        }

        private bool IsLegalSelectedDestination(Vector2Int destination)
        {
            IReadOnlyList<Vector2Int> legalMoves = state.GetLegalMoves(selectedCell.Value);
            foreach (Vector2Int legalMove in legalMoves)
            {
                if (legalMove == destination)
                {
                    return true;
                }
            }

            return false;
        }

        private void UpdateStatusLabel()
        {
            if (state.Winner.HasValue)
            {
                statusLabel.text = state.Winner.Value == PlayerId.Player1
                    ? "プレイヤー1（青）の勝利"
                    : "プレイヤー2（赤）の勝利";
                return;
            }

            if (state.IsDraw)
            {
                statusLabel.text = "引き分け";
                return;
            }

            statusLabel.text = state.CurrentPlayer == PlayerId.Player1
                ? "プレイヤー1（青）のターン"
                : "プレイヤー2（赤）のターン";
        }

        private static bool TryGetPointerPress(out Vector2 screenPosition)
        {
            if (Touchscreen.current != null &&
                Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
                return true;
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                screenPosition = Mouse.current.position.ReadValue();
                return true;
            }

            screenPosition = default;
            return false;
        }

        private bool IsOverResetButton(Vector2 screenPosition)
        {
            return resetButtonRect != null &&
                   RectTransformUtility.RectangleContainsScreenPoint(
                       resetButtonRect, screenPosition);
        }

        private bool TryScreenToCell(Vector2 screenPosition, out Vector2Int cell)
        {
            Vector3 worldPosition = boardCamera.ScreenToWorldPoint(screenPosition);
            Vector3 localPosition = boardRoot.InverseTransformPoint(worldPosition);

            int column = Mathf.FloorToInt(
                localPosition.x / CellSpacing + ColumnCount * 0.5f);
            int row = Mathf.FloorToInt(
                localPosition.y / CellSpacing + RowCount * 0.5f);
            cell = new Vector2Int(column, row);
            return state.IsInside(cell);
        }

        private static Vector3 CellToLocalPosition(Vector2Int cell)
        {
            float x = (cell.x - (ColumnCount - 1) * 0.5f) * CellSpacing;
            float y = (cell.y - (RowCount - 1) * 0.5f) * CellSpacing;
            return new Vector3(x, y, 0f);
        }

        private static void DestroyGeneratedObject(Object generatedObject)
        {
            if (generatedObject == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(generatedObject);
            }
            else
            {
                DestroyImmediate(generatedObject);
            }
        }

        private void OnDestroy()
        {
            DestroyGeneratedObject(squareSprite);
            DestroyGeneratedObject(circleSprite);
            DestroyGeneratedObject(squareTexture);
            DestroyGeneratedObject(circleTexture);

            if (createdEventSystem != null)
            {
                DestroyGeneratedObject(createdEventSystem);
            }

            if (createdCamera != null)
            {
                DestroyGeneratedObject(createdCamera.gameObject);
            }
        }
    }
}

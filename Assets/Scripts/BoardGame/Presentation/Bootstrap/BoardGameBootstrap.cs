using GCCC.BoardGame.Core;
using GCCC.BoardGame.Core.Model;
using GCCC.BoardGame.Presentation.Config;
using GCCC.BoardGame.Presentation.Input;
using GCCC.BoardGame.Presentation.Views;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GCCC.BoardGame.Presentation.Bootstrap
{
    public sealed class BoardGameBootstrap : MonoBehaviour
    {
        [SerializeField] private BoardGameConfig config;

        [SerializeField] private BoardView boardViewPrefab;
        [SerializeField] private PieceViewManager pieceViewsPrefab;
        [SerializeField] private GameHudView hudViewPrefab;

        // プレイヤーごとの駒
        [SerializeField] private Sprite player1PieceSprite;
        [SerializeField] private Sprite player2PieceSprite;

        // 大理石背景
        [SerializeField] private Sprite marbleBackgroundSprite;

        private RuntimeSpriteFactory spriteFactory;
        private GameHudView hudView;

        public GameSession Session { get; private set; }

        public GameCoordinator Coordinator { get; private set; }

        public BoardView BoardView { get; private set; }

        public PieceViewManager PieceViews { get; private set; }

        public GameSnapshot Snapshot => Session?.Snapshot;

        public GridPosition? SelectedCell => Coordinator?.SelectedCell;

        public int GeneratedCellCount =>
            BoardView != null ? BoardView.GeneratedCellCount : 0;

        public int PieceViewCount =>
            PieceViews != null ? PieceViews.PieceViewCount : 0;

        public int MoveIndicatorCount =>
            BoardView != null ? BoardView.MoveIndicatorCount : 0;

        public string StatusText =>
            hudView != null ? hudView.StatusText : string.Empty;

        private void Awake()
        {
            GameDefinition definition = config != null
                ? config.CreateDefinition()
                : GameDefinition.CreateStandard();

            Session = new GameSession(definition);

            spriteFactory = new RuntimeSpriteFactory();

            Camera boardCamera =
                ConfigureCamera(definition.Columns, definition.Rows);

            // 盤
            BoardView = CreatePresentationComponent(
                boardViewPrefab,
                "Board View");

            BoardView.Initialize(
                boardCamera,
                spriteFactory.SquareSprite,
                Session.Snapshot);

            // 駒
            PieceViews = CreatePresentationComponent(
                pieceViewsPrefab,
                "Piece Views");

            PieceViews.Initialize(
                player1PieceSprite,
                player2PieceSprite,
                Session.Snapshot);

            // HUD
            hudView = CreatePresentationComponent(
                hudViewPrefab,
                "Game HUD");

            hudView.Initialize();

            // ゲーム進行
            Coordinator = new GameCoordinator(
                Session,
                BoardView,
                PieceViews,
                hudView);

            hudView.ResetRequested += Coordinator.Reset;

            // 入力
            GameObject inputObject =
                new GameObject("Board Input");

            inputObject.transform.SetParent(transform, false);

            BoardInputController input =
                inputObject.AddComponent<BoardInputController>();

            input.Initialize(
                BoardView,
                hudView,
                Coordinator);

            // 大理石背景
            CreateMarbleBackground(
                definition.Columns,
                definition.Rows);
        }

        private T CreatePresentationComponent<T>(
            T prefab,
            string instanceName)
            where T : Component
        {
            if (prefab != null)
            {
                T instance = Instantiate(prefab, transform);
                instance.gameObject.name = instanceName;
                return instance;
            }

            GameObject instanceObject =
                new GameObject(instanceName);

            instanceObject.transform.SetParent(
                transform,
                false);

            return instanceObject.AddComponent<T>();
        }

        public void HandleCellClick(GridPosition cell)
        {
            Coordinator.HandleCellClick(cell);
        }

        public void ResetGame()
        {
            Coordinator.Reset();
        }

        private static Camera ConfigureCamera(
            int columns,
            int rows)
        {
            Camera boardCamera = Camera.main;

            if (boardCamera == null)
            {
                GameObject cameraObject =
                    new GameObject(
                        "Main Camera",
                        typeof(Camera));

                cameraObject.tag = "MainCamera";

                boardCamera =
                    cameraObject.GetComponent<Camera>();
            }

            boardCamera.transform.SetPositionAndRotation(
                new Vector3(0f, 0f, -10f),
                Quaternion.identity);

            boardCamera.orthographic = true;
            boardCamera.clearFlags =
                CameraClearFlags.SolidColor;

            boardCamera.backgroundColor =
                new Color32(24, 27, 34, 255);

            float verticalSize =
                (rows + 2f) * 0.5f;

            float horizontalSize =
                (columns + 1f) * 0.5f /
                Mathf.Max(boardCamera.aspect, 0.01f);

            boardCamera.orthographicSize =
                Mathf.Max(
                    verticalSize,
                    horizontalSize);

            return boardCamera;
        }

        private void CreateMarbleBackground(
            int columns,
            int rows)
        {
            if (marbleBackgroundSprite == null)
            {
                Debug.LogWarning(
                    "Marble Background Sprite が設定されていません。");
                return;
            }

            GameObject obj =
                new GameObject("Marble Background");

            obj.transform.SetParent(
                transform,
                false);

            SpriteRenderer sr =
                obj.AddComponent<SpriteRenderer>();

            sr.sprite = marbleBackgroundSprite;
            sr.color = Color.white;

            // 盤より後ろ
            sr.sortingOrder = -10;

            Camera camera = Camera.main;

            // カメラが見ている範囲を取得
            float visibleHeight =
                camera.orthographicSize * 2f;

            float visibleWidth =
                visibleHeight * camera.aspect;

            // 画面端より少し大きくする
            float targetWidth =
                Mathf.Max(columns + 8f, visibleWidth + 4f);

            float targetHeight =
                Mathf.Max(rows + 8f, visibleHeight + 4f);

            float spriteWidth =
                marbleBackgroundSprite.bounds.size.x;

            float spriteHeight =
                marbleBackgroundSprite.bounds.size.y;

            sr.transform.localPosition =
                new Vector3(0f, 0f, 1f);

            sr.transform.localScale =
                new Vector3(
                    targetWidth / spriteWidth,
                    targetHeight / spriteHeight,
                    1f);
        }

        private void OnDestroy()
        {
            if (hudView != null &&
                Coordinator != null)
            {
                hudView.ResetRequested -=
                    Coordinator.Reset;
            }

            spriteFactory?.Dispose();
        }
    }
}
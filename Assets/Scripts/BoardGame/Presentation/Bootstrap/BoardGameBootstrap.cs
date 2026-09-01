using System;
using GCCC.BoardGame.Core;
using GCCC.BoardGame.Core.Model;
using GCCC.BoardGame.Core.Rules.CellEffects;
using GCCC.BoardGame.Presentation.Audio;
using GCCC.BoardGame.Presentation.Config;
using GCCC.BoardGame.Presentation.Input;
using GCCC.BoardGame.Presentation.Views;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace GCCC.BoardGame.Presentation.Bootstrap
{
    public sealed class BoardGameBootstrap : MonoBehaviour
    {
        [SerializeField] private BoardGameConfig config;

        [SerializeField] private BoardView boardViewPrefab;
        [SerializeField] private PieceViewManager pieceViewsPrefab;
        [SerializeField] private GameHudView hudViewPrefab;
        [SerializeField] private BoardGameAudioManager audioManagerPrefab;

        // Player1 / Player2 の駒画像
        [SerializeField] private Sprite player1PieceSprite;
        [SerializeField] private Sprite player2PieceSprite;

        // 大理石背景
        [SerializeField] private Sprite marbleBackgroundSprite;

        private RuntimeSpriteFactory spriteFactory;
        private GameHudView hudView;
        private BoardGameAudioManager audioManager;

        public GameSession Session { get; private set; }
        public GameCoordinator Coordinator { get; private set; }
        public BoardView BoardView { get; private set; }
        public PieceViewManager PieceViews { get; private set; }

        public GameHudView HudView => hudView;

        public GameSnapshot Snapshot =>
            Session != null ? Session.Snapshot : null;

        public GridPosition? SelectedCell =>
            Coordinator != null ? Coordinator.SelectedCell : null;

        public int GeneratedCellCount =>
            BoardView != null ? BoardView.GeneratedCellCount : 0;

        public int PieceViewCount =>
            PieceViews != null ? PieceViews.PieceViewCount : 0;

        public int MoveIndicatorCount =>
            BoardView != null ? BoardView.MoveIndicatorCount : 0;

        public int EffectOverlayCount =>
            BoardView != null ? BoardView.EffectOverlayCount : 0;

        public string StatusText =>
            hudView != null ? hudView.StatusText : string.Empty;

        public string ResultText =>
            hudView != null ? hudView.ResultText : string.Empty;

        public bool IsResultVisible =>
            hudView != null && hudView.IsResultVisible;

        public string ReserveText =>
            hudView != null ? hudView.ReserveText : string.Empty;

        public bool IsEffectLegendVisible =>
            hudView != null && hudView.IsEffectLegendVisible;

        private void Awake()
        {
            GameDefinition definition = CreateDefinition();
            Session = CreateSession(definition);
            spriteFactory = new RuntimeSpriteFactory();
            Camera boardCamera = ConfigureCamera(
                definition.Columns, definition.Rows);
            audioManager = CreateAudioManager();
            BoardView = CreateBoard(boardCamera);
            PieceViews = CreatePieceViews();
            hudView = CreateHud();
            EnsureSceneEventSystem();
            Coordinator = CreateCoordinator();
            WireHudEvents();
            CreateInputController();
            CreateMarbleBackground(definition.Columns, definition.Rows);
        }

        private GameDefinition CreateDefinition()
        {
            return config != null
                ? config.CreateDefinition()
                : GameDefinition.CreateStandard();
        }

        private GameSession CreateSession(GameDefinition definition)
        {
            return new GameSession(
                definition,
                cellEffectHandlers: config != null
                    ? config.CreateCellEffectHandlers()
                    : Array.Empty<ICellEffectHandler>());
        }

        private BoardGameAudioManager CreateAudioManager()
        {
            BoardGameAudioManager manager = audioManagerPrefab != null
                ? CreatePresentationComponent(
                    audioManagerPrefab, "Board Game Audio")
                : GetComponent<BoardGameAudioManager>();
            return manager != null
                ? manager
                : CreatePresentationComponent<BoardGameAudioManager>(
                    null, "Board Game Audio");
        }

        private BoardView CreateBoard(Camera boardCamera)
        {
            BoardView view = CreatePresentationComponent(
                boardViewPrefab, "Board View");
            view.Initialize(
                boardCamera, spriteFactory, Session.Snapshot);
            return view;
        }

        private PieceViewManager CreatePieceViews()
        {
            PieceViewManager views = CreatePresentationComponent(
                pieceViewsPrefab, "Piece Views");
            views.Initialize(
                player1PieceSprite, player2PieceSprite, Session.Snapshot);
            return views;
        }

        private GameHudView CreateHud()
        {
            if (hudViewPrefab == null)
            {
                throw new InvalidOperationException(
                    "BoardGameBootstrap requires a configured GameHud prefab.");
            }

            GameHudView view = CreatePresentationComponent(
                hudViewPrefab, "Game HUD");
            view.Initialize(
                audioManager, player1PieceSprite, player2PieceSprite);
            return view;
        }

        private GameCoordinator CreateCoordinator()
        {
            return new GameCoordinator(
                Session,
                BoardView,
                PieceViews,
                hudView,
                audioManager: audioManager);
        }

        private void WireHudEvents()
        {
            hudView.ResetRequested += Coordinator.Reset;
            hudView.FuseRequested += Coordinator.ToggleFusionMode;
            hudView.ReserveDeployRequested += Coordinator.ToggleReserveDeployMode;
            hudView.ReservePieceSelected += Coordinator.ToggleReservePieceSelection;
            hudView.StartScreenRequested += ReturnToTitleScreen;
        }

        private void CreateInputController()
        {
            GameObject inputObject = new GameObject("Board Input");
            inputObject.transform.SetParent(transform, false);
            BoardInputController input =
                inputObject.AddComponent<BoardInputController>();
            input.Initialize(BoardView, hudView, Coordinator);
        }

        private static void EnsureSceneEventSystem()
        {
            if (EventSystem.current == null)
            {
                throw new InvalidOperationException(
                    "SampleScene requires an EventSystem with InputSystemUIInputModule.");
            }
        }

        private T CreatePresentationComponent<T>(
            T prefab,
            string instanceName)
            where T : Component
        {
            if (prefab != null)
            {
                T instance =
                    Instantiate(
                        prefab,
                        transform);

                instance.gameObject.name =
                    instanceName;

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
            if (Coordinator != null)
            {
                Coordinator.HandleCellClick(cell);
            }
        }

        public void ResetGame()
        {
            if (Coordinator != null)
            {
                Coordinator.Reset();
            }
        }

        private static void ReturnToTitleScreen()
        {
            SceneManager.LoadScene(
                BoardGameSceneNames.Title,
                LoadSceneMode.Single);
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

            // AudioListenerを整理
            AudioListener[] listeners =
                FindObjectsByType<AudioListener>(
                    FindObjectsSortMode.None);

            foreach (AudioListener listener in listeners)
            {
                listener.enabled =
                    listener.GetComponent<Camera>() == boardCamera;
            }

            if (boardCamera.GetComponent<AudioListener>() == null)
            {
                boardCamera.gameObject.AddComponent<AudioListener>();
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
                Mathf.Max(
                    boardCamera.aspect,
                    0.01f);

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
                new GameObject(
                    "Marble Background");

            obj.transform.SetParent(
                transform,
                false);

            SpriteRenderer sr =
                obj.AddComponent<SpriteRenderer>();

            sr.sprite =
                marbleBackgroundSprite;

            sr.color =
                Color.white;

            // 盤より後ろ
            sr.sortingOrder = -10;

            Camera camera = Camera.main;

            if (camera == null)
            {
                return;
            }

            // カメラが見ている範囲
            float visibleHeight =
                camera.orthographicSize * 2f;

            float visibleWidth =
                visibleHeight * camera.aspect;

            // 画面端より少し大きくする
            float targetWidth =
                Mathf.Max(
                    columns + 8f,
                    visibleWidth + 4f);

            float targetHeight =
                Mathf.Max(
                    rows + 8f,
                    visibleHeight + 4f);

            float spriteWidth =
                marbleBackgroundSprite.bounds.size.x;

            float spriteHeight =
                marbleBackgroundSprite.bounds.size.y;

            sr.transform.localPosition =
                new Vector3(
                    0f,
                    0f,
                    1f);

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

                hudView.FuseRequested -=
                    Coordinator.ToggleFusionMode;

                hudView.ReserveDeployRequested -=
                    Coordinator.ToggleReserveDeployMode;

                hudView.ReservePieceSelected -=
                    Coordinator.ToggleReservePieceSelection;

                hudView.StartScreenRequested -=
                    ReturnToTitleScreen;
            }

            Coordinator?.Dispose();
            spriteFactory?.Dispose();
        }
    }
}

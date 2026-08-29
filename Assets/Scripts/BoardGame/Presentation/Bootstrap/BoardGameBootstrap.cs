using GCCC.BoardGame.Core;
using GCCC.BoardGame.Core.Model;
using GCCC.BoardGame.Presentation.Audio;
using GCCC.BoardGame.Presentation.Config;
using GCCC.BoardGame.Presentation.Input;
using GCCC.BoardGame.Presentation.Views;
using GCCC.BoardGame.Core.Rules.CellEffects;
using System;
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
        [SerializeField] private BoardGameAudioManager audioManagerPrefab;

        private RuntimeSpriteFactory spriteFactory;
        private GameHudView hudView;
        private BoardGameAudioManager audioManager;

        public GameSession Session { get; private set; }
        public GameCoordinator Coordinator { get; private set; }
        public BoardView BoardView { get; private set; }
        public PieceViewManager PieceViews { get; private set; }
        public GameSnapshot Snapshot => Session?.Snapshot;
        public GridPosition? SelectedCell => Coordinator?.SelectedCell;
        public int GeneratedCellCount => BoardView != null ? BoardView.GeneratedCellCount : 0;
        public int PieceViewCount => PieceViews != null ? PieceViews.PieceViewCount : 0;
        public int MoveIndicatorCount => BoardView != null ? BoardView.MoveIndicatorCount : 0;
        public int EffectOverlayCount =>
            BoardView != null ? BoardView.EffectOverlayCount : 0;
        public string StatusText => hudView != null ? hudView.StatusText : string.Empty;

        public string ResultText => hudView != null ? hudView.ResultText : string.Empty;

        public bool IsResultVisible => hudView != null && hudView.IsResultVisible;
        public string ReserveText => hudView != null ? hudView.ReserveText : string.Empty;
        public bool IsEffectLegendVisible =>
            hudView != null && hudView.IsEffectLegendVisible;

        private void Awake()
        {
            GameDefinition definition = config != null
                ? config.CreateDefinition()
                : GameDefinition.CreateStandard();
            Session = new GameSession(
                definition,
                cellEffectHandlers: config != null
                    ? config.CreateCellEffectHandlers()
                    : Array.Empty<ICellEffectHandler>());
            spriteFactory = new RuntimeSpriteFactory();

            Camera boardCamera = ConfigureCamera(definition.Columns, definition.Rows);
            audioManager = audioManagerPrefab != null
                ? CreatePresentationComponent(audioManagerPrefab, "Board Game Audio")
                : GetComponent<BoardGameAudioManager>();
            if (audioManager == null)
            {
                audioManager = CreatePresentationComponent<BoardGameAudioManager>(
                    null, "Board Game Audio");
            }

            BoardView = CreatePresentationComponent(boardViewPrefab, "Board View");
            BoardView.Initialize(boardCamera, spriteFactory.SquareSprite, Session.Snapshot);

            PieceViews = CreatePresentationComponent(pieceViewsPrefab, "Piece Views");
            PieceViews.Initialize(spriteFactory.CircleSprite, Session.Snapshot);

            hudView = CreatePresentationComponent(hudViewPrefab, "Game HUD");
            hudView.Initialize(audioManager);

            Coordinator = new GameCoordinator(
                Session, BoardView, PieceViews, hudView, audioManager: audioManager);
            hudView.ResetRequested += Coordinator.Reset;
            hudView.FuseRequested += Coordinator.ToggleFusionMode;
            hudView.StartScreenRequested += ReturnToTitleScreen;

            GameObject inputObject = new GameObject("Board Input");
            inputObject.transform.SetParent(transform, false);
            BoardInputController input = inputObject.AddComponent<BoardInputController>();
            input.Initialize(BoardView, hudView, Coordinator);
        }

        private T CreatePresentationComponent<T>(T prefab, string instanceName)
            where T : Component
        {
            if (prefab != null)
            {
                T instance = Instantiate(prefab, transform);
                instance.gameObject.name = instanceName;
                return instance;
            }

            GameObject instanceObject = new GameObject(instanceName);
            instanceObject.transform.SetParent(transform, false);
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

        private static void ReturnToTitleScreen()
        {
            SceneManager.LoadScene(BoardGameSceneNames.Title, LoadSceneMode.Single);
        }

        private static Camera ConfigureCamera(int columns, int rows)
        {
            Camera boardCamera = Camera.main;
            if (boardCamera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera", typeof(Camera));
                cameraObject.tag = "MainCamera";
                boardCamera = cameraObject.GetComponent<Camera>();
            }

            AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            foreach (AudioListener listener in listeners)
            {
                listener.enabled = listener.GetComponent<Camera>() == boardCamera;
            }

            if (boardCamera.GetComponent<AudioListener>() == null)
            {
                boardCamera.gameObject.AddComponent<AudioListener>();
            }

            boardCamera.transform.SetPositionAndRotation(
                new Vector3(0f, 0f, -10f), Quaternion.identity);
            boardCamera.orthographic = true;
            boardCamera.clearFlags = CameraClearFlags.SolidColor;
            boardCamera.backgroundColor = new Color32(24, 27, 34, 255);

            float verticalSize = (rows + 2f) * 0.5f;
            float horizontalSize = (columns + 1f) * 0.5f /
                                   Mathf.Max(boardCamera.aspect, 0.01f);
            boardCamera.orthographicSize = Mathf.Max(verticalSize, horizontalSize);
            return boardCamera;
        }

        private void OnDestroy()
        {
            if (hudView != null && Coordinator != null)
            {
                hudView.ResetRequested -= Coordinator.Reset;
                hudView.FuseRequested -= Coordinator.ToggleFusionMode;
                hudView.StartScreenRequested -= ReturnToTitleScreen;
            }

            Coordinator?.Dispose();
            spriteFactory?.Dispose();
        }
    }
}

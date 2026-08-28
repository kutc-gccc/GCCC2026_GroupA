using System;
using GCCC.BoardGame.Core.Model;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace GCCC.BoardGame.Presentation.Views
{
    public sealed class GameHudView : MonoBehaviour
    {
        [SerializeField] private Button randomizePowerButton;

        private RectTransform resetButtonRect;
        private RectTransform randomizeButtonRect;
        private RectTransform fuseButtonRect;
        private Button fuseButton;
        private Text statusLabel;
        private Text messageLabel;
        private GameObject createdEventSystem;

        public event Action ResetRequested;
        public event Action OnRandomizePowerButtonClicked;
        public event Action FuseRequested;

        public string StatusText => statusLabel != null ? statusLabel.text : string.Empty;
        public Button RandomizePowerButton => randomizePowerButton;

        private void Start()
        {
            // インスペクター側でボタンが割り当てられている場合の予備リスナー登録
            if (randomizePowerButton != null)
            {
                randomizePowerButton.onClick.RemoveAllListeners();
                randomizePowerButton.onClick.AddListener(() =>
                {
                    Debug.Log("[GameHudView] Inspector割り当てボタンが押されました");
                    OnRandomizePowerButtonClicked?.Invoke();
                });
            }
        }

        public void Initialize()
        {
            BuildUi();
        }

        public void SetRandomizeButtonInteractable(bool interactable)
        {
            if (randomizePowerButton != null)
            {
                randomizePowerButton.interactable = interactable;
            }
        }

        public void Render(GameSnapshot snapshot)
        {
            if (statusLabel == null) return;

            if (snapshot.Winner.HasValue)
            {
                statusLabel.text = snapshot.Winner.Value == PlayerId.Player1
                    ? "プレイヤー1（青）の勝利"
                    : "プレイヤー2（赤）の勝利";
                return;
            }

            if (snapshot.IsDraw)
            {
                statusLabel.text = "引き分け";
                return;
            }

            statusLabel.text = snapshot.CurrentPlayer == PlayerId.Player1
                ? "プレイヤー1（青）のターン"
                : "プレイヤー2（赤）のターン";
        }

        public bool IsPointerOverControl(Vector2 screenPosition)
        {
            bool overReset = resetButtonRect != null &&
                RectTransformUtility.RectangleContainsScreenPoint(
                    resetButtonRect, screenPosition);

            bool overRandomize = randomizeButtonRect != null &&
                RectTransformUtility.RectangleContainsScreenPoint(
                    randomizeButtonRect, screenPosition);

            bool overFuse = fuseButtonRect != null &&
                RectTransformUtility.RectangleContainsScreenPoint(
                    fuseButtonRect, screenPosition);

            return overReset || overRandomize || overFuse;
        }

        public void SetFuseButtonInteractable(bool interactable)
        {
            if (fuseButton != null)
            {
                fuseButton.interactable = interactable;
            }
        }

        /// <summary>
        /// 合体結果など、一時的なメッセージをHUDに表示する。
        /// 空文字で非表示扱いになる。
        /// </summary>
        public void ShowMessage(string text)
        {
            if (messageLabel != null)
            {
                messageLabel.text = text ?? string.Empty;
            }
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

            messageLabel = CreateUiText(
                "Fusion Message",
                canvasObject.transform,
                font,
                24,
                TextAnchor.MiddleLeft,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(24f, -96f),
                new Vector2(520f, 48f));

            messageLabel.color = new Color32(255, 213, 79, 255);
            messageLabel.text = string.Empty;

            Text player2Label = CreateUiText(
                "Player 2 Territory Label",
                canvasObject.transform,
                font,
                22,
                TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -12f),
                new Vector2(520f, 42f));

            player2Label.text = "プレイヤー2の陣地";

            Text player1Label = CreateUiText(
                "Player 1 Territory Label",
                canvasObject.transform,
                font,
                22,
                TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 12f),
                new Vector2(520f, 42f));

            player1Label.text = "プレイヤー1の陣地";

            // --- 1. リセットボタン生成 ---
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

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color32(235, 238, 244, 255);

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(OnResetClicked);

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

            resetLabel.rectTransform.anchorMin = Vector2.zero;
            resetLabel.rectTransform.anchorMax = Vector2.one;
            resetLabel.rectTransform.offsetMin = Vector2.zero;
            resetLabel.rectTransform.offsetMax = Vector2.zero;
            resetLabel.text = "リセット";
            resetLabel.color = new Color32(35, 41, 52, 255);

            // --- 2. パワーランダム化ボタン生成 ---
            GameObject randButtonObject = new GameObject(
                "Randomize Power Button",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));

            randButtonObject.transform.SetParent(canvasObject.transform, false);

            randomizeButtonRect = randButtonObject.GetComponent<RectTransform>();
            randomizeButtonRect.anchorMin = Vector2.one;
            randomizeButtonRect.anchorMax = Vector2.one;
            randomizeButtonRect.pivot = Vector2.one;
            randomizeButtonRect.sizeDelta = new Vector2(220f, 64f);

            // リセットボタンの左側に配置
            randomizeButtonRect.anchoredPosition = new Vector2(-220f, -24f);

            Image randImage = randButtonObject.GetComponent<Image>();
            randImage.color = new Color32(235, 238, 244, 255);

            Button randButton = randButtonObject.GetComponent<Button>();
            randButton.targetGraphic = randImage;

            // 生成したボタンをメンバー変数に割り当て＆イベント登録
            randomizePowerButton = randButton;

            randButton.onClick.AddListener(() =>
            {
                Debug.Log("[GameHudView] 動的生成されたパワー変更ボタンが押されました");
                OnRandomizePowerButtonClicked?.Invoke();
            });

            Text randLabel = CreateUiText(
                "Label",
                randButtonObject.transform,
                font,
                20,
                TextAnchor.MiddleCenter,
                Vector2.zero,
                Vector2.zero,
                Vector2.zero,
                Vector2.zero);

            randLabel.rectTransform.anchorMin = Vector2.zero;
            randLabel.rectTransform.anchorMax = Vector2.one;
            randLabel.rectTransform.offsetMin = Vector2.zero;
            randLabel.rectTransform.offsetMax = Vector2.zero;
            randLabel.text = "パワーランダム化";
            randLabel.color = new Color32(35, 41, 52, 255);

            // --- 3. 合体ボタン生成 ---
            GameObject fuseButtonObject = new GameObject(
                "Fuse Button",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));

            fuseButtonObject.transform.SetParent(canvasObject.transform, false);

            fuseButtonRect = fuseButtonObject.GetComponent<RectTransform>();
            fuseButtonRect.anchorMin = Vector2.one;
            fuseButtonRect.anchorMax = Vector2.one;
            fuseButtonRect.pivot = Vector2.one;
            fuseButtonRect.sizeDelta = new Vector2(180f, 64f);
            fuseButtonRect.anchoredPosition = new Vector2(-420f, -24f);

            Image fuseImage = fuseButtonObject.GetComponent<Image>();
            fuseImage.color = new Color32(235, 238, 244, 255);

            fuseButton = fuseButtonObject.GetComponent<Button>();
            fuseButton.targetGraphic = fuseImage;
            fuseButton.onClick.AddListener(OnFuseClicked);
            fuseButton.interactable = false;

            Text fuseLabel = CreateUiText(
                "Label",
                fuseButtonObject.transform,
                font,
                24,
                TextAnchor.MiddleCenter,
                Vector2.zero,
                Vector2.zero,
                Vector2.zero,
                Vector2.zero);

            fuseLabel.rectTransform.anchorMin = Vector2.zero;
            fuseLabel.rectTransform.anchorMax = Vector2.one;
            fuseLabel.rectTransform.offsetMin = Vector2.zero;
            fuseLabel.rectTransform.offsetMax = Vector2.zero;
            fuseLabel.text = "合体";
            fuseLabel.color = new Color32(35, 41, 52, 255);

            // --- EventSystem生成 ---
            if (EventSystem.current == null)
            {
                createdEventSystem = new GameObject(
                    "EventSystem",
                    typeof(EventSystem));

                InputSystemUIInputModule inputModule =
                    createdEventSystem.AddComponent<InputSystemUIInputModule>();

                inputModule.AssignDefaultActions();
            }
        }

        private void OnResetClicked()
        {
            ResetRequested?.Invoke();
        }

        private void OnFuseClicked()
        {
            FuseRequested?.Invoke();
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

        private void OnDestroy()
        {
            if (createdEventSystem == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(createdEventSystem);
            }
            else
            {
                Object.DestroyImmediate(createdEventSystem);
            }
        }
    }
}
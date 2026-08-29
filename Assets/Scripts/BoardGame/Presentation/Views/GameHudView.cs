using System;
using GCCC.BoardGame.Core.Model;
using GCCC.BoardGame.Presentation.Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace GCCC.BoardGame.Presentation.Views
{
    public sealed class GameHudView : MonoBehaviour
    {
        private RectTransform resetButtonRect;
        private RectTransform audioControlsRect;
        private Text statusLabel;
        private GameObject createdEventSystem;
        private Slider bgmSlider;
        private Slider sfxSlider;
        private Button resetButton;
        private BoardGameAudioManager audioManager;

        public event Action ResetRequested;

        public string StatusText => statusLabel != null ? statusLabel.text : string.Empty;

        public void Initialize()
        {
            BuildUi(null);
        }

        public void Initialize(BoardGameAudioManager audioManager)
        {
            this.audioManager = audioManager;
            BuildUi(audioManager);
        }

        // Update メソッドは音量制御の不具合の原因となるため削除しました

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
            return IsPointerOverRect(resetButtonRect, screenPosition) ||
                   IsPointerOverRect(audioControlsRect, screenPosition);
        }

        private void BuildUi(BoardGameAudioManager audioManager)
        {
            GameObject canvasObject = new GameObject(
                "Board UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            Font font = CreateUiFont();
            statusLabel = CreateUiText(
                "Turn Status", canvasObject.transform, font, 28, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(24f, -24f), new Vector2(520f, 64f));

            if (audioManager != null)
            {
                audioControlsRect = CreateAudioControls(
                    canvasObject.transform, font, audioManager);
            }

            Text player2Label = CreateUiText(
                "Player 2 Territory Label", canvasObject.transform, font, 22,
                TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -12f), new Vector2(520f, 42f));
            player2Label.text = "プレイヤー2の陣地";

            Text player1Label = CreateUiText(
                "Player 1 Territory Label", canvasObject.transform, font, 22,
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 12f), new Vector2(520f, 42f));
            player1Label.text = "プレイヤー1の陣地";

            GameObject buttonObject = new GameObject(
                "Reset Button", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(canvasObject.transform, false);
            resetButtonRect = buttonObject.GetComponent<RectTransform>();
            resetButtonRect.anchorMin = Vector2.one;
            resetButtonRect.anchorMax = Vector2.one;
            resetButtonRect.pivot = Vector2.one;
            resetButtonRect.sizeDelta = new Vector2(180f, 64f);
            resetButtonRect.anchoredPosition = new Vector2(-24f, -24f);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color32(235, 238, 244, 255);
            resetButton = buttonObject.GetComponent<Button>();
            resetButton.targetGraphic = image;
            resetButton.onClick.AddListener(OnResetClicked);

            Text resetLabel = CreateUiText(
                "Label", buttonObject.transform, font, 24, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            resetLabel.rectTransform.anchorMin = Vector2.zero;
            resetLabel.rectTransform.anchorMax = Vector2.one;
            resetLabel.rectTransform.offsetMin = Vector2.zero;
            resetLabel.rectTransform.offsetMax = Vector2.zero;
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

        private void OnResetClicked()
        {
            ResetRequested?.Invoke();
        }

        private static bool IsPointerOverRect(RectTransform rect, Vector2 screenPosition)
        {
            return rect != null && RectTransformUtility.RectangleContainsScreenPoint(
                rect, screenPosition);
        }

        private RectTransform CreateAudioControls(
            Transform parent,
            Font font,
            BoardGameAudioManager audioManager)
        {
            GameObject panelObject = new GameObject(
                "Audio Volume Controls", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image));
            panelObject.transform.SetParent(parent, false);
            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(24f, -104f);
            panelRect.sizeDelta = new Vector2(300f, 150f);

            Image panelImage = panelObject.GetComponent<Image>();
            panelImage.color = new Color32(35, 41, 52, 225);

            CreateUiText("BGM Label", panelObject.transform, font, 20,
                TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(16f, -26f), new Vector2(72f, 36f)).text = "BGM";
            CreateUiText("SFX Label", panelObject.transform, font, 20,
                TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(16f, -92f), new Vector2(72f, 36f)).text = "SFX";

            bgmSlider = CreateVolumeSlider(
                panelObject.transform, "BGM Slider", new Vector2(96f, -26f),
                audioManager.BgmVolume);
            sfxSlider = CreateVolumeSlider(
                panelObject.transform, "SFX Slider", new Vector2(96f, -92f),
                audioManager.SfxVolume);

            bgmSlider.onValueChanged.AddListener(audioManager.SetBgmVolume);
            sfxSlider.onValueChanged.AddListener(audioManager.SetSfxVolume);
            return panelRect;
        }

        private static Slider CreateVolumeSlider(
    Transform parent,
    string objectName,
    Vector2 anchoredPosition,
    float value)
{
    // 1. スライダー本体の生成
    GameObject sliderObject = new GameObject(
        objectName, typeof(RectTransform), typeof(CanvasRenderer),
        typeof(Image), typeof(Slider));
    sliderObject.transform.SetParent(parent, false);
    RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
    sliderRect.anchorMin = new Vector2(0f, 1f);
    sliderRect.anchorMax = new Vector2(0f, 1f);
    sliderRect.pivot = new Vector2(0f, 1f);
    sliderRect.anchoredPosition = anchoredPosition;
    sliderRect.sizeDelta = new Vector2(184f, 36f);

    Image background = sliderObject.GetComponent<Image>();
    background.color = new Color32(90, 99, 112, 255);

    Slider slider = sliderObject.GetComponent<Slider>();
    slider.minValue = 0f;
    slider.maxValue = 1f;
    slider.wholeNumbers = false;
    slider.direction = Slider.Direction.LeftToRight;

    // 2. つまみの移動領域（Handle Slide Area）を作成
    // つまみの幅（28px）分だけ左右に余裕を持たせることで、端まで移動したときに値が確実に 0 / 1 になるようにします
    GameObject slideArea = new GameObject("Handle Slide Area", typeof(RectTransform));
    slideArea.transform.SetParent(sliderObject.transform, false);
    RectTransform slideAreaRect = slideArea.GetComponent<RectTransform>();
    slideAreaRect.anchorMin = Vector2.zero;
    slideAreaRect.anchorMax = Vector2.one;
    slideAreaRect.offsetMin = new Vector2(14f, 0f); // 左のパディング（Handle幅の半分）
    slideAreaRect.offsetMax = new Vector2(-14f, 0f); // 右のパディング（Handle幅の半分）

    // 3. つまみ（Handle）の作成
    GameObject handleObject = new GameObject(
        "Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
    handleObject.transform.SetParent(slideArea.transform, false);
    RectTransform handleRect = handleObject.GetComponent<RectTransform>();
    handleRect.anchorMin = new Vector2(0f, 0f);
    handleRect.anchorMax = new Vector2(0f, 1f);
    handleRect.pivot = new Vector2(0.5f, 0.5f);
    handleRect.sizeDelta = new Vector2(28f, 0f); // 幅28、高さは親に合わせる

    Image handleImage = handleObject.GetComponent<Image>();
    handleImage.color = new Color32(235, 238, 244, 255);

    // 4. Slider コンポーネントに割り当て
    slider.handleRect = handleRect;
    slider.targetGraphic = handleImage;

    // 5. 初期値の設定（※割り当てが完了した後に代入）
    slider.value = value;

    return slider;
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
                objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
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
                "Yu Gothic UI", "Meiryo UI", "Hiragino Sans", "Noto Sans CJK JP", "Arial"
            };
            Font font = Font.CreateDynamicFontFromOSFont(preferredFonts, 24);
            return font != null
                ? font
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private void OnDestroy()
        {
            if (resetButton != null)
            {
                resetButton.onClick.RemoveListener(OnResetClicked);
            }

            if (bgmSlider != null && audioManager != null)
            {
                bgmSlider.onValueChanged.RemoveListener(audioManager.SetBgmVolume);
            }

            if (sfxSlider != null && audioManager != null)
            {
                sfxSlider.onValueChanged.RemoveListener(audioManager.SetSfxVolume);
            }

            if (createdEventSystem != null)
            {
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
}
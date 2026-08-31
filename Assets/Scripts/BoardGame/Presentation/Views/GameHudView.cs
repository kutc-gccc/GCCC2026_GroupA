using System;
using System.Collections.Generic;
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
        [SerializeField] private Button randomizePowerButton;

        private RectTransform resetButtonRect;
        private RectTransform randomizeButtonRect;
        private RectTransform fuseButtonRect;
        private RectTransform reserveDeployButtonRect;
        private RectTransform audioControlsRect;
        private Text statusLabel;
        private Text messageLabel;
        private Text resultLabel;
        private ReservePanelView reservePanelView;
        private GameObject effectLegend;
        private GameObject resultOverlay;
        private GameObject createdEventSystem;
        private Slider bgmSlider;
        private Slider sfxSlider;
        private Button resetButton;
        private Button fuseButton;
        private Button reserveDeployButton;
        private Button resultButton;
        private bool randomizeButtonInteractable;
        private bool fuseButtonInteractable;
        private bool reserveDeployButtonInteractable;
        private BoardGameAudioManager audioManager;
        private string reserveText = string.Empty;

        public event Action ResetRequested;
        public event Action OnRandomizePowerButtonClicked;
        public event Action FuseRequested;
        public event Action ReserveDeployRequested;
        public event Action<PieceId> ReservePieceSelected;
        public event Action StartScreenRequested;

        public string StatusText => statusLabel != null ? statusLabel.text : string.Empty;
        public string ResultText => resultLabel != null ? resultLabel.text : string.Empty;
        public bool IsResultVisible => resultOverlay != null && resultOverlay.activeSelf;
        public Button RandomizePowerButton => randomizePowerButton;
        public Button ReserveDeployButton => reserveDeployButton;
        public string ReserveText => reserveText;
        public int ReserveCardCount =>
            reservePanelView != null ? reservePanelView.CardCount : 0;
        public bool IsEffectLegendVisible =>
            effectLegend != null && effectLegend.activeSelf;

        private void Start()
        {
            if (randomizePowerButton != null)
            {
                randomizePowerButton.onClick.RemoveListener(OnRandomizeClicked);
                randomizePowerButton.onClick.AddListener(OnRandomizeClicked);
            }
        }

        public void Initialize()
        {
            Initialize(null, null, null);
        }

        public void Initialize(BoardGameAudioManager audioManager)
        {
            Initialize(audioManager, null, null);
        }

        public void Initialize(
            BoardGameAudioManager audioManager,
            Sprite player1PieceSprite,
            Sprite player2PieceSprite)
        {
            this.audioManager = audioManager;
            BuildUi(audioManager, player1PieceSprite, player2PieceSprite);
        }

        public void SetRandomizeButtonInteractable(bool interactable)
        {
            randomizeButtonInteractable = interactable;
            if (randomizePowerButton != null)
            {
                randomizePowerButton.interactable = interactable && !IsResultVisible;
            }
        }

        public void Render(GameSnapshot snapshot)
        {
            if (statusLabel == null) return;

            reserveText =
                $"リザーブ　青: {snapshot.GetPlayer(PlayerId.Player1).ReservePieces.Count}" +
                $"　赤: {snapshot.GetPlayer(PlayerId.Player2).ReservePieces.Count}";
            reservePanelView.Render(snapshot);
            effectLegend.SetActive(snapshot.CellEffectDefinitions.Count > 0);

            if (snapshot.Winner.HasValue)
            {
                string resultText = snapshot.Winner.Value == PlayerId.Player1
                    ? "プレイヤー1（青）の勝利"
                    : "プレイヤー2（赤）の勝利";
                statusLabel.text = resultText;
                ShowResult(resultText);
                return;
            }

            if (snapshot.IsDraw)
            {
                statusLabel.text = "引き分け";
                ShowResult("引き分け");
                return;
            }

            HideResult();
            statusLabel.text = snapshot.CurrentPlayer == PlayerId.Player1
                ? "プレイヤー1（青）のターン"
                : "プレイヤー2（赤）のターン";
        }

        public bool IsPointerOverControl(Vector2 screenPosition)
        {
            if (IsResultVisible)
            {
                return true;
            }

            return IsPointerOverRect(resetButtonRect, screenPosition) ||
                   IsPointerOverRect(randomizeButtonRect, screenPosition) ||
                   IsPointerOverRect(fuseButtonRect, screenPosition) ||
                   IsPointerOverRect(reserveDeployButtonRect, screenPosition) ||
                   IsPointerOverRect(audioControlsRect, screenPosition) ||
                   (reservePanelView != null &&
                    reservePanelView.IsPointerOver(screenPosition));
        }

        public void SetFuseButtonInteractable(bool interactable)
        {
            fuseButtonInteractable = interactable;
            if (fuseButton != null)
            {
                fuseButton.interactable = interactable && !IsResultVisible;
            }
        }

        public void SetReserveDeployButtonInteractable(bool interactable)
        {
            reserveDeployButtonInteractable = interactable;
            if (reserveDeployButton != null)
            {
                reserveDeployButton.interactable = interactable && !IsResultVisible;
            }
        }

        public void SetDeployableReservePieces(IEnumerable<PieceId> pieceIds)
        {
            reservePanelView?.SetDeployablePieces(pieceIds);
        }

        public void SetSelectedReservePiece(PieceId? pieceId)
        {
            reservePanelView?.SetSelectedPiece(pieceId);
        }

        public ReservePieceCardView GetReserveCard(PieceId pieceId)
        {
            return reservePanelView != null
                ? reservePanelView.GetCard(pieceId)
                : null;
        }

        public void ShowMessage(string text)
        {
            if (messageLabel != null)
            {
                messageLabel.text = text ?? string.Empty;
            }
        }

        private void BuildUi(
            BoardGameAudioManager audioManager,
            Sprite player1PieceSprite,
            Sprite player2PieceSprite)
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

            messageLabel = CreateUiText(
                "Fusion Message", canvasObject.transform, font, 24,
                TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(24f, -96f), new Vector2(520f, 48f));
            messageLabel.color = new Color32(255, 213, 79, 255);
            messageLabel.text = string.Empty;

            GameObject reservePanelObject = new GameObject(
                "Reserve Panels", typeof(RectTransform), typeof(ReservePanelView));
            reservePanelView = reservePanelObject.GetComponent<ReservePanelView>();
            reservePanelView.Initialize(
                canvasObject.transform,
                font,
                player1PieceSprite,
                player2PieceSprite);
            reservePanelView.ReservePieceSelected += OnReservePieceSelected;

            effectLegend = CreateEffectLegend(canvasObject.transform, font);

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

            GameObject randomizeObject = new GameObject(
                "Randomize Power Button", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(Button));
            randomizeObject.transform.SetParent(canvasObject.transform, false);
            randomizeButtonRect = randomizeObject.GetComponent<RectTransform>();
            randomizeButtonRect.anchorMin = Vector2.one;
            randomizeButtonRect.anchorMax = Vector2.one;
            randomizeButtonRect.pivot = Vector2.one;
            randomizeButtonRect.sizeDelta = new Vector2(220f, 64f);
            randomizeButtonRect.anchoredPosition = new Vector2(-220f, -24f);

            Image randomizeImage = randomizeObject.GetComponent<Image>();
            randomizeImage.color = new Color32(235, 238, 244, 255);
            randomizePowerButton = randomizeObject.GetComponent<Button>();
            randomizePowerButton.targetGraphic = randomizeImage;
            randomizePowerButton.onClick.AddListener(OnRandomizeClicked);
            randomizePowerButton.interactable = false;

            Text randomizeLabel = CreateUiText(
                "Label", randomizeObject.transform, font, 20, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            randomizeLabel.rectTransform.anchorMin = Vector2.zero;
            randomizeLabel.rectTransform.anchorMax = Vector2.one;
            randomizeLabel.rectTransform.offsetMin = Vector2.zero;
            randomizeLabel.rectTransform.offsetMax = Vector2.zero;
            randomizeLabel.text = "パワーランダム化";
            randomizeLabel.color = new Color32(35, 41, 52, 255);

            GameObject fuseObject = new GameObject(
                "Fuse Button", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(Button));
            fuseObject.transform.SetParent(canvasObject.transform, false);
            fuseButtonRect = fuseObject.GetComponent<RectTransform>();
            fuseButtonRect.anchorMin = Vector2.one;
            fuseButtonRect.anchorMax = Vector2.one;
            fuseButtonRect.pivot = Vector2.one;
            fuseButtonRect.sizeDelta = new Vector2(180f, 64f);
            fuseButtonRect.anchoredPosition = new Vector2(-460f, -24f);

            Image fuseImage = fuseObject.GetComponent<Image>();
            fuseImage.color = new Color32(235, 238, 244, 255);
            fuseButton = fuseObject.GetComponent<Button>();
            fuseButton.targetGraphic = fuseImage;
            fuseButton.onClick.AddListener(OnFuseClicked);
            fuseButton.interactable = false;

            Text fuseLabel = CreateUiText(
                "Label", fuseObject.transform, font, 24, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            fuseLabel.rectTransform.anchorMin = Vector2.zero;
            fuseLabel.rectTransform.anchorMax = Vector2.one;
            fuseLabel.rectTransform.offsetMin = Vector2.zero;
            fuseLabel.rectTransform.offsetMax = Vector2.zero;
            fuseLabel.text = "合体";
            fuseLabel.color = new Color32(35, 41, 52, 255);

            GameObject reserveDeployObject = new GameObject(
                "Reserve Deploy Button", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(Button));
            reserveDeployObject.transform.SetParent(canvasObject.transform, false);
            reserveDeployButtonRect =
                reserveDeployObject.GetComponent<RectTransform>();
            reserveDeployButtonRect.anchorMin = Vector2.one;
            reserveDeployButtonRect.anchorMax = Vector2.one;
            reserveDeployButtonRect.pivot = Vector2.one;
            reserveDeployButtonRect.sizeDelta = new Vector2(200f, 64f);
            reserveDeployButtonRect.anchoredPosition = new Vector2(-660f, -24f);

            Image reserveDeployImage = reserveDeployObject.GetComponent<Image>();
            reserveDeployImage.color = new Color32(235, 238, 244, 255);
            reserveDeployButton = reserveDeployObject.GetComponent<Button>();
            reserveDeployButton.targetGraphic = reserveDeployImage;
            reserveDeployButton.onClick.AddListener(OnReserveDeployClicked);
            reserveDeployButton.interactable = false;

            Text reserveDeployLabel = CreateUiText(
                "Label", reserveDeployObject.transform, font, 22,
                TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            reserveDeployLabel.rectTransform.anchorMin = Vector2.zero;
            reserveDeployLabel.rectTransform.anchorMax = Vector2.one;
            reserveDeployLabel.rectTransform.offsetMin = Vector2.zero;
            reserveDeployLabel.rectTransform.offsetMax = Vector2.zero;
            reserveDeployLabel.text = "リザーブ配置";
            reserveDeployLabel.color = new Color32(35, 41, 52, 255);

            BuildResultOverlay(canvasObject.transform, font);

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
            if (IsResultVisible)
            {
                return;
            }

            ResetRequested?.Invoke();
        }

        private void OnRandomizeClicked()
        {
            if (IsResultVisible)
            {
                return;
            }

            OnRandomizePowerButtonClicked?.Invoke();
        }

        private void OnFuseClicked()
        {
            if (IsResultVisible)
            {
                return;
            }

            FuseRequested?.Invoke();
        }

        private void OnReserveDeployClicked()
        {
            if (IsResultVisible)
            {
                return;
            }

            ReserveDeployRequested?.Invoke();
        }

        private void OnReservePieceSelected(PieceId pieceId)
        {
            if (IsResultVisible)
            {
                return;
            }

            ReservePieceSelected?.Invoke(pieceId);
        }

        private void OnStartScreenClicked()
        {
            StartScreenRequested?.Invoke();
        }

        private void ShowResult(string text)
        {
            if (resultLabel != null)
            {
                resultLabel.text = text;
            }

            if (resultOverlay != null)
            {
                resultOverlay.SetActive(true);
                resultOverlay.transform.SetAsLastSibling();
            }

            SetBackgroundControlsInteractable(false);
        }

        private void HideResult()
        {
            if (resultLabel != null)
            {
                resultLabel.text = string.Empty;
            }

            if (resultOverlay != null)
            {
                resultOverlay.SetActive(false);
            }

            SetBackgroundControlsInteractable(true);
        }

        private void SetBackgroundControlsInteractable(bool interactable)
        {
            if (resetButton != null)
            {
                resetButton.interactable = interactable;
            }

            if (randomizePowerButton != null)
            {
                randomizePowerButton.interactable =
                    interactable && randomizeButtonInteractable;
            }

            if (fuseButton != null)
            {
                fuseButton.interactable = interactable && fuseButtonInteractable;
            }

            if (reserveDeployButton != null)
            {
                reserveDeployButton.interactable =
                    interactable && reserveDeployButtonInteractable;
            }

            if (!interactable)
            {
                reservePanelView?.SetDeployablePieces(Array.Empty<PieceId>());
            }

            if (bgmSlider != null)
            {
                bgmSlider.interactable = interactable;
            }

            if (sfxSlider != null)
            {
                sfxSlider.interactable = interactable;
            }
        }

        private void BuildResultOverlay(Transform parent, Font font)
        {
            resultOverlay = new GameObject(
                "Result Overlay", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image));
            resultOverlay.transform.SetParent(parent, false);

            RectTransform overlayRect = resultOverlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            Image overlayImage = resultOverlay.GetComponent<Image>();
            overlayImage.color = new Color32(24, 27, 34, 220);
            overlayImage.raycastTarget = true;

            GameObject panelObject = new GameObject(
                "Result Panel", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image));
            panelObject.transform.SetParent(resultOverlay.transform, false);

            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(720f, 400f);

            Image panelImage = panelObject.GetComponent<Image>();
            panelImage.color = new Color32(42, 47, 57, 255);

            resultLabel = CreateUiText(
                "Result Text", panelObject.transform, font, 48,
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 70f),
                new Vector2(640f, 120f));
            resultLabel.fontStyle = FontStyle.Bold;

            GameObject buttonObject = new GameObject(
                "Return To Title Button", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(panelObject.transform, false);

            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = new Vector2(0f, -90f);
            buttonRect.sizeDelta = new Vector2(360f, 72f);

            Image buttonImage = buttonObject.GetComponent<Image>();
            buttonImage.color = new Color32(235, 238, 244, 255);

            resultButton = buttonObject.GetComponent<Button>();
            resultButton.targetGraphic = buttonImage;
            resultButton.onClick.AddListener(OnStartScreenClicked);

            Text buttonLabel = CreateUiText(
                "Label", buttonObject.transform, font, 24, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            buttonLabel.rectTransform.anchorMin = Vector2.zero;
            buttonLabel.rectTransform.anchorMax = Vector2.one;
            buttonLabel.rectTransform.offsetMin = Vector2.zero;
            buttonLabel.rectTransform.offsetMax = Vector2.zero;
            buttonLabel.text = "スタート画面に戻る";
            buttonLabel.color = new Color32(35, 41, 52, 255);

            resultOverlay.SetActive(false);
        }

        private static GameObject CreateEffectLegend(Transform parent, Font font)
        {
            GameObject panelObject = new GameObject(
                "Cell Effect Legend", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image));
            panelObject.transform.SetParent(parent, false);
            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 0f);
            panelRect.anchorMax = new Vector2(0f, 0f);
            panelRect.pivot = new Vector2(0f, 0f);
            panelRect.anchoredPosition = new Vector2(24f, 24f);
            panelRect.sizeDelta = new Vector2(300f, 74f);

            Image panelImage = panelObject.GetComponent<Image>();
            panelImage.color = new Color32(35, 41, 52, 225);
            panelImage.raycastTarget = false;

            Text label = CreateUiText(
                "Legend Text", panelObject.transform, font, 18,
                TextAnchor.MiddleLeft, Vector2.zero, Vector2.zero,
                Vector2.zero, Vector2.zero);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(12f, 6f);
            label.rectTransform.offsetMax = new Vector2(-12f, -6f);
            label.text = "シアン: 滞在中効果\n紫: 一度で永続する効果";
            panelObject.SetActive(false);
            return panelObject;
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
            panelRect.anchoredPosition = new Vector2(24f, -168f);
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

            GameObject slideArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            slideArea.transform.SetParent(sliderObject.transform, false);
            RectTransform slideAreaRect = slideArea.GetComponent<RectTransform>();
            slideAreaRect.anchorMin = Vector2.zero;
            slideAreaRect.anchorMax = Vector2.one;
            slideAreaRect.offsetMin = new Vector2(14f, 0f);
            slideAreaRect.offsetMax = new Vector2(-14f, 0f);

            GameObject handleObject = new GameObject(
                "Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            handleObject.transform.SetParent(slideArea.transform, false);
            RectTransform handleRect = handleObject.GetComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0f, 0f);
            handleRect.anchorMax = new Vector2(0f, 1f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.sizeDelta = new Vector2(28f, 0f);

            Image handleImage = handleObject.GetComponent<Image>();
            handleImage.color = new Color32(235, 238, 244, 255);
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
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

            if (randomizePowerButton != null)
            {
                randomizePowerButton.onClick.RemoveListener(OnRandomizeClicked);
            }

            if (fuseButton != null)
            {
                fuseButton.onClick.RemoveListener(OnFuseClicked);
            }

            if (reserveDeployButton != null)
            {
                reserveDeployButton.onClick.RemoveListener(OnReserveDeployClicked);
            }

            if (reservePanelView != null)
            {
                reservePanelView.ReservePieceSelected -= OnReservePieceSelected;
            }

            if (resultButton != null)
            {
                resultButton.onClick.RemoveListener(OnStartScreenClicked);
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

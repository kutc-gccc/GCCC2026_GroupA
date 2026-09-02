using System;
using System.Collections.Generic;
using GCCC.BoardGame.Core.Model;
using GCCC.BoardGame.Presentation;
using GCCC.BoardGame.Presentation.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace GCCC.BoardGame.Presentation.Views
{
    public sealed class GameHudView : MonoBehaviour, IGameHud
    {
        // モード中のボタンは押し込んで見せる。Prefabの既定色と揃えている。
        private static readonly Color InactiveModeButtonColor =
            new Color32(235, 238, 244, 255);

        private static readonly Color ActiveModeButtonColor =
            new Color32(38, 54, 77, 255);

        private static readonly Color InactiveModeButtonTextColor =
            new Color32(35, 41, 52, 255);

        private static readonly Color ActiveModeButtonTextColor = Color.white;

        private static readonly Color InactiveModeButtonBorderColor =
            new Color32(158, 166, 178, 255);

        [Header("Status")]
        [SerializeField] private Text statusLabel;
        [SerializeField] private Text messageLabel;
        [SerializeField] private ScrollRect messageScrollRect;

        [Header("Controls")]
        [SerializeField] private Button resetButton;
        [SerializeField] private Button randomizePowerButton;
        [SerializeField] private Button fuseButton;
        [SerializeField] private Button reserveDeployButton;
        [SerializeField] private RectTransform audioControlsRect;
        [SerializeField] private Slider bgmSlider;
        [SerializeField] private Slider sfxSlider;

        [Header("Reserve and effects")]
        [SerializeField] private ReservePanelView reservePanelView;
        [SerializeField] private GameObject effectLegend;

        // 凡例のうち特殊マスの2行だけは、盤に特殊マスがある設定でのみ出す。
        // 残りの4行（選択中・移動可能・戦闘可能・合体候補）は常に必要なので、枠ごと消さない。
        [SerializeField] private GameObject whileOccupiedLegendRow;
        [SerializeField] private GameObject permanentLegendRow;
        [SerializeField] private Font uiFont;

        [Header("Result")]
        // ゲーム中でも遊び方を読めるようにする。タイトルと同じPrefabを重ねる。
        [SerializeField] private GameObject howToPage;
        [SerializeField] private Button howToButton;
        [SerializeField] private Button howToCloseButton;

        [SerializeField] private GameObject resultOverlay;
        [SerializeField] private Text resultLabel;
        [SerializeField] private Button resultButton;

        private bool randomizeButtonInteractable;
        private bool fuseButtonInteractable;
        private bool reserveDeployButtonInteractable;
        private bool isInitialized;
        private BoardGameAudioManager audioManager;
        private string reserveText = string.Empty;

        public event Action ResetRequested;
        public event Action OnRandomizePowerButtonClicked;
        public event Action FuseRequested;
        public event Action ReserveDeployRequested;
        public event Action<PieceId> ReservePieceSelected;
        public event Action StartScreenRequested;

        public string StatusText => statusLabel != null ? statusLabel.text : string.Empty;
        public string MessageText => messageLabel != null ? messageLabel.text : string.Empty;
        public string ResultText => resultLabel != null ? resultLabel.text : string.Empty;
        public bool IsResultVisible => resultOverlay != null && resultOverlay.activeSelf;

        /// <summary>遊び方を重ねて表示しているか。開いている間は盤面と操作を止める。</summary>
        public bool IsHowToVisible => howToPage != null && howToPage.activeSelf;

        /// <summary>結果か遊び方が重なっていて、背景を操作できない状態か。</summary>
        public bool IsOverlayVisible => IsResultVisible || IsHowToVisible;
        public Button RandomizePowerButton => randomizePowerButton;
        public Button ReserveDeployButton => reserveDeployButton;
        public string ReserveText => reserveText;
        public int ReserveCardCount =>
            reservePanelView != null ? reservePanelView.CardCount : 0;
        /// <summary>特殊マスの2行（滞在中効果・永続効果）が出ているかどうか。</summary>
        public bool IsEffectLegendVisible =>
            whileOccupiedLegendRow != null && whileOccupiedLegendRow.activeSelf &&
            permanentLegendRow != null && permanentLegendRow.activeSelf;

        public void Initialize()
        {
            Initialize(null, null, null);
        }

        public void Initialize(BoardGameAudioManager manager)
        {
            Initialize(manager, null, null);
        }

        public void Initialize(
            BoardGameAudioManager manager,
            Sprite player1PieceSprite,
            Sprite player2PieceSprite)
        {
            if (!ValidateRequiredReferences())
            {
                Debug.LogError(
                    "GameHudView requires the configured GameHud prefab. " +
                    "No runtime fallback UI will be generated.",
                    this);
                enabled = false;
                return;
            }

            UnbindListeners();
            audioManager = manager;
            reservePanelView.Initialize(
                uiFont, player1PieceSprite, player2PieceSprite);
            reservePanelView.ReservePieceSelected += OnReservePieceSelected;

            resetButton.onClick.AddListener(OnResetClicked);
            randomizePowerButton.onClick.AddListener(OnRandomizeClicked);
            fuseButton.onClick.AddListener(OnFuseClicked);
            reserveDeployButton.onClick.AddListener(OnReserveDeployClicked);
            resultButton.onClick.AddListener(OnStartScreenClicked);
            howToButton.onClick.AddListener(OpenHowTo);
            howToCloseButton.onClick.AddListener(CloseHowTo);

            if (audioManager != null)
            {
                bgmSlider.SetValueWithoutNotify(audioManager.BgmVolume);
                sfxSlider.SetValueWithoutNotify(audioManager.SfxVolume);
                bgmSlider.onValueChanged.AddListener(audioManager.SetBgmVolume);
                sfxSlider.onValueChanged.AddListener(audioManager.SetSfxVolume);
                audioControlsRect.gameObject.SetActive(true);
            }
            else
            {
                audioControlsRect.gameObject.SetActive(false);
            }

            isInitialized = true;
            HideResult();
        }

        public void Render(GameSnapshot snapshot)
        {
            if (!isInitialized || snapshot == null)
            {
                return;
            }

            reserveText =
                $"リザーブ　プレイヤー1: {snapshot.GetPlayer(PlayerId.Player1).ReservePieces.Count}" +
                $"　プレイヤー2: {snapshot.GetPlayer(PlayerId.Player2).ReservePieces.Count}";

            reservePanelView.Render(snapshot);
            // 凡例の枠は常に出す。Prefabでは非アクティブ保存なので明示的に有効化する。
            effectLegend.SetActive(true);

            bool hasCellEffects = snapshot.CellEffectDefinitions.Count > 0;
            whileOccupiedLegendRow.SetActive(hasCellEffects);
            permanentLegendRow.SetActive(hasCellEffects);

            if (snapshot.Winner.HasValue)
            {
                string text = $"{PlayerLabel(snapshot.Winner.Value)}の勝利";
                statusLabel.text = text;
                ShowResult(text);
                return;
            }

            if (snapshot.IsDraw)
            {
                statusLabel.text = "引き分け";
                ShowResult("引き分け");
                return;
            }

            HideResult();
            statusLabel.text = $"{PlayerLabel(snapshot.CurrentPlayer)}のターン";
        }

        /// <summary>
        /// 駒は両プレイヤーとも濃緑で、所有者は三角の向きでしか区別できない。
        /// 所有者を出す表示には盤面と同じ ▲▼ を必ず添える。
        /// </summary>
        private static string PlayerLabel(PlayerId player)
        {
            return player == PlayerId.Player1
                ? "▲ プレイヤー1"
                : "▼ プレイヤー2";
        }

        public bool IsPointerOverControl(Vector2 screenPosition)
        {
            if (IsOverlayVisible)
            {
                return true;
            }

            return IsPointerOver(howToButton, screenPosition) ||
                   IsPointerOver(resetButton, screenPosition) ||
                   IsPointerOver(randomizePowerButton, screenPosition) ||
                   IsPointerOver(fuseButton, screenPosition) ||
                   IsPointerOver(reserveDeployButton, screenPosition) ||
                   IsPointerOverRect(messageScrollRect != null
                       ? messageScrollRect.transform as RectTransform : null, screenPosition) ||
                   IsPointerOverRect(audioControlsRect, screenPosition) ||
                   reservePanelView.IsPointerOver(screenPosition);
        }

        /// <summary>重なりの出入りで、覚えている可否をそのまま引き直す。</summary>
        private void RefreshControlInteractivity()
        {
            SetRandomizeButtonInteractable(randomizeButtonInteractable);
            SetFuseButtonInteractable(fuseButtonInteractable);
            SetReserveDeployButtonInteractable(reserveDeployButtonInteractable);
            if (messageScrollRect != null)
            {
                messageScrollRect.enabled = !IsOverlayVisible;
            }
        }

        public void SetRandomizeButtonInteractable(bool interactable)
        {
            randomizeButtonInteractable = interactable;
            if (randomizePowerButton != null)
            {
                randomizePowerButton.interactable = interactable && !IsOverlayVisible;
            }
        }

        public void SetFuseButtonInteractable(bool interactable)
        {
            fuseButtonInteractable = interactable;
            if (fuseButton != null)
            {
                fuseButton.interactable = interactable && !IsOverlayVisible;
            }
        }

        public void SetReserveDeployButtonInteractable(bool interactable)
        {
            reserveDeployButtonInteractable = interactable;
            if (reserveDeployButton != null)
            {
                reserveDeployButton.interactable = interactable && !IsOverlayVisible;
            }
        }

        /// <summary>
        /// 「合体」「リザーブ配置」はトグルなので、押した後の状態をボタンに出す。
        /// 有効・無効とは別の軸なので、色で押し込みを表現する。
        /// </summary>
        public void SetFuseModeActive(bool active)
        {
            ApplyModeAppearance(fuseButton, active);
        }

        public void SetReserveDeployModeActive(bool active)
        {
            ApplyModeAppearance(reserveDeployButton, active);
        }

        private static void ApplyModeAppearance(Button button, bool active)
        {
            if (button == null)
            {
                return;
            }

            Image background = button.targetGraphic as Image;
            if (background == null)
            {
                return;
            }

            background.color = active
                ? ActiveModeButtonColor
                : InactiveModeButtonColor;

            Transform labelTransform = button.transform.Find("Label");
            Text label = labelTransform != null
                ? labelTransform.GetComponent<Text>()
                : null;
            if (label != null)
            {
                label.color = active
                    ? ActiveModeButtonTextColor
                    : InactiveModeButtonTextColor;
            }

            Color borderColor = active
                ? ActiveModeButtonColor
                : InactiveModeButtonBorderColor;
            ApplyBorderColor(button.transform, "Top Border", borderColor);
            ApplyBorderColor(button.transform, "Bottom Border", borderColor);
            ApplyBorderColor(button.transform, "Left Border", borderColor);
            ApplyBorderColor(button.transform, "Right Border", borderColor);
        }

        private static void ApplyBorderColor(
            Transform parent,
            string childName,
            Color color)
        {
            Transform edge = parent.Find(childName);
            Image edgeImage = edge != null ? edge.GetComponent<Image>() : null;
            if (edgeImage != null)
            {
                edgeImage.color = color;
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
                if (messageScrollRect != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(
                        messageScrollRect.transform as RectTransform);
                    messageLabel.rectTransform.SetSizeWithCurrentAnchors(
                        RectTransform.Axis.Vertical,
                        Mathf.Max(messageScrollRect.viewport.rect.height, messageLabel.preferredHeight + 8f));
                    messageScrollRect.StopMovement();
                    // Equal content/viewport heights have no normalized scroll range.
                    messageScrollRect.content.anchoredPosition = new Vector2(
                        messageScrollRect.content.anchoredPosition.x, 0f);
                    messageScrollRect.verticalNormalizedPosition = 1f;
                }
            }
        }

        private bool ValidateRequiredReferences()
        {
            return statusLabel != null &&
                   messageLabel != null &&
                   messageScrollRect != null &&
                   resetButton != null &&
                   randomizePowerButton != null &&
                   fuseButton != null &&
                   reserveDeployButton != null &&
                   audioControlsRect != null &&
                   bgmSlider != null &&
                   sfxSlider != null &&
                   reservePanelView != null &&
                   effectLegend != null &&
                   whileOccupiedLegendRow != null &&
                   permanentLegendRow != null &&
                   uiFont != null &&
                   howToPage != null &&
                   howToButton != null &&
                   howToCloseButton != null &&
                   resultOverlay != null &&
                   resultLabel != null &&
                   resultButton != null;
        }

        private void OnResetClicked()
        {
            if (!IsOverlayVisible)
            {
                ResetRequested?.Invoke();
            }
        }

        private void OnRandomizeClicked()
        {
            if (!IsOverlayVisible)
            {
                OnRandomizePowerButtonClicked?.Invoke();
            }
        }

        private void OnFuseClicked()
        {
            if (!IsOverlayVisible)
            {
                FuseRequested?.Invoke();
            }
        }

        private void OnReserveDeployClicked()
        {
            if (!IsOverlayVisible)
            {
                ReserveDeployRequested?.Invoke();
            }
        }

        private void OnReservePieceSelected(PieceId pieceId)
        {
            if (!IsOverlayVisible)
            {
                ReservePieceSelected?.Invoke(pieceId);
            }
        }

        /// <summary>ゲームを進めたまま遊び方を重ねる。勝敗が出ているときは結果を優先する。</summary>
        public void OpenHowTo()
        {
            if (howToPage == null || IsResultVisible)
            {
                return;
            }

            howToPage.SetActive(true);
            RefreshControlInteractivity();
        }

        public void CloseHowTo()
        {
            if (howToPage == null)
            {
                return;
            }

            howToPage.SetActive(false);
            RefreshControlInteractivity();
        }

        private void OnStartScreenClicked()
        {
            StartScreenRequested?.Invoke();
        }

        private void ShowResult(string text)
        {
            resultLabel.text = text;
            resultOverlay.SetActive(true);
            resultOverlay.transform.SetAsLastSibling();
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
            if (!isInitialized)
            {
                return;
            }

            resetButton.interactable = interactable;
            randomizePowerButton.interactable =
                interactable && randomizeButtonInteractable;
            fuseButton.interactable = interactable && fuseButtonInteractable;
            reserveDeployButton.interactable =
                interactable && reserveDeployButtonInteractable;
            bgmSlider.interactable = interactable;
            sfxSlider.interactable = interactable;
            messageScrollRect.enabled = interactable && !IsOverlayVisible;

            if (!interactable)
            {
                reservePanelView.SetDeployablePieces(Array.Empty<PieceId>());
            }
        }

        private static bool IsPointerOver(Button button, Vector2 screenPosition)
        {
            return button != null &&
                   IsPointerOverRect(button.transform as RectTransform, screenPosition);
        }

        private static bool IsPointerOverRect(
            RectTransform rect,
            Vector2 screenPosition)
        {
            return rect != null &&
                   RectTransformUtility.RectangleContainsScreenPoint(
                       rect, screenPosition);
        }

        private void UnbindListeners()
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

            if (resultButton != null)
            {
                resultButton.onClick.RemoveListener(OnStartScreenClicked);
            }

            if (howToButton != null)
            {
                howToButton.onClick.RemoveListener(OpenHowTo);
            }

            if (howToCloseButton != null)
            {
                howToCloseButton.onClick.RemoveListener(CloseHowTo);
            }

            if (reservePanelView != null)
            {
                reservePanelView.ReservePieceSelected -= OnReservePieceSelected;
            }

            if (bgmSlider != null && audioManager != null)
            {
                bgmSlider.onValueChanged.RemoveListener(audioManager.SetBgmVolume);
            }

            if (sfxSlider != null && audioManager != null)
            {
                sfxSlider.onValueChanged.RemoveListener(audioManager.SetSfxVolume);
            }
        }

        private void OnDestroy()
        {
            UnbindListeners();
        }
    }
}

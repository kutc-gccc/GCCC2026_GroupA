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
        [SerializeField] private Font uiFont;

        [Header("Result")]
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
        public string ResultText => resultLabel != null ? resultLabel.text : string.Empty;
        public bool IsResultVisible => resultOverlay != null && resultOverlay.activeSelf;
        public Button RandomizePowerButton => randomizePowerButton;
        public Button ReserveDeployButton => reserveDeployButton;
        public string ReserveText => reserveText;
        public int ReserveCardCount =>
            reservePanelView != null ? reservePanelView.CardCount : 0;
        public bool IsEffectLegendVisible =>
            effectLegend != null && effectLegend.activeSelf;

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
            effectLegend.SetActive(snapshot.CellEffectDefinitions.Count > 0);

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
            if (IsResultVisible)
            {
                return true;
            }

            return IsPointerOver(resetButton, screenPosition) ||
                   IsPointerOver(randomizePowerButton, screenPosition) ||
                   IsPointerOver(fuseButton, screenPosition) ||
                   IsPointerOver(reserveDeployButton, screenPosition) ||
                   IsPointerOverRect(audioControlsRect, screenPosition) ||
                   reservePanelView.IsPointerOver(screenPosition);
        }

        public void SetRandomizeButtonInteractable(bool interactable)
        {
            randomizeButtonInteractable = interactable;
            if (randomizePowerButton != null)
            {
                randomizePowerButton.interactable = interactable && !IsResultVisible;
            }
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
            }
        }

        private bool ValidateRequiredReferences()
        {
            return statusLabel != null &&
                   messageLabel != null &&
                   resetButton != null &&
                   randomizePowerButton != null &&
                   fuseButton != null &&
                   reserveDeployButton != null &&
                   audioControlsRect != null &&
                   bgmSlider != null &&
                   sfxSlider != null &&
                   reservePanelView != null &&
                   effectLegend != null &&
                   uiFont != null &&
                   resultOverlay != null &&
                   resultLabel != null &&
                   resultButton != null;
        }

        private void OnResetClicked()
        {
            if (!IsResultVisible)
            {
                ResetRequested?.Invoke();
            }
        }

        private void OnRandomizeClicked()
        {
            if (!IsResultVisible)
            {
                OnRandomizePowerButtonClicked?.Invoke();
            }
        }

        private void OnFuseClicked()
        {
            if (!IsResultVisible)
            {
                FuseRequested?.Invoke();
            }
        }

        private void OnReserveDeployClicked()
        {
            if (!IsResultVisible)
            {
                ReserveDeployRequested?.Invoke();
            }
        }

        private void OnReservePieceSelected(PieceId pieceId)
        {
            if (!IsResultVisible)
            {
                ReservePieceSelected?.Invoke(pieceId);
            }
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

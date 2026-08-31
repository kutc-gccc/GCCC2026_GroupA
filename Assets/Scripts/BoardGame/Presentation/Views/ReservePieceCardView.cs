using System;
using GCCC.BoardGame.Core.Model;
using UnityEngine;
using UnityEngine.UI;

namespace GCCC.BoardGame.Presentation.Views
{
    public sealed class ReservePieceCardView : MonoBehaviour
    {
        private static readonly Color32 NormalColor =
            new Color32(64, 72, 86, 245);
        private static readonly Color32 DisabledColor =
            new Color32(54, 59, 69, 190);
        private static readonly Color32 SelectedColor =
            new Color32(92, 83, 48, 255);
        private static readonly Color32 SelectedOutlineColor =
            new Color32(255, 213, 79, 255);

        private Button button;
        private Image background;
        private Image pieceImage;
        private Text combatPowerLabel;
        private Text movementProfileLabel;
        private Outline selectionOutline;
        private PieceId pieceId;

        public event Action<PieceId> Selected;

        public PieceId PieceId => pieceId;

        public bool IsInteractable => button != null && button.interactable;

        public string CombatPowerText =>
            combatPowerLabel != null ? combatPowerLabel.text : string.Empty;

        public string MovementProfileText =>
            movementProfileLabel != null ? movementProfileLabel.text : string.Empty;

        public Sprite PieceSprite => pieceImage != null ? pieceImage.sprite : null;

        public void Initialize(
            ReservePieceState state,
            Sprite sprite,
            Font font)
        {
            pieceId = state.Id;
            EnsureUi(font);
            pieceImage.sprite = sprite;
            pieceImage.preserveAspect = true;
            combatPowerLabel.text = state.CombatPower.ToString();
            movementProfileLabel.text = state.MovementProfileId.Value;
            button.onClick.RemoveListener(HandleClick);
            button.onClick.AddListener(HandleClick);
        }

        public void SetVisualState(bool interactable, bool selected)
        {
            if (button == null)
            {
                return;
            }

            button.interactable = interactable;
            background.color = selected
                ? SelectedColor
                : interactable ? NormalColor : DisabledColor;
            selectionOutline.enabled = selected;
            pieceImage.color = interactable || selected
                ? Color.white
                : new Color(1f, 1f, 1f, 0.5f);
            combatPowerLabel.color = interactable || selected
                ? Color.white
                : new Color(1f, 1f, 1f, 0.55f);
            movementProfileLabel.color = interactable || selected
                ? new Color32(221, 226, 235, 255)
                : new Color32(170, 175, 184, 150);
        }

        private void EnsureUi(Font font)
        {
            if (button != null)
            {
                return;
            }

            RectTransform cardRect = GetComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(88f, 108f);

            background = GetComponent<Image>();
            if (background == null)
            {
                background = gameObject.AddComponent<Image>();
            }

            button = GetComponent<Button>();
            if (button == null)
            {
                button = gameObject.AddComponent<Button>();
            }

            button.targetGraphic = background;

            selectionOutline = GetComponent<Outline>();
            if (selectionOutline == null)
            {
                selectionOutline = gameObject.AddComponent<Outline>();
            }

            selectionOutline.effectColor = SelectedOutlineColor;
            selectionOutline.effectDistance = new Vector2(3f, -3f);
            selectionOutline.enabled = false;

            pieceImage = CreateImage("Piece Sprite", transform);
            RectTransform pieceRect = pieceImage.rectTransform;
            pieceRect.anchorMin = new Vector2(0.5f, 1f);
            pieceRect.anchorMax = new Vector2(0.5f, 1f);
            pieceRect.pivot = new Vector2(0.5f, 1f);
            pieceRect.anchoredPosition = new Vector2(0f, -6f);
            pieceRect.sizeDelta = new Vector2(58f, 58f);
            pieceImage.raycastTarget = false;

            combatPowerLabel = CreateText(
                "Combat Power", transform, font, 28, TextAnchor.MiddleCenter);
            RectTransform powerRect = combatPowerLabel.rectTransform;
            powerRect.anchorMin = new Vector2(0.5f, 1f);
            powerRect.anchorMax = new Vector2(0.5f, 1f);
            powerRect.pivot = new Vector2(0.5f, 1f);
            powerRect.anchoredPosition = new Vector2(0f, -15f);
            powerRect.sizeDelta = new Vector2(58f, 40f);
            combatPowerLabel.fontStyle = FontStyle.Bold;

            movementProfileLabel = CreateText(
                "Movement Profile", transform, font, 14, TextAnchor.MiddleCenter);
            RectTransform profileRect = movementProfileLabel.rectTransform;
            profileRect.anchorMin = new Vector2(0.5f, 0f);
            profileRect.anchorMax = new Vector2(0.5f, 0f);
            profileRect.pivot = new Vector2(0.5f, 0f);
            profileRect.anchoredPosition = new Vector2(0f, 5f);
            profileRect.sizeDelta = new Vector2(82f, 26f);
        }

        private void HandleClick()
        {
            Selected?.Invoke(pieceId);
        }

        private static Image CreateImage(string name, Transform parent)
        {
            GameObject imageObject = new GameObject(
                name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            return imageObject.GetComponent<Image>();
        }

        private static Text CreateText(
            string name,
            Transform parent,
            Font font,
            int fontSize,
            TextAnchor alignment)
        {
            GameObject textObject = new GameObject(
                name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClick);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using GCCC.BoardGame.Core.Model;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace GCCC.BoardGame.Presentation.Views
{
    public sealed class ReservePanelView : MonoBehaviour
    {
        private static readonly Color32 PanelColor =
            new Color32(35, 41, 52, 225);

        private readonly Dictionary<PieceId, ReservePieceCardView> cards =
            new Dictionary<PieceId, ReservePieceCardView>();
        private readonly HashSet<PieceId> deployablePieceIds =
            new HashSet<PieceId>();

        private RectTransform player1PanelRect;
        private RectTransform player2PanelRect;
        private Transform player1CardsRoot;
        private Transform player2CardsRoot;
        private Text player1Header;
        private Text player2Header;
        private Text player1EmptyLabel;
        private Text player2EmptyLabel;
        private Font font;
        private Sprite player1Sprite;
        private Sprite player2Sprite;
        private PlayerId currentPlayer;
        private bool isGameOver;
        private PieceId? selectedPieceId;

        public event Action<PieceId> ReservePieceSelected;

        public int CardCount => cards.Count;

        public void Initialize(
            Transform parent,
            Font uiFont,
            Sprite player1PieceSprite,
            Sprite player2PieceSprite)
        {
            transform.SetParent(parent, false);
            RectTransform rootRect = GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            font = uiFont;
            player1Sprite = player1PieceSprite;
            player2Sprite = player2PieceSprite;

            player2PanelRect = CreatePlayerPanel(
                "Player 2 Reserve Panel",
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-24f, -128f),
                PlayerId.Player2,
                out player2Header,
                out player2CardsRoot,
                out player2EmptyLabel);

            player1PanelRect = CreatePlayerPanel(
                "Player 1 Reserve Panel",
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(-24f, 128f),
                PlayerId.Player1,
                out player1Header,
                out player1CardsRoot,
                out player1EmptyLabel);
        }

        public void Render(GameSnapshot snapshot)
        {
            currentPlayer = snapshot.CurrentPlayer;
            isGameOver = snapshot.IsGameOver;

            IReadOnlyList<ReservePieceState> player1Reserves =
                snapshot.GetPlayer(PlayerId.Player1).ReservePieces;
            IReadOnlyList<ReservePieceState> player2Reserves =
                snapshot.GetPlayer(PlayerId.Player2).ReservePieces;

            player1Header.text = $"プレイヤー1 リザーブ: {player1Reserves.Count}";
            player2Header.text = $"プレイヤー2 リザーブ: {player2Reserves.Count}";
            player1EmptyLabel.gameObject.SetActive(player1Reserves.Count == 0);
            player2EmptyLabel.gameObject.SetActive(player2Reserves.Count == 0);

            HashSet<PieceId> visibleIds = new HashSet<PieceId>(
                player1Reserves.Select(piece => piece.Id)
                    .Concat(player2Reserves.Select(piece => piece.Id)));
            foreach (PieceId staleId in cards.Keys
                         .Where(id => !visibleIds.Contains(id))
                         .ToArray())
            {
                RemoveCard(staleId);
            }

            RenderPlayerCards(player1Reserves, player1CardsRoot, player1Sprite);
            RenderPlayerCards(player2Reserves, player2CardsRoot, player2Sprite);

            if (selectedPieceId.HasValue && !visibleIds.Contains(selectedPieceId.Value))
            {
                selectedPieceId = null;
            }

            RefreshCardStates();
        }

        public void SetDeployablePieces(IEnumerable<PieceId> pieceIds)
        {
            deployablePieceIds.Clear();
            if (pieceIds != null)
            {
                foreach (PieceId pieceId in pieceIds)
                {
                    deployablePieceIds.Add(pieceId);
                }
            }

            RefreshCardStates();
        }

        public void SetSelectedPiece(PieceId? pieceId)
        {
            selectedPieceId = pieceId;
            RefreshCardStates();
        }

        public bool IsPointerOver(Vector2 screenPosition)
        {
            return IsPointerOverRect(player1PanelRect, screenPosition) ||
                   IsPointerOverRect(player2PanelRect, screenPosition);
        }

        public ReservePieceCardView GetCard(PieceId pieceId)
        {
            cards.TryGetValue(pieceId, out ReservePieceCardView card);
            return card;
        }

        private void RenderPlayerCards(
            IReadOnlyList<ReservePieceState> reservePieces,
            Transform cardsRoot,
            Sprite sprite)
        {
            for (int index = 0; index < reservePieces.Count; index++)
            {
                ReservePieceState state = reservePieces[index];
                if (!cards.TryGetValue(state.Id, out ReservePieceCardView card))
                {
                    card = CreateCard(state, cardsRoot, sprite);
                    cards.Add(state.Id, card);
                }

                RectTransform cardRect = card.GetComponent<RectTransform>();
                cardRect.SetParent(cardsRoot, false);
                cardRect.anchoredPosition = new Vector2(index * 96f, 0f);
                card.Initialize(state, sprite, font);
            }
        }

        private ReservePieceCardView CreateCard(
            ReservePieceState state,
            Transform parent,
            Sprite sprite)
        {
            GameObject cardObject = new GameObject(
                $"Reserve Card {state.Id.Value}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(Outline),
                typeof(ReservePieceCardView));
            cardObject.transform.SetParent(parent, false);
            ReservePieceCardView card = cardObject.GetComponent<ReservePieceCardView>();
            card.Initialize(state, sprite, font);
            card.Selected += HandleCardSelected;
            return card;
        }

        private RectTransform CreatePlayerPanel(
            string objectName,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 anchoredPosition,
            PlayerId player,
            out Text header,
            out Transform cardsRoot,
            out Text emptyLabel)
        {
            GameObject panelObject = new GameObject(
                objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelObject.transform.SetParent(transform, false);
            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = anchor;
            panelRect.anchorMax = anchor;
            panelRect.pivot = pivot;
            panelRect.anchoredPosition = anchoredPosition;
            panelRect.sizeDelta = new Vector2(620f, 170f);
            panelObject.GetComponent<Image>().color = PanelColor;

            header = CreateText(
                "Header", panelObject.transform, font, 22, TextAnchor.MiddleLeft);
            RectTransform headerRect = header.rectTransform;
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(0f, 1f);
            headerRect.pivot = new Vector2(0f, 1f);
            headerRect.anchoredPosition = new Vector2(16f, -8f);
            headerRect.sizeDelta = new Vector2(588f, 32f);
            header.color = player == PlayerId.Player1
                ? new Color32(134, 196, 255, 255)
                : new Color32(255, 145, 145, 255);

            GameObject cardsObject = new GameObject("Cards", typeof(RectTransform));
            cardsObject.transform.SetParent(panelObject.transform, false);
            RectTransform cardsRect = cardsObject.GetComponent<RectTransform>();
            cardsRect.anchorMin = new Vector2(0f, 1f);
            cardsRect.anchorMax = new Vector2(0f, 1f);
            cardsRect.pivot = new Vector2(0f, 1f);
            cardsRect.anchoredPosition = new Vector2(16f, -50f);
            cardsRect.sizeDelta = new Vector2(588f, 108f);
            cardsRoot = cardsObject.transform;

            emptyLabel = CreateText(
                "Empty", panelObject.transform, font, 20, TextAnchor.MiddleCenter);
            RectTransform emptyRect = emptyLabel.rectTransform;
            emptyRect.anchorMin = new Vector2(0.5f, 0.5f);
            emptyRect.anchorMax = new Vector2(0.5f, 0.5f);
            emptyRect.pivot = new Vector2(0.5f, 0.5f);
            emptyRect.anchoredPosition = new Vector2(0f, -18f);
            emptyRect.sizeDelta = new Vector2(560f, 52f);
            emptyLabel.text = "リザーブなし";
            emptyLabel.color = new Color32(190, 195, 204, 255);

            return panelRect;
        }

        private void RefreshCardStates()
        {
            foreach (ReservePieceCardView card in cards.Values)
            {
                bool selected = selectedPieceId.HasValue &&
                    card.PieceId == selectedPieceId.Value;
                bool interactable = !isGameOver &&
                    deployablePieceIds.Contains(card.PieceId) &&
                    IsCurrentPlayerCard(card.PieceId);
                card.SetVisualState(interactable, selected);
            }
        }

        private bool IsCurrentPlayerCard(PieceId pieceId)
        {
            ReservePieceCardView card = cards[pieceId];
            return card.transform.parent ==
                (currentPlayer == PlayerId.Player1
                    ? player1CardsRoot
                    : player2CardsRoot);
        }

        private void HandleCardSelected(PieceId pieceId)
        {
            ReservePieceSelected?.Invoke(pieceId);
        }

        private void RemoveCard(PieceId pieceId)
        {
            if (!cards.TryGetValue(pieceId, out ReservePieceCardView card))
            {
                return;
            }

            card.Selected -= HandleCardSelected;
            cards.Remove(pieceId);
            DestroyGeneratedObject(card.gameObject);
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

        private static bool IsPointerOverRect(
            RectTransform rect,
            Vector2 screenPosition)
        {
            return rect != null && RectTransformUtility.RectangleContainsScreenPoint(
                rect, screenPosition);
        }

        private static void DestroyGeneratedObject(Object generatedObject)
        {
            if (generatedObject == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(generatedObject);
            }
            else
            {
                Object.DestroyImmediate(generatedObject);
            }
        }

        private void OnDestroy()
        {
            foreach (ReservePieceCardView card in cards.Values)
            {
                if (card != null)
                {
                    card.Selected -= HandleCardSelected;
                }
            }

            cards.Clear();
        }
    }
}

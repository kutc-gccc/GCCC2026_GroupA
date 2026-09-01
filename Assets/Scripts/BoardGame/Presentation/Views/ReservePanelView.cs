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
        [SerializeField] private RectTransform player1PanelRect;
        [SerializeField] private RectTransform player2PanelRect;
        [SerializeField] private Transform player1CardsRoot;
        [SerializeField] private Transform player2CardsRoot;
        [SerializeField] private Text player1Header;
        [SerializeField] private Text player2Header;
        [SerializeField] private ReservePieceCardView cardPrefab;

        private readonly Dictionary<PieceId, ReservePieceCardView> cards =
            new Dictionary<PieceId, ReservePieceCardView>();
        private readonly HashSet<PieceId> deployablePieceIds =
            new HashSet<PieceId>();

        private Font font;
        private Sprite player1Sprite;
        private Sprite player2Sprite;
        private PlayerId currentPlayer;
        private bool isGameOver;
        private bool isInitialized;
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
            Initialize(uiFont, player1PieceSprite, player2PieceSprite);
        }

        public void Initialize(
            Font uiFont,
            Sprite player1PieceSprite,
            Sprite player2PieceSprite)
        {
            if (!ValidateRequiredReferences())
            {
                Debug.LogError(
                    "ReservePanelView requires its prefab panel references.",
                    this);
                enabled = false;
                return;
            }

            font = uiFont;
            player1Sprite = player1PieceSprite;
            player2Sprite = player2PieceSprite;
            isInitialized = true;
        }

        public void Render(GameSnapshot snapshot)
        {
            if (!isInitialized || snapshot == null)
            {
                return;
            }

            currentPlayer = snapshot.CurrentPlayer;
            isGameOver = snapshot.IsGameOver;

            IReadOnlyList<ReservePieceState> player1Reserves =
                snapshot.GetPlayer(PlayerId.Player1).ReservePieces;
            IReadOnlyList<ReservePieceState> player2Reserves =
                snapshot.GetPlayer(PlayerId.Player2).ReservePieces;

            player1Header.text = $"プレイヤー1 リザーブ: {player1Reserves.Count}";
            player2Header.text = $"プレイヤー2 リザーブ: {player2Reserves.Count}";
            player1CardsRoot.gameObject.SetActive(player1Reserves.Count > 0);
            player2CardsRoot.gameObject.SetActive(player2Reserves.Count > 0);

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

            if (selectedPieceId.HasValue &&
                !visibleIds.Contains(selectedPieceId.Value))
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

        private bool ValidateRequiredReferences()
        {
            return player1PanelRect != null &&
                   player2PanelRect != null &&
                   player1CardsRoot != null &&
                   player2CardsRoot != null &&
                   player1Header != null &&
                   player2Header != null;
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

                card.transform.SetParent(cardsRoot, false);
                card.transform.SetSiblingIndex(index);
                card.Initialize(state, sprite, font);
            }
        }

        private ReservePieceCardView CreateCard(
            ReservePieceState state,
            Transform parent,
            Sprite sprite)
        {
            ReservePieceCardView card;
            if (cardPrefab != null)
            {
                card = Instantiate(cardPrefab, parent);
            }
            else
            {
                GameObject cardObject = new GameObject(
                    $"Reserve Card {state.Id.Value}",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Button),
                    typeof(Outline),
                    typeof(LayoutElement),
                    typeof(ReservePieceCardView));
                cardObject.transform.SetParent(parent, false);
                LayoutElement layout = cardObject.GetComponent<LayoutElement>();
                layout.preferredWidth = 88f;
                layout.preferredHeight = 104f;
                card = cardObject.GetComponent<ReservePieceCardView>();
            }

            card.gameObject.name = $"Reserve Card {state.Id.Value}";
            card.Initialize(state, sprite, font);
            card.Selected += HandleCardSelected;
            return card;
        }

        private void RefreshCardStates()
        {
            foreach (ReservePieceCardView card in cards.Values)
            {
                bool selected = selectedPieceId.HasValue &&
                    card.PieceId == selectedPieceId.Value;
                bool interactable = !isGameOver &&
                    deployablePieceIds.Contains(card.PieceId) &&
                    IsCurrentPlayerCard(card);
                card.SetVisualState(interactable, selected);
            }
        }

        private bool IsCurrentPlayerCard(ReservePieceCardView card)
        {
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

        private static bool IsPointerOverRect(
            RectTransform rect,
            Vector2 screenPosition)
        {
            return rect != null &&
                   RectTransformUtility.RectangleContainsScreenPoint(
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

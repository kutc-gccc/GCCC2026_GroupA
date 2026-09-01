using System.Collections.Generic;
using System.Linq;
using GCCC.BoardGame.Core.Events;
using GCCC.BoardGame.Core.Model;
using GCCC.BoardGame.Presentation;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GCCC.BoardGame.Presentation.Views
{
    public sealed class PieceViewManager : MonoBehaviour, IPieceViewCollection
    {
        [SerializeField] private PieceView pieceViewPrefab;

        private readonly Dictionary<PieceId, PieceView>
            pieceViews =
                new Dictionary<PieceId, PieceView>();

        private Sprite player1PieceSprite;
        private Sprite player2PieceSprite;

        private int columns;
        private int rows;

        public int PieceViewCount =>
            pieceViews.Count;

        public void Initialize(
            Sprite player1Sprite,
            Sprite player2Sprite,
            GameSnapshot snapshot)
        {
            player1PieceSprite =
                player1Sprite;

            player2PieceSprite =
                player2Sprite;

            columns =
                snapshot.Columns;

            rows =
                snapshot.Rows;

            Rebuild(snapshot);
        }

        public void Rebuild(
            GameSnapshot snapshot)
        {
            columns =
                snapshot.Columns;

            rows =
                snapshot.Rows;

            Reconcile(snapshot);
        }

        public void Reconcile(GameSnapshot snapshot)
        {
            columns = snapshot.Columns;
            rows = snapshot.Rows;

            HashSet<PieceId> visibleIds = new HashSet<PieceId>(
                snapshot.Pieces.Select(piece => piece.Id));
            foreach (PieceId removedId in pieceViews.Keys
                         .Where(id => !visibleIds.Contains(id))
                         .ToArray())
            {
                RemovePieceView(removedId);
            }

            foreach (PieceState state in snapshot.Pieces)
            {
                if (pieceViews.TryGetValue(state.Id, out PieceView existingView) &&
                    existingView != null)
                {
                    existingView.Render(state, GetPieceSprite(state.Owner));
                }
                else
                {
                    CreatePieceView(state);
                }
            }
        }

        public void ApplyEvents(
            IReadOnlyList<GameEvent> events,
            GameSnapshot snapshot)
        {
            Reconcile(snapshot);
        }

        private void CreatePieceView(
            PieceState state)
        {
            Sprite sprite =
                GetPieceSprite(state.Owner);

            PieceView pieceView;

            if (pieceViewPrefab != null)
            {
                pieceView =
                    Instantiate(
                        pieceViewPrefab,
                        transform);
            }
            else
            {
                GameObject pieceObject =
                    new GameObject(
                        $"Piece ({state.Id})");

                pieceObject.transform.SetParent(
                    transform,
                    false);

                pieceView =
                    pieceObject.AddComponent<PieceView>();
            }

            pieceView.gameObject.name =
                $"Piece ({state.Owner}) [{state.Id}]";

            pieceView.Initialize(
                state,
                sprite,
                columns,
                rows);

            pieceViews[state.Id] =
                pieceView;
        }

        private Sprite GetPieceSprite(
            PlayerId owner)
        {
            if (owner == PlayerId.Player1)
            {
                return player1PieceSprite;
            }

            return player2PieceSprite;
        }

        private void ClearAll()
        {
            foreach (PieceView pieceView
                     in pieceViews.Values)
            {
                if (pieceView != null)
                {
                    DestroyGeneratedObject(
                        pieceView.gameObject);
                }
            }

            pieceViews.Clear();
        }

        private void RemovePieceView(PieceId id)
        {
            if (!pieceViews.TryGetValue(id, out PieceView pieceView))
            {
                return;
            }

            pieceViews.Remove(id);
            if (pieceView != null)
            {
                DestroyGeneratedObject(pieceView.gameObject);
            }
        }

        private static void DestroyGeneratedObject(
            Object generatedObject)
        {
            if (generatedObject == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(
                    generatedObject);
            }
            else
            {
                Object.DestroyImmediate(
                    generatedObject);
            }
        }

        private void OnDestroy()
        {
            ClearAll();
        }
    }
}

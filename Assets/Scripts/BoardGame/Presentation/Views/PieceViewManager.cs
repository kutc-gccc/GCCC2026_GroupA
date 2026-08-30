using System.Collections.Generic;
using GCCC.BoardGame.Core.Events;
using GCCC.BoardGame.Core.Model;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GCCC.BoardGame.Presentation.Views
{
    public sealed class PieceViewManager : MonoBehaviour
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
            ClearAll();

            columns =
                snapshot.Columns;

            rows =
                snapshot.Rows;

            foreach (PieceState state
                     in snapshot.Pieces)
            {
                CreatePieceView(state);
            }
        }

        public void ApplyEvents(
            IReadOnlyList<GameEvent> events,
            GameSnapshot snapshot)
        {
            // 現在はイベントごとのアニメーションよりも
            // 最新のGameSnapshotを確実に表示することを優先する。
            //
            // そのため、移動・撃破・合体・戦闘力変更など、
            // どのイベントが発生しても盤面を再構築する。

            Rebuild(snapshot);
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
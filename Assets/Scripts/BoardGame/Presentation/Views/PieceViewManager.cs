using System.Collections.Generic;
using GCCC.BoardGame.Core.Events;
using GCCC.BoardGame.Core.Model;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GCCC.BoardGame.Presentation.Views
{
    public sealed class PieceViewManager : MonoBehaviour
    {
        private readonly Dictionary<PieceId, PieceView> views =
            new Dictionary<PieceId, PieceView>();

        // Player1用・Player2用の2種類のSprite
        private Sprite player1Sprite;
        private Sprite player2Sprite;

        private int columns;
        private int rows;

        public int PieceViewCount => views.Count;

        public void Initialize(
            Sprite player1PieceSprite,
            Sprite player2PieceSprite,
            GameSnapshot snapshot)
        {
            player1Sprite = player1PieceSprite;
            player2Sprite = player2PieceSprite;

            columns = snapshot.Columns;
            rows = snapshot.Rows;

            Rebuild(snapshot);
        }

        public void ApplyEvents(
            IReadOnlyList<GameEvent> events,
            GameSnapshot snapshot)
        {
            foreach (GameEvent gameEvent in events)
            {
                switch (gameEvent)
                {
                    case PieceDestroyed destroyed:
                        RemoveView(destroyed.PieceId);
                        break;

                    case PieceMoved moved:
                        RenderFromSnapshot(moved.PieceId, snapshot);
                        break;

                    case PiecePowerChanged powerChanged:
                        RenderFromSnapshot(powerChanged.PieceId, snapshot);
                        break;

                    case PiecesFused fused:
                        RemoveView(fused.FirstPieceId);
                        RemoveView(fused.SecondPieceId);
                        RenderFromSnapshot(fused.ResultingPieceId, snapshot);
                        break;

                    case CellEffectTriggered effectTriggered:
                        RenderFromSnapshot(effectTriggered.PieceId, snapshot);
                        break;
                }
            }
        }

        public void Rebuild(GameSnapshot snapshot)
        {
            foreach (PieceView view in views.Values)
            {
                DestroyGeneratedObject(view.gameObject);
            }

            views.Clear();

            foreach (PieceState piece in snapshot.Pieces)
            {
                CreateView(piece);
            }
        }

        private void RenderFromSnapshot(
            PieceId id,
            GameSnapshot snapshot)
        {
            if (!snapshot.TryGetPiece(id, out PieceState piece))
            {
                return;
            }

            if (views.TryGetValue(id, out PieceView existing))
            {
                existing.Render(piece);
                return;
            }

            CreateView(piece);
        }

        private void CreateView(PieceState piece)
        {
            GameObject pieceObject =
                new GameObject($"Piece View {piece.Id.Value}");

            pieceObject.transform.SetParent(transform, false);

            PieceView view =
                pieceObject.AddComponent<PieceView>();

            // プレイヤーによって使用する三角形を変更
            Sprite sprite =
                piece.Owner == PlayerId.Player1
                    ? player1Sprite
                    : player2Sprite;

            view.Initialize(
                piece,
                sprite,
                columns,
                rows);

            views.Add(piece.Id, view);
        }

        private void RemoveView(PieceId id)
        {
            if (!views.TryGetValue(id, out PieceView view))
            {
                return;
            }

            views.Remove(id);
            DestroyGeneratedObject(view.gameObject);
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
                Object.Destroy(generatedObject);
            }
            else
            {
                Object.DestroyImmediate(generatedObject);
            }
        }
    }
}
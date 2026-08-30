using GCCC.BoardGame.Core.Model;
using UnityEngine;

namespace GCCC.BoardGame.Presentation.Views
{
    public sealed class PieceView : MonoBehaviour
    {
        // 駒の大きさ
        private const float PieceScale = 0.45f;

        // 駒の色
        private static readonly Color PieceColor =
            Color.white;

        private SpriteRenderer pieceRenderer;
        private TextMesh combatPowerLabel;

        private int columns;
        private int rows;

        private PieceState state;

        public PieceId PieceId =>
            state != null
                ? state.Id
                : default;

        public void Initialize(
            PieceState pieceState,
            Sprite pieceSprite,
            int boardColumns,
            int boardRows)
        {
            state = pieceState;

            columns = boardColumns;
            rows = boardRows;

            EnsureRenderer();

            pieceRenderer.sprite =
                pieceSprite;

            pieceRenderer.color =
                PieceColor;

            Render();
        }

        public void Render(
            PieceState pieceState,
            Sprite pieceSprite)
        {
            state = pieceState;

            EnsureRenderer();

            pieceRenderer.sprite =
                pieceSprite;

            pieceRenderer.color =
                PieceColor;

            Render();
        }

        private void Render()
        {
            if (state == null)
            {
                return;
            }

            transform.localPosition =
                BoardGeometry.CellToLocalPosition(
                    state.Position,
                    columns,
                    rows);

            transform.localScale =
                Vector3.one * PieceScale;

            UpdateCombatPowerLabel();
        }

        private void EnsureRenderer()
        {
            // ========================================
            // 駒のSpriteRenderer
            // ========================================

            if (pieceRenderer == null)
            {
                pieceRenderer =
                    GetComponent<SpriteRenderer>();

                if (pieceRenderer == null)
                {
                    pieceRenderer =
                        gameObject.AddComponent<SpriteRenderer>();
                }
            }

            pieceRenderer.sortingOrder = 10;

            // ========================================
            // 戦闘力表示
            // ========================================

            if (combatPowerLabel == null)
            {
                GameObject labelObject =
                    new GameObject(
                        "Combat Power");

                labelObject.transform.SetParent(
                    transform,
                    false);

                // 駒の中央
                labelObject.transform.localPosition =
                    new Vector3(
                        0f,
                        0f,
                        -0.01f);

                labelObject.transform.localRotation =
                    Quaternion.identity;

                combatPowerLabel =
                    labelObject.AddComponent<TextMesh>();

                // 文字を中央揃え
                combatPowerLabel.anchor =
                    TextAnchor.MiddleCenter;

                combatPowerLabel.alignment =
                    TextAlignment.Center;

                // 文字を大きくする
                combatPowerLabel.fontSize = 64;

                // 駒の大きさに合わせた文字サイズ
                combatPowerLabel.characterSize = 0.20f;

                // 濃い緑色の駒の上でも見やすいように白
                combatPowerLabel.color =
                    Color.white;

                // フォントを太く見せる
                combatPowerLabel.fontStyle =
                    FontStyle.Bold;

                MeshRenderer meshRenderer =
                    labelObject.GetComponent<MeshRenderer>();

                if (meshRenderer != null)
                {
                    // SpriteRendererより手前に表示
                    meshRenderer.sortingOrder = 11;

                    // Spriteと同じSorting Layerを使用
                    meshRenderer.sortingLayerID =
                        pieceRenderer.sortingLayerID;
                }
            }
        }

        private void UpdateCombatPowerLabel()
        {
            if (combatPowerLabel == null ||
                state == null)
            {
                return;
            }

            combatPowerLabel.text =
                state.CombatPower.ToString();
        }
    }
}
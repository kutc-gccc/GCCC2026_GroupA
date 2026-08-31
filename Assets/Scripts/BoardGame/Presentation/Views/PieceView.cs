using GCCC.BoardGame.Core.Model;
using UnityEngine;

namespace GCCC.BoardGame.Presentation.Views
{
    public sealed class PieceView : MonoBehaviour
    {
        private const float PieceScale = 0.72f;
        private static readonly Color FusedLabelColor = Color.black;

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
            pieceRenderer.sprite = pieceSprite;
            pieceRenderer.color = Color.white;
            Render();
        }

        public void Render(
            PieceState pieceState,
            Sprite pieceSprite)
        {
            state = pieceState;

            EnsureRenderer();
            pieceRenderer.sprite = pieceSprite;
            pieceRenderer.color = Color.white;
            transform.localPosition = BoardGeometry.CellToLocalPosition(
                state.Position, columns, rows);
            UpdateCombatPowerLabel();
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
            if (pieceRenderer == null)
            {
                pieceRenderer = GetComponent<SpriteRenderer>();
                if (pieceRenderer == null)
                {
                    pieceRenderer = gameObject.AddComponent<SpriteRenderer>();
                }
            }

            pieceRenderer.sortingOrder = 10;

            if (combatPowerLabel == null)
            {
                GameObject labelObject = new GameObject("Combat Power");
                labelObject.transform.SetParent(transform, false);
                labelObject.transform.localPosition = new Vector3(0f, 0f, -0.01f);
                labelObject.transform.localRotation = Quaternion.identity;

                combatPowerLabel = labelObject.AddComponent<TextMesh>();
                combatPowerLabel.anchor = TextAnchor.MiddleCenter;
                combatPowerLabel.alignment = TextAlignment.Center;
                combatPowerLabel.fontSize = 64;
                combatPowerLabel.characterSize = 0.20f;
                combatPowerLabel.color = Color.white;
                combatPowerLabel.fontStyle = FontStyle.Bold;

                MeshRenderer meshRenderer = labelObject.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                {
                    meshRenderer.sortingOrder = 11;
                    meshRenderer.sortingLayerID = pieceRenderer.sortingLayerID;
                }
            }
        }

        private void UpdateCombatPowerLabel()
        {
            if (combatPowerLabel == null || state == null)
            {
                return;
            }

            combatPowerLabel.text = state.CombatPower.ToString();
            combatPowerLabel.color = state.HasFused ? FusedLabelColor : Color.white;
        }
    }
}

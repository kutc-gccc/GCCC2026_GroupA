using GCCC.BoardGame.Core.Model;
using UnityEngine;

namespace GCCC.BoardGame.Presentation.Views
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PieceView : MonoBehaviour
    {
        private const float PieceScale = 0.72f;
        private static readonly Color Player1Color = new Color32(42, 91, 153, 255);
        private static readonly Color Player2Color = new Color32(196, 61, 54, 255);

        private SpriteRenderer pieceRenderer;
        private TextMesh combatPowerLabel;
        private int columns;
        private int rows;

        public PieceId PieceId { get; private set; }

        public void Initialize(PieceState state, Sprite sprite, int boardColumns, int boardRows)
        {
            columns = boardColumns;
            rows = boardRows;
            pieceRenderer = GetComponent<SpriteRenderer>();
            pieceRenderer.sprite = sprite;
            pieceRenderer.sortingOrder = 3;
            transform.localScale = Vector3.one * PieceScale;
            CreateCombatPowerLabel();
            Render(state);
        }

        public void Render(PieceState state)
        {
            PieceId = state.Id;
            gameObject.name = $"{state.Owner} Piece ({state.Position.Column}, {state.Position.Row})";
            pieceRenderer.color = state.Owner == PlayerId.Player1 ? Player1Color : Player2Color;
            transform.localPosition = BoardGeometry.CellToLocalPosition(
                state.Position, columns, rows);
            combatPowerLabel.text = state.EffectiveCombatPower.ToString();
        }

        private void CreateCombatPowerLabel()
        {
            GameObject labelObject = new GameObject("Combat Power", typeof(TextMesh));
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 0f, -0.01f);
            labelObject.transform.localScale = Vector3.one / PieceScale;

            combatPowerLabel = labelObject.GetComponent<TextMesh>();
            combatPowerLabel.anchor = TextAnchor.MiddleCenter;
            combatPowerLabel.alignment = TextAlignment.Center;
            combatPowerLabel.fontSize = 64;
            combatPowerLabel.characterSize = 0.1f;
            combatPowerLabel.color = Color.white;
            labelObject.GetComponent<MeshRenderer>().sortingOrder = 4;
        }
    }
}

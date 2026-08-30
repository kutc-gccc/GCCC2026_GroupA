using GCCC.BoardGame.Core.Model;
using UnityEngine;

namespace GCCC.BoardGame.Presentation.Views
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PieceView : MonoBehaviour
    {
        private const float PieceScale = 0.45f;

        private static readonly Color PieceColor = Color.white;

        private SpriteRenderer pieceRenderer;
        private TextMesh combatPowerLabel;

        private Sprite pieceSprite;

        private int columns;
        private int rows;

        public PieceId PieceId { get; private set; }

        public void Initialize(
            PieceState state,
            Sprite sprite,
            int boardColumns,
            int boardRows)
        {
            columns = boardColumns;
            rows = boardRows;

            pieceSprite = sprite;

            pieceRenderer =
                GetComponent<SpriteRenderer>();

            pieceRenderer.sprite = pieceSprite;
            pieceRenderer.sortingOrder = 3;

            pieceRenderer.color = PieceColor;

            transform.localScale =
                Vector3.one * PieceScale;

            CreateCombatPowerLabel();

            Render(state);
        }

        public void Render(PieceState state)
        {
            PieceId = state.Id;

            gameObject.name =
                $"{state.Owner} Piece " +
                $"({state.Position.Column}, {state.Position.Row})";

            // Initializeで渡されたSpriteを使用
            pieceRenderer.sprite = pieceSprite;

            // 駒はPlayer1・Player2とも白
            pieceRenderer.color = PieceColor;

            transform.localPosition =
                BoardGeometry.CellToLocalPosition(
                    state.Position,
                    columns,
                    rows);

            combatPowerLabel.text =
                state.CombatPower.ToString();
        }

        private void CreateCombatPowerLabel()
        {
            GameObject labelObject =
                new GameObject(
                    "Combat Power",
                    typeof(TextMesh));

            labelObject.transform.SetParent(
                transform,
                false);

            labelObject.transform.localPosition =
                new Vector3(
                    0f,
                    0f,
                    -0.01f);

            labelObject.transform.localScale =
                Vector3.one / PieceScale;

            combatPowerLabel =
                labelObject.GetComponent<TextMesh>();

            combatPowerLabel.anchor =
                TextAnchor.MiddleCenter;

            combatPowerLabel.alignment =
                TextAlignment.Center;

            combatPowerLabel.fontSize = 64;

            combatPowerLabel.characterSize = 0.1f;

            combatPowerLabel.color =
            Color.white;

            labelObject
                .GetComponent<MeshRenderer>()
                .sortingOrder = 4;
        }
    }
}
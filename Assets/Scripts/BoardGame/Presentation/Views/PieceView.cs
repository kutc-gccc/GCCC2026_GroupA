using GCCC.BoardGame.Core.Model;
using UnityEngine;

namespace GCCC.BoardGame.Presentation.Views
{
    public sealed class PieceView : MonoBehaviour
    {
        private const float PieceScale = 0.45f;

        // 合体してできた駒は、駒そのものを少し大きくして盤上で見分けられるようにする。
        private const float FusedPieceScale = 0.52f;

        private const float PowerCharacterSize = 0.20f;

        /// <summary>縁取りを置く方向の数。多いほど輪郭が滑らかになる。</summary>
        private const int PowerOutlineDirections = 8;

        /// <summary>縁取りの太さ。数字の高さに対する割合で指定する。</summary>
        private const float PowerOutlineThickness = 0.05f;

        private static readonly Color PieceColor =
            Color.white;

        private static readonly Color NormalPowerColor =
            Color.white;

        // 合体後の数字は黒。駒の濃緑の上では黒だけでは読めない（実測2.86:1）ので、白で縁取る。
        private static readonly Color FusedPowerColor =
            Color.black;

        private static readonly Color FusedPowerOutlineColor =
            Color.white;

        private SpriteRenderer pieceRenderer;
        private TextMesh combatPowerLabel;
        private TextMesh[] combatPowerOutline;

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

            float pieceScale =
                state.HasFused
                    ? FusedPieceScale
                    : PieceScale;

            transform.localScale =
                Vector3.one * pieceScale;

            UpdateCombatPowerLabel(pieceScale);
        }

        private void EnsureRenderer()
        {
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

            if (combatPowerLabel == null)
            {
                GameObject labelObject =
                    new GameObject(
                        "Combat Power");

                labelObject.transform.SetParent(
                    transform,
                    false);

                labelObject.transform.localPosition =
                    new Vector3(
                        0f,
                        0f,
                        -0.01f);

                labelObject.transform.localRotation =
                    Quaternion.identity;

                combatPowerLabel =
                    labelObject.AddComponent<TextMesh>();

                combatPowerLabel.anchor =
                    TextAnchor.MiddleCenter;

                combatPowerLabel.alignment =
                    TextAlignment.Center;

                combatPowerLabel.fontSize = 64;

                // 色と大きさは合体の有無で変わるので、UpdateCombatPowerLabelで毎回決める。
                combatPowerLabel.characterSize =
                    PowerCharacterSize;

                combatPowerLabel.color =
                    NormalPowerColor;

                combatPowerLabel.fontStyle =
                    FontStyle.Bold;

                MeshRenderer meshRenderer =
                    labelObject.GetComponent<MeshRenderer>();

                if (meshRenderer != null)
                {
                    // 縁取りを1つ下の順序に敷くため、数字は12にする。
                    meshRenderer.sortingOrder = 12;

                    meshRenderer.sortingLayerID =
                        pieceRenderer.sortingLayerID;
                }
            }
        }

        private void UpdateCombatPowerLabel(float pieceScale)
        {
            if (combatPowerLabel == null ||
                state == null)
            {
                return;
            }

            // 数字は駒の子なので、駒を拡大すると一緒に大きくなる。
            // 数字の見た目の大きさは変えない指定なので、拡大ぶんを打ち消す。
            float characterSize =
                PowerCharacterSize * PieceScale / pieceScale;

            combatPowerLabel.text =
                state.EffectiveCombatPower.ToString();

            combatPowerLabel.color =
                state.HasFused
                    ? FusedPowerColor
                    : NormalPowerColor;

            combatPowerLabel.characterSize = characterSize;

            UpdateCombatPowerOutline(characterSize);
        }

        /// <summary>
        /// 合体後の黒い数字を読めるようにする白い縁取り。
        /// TextMeshに縁取り機能がないので、白い複製を周囲8方向へずらして数字の後ろに敷く。
        /// </summary>
        private void UpdateCombatPowerOutline(float characterSize)
        {
            if (!state.HasFused)
            {
                if (combatPowerOutline != null)
                {
                    foreach (TextMesh outline in combatPowerOutline)
                    {
                        outline.gameObject.SetActive(false);
                    }
                }

                return;
            }

            EnsureCombatPowerOutline();

            // TextMeshの文字高はfontSize×characterSize/10。その割合で太さを決める。
            float offset =
                combatPowerLabel.fontSize * characterSize / 10f * PowerOutlineThickness;

            for (int i = 0; i < combatPowerOutline.Length; i++)
            {
                float angle = i * Mathf.PI * 2f / PowerOutlineDirections;
                TextMesh outline = combatPowerOutline[i];

                outline.gameObject.SetActive(true);
                outline.text = combatPowerLabel.text;
                outline.characterSize = characterSize;
                outline.transform.localPosition =
                    new Vector3(
                        Mathf.Cos(angle) * offset,
                        Mathf.Sin(angle) * offset,
                        0f);
            }
        }

        private void EnsureCombatPowerOutline()
        {
            if (combatPowerOutline != null)
            {
                return;
            }

            MeshRenderer labelRenderer =
                combatPowerLabel.GetComponent<MeshRenderer>();
            combatPowerOutline = new TextMesh[PowerOutlineDirections];

            for (int i = 0; i < PowerOutlineDirections; i++)
            {
                GameObject outlineObject =
                    new GameObject($"Combat Power Outline {i}");

                outlineObject.transform.SetParent(
                    combatPowerLabel.transform,
                    false);

                TextMesh outline =
                    outlineObject.AddComponent<TextMesh>();

                outline.font = combatPowerLabel.font;
                outline.anchor = TextAnchor.MiddleCenter;
                outline.alignment = TextAlignment.Center;
                outline.fontSize = combatPowerLabel.fontSize;
                outline.fontStyle = FontStyle.Bold;
                outline.color = FusedPowerOutlineColor;

                MeshRenderer outlineRenderer =
                    outlineObject.GetComponent<MeshRenderer>();

                if (outlineRenderer != null &&
                    labelRenderer != null)
                {
                    outlineRenderer.sharedMaterial =
                        labelRenderer.sharedMaterial;

                    outlineRenderer.sortingLayerID =
                        labelRenderer.sortingLayerID;

                    outlineRenderer.sortingOrder =
                        labelRenderer.sortingOrder - 1;
                }

                combatPowerOutline[i] = outline;
            }
        }
    }
}

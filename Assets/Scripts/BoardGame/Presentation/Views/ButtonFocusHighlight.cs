using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GCCC.BoardGame.Presentation.Views
{
    /// <summary>
    /// ボタンにホバーとキーボードフォーカスの反応を付ける。
    /// </summary>
    /// <remarks>
    /// uGUIの<see cref="Selectable.Transition.ColorTint"/>は状態色を`targetGraphic`の色へ
    /// 乗算するため、塗りが透明なボタンでは何色を当てても変化しない。実測でも、枠線だけの
    /// 「遊び方」ボタンはフォーカスを移しても1画素も変わらなかった。塗りがあるボタンでも
    /// 既定の状態色では最大3.7/255しか動かず、目では判別しにくい。
    ///
    /// そこで塗りの有無によらず同じ反応にするため、専用の重ね絵を1枚フェードさせる。
    /// 既存の色（枠線・ラベル・選択中の表示）には触れないので、他の描画と取り合いにならない。
    /// </remarks>
    [RequireComponent(typeof(Selectable))]
    public sealed class ButtonFocusHighlight : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        ISelectHandler,
        IDeselectHandler
    {
        /// <summary>重ね絵の子オブジェクト名。Scene・Prefab・実行時生成で共通に使う。</summary>
        public const string HighlightObjectName = "Focus Highlight";

        [SerializeField] private Graphic highlight;

        [SerializeField, Range(0f, 1f)] private float highlightAlpha = 0.16f;

        private Selectable selectable;
        private bool pointerInside;
        private bool focused;

        /// <summary>いま反応を出している状態か。目視できない差なので、テストはここを見る。</summary>
        public bool IsHighlighted =>
            (pointerInside || focused) &&
            (selectable == null || selectable.IsInteractable());

        private void Awake()
        {
            selectable = GetComponent<Selectable>();

            if (highlight == null)
            {
                Transform found = transform.Find(HighlightObjectName);
                highlight = found != null ? found.GetComponent<Graphic>() : null;
            }

            if (highlight == null)
            {
                Debug.LogError(
                    $"[ButtonFocusHighlight] {name} に「{HighlightObjectName}」がありません。" +
                    "反応を出せないため、押せることが画面から分かりません。",
                    this);
                return;
            }

            highlight.raycastTarget = false;
            Apply();
        }

        private void OnEnable()
        {
            Apply();
        }

        private void OnDisable()
        {
            // 非表示のあいだにポインタが外れても通知が来ないので、状態を落としておく。
            pointerInside = false;
            focused = false;
            Apply();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            pointerInside = true;
            Apply();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pointerInside = false;
            Apply();
        }

        public void OnSelect(BaseEventData eventData)
        {
            focused = true;
            Apply();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            focused = false;
            Apply();
        }

        private void Apply()
        {
            if (highlight == null)
            {
                return;
            }

            Color color = highlight.color;
            color.a = IsHighlighted ? highlightAlpha : 0f;
            highlight.color = color;
        }
    }
}

using System.Collections.Generic;
using GCCC.BoardGame.Core.Model;
using GCCC.BoardGame.Presentation.Config;
using UnityEngine;
using UnityEngine.UI;

namespace GCCC.BoardGame.Presentation.Views
{
    /// <summary>
    /// 遊び方ページの中身を生成し、節を切り替える。
    /// 寸法はCanvasの参照解像度1920×1080に対する実pxで、内容領域は1413×617を前提にする。
    /// </summary>
    /// <remarks>
    /// 盤面60マスや方向図63マスは繰り返しなので、<see cref="ReservePanelView"/>や
    /// <see cref="BoardView"/>と同じく実行時に生成する。
    /// 高さは文言の実測値から決めるので、<see cref="HowToPlayContent"/>の文章を差し替えても枠から溢れない。
    /// 盤に出る色とスプライトは<see cref="BoardView"/>・<see cref="RuntimeSpriteFactory"/>から借りる。
    /// 説明とゲーム画面で見た目がずれないよう、値を複製しない。
    /// 必須参照が欠けている場合は不完全なUIを作らず、エラーを出して何も生成しない。
    /// </remarks>
    public sealed partial class HowToPlayView : MonoBehaviour
    {
        private const float NavWidth = 240f;
        private const float ColumnGap = 40f;
        private const float DesignContentWidth = 1413f;
        private const float LeadTop = 54f;
        private const float FigureGap = 26f;
        private const float NoteGap = 22f;
        private const float BoardCell = 42f;
        private const float BoardGap = 3f;
        private const float DirCell = 34f;
        private const float DirGap = 4f;
        private const float TokenSize = 60f;

        private static readonly Color Ink = new Color32(242, 245, 249, 255);
        // 本文色。大理石の模様でパネル地が±3ほど揺れるので、AA(4.5)に余裕を持たせた値にする。
        private static readonly Color Soft = new Color32(226, 230, 236, 255);
        private static readonly Color Line = new Color32(60, 70, 85, 255);
        private static readonly Color Amber = new Color32(255, 193, 7, 255);
        private static readonly Color Green = new Color32(62, 154, 104, 255);
        private static readonly Color Wood = new Color32(169, 120, 75, 255);
        private static readonly Color WoodLine = new Color32(110, 76, 46, 255);
        private static readonly Color Territory = new Color32(87, 95, 108, 255);
        private static readonly Color Fill = new Color32(255, 255, 255, 15);
        private static readonly Color ButtonFace = new Color32(232, 235, 239, 255);
        private static readonly Color ButtonInk = new Color32(27, 33, 43, 255);
        private static readonly Color Clear = new Color(1f, 1f, 1f, 0f);

        /// <summary>
        /// カードと注記の下地。明るい面に白を重ねるとLinear合成で持ち上がりすぎて
        /// 本文のコントラストが確保できないため、黒を重ねて沈める。
        /// </summary>
        private static readonly Color CardFill = new Color(0f, 0f, 0f, 0.35f);

        [SerializeField] private Font uiFont;
        [SerializeField] private Sprite upPieceSprite;
        [SerializeField] private Sprite downPieceSprite;

        /// <summary>節1の盤の図をゲーム本体と同じ配置で描くために使う。</summary>
        [SerializeField] private BoardGameConfig boardConfig;

        private readonly List<Button> navButtons = new List<Button>();
        private readonly List<GameObject> panes = new List<GameObject>();
        private GameDefinition boardDefinition;
        private RuntimeSpriteFactory sprites;
        private bool built;

        public int SectionCount => panes.Count;
        public int SelectedSection { get; private set; }

        private void OnEnable()
        {
            // 遊び方ページは非表示で始まるので、初めて開かれたときに生成する。
            Build();
        }

        private void OnDestroy()
        {
            sprites?.Dispose();
        }

        /// <summary>遊び方ページを開き直したときに先頭の節へ戻す。</summary>
        public void ResetToFirstSection()
        {
            if (built)
            {
                Select(0);
            }
        }

        public Button GetNavButton(int index)
        {
            return index >= 0 && index < navButtons.Count ? navButtons[index] : null;
        }

        public GameObject GetPane(int index)
        {
            return index >= 0 && index < panes.Count ? panes[index] : null;
        }

        private void Build()
        {
            if (built)
            {
                return;
            }

            if (uiFont == null || upPieceSprite == null || downPieceSprite == null ||
                boardConfig == null)
            {
                Debug.LogError(
                    "[HowToPlayView] フォント・駒スプライト・盤の設定のいずれかが未設定のため、" +
                    "遊び方の内容を生成しません。",
                    this);
                return;
            }

            boardDefinition = boardConfig.CreateDefinition();
            sprites = new RuntimeSpriteFactory();
            RectTransform root = GetComponent<RectTransform>();
            BuildNav(root);
            BuildPanes(root, PaneWidth(root));
            built = true;
            Select(0);
        }

        /// <summary>本文側の幅。CanvasScalerが働く前に呼ばれた場合は設計値で代替する。</summary>
        private static float PaneWidth(RectTransform root)
        {
            float width = root.rect.width > 1f ? root.rect.width : DesignContentWidth;
            return Mathf.Max(width - NavWidth - ColumnGap, 600f);
        }

        private void BuildNav(RectTransform root)
        {
            RectTransform nav = CreateChild("Nav", root);
            nav.anchorMin = new Vector2(0f, 0f);
            nav.anchorMax = new Vector2(0f, 1f);
            nav.pivot = new Vector2(0f, 0.5f);
            nav.sizeDelta = new Vector2(NavWidth, 0f);
            nav.anchoredPosition = Vector2.zero;

            RectTransform divider = CreateChild("Nav Divider", nav);
            divider.anchorMin = new Vector2(1f, 0f);
            divider.anchorMax = new Vector2(1f, 1f);
            divider.pivot = new Vector2(1f, 0.5f);
            divider.sizeDelta = new Vector2(1f, 0f);
            divider.anchoredPosition = Vector2.zero;
            AddImage(divider, Line);

            float y = 0f;
            for (int i = 0; i < HowToPlayContent.Sections.Count; i++)
            {
                // 「あとで」は前の5節と性格が違うので少し離す。
                if (i == HowToPlayContent.Sections.Count - 1)
                {
                    y += 18f;
                }

                RectTransform item = CreateChild($"Nav {i + 1}", nav);
                item.anchorMin = new Vector2(0f, 1f);
                item.anchorMax = new Vector2(1f, 1f);
                item.pivot = new Vector2(0.5f, 1f);
                item.sizeDelta = new Vector2(0f, 52f);
                item.anchoredPosition = new Vector2(0f, -y);
                y += 52f;

                Image background = AddImage(item, Clear);
                background.raycastTarget = true;
                Button button = item.gameObject.AddComponent<Button>();
                button.targetGraphic = background;
                int index = i;
                button.onClick.AddListener(() => Select(index));
                navButtons.Add(button);

                // 背景が透明なのでColorTintでは何も変わらない。専用の重ね絵で反応を出す。
                // Selectが書き換える色とは別物なので、選択中の表示と取り合いにならない。
                RectTransform focus = CreateChild(
                    ButtonFocusHighlight.HighlightObjectName, item);
                Stretch(focus, 0f, 0f, 0f, 0f);
                AddImage(focus, new Color(1f, 1f, 1f, 0f));
                item.gameObject.AddComponent<ButtonFocusHighlight>();

                RectTransform accent = CreateChild("Accent", item);
                LeftBar(accent);
                AddImage(accent, Clear);

                Text number = CreateText(item, "Number", (i + 1).ToString(), 20, TextAnchor.MiddleLeft, Soft);
                TopLeft(number.rectTransform, 22f, 8f, 28f, 36f);

                Text label = CreateText(
                    item, "Label", HowToPlayContent.Sections[i].NavLabel, 24, TextAnchor.MiddleLeft, Soft);
                TopLeft(label.rectTransform, 56f, 8f, 170f, 36f);
            }
        }

        private void BuildPanes(RectTransform root, float width)
        {
            for (int i = 0; i < HowToPlayContent.Sections.Count; i++)
            {
                HowToPlayContent.Section section = HowToPlayContent.Sections[i];
                RectTransform pane = CreateChild($"Pane {i + 1}", root);
                Stretch(pane, NavWidth + ColumnGap, 0f, 0f, 0f);
                panes.Add(pane.gameObject);

                Text heading = CreateText(pane, "Heading", section.Heading, 34, TextAnchor.UpperLeft, Ink);
                heading.fontStyle = FontStyle.Bold;
                TopLeft(heading.rectTransform, 0f, 0f, width, 48f);

                Text lead = CreateText(pane, "Lead", section.Lead, 25, TextAnchor.UpperLeft, Soft);
                lead.lineSpacing = 1.25f;
                TopLeft(lead.rectTransform, 0f, LeadTop, width, 38f);
                float leadHeight = Fit(lead, 38f);

                float figureTop = LeadTop + leadHeight + FigureGap;
                RectTransform figure = CreateChild("Figure", pane);
                TopLeft(figure, 0f, figureTop, width, 0f);
                float figureHeight = BuildFigure(section.Figure, figure, width);

                if (string.IsNullOrEmpty(section.Note))
                {
                    continue;
                }

                // 節1の盤は縦に長いので、注記を下ではなく盤の右の空きへ回す。
                Rect area = section.Figure == HowToPlayContent.FigureKind.Board
                    ? new Rect(BoardKeyLeft(), figureTop + 196f, width - BoardKeyLeft(), 62f)
                    : new Rect(0f, figureTop + figureHeight + NoteGap, width, 62f);
                BuildNote(pane, section.Note, area);
            }
        }

        private void BuildNote(RectTransform pane, string text, Rect area)
        {
            RectTransform note = CreateChild("Note", pane);
            TopLeft(note, area.x, area.y, area.width, area.height);
            AddImage(note, CardFill);

            RectTransform accent = CreateChild("Accent", note);
            LeftBar(accent);
            AddImage(accent, Amber);

            Text label = CreateText(note, "Text", text, 23, TextAnchor.MiddleLeft, Ink);
            label.lineSpacing = 1.2f;
            TopLeft(label.rectTransform, 22f, 9f, area.width - 42f, area.height - 18f);
            note.sizeDelta = new Vector2(area.width, Fit(label, area.height - 18f) + 18f);
        }

        private void Select(int index)
        {
            SelectedSection = index;
            for (int i = 0; i < panes.Count; i++)
            {
                panes[i].SetActive(i == index);
            }

            for (int i = 0; i < navButtons.Count; i++)
            {
                bool active = i == index;
                Transform item = navButtons[i].transform;
                item.GetComponent<Image>().color = active ? CardFill : Clear;
                item.Find("Accent").GetComponent<Image>().color = active ? Amber : Clear;
                item.Find("Number").GetComponent<Text>().color = active ? Amber : Soft;
                Text label = item.Find("Label").GetComponent<Text>();
                label.color = active ? Ink : Soft;
                label.fontStyle = active ? FontStyle.Bold : FontStyle.Normal;
            }
        }

        // ================= 生成の下請け =================

        /// <summary>
        /// 文言の実測に合わせて高さを広げ、確定した高さを返す。
        /// <see cref="HowToPlayContent"/>の文章を差し替えても枠から溢れないようにするための処理。
        /// </summary>
        private static float Fit(Text text, float minimum)
        {
            RectTransform rect = text.rectTransform;
            float height = Mathf.Max(minimum, text.preferredHeight);
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
            return height;
        }

        private static RectTransform CreateChild(string name, Transform parent)
        {
            GameObject child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child.GetComponent<RectTransform>();
        }

        private static Image AddImage(RectTransform target, Color color)
        {
            Image image = target.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private Text CreateText(
            Transform parent, string name, string value, int size, TextAnchor alignment, Color color)
        {
            RectTransform rect = CreateChild(name, parent);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = uiFont;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.text = value;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>左上原点で配置する。yは下方向が正。</summary>
        private static void TopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, -y);
        }

        /// <summary>左端に縦いっぱいの細い帯を敷く。選択中の節と注記の目印に使う。</summary>
        private static void LeftBar(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(5f, 0f);
            rect.anchoredPosition = Vector2.zero;
        }

        private static void Center(RectTransform rect, float width, float height)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = Vector2.zero;
        }

        private static void Stretch(RectTransform rect, float left, float top, float right, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }
    }
}

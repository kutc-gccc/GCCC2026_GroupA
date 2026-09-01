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
    /// 必須参照が欠けている場合は不完全なUIを作らず、エラーを出して何も生成しない。
    /// </remarks>
    public sealed class HowToPlayView : MonoBehaviour
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
        private static readonly Color Soft = new Color32(178, 188, 202, 255);
        private static readonly Color Line = new Color32(60, 70, 85, 255);
        private static readonly Color Amber = new Color32(255, 193, 7, 255);
        private static readonly Color Danger = new Color32(224, 90, 78, 255);
        private static readonly Color Fusion = new Color32(74, 155, 232, 255);
        private static readonly Color Green = new Color32(62, 154, 104, 255);
        private static readonly Color Wood = new Color32(169, 120, 75, 255);
        private static readonly Color WoodLine = new Color32(110, 76, 46, 255);
        private static readonly Color Territory = new Color32(87, 95, 108, 255);
        private static readonly Color Fill = new Color32(255, 255, 255, 15);
        private static readonly Color FillStrong = new Color32(255, 255, 255, 26);
        private static readonly Color Clear = new Color(1f, 1f, 1f, 0f);

        [SerializeField] private Font uiFont;
        [SerializeField] private Sprite upPieceSprite;
        [SerializeField] private Sprite downPieceSprite;

        /// <summary>節1の盤の図をゲーム本体と同じ配置で描くために使う。</summary>
        [SerializeField] private BoardGameConfig boardConfig;

        private readonly List<Button> navButtons = new List<Button>();
        private readonly List<GameObject> panes = new List<GameObject>();
        private GameDefinition boardDefinition;
        private bool built;

        public int SectionCount => panes.Count;
        public int SelectedSection { get; private set; }

        private void OnEnable()
        {
            // 遊び方ページは非表示で始まるので、初めて開かれたときに生成する。
            Build();
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
                    ? new Rect(BoardKeyLeft(), figureTop + 180f, width - BoardKeyLeft(), 62f)
                    : new Rect(0f, figureTop + figureHeight + NoteGap, width, 62f);
                BuildNote(pane, section.Note, area);
            }
        }

        private void BuildNote(RectTransform pane, string text, Rect area)
        {
            RectTransform note = CreateChild("Note", pane);
            TopLeft(note, area.x, area.y, area.width, area.height);
            AddImage(note, FillStrong);

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
                item.GetComponent<Image>().color = active ? FillStrong : Clear;
                item.Find("Accent").GetComponent<Image>().color = active ? Amber : Clear;
                item.Find("Number").GetComponent<Text>().color = active ? Amber : Soft;
                Text label = item.Find("Label").GetComponent<Text>();
                label.color = active ? Ink : Soft;
                label.fontStyle = active ? FontStyle.Bold : FontStyle.Normal;
            }
        }

        // ================= 図 =================

        /// <summary>図を組み立て、その高さを返す。</summary>
        private float BuildFigure(HowToPlayContent.FigureKind kind, RectTransform figure, float width)
        {
            switch (kind)
            {
                case HowToPlayContent.FigureKind.Board: return BuildBoard(figure, width);
                case HowToPlayContent.FigureKind.PieceIdentity: return BuildIdentities(figure);
                case HowToPlayContent.FigureKind.TurnActions: return BuildTurnActions(figure, width);
                case HowToPlayContent.FigureKind.MoveDirections: return BuildDirections(figure);
                case HowToPlayContent.FigureKind.Combat: return BuildCombat(figure, width);
                default: return BuildLaterTopics(figure, width);
            }
        }

        private float BoardWidth()
        {
            return boardDefinition.Columns * BoardCell
                + (boardDefinition.Columns - 1) * BoardGap + 12f;
        }

        /// <summary>盤の右に置く凡例と注記の左端。</summary>
        private float BoardKeyLeft()
        {
            return BoardWidth() + 56f;
        }

        private float BuildBoard(RectTransform figure, float width)
        {
            int rows = boardDefinition.Rows;
            float boardWidth = BoardWidth();
            float boardHeight = rows * BoardCell + (rows - 1) * BoardGap + 12f;

            RectTransform board = CreateChild("Board", figure);
            TopLeft(board, 0f, 0f, boardWidth, boardHeight);
            AddImage(board, WoodLine);

            foreach (CellDefinition cell in boardDefinition.Cells)
            {
                RectTransform square = CreateChild(
                    $"Cell {cell.Position.Column},{cell.Position.Row}", board);
                TopLeft(square,
                    CellLeft(cell.Position.Column),
                    CellTop(cell.Position.Row),
                    BoardCell, BoardCell);
                AddImage(square, CellColor(cell));
            }

            foreach (InitialPieceDefinition piece in boardDefinition.InitialPieces)
            {
                RectTransform icon = CreateChild(
                    $"Piece {piece.Position.Column},{piece.Position.Row}", board);
                TopLeft(icon,
                    CellLeft(piece.Position.Column) + 6f,
                    CellTop(piece.Position.Row) + 6f,
                    30f, 30f);
                Image image = AddImage(icon, Color.white);
                image.sprite = piece.Owner == PlayerId.Player1 ? upPieceSprite : downPieceSprite;
                image.preserveAspect = true;
            }

            float keyWidth = width - BoardKeyLeft();
            for (int i = 0; i < HowToPlayContent.BoardKeys.Length; i++)
            {
                RectTransform row = CreateChild($"Key {i + 1}", figure);
                TopLeft(row, BoardKeyLeft(), 8f + i * 44f, keyWidth, 36f);

                RectTransform swatch = CreateChild("Swatch", row);
                TopLeft(swatch, 0f, 4f, 28f, 28f);
                AddImage(swatch, i == 2 ? Wood : Territory);

                Text label = CreateText(row, "Label", HowToPlayContent.BoardKeys[i], 23, TextAnchor.MiddleLeft, Ink);
                TopLeft(label.rectTransform, 42f, 0f, keyWidth - 42f, 36f);
            }

            return boardHeight;
        }

        private static float CellLeft(int column)
        {
            return 6f + column * (BoardCell + BoardGap);
        }

        /// <summary>盤面座標は下がRow=0なので、上から並べる図では行を反転する。</summary>
        private float CellTop(int row)
        {
            return 6f + (boardDefinition.Rows - 1 - row) * (BoardCell + BoardGap);
        }

        private Color CellColor(CellDefinition cell)
        {
            if (cell.TerritoryOwner.HasValue)
            {
                return Territory;
            }

            // 盤面と同じ色で塗る。効果マスの位置も色も設定側が持っているので、ここでは持たない。
            if (cell.EffectIds.Count > 0 &&
                boardDefinition.TryGetCellEffectDefinition(
                    cell.EffectIds[0], out CellEffectDefinition effect))
            {
                return OverWood(effect.Lifetime == CellEffectLifetime.WhileOccupied
                    ? BoardView.WhileOccupiedEffectColor
                    : BoardView.PermanentEffectColor);
            }

            return Wood;
        }

        /// <summary>盤面の効果色は半透明なので、盤に重ねた見た目へ揃える。</summary>
        private static Color OverWood(Color effect)
        {
            return Color.Lerp(Wood, new Color(effect.r, effect.g, effect.b, 1f), effect.a);
        }

        private float BuildIdentities(RectTransform figure)
        {
            const float cardWidth = 340f;
            const float bodyTop = 130f;
            var cards = new List<RectTransform>();
            float height = 0f;
            float x = 0f;

            foreach (HowToPlayContent.Identity identity in HowToPlayContent.Identities)
            {
                RectTransform card = CreateChild(identity.Up ? "Identity Up" : "Identity Down", figure);
                TopLeft(card, x, 0f, cardWidth, 0f);
                AddImage(card, FillStrong);
                cards.Add(card);
                x += cardWidth + 28f;

                RectTransform icon = CreateChild("Piece", card);
                TopLeft(icon, (cardWidth - 60f) * 0.5f, 22f, 60f, 60f);
                Image image = AddImage(icon, Color.white);
                image.sprite = identity.Up ? upPieceSprite : downPieceSprite;
                image.preserveAspect = true;

                Text title = CreateText(card, "Title", identity.Title, 26, TextAnchor.UpperCenter, Ink);
                title.fontStyle = FontStyle.Bold;
                TopLeft(title.rectTransform, 12f, 92f, cardWidth - 24f, 38f);

                Text body = CreateText(card, "Body", identity.Body, 22, TextAnchor.UpperCenter, Soft);
                body.lineSpacing = 1.2f;
                TopLeft(body.rectTransform, 12f, bodyTop, cardWidth - 24f, 40f);
                height = Mathf.Max(height, bodyTop + Fit(body, 40f) + 16f);
            }

            SetHeights(cards, height);
            return height;
        }

        private float BuildTurnActions(RectTransform figure, float width)
        {
            float cardWidth = Mathf.Min((width - 48f) / 3f, 360f);
            const float bodyTop = 92f;
            var cards = new List<RectTransform>();
            float height = 0f;
            float x = 0f;

            foreach (HowToPlayContent.TurnAction action in HowToPlayContent.TurnActions)
            {
                RectTransform card = CreateChild($"Action {action.Title}", figure);
                TopLeft(card, x, 0f, cardWidth, 0f);
                AddImage(card, FillStrong);
                cards.Add(card);
                x += cardWidth + 24f;

                RectTransform chip = CreateChild("Chip", card);
                TopLeft(chip, 22f, 18f, 108f, 30f);
                AddImage(chip, ChipColor(action.Chip));
                Text chipText = CreateText(
                    chip, "Text", action.Chip, 19, TextAnchor.MiddleCenter, new Color32(27, 33, 43, 255));
                chipText.fontStyle = FontStyle.Bold;
                Stretch(chipText.rectTransform, 0f, 0f, 0f, 0f);

                Text title = CreateText(card, "Title", action.Title, 26, TextAnchor.UpperLeft, Ink);
                title.fontStyle = FontStyle.Bold;
                TopLeft(title.rectTransform, 22f, 54f, cardWidth - 44f, 38f);

                Text body = CreateText(card, "Body", action.Body, 22, TextAnchor.UpperLeft, Soft);
                body.lineSpacing = 1.15f;
                TopLeft(body.rectTransform, 22f, bodyTop, cardWidth - 44f, 34f);
                height = Mathf.Max(height, bodyTop + Fit(body, 34f) + 18f);
            }

            SetHeights(cards, height);
            return height;
        }

        private static Color ChipColor(string chip)
        {
            switch (chip)
            {
                case "ボタン": return Amber;
                case "青い枠": return Fusion;
                default: return new Color32(240, 243, 247, 255);
            }
        }

        private float BuildDirections(RectTransform figure)
        {
            float gridSize = 3f * DirCell + 2f * DirGap;
            float height = gridSize + 88f;
            float x = 0f;

            foreach (HowToPlayContent.DirectionStep step in HowToPlayContent.DirectionSteps)
            {
                RectTransform item = CreateChild($"Power {step.Power}", figure);
                TopLeft(item, x, 0f, gridSize, height);
                x += gridSize + 26f;

                for (int i = 0; i < 9; i++)
                {
                    RectTransform cell = CreateChild($"Cell {i}", item);
                    TopLeft(cell,
                        (i % 3) * (DirCell + DirGap),
                        (i / 3) * (DirCell + DirGap),
                        DirCell, DirCell);

                    if (i == 4)
                    {
                        // 中央は駒自身。移動先ではないので枠だけにする。
                        AddImage(cell, new Color(1f, 1f, 1f, 0.02f));
                        Outline outline = cell.gameObject.AddComponent<Outline>();
                        outline.effectColor = Soft;
                        outline.effectDistance = new Vector2(1.5f, -1.5f);
                        continue;
                    }

                    AddImage(cell, step.Open[i] ? Green : Fill);
                }

                Text power = CreateText(item, "Power", step.Power, 25, TextAnchor.UpperCenter, Ink);
                power.fontStyle = FontStyle.Bold;
                TopLeft(power.rectTransform, 0f, gridSize + 10f, gridSize, 36f);

                Text lost = CreateText(item, "Lost", step.Lost, 21, TextAnchor.UpperCenter, Soft);
                TopLeft(lost.rectTransform, -16f, gridSize + 50f, gridSize + 32f, 32f);
            }

            return height;
        }

        private float BuildCombat(RectTransform figure, float width)
        {
            float afterX = 22f + (TokenSize + 10f) * 2f + 54f;
            float textX = afterX + (TokenSize + 10f) * 2f + 22f;
            const float bodyTop = 40f;
            float y = 0f;

            foreach (HowToPlayContent.CombatCase combat in HowToPlayContent.CombatCases)
            {
                RectTransform row = CreateChild($"Combat {combat.Tag}", figure);
                TopLeft(row, 0f, y, width, 0f);
                AddImage(row, FillStrong);

                BuildToken(row, 22f, combat.AttackerBefore, true);
                BuildToken(row, 22f + TokenSize + 10f, combat.DefenderBefore, false);

                Text arrow = CreateText(row, "Arrow", "→", 30, TextAnchor.MiddleCenter, Soft);
                TopLeft(arrow.rectTransform, 22f + (TokenSize + 10f) * 2f, 16f, 48f, 44f);

                BuildToken(row, afterX, combat.AttackerAfter, true);
                BuildToken(row, afterX + TokenSize + 10f, combat.DefenderAfter, false);

                float tagWidth = TagWidth(combat.Tag);
                RectTransform tag = CreateChild("Tag", row);
                TopLeft(tag, textX, 10f, tagWidth, 28f);
                AddImage(tag, TagColor(combat.Tag));
                Text tagText = CreateText(tag, "Text", combat.Tag, 18, TextAnchor.MiddleCenter,
                    combat.Tag == "負け" ? Color.white : new Color32(12, 20, 14, 255));
                tagText.fontStyle = FontStyle.Bold;
                Stretch(tagText.rectTransform, 0f, 0f, 0f, 0f);

                Text title = CreateText(row, "Title", combat.Title, 25, TextAnchor.UpperLeft, Ink);
                title.fontStyle = FontStyle.Bold;
                TopLeft(title.rectTransform, textX + tagWidth + 12f, 6f,
                    width - textX - tagWidth - 34f, 36f);

                Text body = CreateText(row, "Body", combat.Body, 22, TextAnchor.UpperLeft, Soft);
                body.lineSpacing = 1.15f;
                TopLeft(body.rectTransform, textX, bodyTop, width - textX - 22f, 32f);

                float rowHeight = Mathf.Max(76f, bodyTop + Fit(body, 32f) + 10f);
                row.sizeDelta = new Vector2(width, rowHeight);
                y += rowHeight + 12f;
            }

            return Mathf.Max(y - 12f, 0f);
        }

        private static float TagWidth(string tag)
        {
            return tag.Length * 19f + 20f;
        }

        private static Color TagColor(string tag)
        {
            switch (tag)
            {
                case "勝ち": return Green;
                case "負け": return new Color32(108, 119, 136, 255);
                default: return Danger;
            }
        }

        private void BuildToken(RectTransform row, float x, int power, bool up)
        {
            bool alive = power > 0;
            RectTransform token = CreateChild(up ? "Attacker" : "Defender", row);
            TopLeft(token, x, 11f, TokenSize, TokenSize);
            AddImage(token, alive ? Wood : new Color(Wood.r, Wood.g, Wood.b, 0.25f));

            RectTransform icon = CreateChild("Piece", token);
            Center(icon, 52f, 52f);
            Image image = AddImage(icon, alive ? Color.white : new Color(1f, 1f, 1f, 0.25f));
            image.sprite = up ? upPieceSprite : downPieceSprite;
            image.preserveAspect = true;

            // 盤上と同じく、数字は駒の上に重ねる。緑地に白なので縁取りで読めるようにする。
            Text number = CreateText(token, "Power", power.ToString(), 24, TextAnchor.MiddleCenter,
                alive ? Color.white : new Color(1f, 1f, 1f, 0.4f));
            number.fontStyle = FontStyle.Bold;
            Stretch(number.rectTransform, 0f, 0f, 0f, 0f);
            Outline edge = number.gameObject.AddComponent<Outline>();
            edge.effectColor = new Color(0f, 0f, 0f, alive ? 0.75f : 0.2f);
            edge.effectDistance = new Vector2(2f, -2f);
        }

        private float BuildLaterTopics(RectTransform figure, float width)
        {
            float cardWidth = (width - 24f) * 0.5f;
            const float bodyTop = 46f;
            var cards = new List<RectTransform>();
            float cardHeight = 0f;

            for (int i = 0; i < HowToPlayContent.LaterTopics.Count; i++)
            {
                HowToPlayContent.LaterTopic topic = HowToPlayContent.LaterTopics[i];
                RectTransform card = CreateChild($"Later {topic.Title}", figure);
                TopLeft(card, (i % 2) * (cardWidth + 24f), 0f, cardWidth, 0f);
                AddImage(card, FillStrong);
                cards.Add(card);

                RectTransform accent = CreateChild("Accent", card);
                LeftBar(accent);
                AddImage(accent, AccentColor(topic.Accent));

                Text title = CreateText(card, "Title", topic.Title, 24, TextAnchor.UpperLeft, Ink);
                title.fontStyle = FontStyle.Bold;
                TopLeft(title.rectTransform, 22f, 12f, cardWidth - 44f, 34f);

                Text body = CreateText(card, "Body", topic.Body, 21, TextAnchor.UpperLeft, Soft);
                body.lineSpacing = 1.15f;
                TopLeft(body.rectTransform, 22f, bodyTop, cardWidth - 44f, 32f);
                cardHeight = Mathf.Max(cardHeight, bodyTop + Fit(body, 32f) + 16f);
            }

            // 2列なので、すべて同じ高さに揃えてから2段目を下げる。
            for (int i = 0; i < cards.Count; i++)
            {
                cards[i].sizeDelta = new Vector2(cards[i].sizeDelta.x, cardHeight);
                cards[i].anchoredPosition = new Vector2(
                    cards[i].anchoredPosition.x, -(i / 2) * (cardHeight + 16f));
            }

            int rows = Mathf.CeilToInt(cards.Count * 0.5f);
            return rows * cardHeight + (rows - 1) * 16f;
        }

        private static Color AccentColor(HowToPlayContent.LaterAccent accent)
        {
            switch (accent)
            {
                case HowToPlayContent.LaterAccent.Cyan: return OverWood(BoardView.WhileOccupiedEffectColor);
                case HowToPlayContent.LaterAccent.Violet: return OverWood(BoardView.PermanentEffectColor);
                case HowToPlayContent.LaterAccent.Fusion: return Fusion;
                default: return Amber;
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

        private static void SetHeights(List<RectTransform> targets, float height)
        {
            foreach (RectTransform target in targets)
            {
                target.sizeDelta = new Vector2(target.sizeDelta.x, height);
            }
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

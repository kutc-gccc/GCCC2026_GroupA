using System.Collections.Generic;
using GCCC.BoardGame.Core.Model;
using UnityEngine;
using UnityEngine.UI;

namespace GCCC.BoardGame.Presentation.Views
{
    /// <summary>
    /// 遊び方ページの各節の図を組み立てる部分。
    /// 節ごとに描くものが違うので、ページの足場（ナビ・ペイン・注記）とは分けている。
    /// どの<c>Build*</c>も図の高さを返し、呼び出し側がその下へ注記を置く。
    /// </summary>
    public sealed partial class HowToPlayView
    {
        // ================= 図 =================

        /// <summary>図を組み立て、その高さを返す。</summary>
        private float BuildFigure(HowToPlayContent.FigureKind kind, RectTransform figure, float width)
        {
            switch (kind)
            {
                case HowToPlayContent.FigureKind.Board: return BuildBoard(figure, width);
                case HowToPlayContent.FigureKind.PieceIdentity: return BuildPieceOrientation(figure);
                case HowToPlayContent.FigureKind.TurnActions: return BuildTurnFlow(figure, width);
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
            for (int i = 0; i < HowToPlayContent.BoardKeys.Count; i++)
            {
                HowToPlayContent.BoardKey key = HowToPlayContent.BoardKeys[i];
                RectTransform row = CreateChild($"Key {i + 1}", figure);
                TopLeft(row, BoardKeyLeft(), 8f + i * 44f, keyWidth, 36f);
                BuildKeySwatch(row, key.Swatch);

                Text label = CreateText(row, "Label", key.Label, 23, TextAnchor.MiddleLeft, Ink);
                TopLeft(label.rectTransform, 42f, 0f, keyWidth - 42f, 36f);
            }

            return boardHeight;
        }

        /// <summary>凡例の見本。効果マスだけはシアンと紫を半分ずつ並べる。</summary>
        private void BuildKeySwatch(RectTransform row, HowToPlayContent.KeySwatch swatch)
        {
            if (swatch != HowToPlayContent.KeySwatch.Effect)
            {
                RectTransform single = CreateChild("Swatch", row);
                TopLeft(single, 0f, 4f, 28f, 28f);
                AddImage(single, swatch == HowToPlayContent.KeySwatch.Wood ? Wood : Territory);
                return;
            }

            RectTransform left = CreateChild("Swatch While Occupied", row);
            TopLeft(left, 0f, 4f, 14f, 28f);
            AddImage(left, OverWood(BoardView.WhileOccupiedEffectColor));

            RectTransform right = CreateChild("Swatch Permanent", row);
            TopLeft(right, 14f, 4f, 14f, 28f);
            AddImage(right, OverWood(BoardView.PermanentEffectColor));
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

        /// <summary>
        /// 盤に半透明で重なる色を、重ねた後の不透明色にする。
        /// ゲーム本体はLinear空間で合成するので、ここも同じ空間で計算しないと色がずれる。
        /// </summary>
        private static Color OverWood(Color overlay)
        {
            Color src = overlay.linear;
            Color dst = Wood.linear;
            float a = overlay.a;
            return new Color(
                src.r * a + dst.r * (1f - a),
                src.g * a + dst.g * (1f - a),
                src.b * a + dst.b * (1f - a),
                1f).gamma;
        }

        // ---- 節2: 駒の向き ----

        private float BuildPieceOrientation(RectTransform figure)
        {
            const int columns = 6;
            const int rows = 6;
            float cutWidth = columns * BoardCell + (columns - 1) * BoardGap + 12f;
            float cutHeight = rows * BoardCell + (rows - 1) * BoardGap + 12f;

            RectTransform board = CreateChild("Board Excerpt", figure);
            TopLeft(board, 0f, 0f, cutWidth, cutHeight);
            AddImage(board, WoodLine);

            // 上から 相手の陣地 / ▼の初期列 / 空き2行 / ▲の初期列 / 自分の陣地。
            for (int row = 0; row < rows; row++)
            {
                bool territory = row == 0 || row == rows - 1;
                for (int column = 0; column < columns; column++)
                {
                    RectTransform cell = CreateChild($"Cell {column},{row}", board);
                    TopLeft(cell,
                        CellLeft(column),
                        6f + row * (BoardCell + BoardGap),
                        BoardCell, BoardCell);
                    AddImage(cell, territory ? Territory : Wood);

                    if (row == 1)
                    {
                        AddPiece(cell, up: false, power: 0);
                    }
                    else if (row == rows - 2)
                    {
                        AddPiece(cell, up: true, power: 0);
                    }
                }
            }

            float textLeft = cutWidth + 44f;
            float y = 10f;
            foreach (HowToPlayContent.Identity identity in HowToPlayContent.Identities)
            {
                RectTransform block = CreateChild(identity.Up ? "Up" : "Down", figure);
                TopLeft(block, textLeft, y, 720f, 96f);
                y += 140f;

                RectTransform icon = CreateChild("Piece", block);
                TopLeft(icon, 0f, 2f, 34f, 34f);
                Image image = AddImage(icon, Color.white);
                image.sprite = identity.Up ? upPieceSprite : downPieceSprite;
                image.preserveAspect = true;

                Text title = CreateText(block, "Title", identity.Title, 26, TextAnchor.UpperLeft, Ink);
                title.fontStyle = FontStyle.Bold;
                TopLeft(title.rectTransform, 48f, 0f, 672f, 38f);

                Text body = CreateText(block, "Body", identity.Body, 23, TextAnchor.UpperLeft, Soft);
                TopLeft(body.rectTransform, 48f, 42f, 672f, 34f);
                Fit(body, 34f);
            }

            return cutHeight;
        }

        // ---- 節3: 1手の行動 ----

        private float BuildTurnFlow(RectTransform figure, float width)
        {
            const float column = 300f;
            const float gap = 26f;
            float excerpt = 3f * BoardCell + 2f * BoardGap + 12f;

            float x = 0f;
            float stepsHeight = 0f;
            for (int i = 0; i < HowToPlayContent.OperationSteps.Count; i++)
            {
                HowToPlayContent.OperationStep step = HowToPlayContent.OperationSteps[i];
                RectTransform item = CreateChild($"Step {i + 1}", figure);
                TopLeft(item, x, 0f, column, excerpt);

                BuildStepExcerpt(item, i);

                Text title = CreateText(item, "Title", step.Title, 22, TextAnchor.UpperLeft, Ink);
                title.fontStyle = FontStyle.Bold;
                TopLeft(title.rectTransform, 0f, excerpt + 12f, column, 32f);

                Text body = CreateText(item, "Body", step.Body, 20, TextAnchor.UpperLeft, Soft);
                TopLeft(body.rectTransform, 0f, excerpt + 44f, column, 28f);
                stepsHeight = Mathf.Max(stepsHeight, excerpt + 44f + Fit(body, 28f));

                x += column;
                if (i < HowToPlayContent.OperationSteps.Count - 1)
                {
                    Text arrow = CreateText(figure, "Arrow", "→", 34, TextAnchor.MiddleCenter, Soft);
                    TopLeft(arrow.rectTransform, x + gap, excerpt * 0.5f - 26f, 34f, 52f);
                    x += gap + 34f + gap;
                }
            }

            float y = stepsHeight + 26f;
            foreach (HowToPlayContent.ButtonAction action in HowToPlayContent.ButtonActions)
            {
                y += BuildButtonAction(figure, action, y, width) + 12f;
            }

            return y - 12f;
        }

        /// <summary>
        /// 操作の3コマで使う3×3の盤の抜粋。中央が自分の駒、上が敵。
        /// 0:選択、1:候補表示、2:移動後。
        /// </summary>
        private void BuildStepExcerpt(RectTransform parent, int step)
        {
            const int columns = 3;
            const int enemy = 1;
            const int self = 4;
            const int destination = 5;
            float size = columns * BoardCell + (columns - 1) * BoardGap + 12f;

            RectTransform board = CreateChild("Board Excerpt", parent);
            TopLeft(board, 0f, 0f, size, size);
            AddImage(board, WoodLine);

            for (int i = 0; i < columns * columns; i++)
            {
                RectTransform cell = CreateChild($"Cell {i}", board);
                TopLeft(cell,
                    CellLeft(i % columns),
                    6f + (i / columns) * (BoardCell + BoardGap),
                    BoardCell, BoardCell);
                AddImage(cell, Wood);

                if (i == enemy)
                {
                    AddPiece(cell, up: false, power: 2);
                    if (step == 1)
                    {
                        AddFrame(cell, BoardView.CombatMoveColor);
                    }

                    continue;
                }

                bool holdsSelf = step < 2 ? i == self : i == destination;
                if (holdsSelf)
                {
                    AddPiece(cell, up: true, power: 1);
                    if (step < 2)
                    {
                        AddFrame(cell, BoardView.SelectionColor);
                    }

                    continue;
                }

                // 候補表示の段だけ、空きマスに移動先の点を出す。
                if (step == 1 && i != self)
                {
                    AddDot(cell);
                }
            }
        }

        /// <summary>ボタンを使う行動の1行を組み立て、その高さを返す。</summary>
        private float BuildButtonAction(
            RectTransform figure, HowToPlayContent.ButtonAction action, float y, float width)
        {
            RectTransform row = CreateChild($"Action {action.Button}", figure);
            TopLeft(row, 0f, y, width, 60f);
            AddImage(row, CardFill);

            float x = 22f;
            Text lead = CreateText(row, "Lead", action.Lead, 22, TextAnchor.MiddleLeft, Soft);
            TopLeft(lead.rectTransform, x, 13f, lead.preferredWidth + 4f, 34f);
            x += lead.preferredWidth + 20f;

            Text first = CreateText(row, "Arrow 1", "→", 26, TextAnchor.MiddleCenter, Soft);
            TopLeft(first.rectTransform, x, 10f, 34f, 40f);
            x += 50f;

            Text buttonLabel = CreateText(row, "Button Text", action.Button, 22, TextAnchor.MiddleCenter, ButtonInk);
            buttonLabel.fontStyle = FontStyle.Bold;
            float buttonWidth = buttonLabel.preferredWidth + 36f;
            RectTransform chip = CreateChild("Button", row);
            TopLeft(chip, x, 10f, buttonWidth, 40f);
            AddImage(chip, ButtonFace);
            buttonLabel.rectTransform.SetParent(chip, false);
            Stretch(buttonLabel.rectTransform, 0f, 0f, 0f, 0f);
            x += buttonWidth + 16f;

            Text second = CreateText(row, "Arrow 2", "→", 26, TextAnchor.MiddleCenter, Soft);
            TopLeft(second.rectTransform, x, 10f, 34f, 40f);
            x += 50f;

            if (action.ShowFusionSwatch)
            {
                // 盤のマスと同じ大きさにしないと、枠線が細くなって色が読み取れない。
                RectTransform swatch = CreateChild("Fusion Swatch", row);
                TopLeft(swatch, x, 9f, BoardCell, BoardCell);
                AddImage(swatch, Wood);
                AddFrame(swatch, BoardView.FusionCandidateColor);
                x += BoardCell + 12f;
            }

            Text result = CreateText(row, "Result", action.Result, 22, TextAnchor.MiddleLeft, Soft);
            TopLeft(result.rectTransform, x, 13f, Mathf.Max(width - x - 22f, 120f), 34f);

            // 環境によって字面の計測が変わるので、高さは実測に追従させる。
            float rowHeight = Mathf.Max(60f, Fit(result, 34f) + 26f);
            row.sizeDelta = new Vector2(width, rowHeight);
            return rowHeight;
        }

        // ---- 節4: 動ける向き（現行のまま） ----

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

        // ---- 節5: 戦闘 ----

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
                AddImage(row, CardFill);

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

                float rowHeight = Mathf.Max(82f, bodyTop + Fit(body, 32f) + 10f);
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
                default: return Amber;
            }
        }

        /// <summary>戦闘の前後を示す駒。倒された駒は盤から消えるので、空のマスにする。</summary>
        private void BuildToken(RectTransform row, float x, int power, bool up)
        {
            RectTransform token = CreateChild(up ? "Attacker" : "Defender", row);
            TopLeft(token, x, 11f, TokenSize, TokenSize);
            AddImage(token, Wood);
            if (power > 0)
            {
                AddPiece(token, up, power);
            }
        }

        // ---- 節6: あとで ----

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
                AddImage(card, CardFill);
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
                case HowToPlayContent.LaterAccent.Fusion: return OverWood(BoardView.FusionCandidateColor);
                default: return Amber;
            }
        }

        // ================= 盤のマスの中身 =================

        /// <summary>マスに駒を置く。<paramref name="power"/>が0以下なら数字を出さない。</summary>
        private void AddPiece(RectTransform cell, bool up, int power)
        {
            RectTransform icon = CreateChild("Piece", cell);
            Center(icon, cell.sizeDelta.x * 0.72f, cell.sizeDelta.y * 0.72f);
            Image image = AddImage(icon, Color.white);
            image.sprite = up ? upPieceSprite : downPieceSprite;
            image.preserveAspect = true;

            if (power <= 0)
            {
                return;
            }

            // 盤上と同じく、数字は駒の上に重ねる。緑地に白なので縁取りで読めるようにする。
            Text number = CreateText(cell, "Power", power.ToString(), 22, TextAnchor.MiddleCenter, Color.white);
            number.fontStyle = FontStyle.Bold;
            Stretch(number.rectTransform, 0f, 0f, 0f, 0f);
            Outline edge = number.gameObject.AddComponent<Outline>();
            edge.effectColor = new Color(0f, 0f, 0f, 0.75f);
            edge.effectDistance = new Vector2(2f, -2f);
        }

        /// <summary>選択中・戦闘・合体候補の枠。盤と同じ枠スプライトを使う。</summary>
        private void AddFrame(RectTransform cell, Color color)
        {
            RectTransform frame = CreateChild("Frame", cell);
            Stretch(frame, 0f, 0f, 0f, 0f);
            Image image = AddImage(frame, color);
            image.sprite = sprites.FrameSprite;
        }

        /// <summary>移動先を示す白い点。盤と同じ円スプライトを使う。</summary>
        private void AddDot(RectTransform cell)
        {
            RectTransform dot = CreateChild("Dot", cell);
            Center(dot, 13f, 13f);
            Image image = AddImage(dot, BoardView.LegalMoveColor);
            image.sprite = sprites.CircleSprite;
        }
    }
}

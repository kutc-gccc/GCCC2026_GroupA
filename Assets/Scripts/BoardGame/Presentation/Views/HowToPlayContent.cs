using System.Collections.Generic;

namespace GCCC.BoardGame.Presentation.Views
{
    /// <summary>
    /// 遊び方ページの文言と図の定義。文章を直すときはこのファイルだけを編集する。
    /// レイアウトの寸法や生成手順は<see cref="HowToPlayView"/>が持つ。
    /// </summary>
    /// <remarks>
    /// ルールの一次情報源はdocs/GAME_RULES.mdであって、ここではない。
    /// 値を変えるときは必ずGAME_RULES.mdと突き合わせること。
    /// </remarks>
    internal static class HowToPlayContent
    {
        /// <summary>節の図の種類。<see cref="HowToPlayView"/>が生成方法を切り替える。</summary>
        internal enum FigureKind
        {
            Board,
            PieceIdentity,
            TurnActions,
            MoveDirections,
            Combat,
            LaterTopics
        }

        internal sealed class Section
        {
            internal Section(string navLabel, string heading, string lead, FigureKind figure, string note)
            {
                NavLabel = navLabel;
                Heading = heading;
                Lead = lead;
                Figure = figure;
                Note = note;
            }

            internal string NavLabel { get; }
            internal string Heading { get; }
            internal string Lead { get; }
            internal FigureKind Figure { get; }

            /// <summary>図の下に置く注記。空なら生成しない。</summary>
            internal string Note { get; }
        }

        internal static readonly IReadOnlyList<Section> Sections = new[]
        {
            new Section(
                "勝ち方",
                "相手の陣地に、1個でも届かせたら勝ち",
                "盤は縦10マス。駒を進めて、自分から見ていちばん奥の相手の陣地に入れば、その瞬間に勝ちです。",
                FigureKind.Board,
                "相手を全滅させても勝ちではありません。倒すのは道を空けるため。"),
            new Section(
                "自分の駒",
                "自分の駒は「向き」で見分ける",
                "色分けはありません。どちらの駒も同じ緑です。三角が向いている先が、その駒の目指す陣地です。",
                FigureKind.PieceIdentity,
                "迷ったら画面左上の手番表示を見てください。いま動かせるのがどちらか書かれています。"),
            new Section(
                "1手の行動",
                "選んだ駒でできることは、3つのうち1つだけ",
                "自分の駒を選んでから、どれかを1回。終わると相手の番になります。リザーブを置くのも1手です。",
                FigureKind.TurnActions,
                "選ぶだけでは手番は進みません。同じ駒をもう一度押すと選択を解除できます。"),
            new Section(
                "動ける向き",
                "強い駒ほど、動ける向きが減る",
                "動ける向きは、いまの強さで決まります。強くなるほど1つずつ減り、弱くなればその強さの向きへ戻ります。",
                FigureKind.MoveDirections,
                "強くすれば勝てる、ではありません。強い駒はまっすぐしか進めず、回り込めなくなります。"),
            new Section(
                "戦闘",
                "ぶつかると、お互いに同じだけ削り合う",
                "攻めた側だけが減るのではなく、両方とも相手の数字ぶんだけ減ります。0以下の駒は消えます。",
                FigureKind.Combat,
                string.Empty),
            new Section(
                "あとで",
                "ここから先は、遊びながらで大丈夫",
                "残りは出てきたときに画面が教えてくれます。いま覚える必要はありません。",
                FigureKind.LaterTopics,
                "持てる駒は盤上とリザーブを合わせて6個まで。上限に達していると、それ以上は増えません。")
        };

        // ---- 節1: 盤面の凡例 ----
        /// <summary>凡例に添える見本の種別。</summary>
        internal enum KeySwatch
        {
            Territory,
            Wood,

            /// <summary>効果マス。シアンと紫の二色見本にする。</summary>
            Effect
        }

        internal sealed class BoardKey
        {
            internal BoardKey(KeySwatch swatch, string label)
            {
                Swatch = swatch;
                Label = label;
            }

            internal KeySwatch Swatch { get; }
            internal string Label { get; }
        }

        internal static readonly IReadOnlyList<BoardKey> BoardKeys = new[]
        {
            new BoardKey(KeySwatch.Territory, "三角が向く先 ＝ 相手の陣地。入れば勝ち"),
            new BoardKey(KeySwatch.Territory, "その反対側 ＝ 自分の陣地。自分では入れない"),
            new BoardKey(KeySwatch.Wood, "ふつうのマス"),
            new BoardKey(KeySwatch.Effect, "色つきのマス ＝ 特別な効果（あとで）")
        };

        // ---- 節2: 駒の見分け ----
        internal sealed class Identity
        {
            internal Identity(bool up, string title, string body)
            {
                Up = up;
                Title = title;
                Body = body;
            }

            internal bool Up { get; }
            internal string Title { get; }
            internal string Body { get; }
        }

        // 図は盤を上から下へ描くので、並びもそれに合わせる。
        internal static readonly IReadOnlyList<Identity> Identities = new[]
        {
            new Identity(false, "▼ 下向き ＝ 後手", "盤の上側から始まり、下の陣地を目指す"),
            new Identity(true, "▲ 上向き ＝ 先手", "盤の下側から始まり、上の陣地を目指す")
        };

        // ---- 節3: 手番の行動 ----
        /// <summary>駒を動かす手順の1コマ。図は<see cref="HowToPlayView"/>が盤の抜粋で描く。</summary>
        internal sealed class OperationStep
        {
            internal OperationStep(string title, string body)
            {
                Title = title;
                Body = body;
            }

            internal string Title { get; }
            internal string Body { get; }
        }

        internal static readonly IReadOnlyList<OperationStep> OperationSteps = new[]
        {
            new OperationStep("① 自分の駒を押す", "琥珀の枠が付く（選択中）"),
            new OperationStep("② 候補が出る", "白い点＝移動　赤い枠＝戦闘"),
            new OperationStep("③ 行き先を押す", "進む。敵なら戦闘になる")
        };

        /// <summary>
        /// ボタンを押してから使う行動。<see cref="Button"/>はゲーム画面のボタン名と一致させること。
        /// 合体は駒を選ぶだけでは始まらず、ボタンを押して初めて青い枠が出る。
        /// </summary>
        internal sealed class ButtonAction
        {
            internal ButtonAction(string lead, string button, string result, bool showFusionSwatch)
            {
                Lead = lead;
                Button = button;
                Result = result;
                ShowFusionSwatch = showFusionSwatch;
            }

            internal string Lead { get; }
            internal string Button { get; }
            internal string Result { get; }

            /// <summary>結果の前に青い枠の見本を置くかどうか。</summary>
            internal bool ShowFusionSwatch { get; }
        }

        internal static readonly IReadOnlyList<ButtonAction> ButtonActions = new[]
        {
            new ButtonAction(
                "自分の駒を選ぶ", "パワーランダム化", "強さが 1〜3 に変わる。弱くなることもある", false),
            new ButtonAction(
                "自分の駒を選ぶ", "合体", "青い枠が付いた味方を選ぶ", true)
        };

        // ---- 節4: 戦闘力ごとの移動方向 ----
        /// <summary>
        /// 3×3の升目。左上から右下へ並び、中央は駒自身。trueが移動可能。
        /// 値はdocs/GAME_RULES.md §5の標準プロファイルと一致させること。
        /// </summary>
        internal sealed class DirectionStep
        {
            internal DirectionStep(string power, string lost, bool[] open)
            {
                Power = power;
                Lost = lost;
                Open = open;
            }

            internal string Power { get; }
            internal string Lost { get; }
            internal bool[] Open { get; }
        }

        private const bool O = true;
        private const bool X = false;

        internal static readonly IReadOnlyList<DirectionStep> DirectionSteps = new[]
        {
            new DirectionStep("1", "全8方向", new[] { O, O, O, O, X, O, O, O, O }),
            new DirectionStep("2", "−右上",   new[] { O, O, X, O, X, O, O, O, O }),
            new DirectionStep("3", "−右下",   new[] { O, O, X, O, X, O, O, O, X }),
            new DirectionStep("4", "−左上",   new[] { X, O, X, O, X, O, O, O, X }),
            new DirectionStep("5", "−左下",   new[] { X, O, X, O, X, O, X, O, X }),
            new DirectionStep("6", "−左",     new[] { X, O, X, X, X, O, X, O, X }),
            new DirectionStep("7+", "−右",    new[] { X, O, X, X, X, X, X, O, X })
        };

        // ---- 節5: 戦闘 ----
        internal sealed class CombatCase
        {
            internal CombatCase(
                string tag, int attackerBefore, int defenderBefore,
                int attackerAfter, int defenderAfter, string title, string body)
            {
                Tag = tag;
                AttackerBefore = attackerBefore;
                DefenderBefore = defenderBefore;
                AttackerAfter = attackerAfter;
                DefenderAfter = defenderAfter;
                Title = title;
                Body = body;
            }

            internal string Tag { get; }
            internal int AttackerBefore { get; }
            internal int DefenderBefore { get; }
            internal int AttackerAfter { get; }
            internal int DefenderAfter { get; }
            internal string Title { get; }
            internal string Body { get; }
        }

        internal static readonly IReadOnlyList<CombatCase> CombatCases = new[]
        {
            new CombatCase("序盤はこれ", 1, 1, 0, 0,
                "同じ強さ ＝ 両方消える",
                "最初の駒はすべて 1 なので、序盤の戦闘は必ず相打ちです。"),
            new CombatCase("勝ち", 5, 2, 3, 0,
                "強いほうが残る。ただし削られる",
                "5 は 2 を倒しますが、自分も 3 に減ってそのマスへ進みます。"),
            new CombatCase("負け", 2, 5, 0, 3,
                "弱いほうから挑むと、自分だけ消える",
                "守った側はその場に残ります。相手陣地へ入る手で負けた場合も、勝ちにはなりません。")
        };

        // ---- 節6: あとで覚えること ----
        internal enum LaterAccent
        {
            Cyan,
            Violet,
            Fusion,
            Amber
        }

        internal sealed class LaterTopic
        {
            internal LaterTopic(LaterAccent accent, string title, string body)
            {
                Accent = accent;
                Title = title;
                Body = body;
            }

            internal LaterAccent Accent { get; }
            internal string Title { get; }
            internal string Body { get; }
        }

        internal static readonly IReadOnlyList<LaterTopic> LaterTopics = new[]
        {
            new LaterTopic(LaterAccent.Cyan, "戦闘力+2（シアンの塗り）",
                "止まっている間だけ強さ +2。離れると戻ります。"),
            new LaterTopic(LaterAccent.Violet, "リザーブ獲得（紫の塗り）",
                "止まるたびリザーブを1つもらえます。同じ駒で何度でも。"),
            new LaterTopic(LaterAccent.Fusion, "合体",
                "2駒の強さを合わせて1駒に。25%で失敗。合体した駒は少し大きくなり、再合体はできません。"),
            new LaterTopic(LaterAccent.Amber, "リザーブ",
                "画面右のリザーブ一覧から、自陣側の空きマスへ置けます。置くと1手使います。")
        };
    }
}

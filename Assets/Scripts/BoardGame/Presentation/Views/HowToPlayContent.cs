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
                "盤は縦10マス。駒を進めて、いちばん奥の相手の陣地に入れば、その瞬間に勝ちです。",
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
                "1手でできることは、3つのうち1つだけ",
                "自分の駒を選んでから、どれかを1回。終わると相手の番になります。",
                FigureKind.TurnActions,
                "駒をもう一度押すと選択を解除できます。選ぶだけでは手番は進みません。"),
            new Section(
                "動ける向き",
                "強い駒ほど、動ける向きが減る",
                "強さが上がるたびに、動ける向きがひとつずつ永久に減ります。減った向きは、さらに強くなっても戻りません。",
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
                "持てる駒は盤上と控えを合わせて6個まで。先に知っておくと、控えが増えない理由で悩みません。")
        };

        // ---- 節1: 盤面の凡例 ----
        internal static readonly string[] BoardKeys =
        {
            "いちばん上 ＝ 相手の陣地。入れば勝ち",
            "いちばん下 ＝ 自分の陣地。自分では入れない",
            "ふつうのマス"
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

        internal static readonly IReadOnlyList<Identity> Identities = new[]
        {
            new Identity(true, "▲ 上向き ＝ 先手", "上の陣地を目指す\n盤の下側から始まる"),
            new Identity(false, "▼ 下向き ＝ 後手", "下の陣地を目指す\n盤の上側から始まる")
        };

        // ---- 節3: 手番の行動 ----
        internal sealed class TurnAction
        {
            internal TurnAction(string chip, string title, string body)
            {
                Chip = chip;
                Title = title;
                Body = body;
            }

            internal string Chip { get; }
            internal string Title { get; }
            internal string Body { get; }
        }

        internal static readonly IReadOnlyList<TurnAction> TurnActions = new[]
        {
            new TurnAction("白い点", "動かす", "点のマスへ1マス進む。赤い枠の敵駒へ進むと戦闘になる。"),
            new TurnAction("ボタン", "パワーを振り直す", "強さを1〜3のどれかに引き直す。今より弱くなることもある。"),
            new TurnAction("青い枠", "となりの味方と合体", "2駒を1つにまとめる。失敗しても手番は終わる。")
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
            new LaterTopic(LaterAccent.Cyan, "シアンのマス",
                "止まっている間だけ強さ +2。離れると戻ります。"),
            new LaterTopic(LaterAccent.Violet, "紫のマス",
                "控えの駒を1つもらえます。1駒につき1回だけ。"),
            new LaterTopic(LaterAccent.Fusion, "合体",
                "2駒の強さを合わせて1駒に。25%で失敗し、そのまま手番が終わります。"),
            new LaterTopic(LaterAccent.Amber, "控えの駒",
                "画面右の一覧から、自陣側の空きマスへ置けます。置くと1手使います。")
        };
    }
}

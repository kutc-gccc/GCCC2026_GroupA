using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using GCCC.BoardGame.Presentation;
using GCCC.BoardGame.Presentation.Views;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace GCCC.BoardGame.Tests
{
    public sealed partial class BoardGameBootstrapTests
    {
        /// <summary>
        /// ゲーム画面（<c>GameHud.prefab</c> の凡例とボタン）と遊び方ページの両方に出るべき言葉。
        /// どちらか一方の表記を変えたらここで落ちる。
        /// </summary>
        private static readonly string[] SharedTerms =
        {
            "パワーランダム化", "リザーブ", "戦闘力+2", "リザーブ獲得",
            "選択中", "琥珀の枠", "白い点", "赤い枠", "青い枠", "合体",
            "合体した駒"
        };

        /// <summary>ゲーム画面と食い違っていた古い言い回し。復活していないことを確かめる。</summary>
        private static readonly string[] RetiredWordings =
        {
            "パワーを振り直す", "控えの駒", "シアンのマス", "紫のマス",
            "滞在中効果", "永続効果"
        };

        private static readonly string[] ExpectedNavLabels =
        {
            "勝ち方", "自分の駒", "1手の行動", "動ける向き", "戦闘", "あとで"
        };

        [UnityTest]
        public IEnumerator HowToPageBuildsEverySectionAndSwitchesBetweenThem()
        {
            HowToPlayView view = null;
            yield return OpenHowToPage(result => view = result);

            Assert.That(view.SectionCount, Is.EqualTo(ExpectedNavLabels.Length));
            Assert.That(view.SelectedSection, Is.Zero, "開いた直後は先頭の節を出す。");

            for (int i = 0; i < ExpectedNavLabels.Length; i++)
            {
                Assert.That(
                    view.GetNavButton(i).transform.Find("Label").GetComponent<Text>().text,
                    Is.EqualTo(ExpectedNavLabels[i]));
                Assert.That(
                    view.GetPane(i).activeSelf, Is.EqualTo(i == 0),
                    $"節{i + 1}の表示状態が選択中の節と食い違っている。");
            }

            view.GetNavButton(3).onClick.Invoke();
            yield return null;

            Assert.That(view.SelectedSection, Is.EqualTo(3));
            for (int i = 0; i < view.SectionCount; i++)
            {
                Assert.That(view.GetPane(i).activeSelf, Is.EqualTo(i == 3));
            }
        }

        /// <summary>
        /// 節を見ていた状態から戻って開き直したとき、先頭の節に戻ることを確かめる。
        /// </summary>
        [UnityTest]
        public IEnumerator HowToPageReturnsToFirstSectionWhenReopened()
        {
            HowToPlayView view = null;
            yield return OpenHowToPage(result => view = result);

            view.GetNavButton(4).onClick.Invoke();
            yield return null;
            Assert.That(view.SelectedSection, Is.EqualTo(4));

            GameObject.Find("How To Back Button").GetComponent<Button>().onClick.Invoke();
            yield return null;
            GameObject.Find("How To Button").GetComponent<Button>().onClick.Invoke();
            yield return null;

            Assert.That(view.SelectedSection, Is.Zero);
            Assert.That(view.GetPane(0).activeSelf, Is.True);
        }

        /// <summary>
        /// 文章を差し替えたときに気づけるよう、どの節も内容領域に収まることを確かめる。
        /// はみ出しは画面外に切れて出るので、目視より先にここで落とす。
        /// </summary>
        [UnityTest]
        public IEnumerator HowToPageSectionsFitInsideTheContentArea()
        {
            HowToPlayView view = null;
            yield return OpenHowToPage(result => view = result);

            RectTransform content = view.GetComponent<RectTransform>();
            int selected = view.SelectedSection;

            for (int i = 0; i < view.SectionCount; i++)
            {
                GameObject pane = view.GetPane(i);
                pane.SetActive(true);
                Canvas.ForceUpdateCanvases();

                Assert.That(
                    LowestEdge(pane, content),
                    Is.LessThanOrEqualTo(content.rect.height),
                    $"節{i + 1}が遊び方ページの内容領域の下へはみ出している。");

                foreach (Text text in pane.GetComponentsInChildren<Text>(true))
                {
                    Assert.That(text.font, Is.Not.Null);
                    Assert.That(
                        text.preferredHeight,
                        Is.LessThanOrEqualTo(text.rectTransform.rect.height + 2f),
                        $"節{i + 1}の「{text.text}」が折り返して枠に収まっていない。");
                }

                pane.SetActive(i == selected);
            }
        }

        [UnityTest]
        public IEnumerator HowToPlayViewStopsWithAnErrorWhenReferencesAreMissing()
        {
            GameObject host = new GameObject("How To Play Host", typeof(RectTransform));
            host.SetActive(false);
            HowToPlayView view = host.AddComponent<HowToPlayView>();

            LogAssert.Expect(LogType.Error, new Regex("HowToPlayView"));
            host.SetActive(true);
            yield return null;

            Assert.That(view.SectionCount, Is.Zero, "参照が欠けたまま不完全なUIを作らない。");
            Object.Destroy(host);
        }

        /// <summary>
        /// 遊び方ページとゲーム画面で同じ言葉を使っていることを確かめる。
        /// ページを読んで操作しに行ったとき、その名前のボタンや表示が見つからない事故を防ぐ。
        /// </summary>
        [UnityTest]
        public IEnumerator HowToPageUsesTheSameWordsAsTheGameScreen()
        {
            yield return SceneManager.LoadSceneAsync(
                BoardGameSceneNames.Game, LoadSceneMode.Single);
            yield return null;

            GameObject hud = GameObject.Find("Game HUD");
            Assert.That(hud, Is.Not.Null);
            string onScreen = CollectText(hud.GetComponentsInChildren<Text>(true));

            HowToPlayView view = null;
            yield return OpenHowToPage(result => view = result);

            var page = new List<Text>();
            for (int i = 0; i < view.SectionCount; i++)
            {
                page.AddRange(view.GetPane(i).GetComponentsInChildren<Text>(true));
            }

            string explained = CollectText(page.ToArray());

            foreach (string term in SharedTerms)
            {
                Assert.That(onScreen, Does.Contain(term),
                    $"ゲーム画面に「{term}」が出ていない。用語の一次情報が変わった可能性がある。");
                Assert.That(explained, Does.Contain(term),
                    $"遊び方ページが「{term}」を使っていない。ゲーム画面の表記に合わせること。");
            }

            foreach (string retired in RetiredWordings)
            {
                Assert.That(explained, Does.Not.Contain(retired),
                    $"遊び方ページに旧表記「{retired}」が残っている。");
            }
        }

        /// <summary>
        /// 押せるものにホバー・フォーカスの反応が付いていることを確かめる。
        /// uGUIのColorTintは塗りが透明だと何も変えないため、実測では「遊び方」ボタンに
        /// フォーカスを移しても1画素も変化していなかった。目視では気づけないので、
        /// 反応を出す部品が付いているかと、実際に状態が変わるかをここで見る。
        /// </summary>
        [UnityTest]
        public IEnumerator ButtonsShowAHoverAndFocusResponse()
        {
            yield return SceneManager.LoadSceneAsync(
                BoardGameSceneNames.Title, LoadSceneMode.Single);
            yield return null;

            // タイトルページを閉じる前に、そちら側のボタンを見る
            AssertHasFocusResponse("Game Start Button");
            AssertHasFocusResponse("How To Button");

            HowToPlayView view = null;
            yield return OpenHowToPage(result => view = result);
            AssertHasFocusResponse("How To Back Button");

            // 実行中のマウスカーソルがどこにあるかはテストから選べない。カーソルが乗った
            // ままのボタンは「触れている」状態になるので、ポインタの出入りはこちらから渡し、
            // 検査の直前に必ず外れた状態を作る。そうしないと結果が机の上の都合で変わる。
            var pointer = new PointerEventData(EventSystem.current);

            // ナビは実行時生成なので、生成側でも付けていることを確かめる
            for (int i = 0; i < view.SectionCount; i++)
            {
                ButtonFocusHighlight highlight =
                    view.GetNavButton(i).GetComponent<ButtonFocusHighlight>();
                Assert.That(highlight, Is.Not.Null, $"ナビ{i + 1}に部品が付いていない。");
                highlight.OnPointerExit(pointer);
                Assert.That(highlight.IsHighlighted, Is.False, "触れていないので反応は出さない。");
            }

            ButtonFocusHighlight target = view.GetNavButton(2).GetComponent<ButtonFocusHighlight>();
            Graphic overlay = view.GetNavButton(2).transform
                .Find(ButtonFocusHighlight.HighlightObjectName).GetComponent<Graphic>();

            // ポインタが乗ると状態が変わる
            target.OnPointerEnter(pointer);
            Assert.That(target.IsHighlighted, Is.True, "ポインタが乗ったら反応を出す。");
            Assert.That(overlay.color.a, Is.GreaterThan(0f), "重ね絵が見える状態になっていない。");

            target.OnPointerExit(pointer);
            Assert.That(target.IsHighlighted, Is.False, "ポインタが外れたら戻す。");
            Assert.That(overlay.color.a, Is.EqualTo(0f));

            // フォーカスを移しても状態が変わる
            EventSystem.current.SetSelectedGameObject(view.GetNavButton(2).gameObject);
            yield return null;
            target.OnPointerExit(pointer);

            Assert.That(target.IsHighlighted, Is.True, "フォーカスが当たったら反応を出す。");
            Assert.That(overlay.color.a, Is.GreaterThan(0f), "重ね絵が見える状態になっていない。");

            EventSystem.current.SetSelectedGameObject(null);
            yield return null;
            target.OnPointerExit(pointer);

            Assert.That(target.IsHighlighted, Is.False, "フォーカスが外れたら戻す。");
            Assert.That(overlay.color.a, Is.EqualTo(0f));
        }

        private static void AssertHasFocusResponse(string buttonName)
        {
            GameObject button = GameObject.Find(buttonName);
            Assert.That(button, Is.Not.Null, $"{buttonName}が見つからない。");
            Assert.That(button.GetComponent<ButtonFocusHighlight>(), Is.Not.Null,
                $"{buttonName}に反応を出す部品が付いていない。");
            Assert.That(button.transform.Find(ButtonFocusHighlight.HighlightObjectName),
                Is.Not.Null, $"{buttonName}に重ね絵が無い。");
        }

        private static string CollectText(Text[] texts)
        {
            var all = new System.Text.StringBuilder();
            foreach (Text text in texts)
            {
                all.AppendLine(text.text);
            }

            return all.ToString();
        }

        private static IEnumerator OpenHowToPage(System.Action<HowToPlayView> onOpened)
        {
            yield return SceneManager.LoadSceneAsync(
                BoardGameSceneNames.Title, LoadSceneMode.Single);
            yield return null;

            GameObject.Find("How To Button").GetComponent<Button>().onClick.Invoke();
            yield return null;
            Canvas.ForceUpdateCanvases();

            HowToPlayView view = Object.FindFirstObjectByType<HowToPlayView>();
            Assert.That(view, Is.Not.Null, "遊び方ページにHowToPlayViewが付いていない。");
            onOpened(view);
        }

        /// <summary>節の中で最も下にある端を、内容領域の左上を原点とした座標で返す。</summary>
        private static float LowestEdge(GameObject pane, RectTransform content)
        {
            Vector3[] corners = new Vector3[4];
            float lowest = 0f;

            foreach (RectTransform rect in pane.GetComponentsInChildren<RectTransform>(true))
            {
                if (rect.gameObject == pane)
                {
                    continue;
                }

                rect.GetWorldCorners(corners);
                for (int i = 0; i < corners.Length; i++)
                {
                    float y = content.rect.height * (1f - content.pivot.y)
                        - content.InverseTransformPoint(corners[i]).y;
                    lowest = Mathf.Max(lowest, y);
                }
            }

            return lowest;
        }
    }
}

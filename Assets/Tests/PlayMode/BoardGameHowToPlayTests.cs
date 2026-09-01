using System.Collections;
using System.Text.RegularExpressions;
using GCCC.BoardGame.Presentation;
using GCCC.BoardGame.Presentation.Views;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace GCCC.BoardGame.Tests
{
    public sealed partial class BoardGameBootstrapTests
    {
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

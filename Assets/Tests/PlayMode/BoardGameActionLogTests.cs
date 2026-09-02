using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GCCC.BoardGame.Core.Model;
using GCCC.BoardGame.Presentation.Views;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace GCCC.BoardGame.Tests
{
    public sealed partial class BoardGameBootstrapTests
    {
        [UnityTest]
        public IEnumerator ActionLogFromBootstrapSurvivesSelectionAndHowToThenResets()
        {
            Move(new GridPosition(2, 1), new GridPosition(2, 2));
            GameHudView hud = bootstrap.HudView;
            string result = hud.MessageText;
            Assert.That(result, Does.StartWith("▲ P1：移動"));
            Assert.That(result, Does.Contain("戦闘力＋2：1→3（このマスにいる間）"));
            bootstrap.HandleCellClick(new GridPosition(0, 8));
            bootstrap.HandleCellClick(new GridPosition(0, 8));
            Assert.That(hud.MessageText, Is.EqualTo(result));
            hud.OpenHowTo();
            yield return null;
            Assert.That(hud.MessageText, Is.EqualTo(result));
            hud.CloseHowTo();
            Assert.That(hud.MessageText, Is.EqualTo(result));
            Move(new GridPosition(0, 8), new GridPosition(0, 7));
            Assert.That(hud.MessageText, Does.StartWith("▼ P2：移動"));
            bootstrap.Coordinator.Reset();
            Assert.That(hud.MessageText, Is.Empty);
        }

        [UnityTest]
        public IEnumerator ActionLogScrollsWithoutChangingGameAndReturnsToTopForNewResult()
        {
            GameHudView hud = bootstrap.HudView;
            ScrollRect scroll = hud.transform.Find("Board UI/Status Stack/Action Result Scroll").GetComponent<ScrollRect>();
            string longText = string.Join("\n", Enumerable.Range(1, 20).Select(i => $"効果{i}：戦闘力＋2（このマスにいる間）"));
            hud.ShowMessage(longText);
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.That(scroll.content.rect.height, Is.GreaterThan(scroll.viewport.rect.height));
            Assert.That(scroll.verticalNormalizedPosition, Is.EqualTo(1).Within(0.001f));
            Vector2 point = RectTransformUtility.WorldToScreenPoint(null,
                scroll.viewport.TransformPoint(scroll.viewport.rect.center));
            Assert.That(hud.IsPointerOverControl(point), Is.True);
            var pointer = new PointerEventData(EventSystem.current) { position = point, scrollDelta = new Vector2(0, -4) };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits, Is.Not.Empty);
            GameObject receiver = ExecuteEvents.GetEventHandler<IScrollHandler>(hits[0].gameObject);
            Assert.That(receiver, Is.EqualTo(scroll.gameObject));
            GameSnapshot before = bootstrap.Snapshot;
            ExecuteEvents.Execute(receiver, pointer, ExecuteEvents.scrollHandler);
            yield return null;
            Assert.That(scroll.verticalNormalizedPosition, Is.LessThan(1));
            Assert.That(hud.MessageText, Is.EqualTo(longText));
            Assert.That(bootstrap.Snapshot, Is.SameAs(before));
            hud.OpenHowTo();
            Assert.That(scroll.enabled, Is.False);
            hud.CloseHowTo();
            Assert.That(scroll.enabled, Is.True);
            hud.ShowMessage("▲ P1：移動しました");
            yield return null;
            // When everything fits, Unity's normalized value may be zero; the text must be at the top.
            Assert.That(scroll.content.anchoredPosition.y, Is.EqualTo(0).Within(0.001f));
            Assert.That(scroll.content.rect.height, Is.EqualTo(scroll.viewport.rect.height).Within(1));
            Assert.That(scroll.verticalScrollbar.gameObject.activeSelf, Is.False);
            hud.ShowMessage(longText);
            yield return null;
            Assert.That(scroll.verticalNormalizedPosition, Is.EqualTo(1).Within(0.001f));
            // The resized result area must remain above the how-to button, even with long text.
            var corners = new Vector3[4];
            ((RectTransform)scroll.transform).GetWorldCorners(corners);
            float bottom = corners[0].y;
            ((RectTransform)hud.transform.Find("Board UI/How To Button")).GetWorldCorners(corners);
            Assert.That(bottom, Is.GreaterThan(corners[1].y));
        }
    }
}

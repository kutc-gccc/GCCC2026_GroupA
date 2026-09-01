using System;
using GCCC.BoardGame.Presentation.Bootstrap;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace GCCC.BoardGame.EditorTools
{
    internal static class TitleScreenPageMigration
    {
        private const string MenuPath = "GCCC/Migrate Title How-To Page";
        private const string ScenePath = "Assets/Scenes/TitleScene.unity";
        private const string SansFontPath = "Assets/NotoSansJP-VariableFont_wght.ttf";
        private const string SerifFontPath = "Assets/NotoSerifJP-VariableFont_wght.ttf";

        private static readonly Color32 TitleColor =
            new Color32(38, 54, 77, 255);
        private static readonly Color32 ButtonColor =
            new Color32(38, 54, 77, 255);
        private static readonly Color32 PanelColor =
            new Color32(35, 41, 52, 235);

        [MenuItem(MenuPath)]
        private static void Apply()
        {
            Font sansFont = AssetDatabase.LoadAssetAtPath<Font>(SansFontPath);
            if (sansFont == null)
            {
                throw new MissingReferenceException(
                    $"UI font not found: {SansFontPath}");
            }

            Font serifFont = AssetDatabase.LoadAssetAtPath<Font>(SerifFontPath);
            if (serifFont == null)
            {
                throw new MissingReferenceException(
                    $"Title font not found: {SerifFontPath}");
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            TitleScreenController controller =
                Object.FindFirstObjectByType<TitleScreenController>();
            if (controller == null)
            {
                throw new MissingReferenceException(
                    "TitleScreenController was not found in TitleScene.");
            }

            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                throw new MissingReferenceException(
                    "Title Canvas was not found in TitleScene.");
            }

            RebuildPages(controller, canvas.transform, sansFont, serifFont);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("GCCC Title how-to page migration completed.");
        }

        private static void RebuildPages(
            TitleScreenController controller,
            Transform canvas,
            Font sansFont,
            Font serifFont)
        {
            for (int index = canvas.childCount - 1; index >= 0; index--)
            {
                Transform child = canvas.GetChild(index);
                if (child.name != "Background")
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }

            RectTransform titlePage = CreateStretchRect("Title Page", canvas);
            CreateTitle(titlePage, sansFont, serifFont);
            RectTransform menu = CreateRect(
                "Title Menu",
                titlePage,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -90f),
                new Vector2(420f, 190f));
            VerticalLayoutGroup layout = menu.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 20f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            Button startButton = CreateButton(
                "Game Start Button", menu, sansFont, "ゲーム開始",
                TitleButtonStyle.Primary, 420f, 88f);
            Button howToButton = CreateButton(
                "How To Button", menu, sansFont, "遊び方",
                TitleButtonStyle.Secondary, 360f, 72f);

            RectTransform howToPage = CreateStretchRect("How To Page", canvas);
            Image shade = howToPage.gameObject.AddComponent<Image>();
            shade.color = new Color32(0, 0, 0, 75);
            shade.raycastTarget = true;

            RectTransform panel = CreateStretchRect("How To Panel", howToPage);
            panel.anchorMin = new Vector2(0.1f, 0.08f);
            panel.anchorMax = new Vector2(0.9f, 0.92f);
            Image panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.color = PanelColor;

            Text heading = CreateText(
                "How To Title", panel, serifFont, 64, FontStyle.Bold,
                TextAnchor.MiddleCenter, "遊び方", Color.white);
            SetAnchoredRect(
                heading.rectTransform,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -42f),
                new Vector2(800f, 90f));

            RectTransform content = CreateStretchRect("How To Content", panel);
            content.anchorMin = new Vector2(0.12f, 0.25f);
            content.anchorMax = new Vector2(0.88f, 0.72f);
            Text placeholder = CreateText(
                "How To Placeholder", content, sansFont, 28, FontStyle.Normal,
                TextAnchor.MiddleCenter,
                "遊び方の内容は後ほど追加予定です。",
                Color.white);
            Stretch(placeholder.rectTransform);

            Button backButton = CreateButton(
                "How To Back Button", panel, sansFont, "戻る",
                TitleButtonStyle.Primary, 320f, 72f);
            RectTransform backRect = backButton.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.5f, 0f);
            backRect.anchorMax = new Vector2(0.5f, 0f);
            backRect.pivot = new Vector2(0.5f, 0f);
            backRect.anchoredPosition = new Vector2(0f, 38f);
            backRect.sizeDelta = new Vector2(320f, 72f);
            Object.DestroyImmediate(backButton.GetComponent<LayoutElement>());

            howToPage.gameObject.SetActive(false);
            Assign(controller, "titlePage", titlePage.gameObject);
            Assign(controller, "howToPage", howToPage.gameObject);
            Assign(controller, "startButton", startButton);
            Assign(controller, "howToButton", howToButton);
            Assign(controller, "backButton", backButton);
        }

        private static void CreateTitle(
            Transform parent,
            Font sansFont,
            Font serifFont)
        {
            Text title = CreateText(
                "Title Text", parent, serifFont, 112, FontStyle.Bold,
                TextAnchor.MiddleCenter, "Number War", TitleColor);
            SetAnchoredRect(
                title.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 176f),
                new Vector2(1100f, 170f));

            Text subtitle = CreateText(
                "Title Subtitle", parent, sansFont, 26, FontStyle.Bold,
                TextAnchor.MiddleCenter,
                "6×10 の陣地到達型ボードゲーム",
                TitleColor);
            SetAnchoredRect(
                subtitle.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 76f),
                new Vector2(900f, 48f));
        }

        private static Button CreateButton(
            string name,
            Transform parent,
            Font font,
            string label,
            TitleButtonStyle style,
            float width,
            float height)
        {
            GameObject buttonObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(LayoutElement));
            buttonObject.transform.SetParent(parent, false);
            Image image = buttonObject.GetComponent<Image>();
            image.color = style == TitleButtonStyle.Primary
                ? ButtonColor
                : Color.clear;
            if (style == TitleButtonStyle.Secondary)
            {
                CreateButtonBorder(buttonObject.transform);
            }
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = style == TitleButtonStyle.Primary
                ? new Color32(230, 235, 243, 255)
                : new Color32(225, 231, 240, 255);
            colors.pressedColor = new Color32(205, 214, 226, 255);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color32(175, 183, 195, 255);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            LayoutElement element = buttonObject.GetComponent<LayoutElement>();
            element.preferredWidth = width;
            element.preferredHeight = height;

            Text text = CreateText(
                "Label", buttonObject.transform, font,
                style == TitleButtonStyle.Primary ? 32 : 28,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                label,
                style == TitleButtonStyle.Primary ? Color.white : TitleColor);
            Stretch(text.rectTransform);
            return button;
        }

        private static void CreateButtonBorder(Transform parent)
        {
            CreateBorderEdge(
                "Top Border", parent,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -1.5f), new Vector2(0f, 3f));
            CreateBorderEdge(
                "Bottom Border", parent,
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0f, 1.5f), new Vector2(0f, 3f));
            CreateBorderEdge(
                "Left Border", parent,
                new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(1.5f, 0f), new Vector2(3f, 0f));
            CreateBorderEdge(
                "Right Border", parent,
                new Vector2(1f, 0f), new Vector2(1f, 1f),
                new Vector2(-1.5f, 0f), new Vector2(3f, 0f));
        }

        private static void CreateBorderEdge(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 position,
            Vector2 size)
        {
            GameObject edgeObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            edgeObject.transform.SetParent(parent, false);
            RectTransform edgeRect = edgeObject.GetComponent<RectTransform>();
            edgeRect.anchorMin = anchorMin;
            edgeRect.anchorMax = anchorMax;
            edgeRect.pivot = new Vector2(0.5f, 0.5f);
            edgeRect.anchoredPosition = position;
            edgeRect.sizeDelta = size;
            Image edge = edgeObject.GetComponent<Image>();
            edge.color = ButtonColor;
            edge.raycastTarget = false;
        }

        private enum TitleButtonStyle
        {
            Primary,
            Secondary
        }

        private static Text CreateText(
            string name,
            Transform parent,
            Font font,
            int fontSize,
            FontStyle fontStyle,
            TextAnchor alignment,
            string value,
            Color color)
        {
            GameObject textObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.text = value;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform CreateStretchRect(
            string name,
            Transform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            Stretch(rect);
            return rect;
        }

        private static RectTransform CreateRect(
            string name,
            Transform parent,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 position,
            Vector2 size)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            SetAnchoredRect(rect, anchor, pivot, position, size);
            return rect;
        }

        private static void SetAnchoredRect(
            RectTransform rect,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 position,
            Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Assign(Object target, string fieldName, Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);
            if (property == null)
            {
                throw new InvalidOperationException(
                    $"Missing serialized field '{fieldName}' on {target.GetType().Name}.");
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}

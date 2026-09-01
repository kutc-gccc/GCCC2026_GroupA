using System;
using GCCC.BoardGame.Presentation.Views;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace GCCC.BoardGame.EditorTools
{
    internal static class BoardGameHudPrefabMigration
    {
        private const string MenuPath = "GCCC/Migrate HUD Prefab and SampleScene";
        private const string PrefabPath = "Assets/Prefabs/BoardGame/GameHud.prefab";
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";
        // 可変フォントは legacy UI Text がウェイト軸を扱えず細く潰れるため、
        // 静的な Medium を使う。
        private const string FontPath = "Assets/NotoSansJP-Medium.ttf";

        private static readonly Color32 PanelColor =
            new Color32(35, 41, 52, 225);
        private static readonly Color32 NavyColor =
            new Color32(38, 54, 77, 255);
        private static readonly Color32 ButtonColor =
            new Color32(235, 238, 244, 255);
        private static readonly Color32 ButtonTextColor =
            new Color32(35, 41, 52, 255);
        private static readonly Color32 LegendSurfaceColor =
            new Color32(248, 249, 251, 248);
        private static readonly Color32 BorderColor =
            new Color32(158, 166, 178, 255);
        private static readonly Color32 DangerColor =
            new Color32(198, 40, 40, 255);

        [MenuItem(MenuPath)]
        private static void Apply()
        {
            BuildHudPrefab();
            EnsureSampleSceneEventSystem();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("GCCC HUD prefab and SampleScene migration completed.");
        }

        private static void BuildHudPrefab()
        {
            Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (font == null)
            {
                throw new MissingReferenceException($"UI font not found: {FontPath}");
            }

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                while (root.transform.childCount > 0)
                {
                    Object.DestroyImmediate(root.transform.GetChild(0).gameObject);
                }

                GameHudView hud = root.GetComponent<GameHudView>();
                if (hud == null)
                {
                    hud = root.AddComponent<GameHudView>();
                }

                GameObject canvasObject = CreateObject(
                    "Board UI",
                    root.transform,
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster));
                Canvas canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;

                Text status = BuildStatusArea(canvasObject.transform, font, out Text message);
                BuildOperationBar(
                    canvasObject.transform,
                    font,
                    out Button randomize,
                    out Button fuse,
                    out Button reserveDeploy);
                Button reset = BuildResetButton(canvasObject.transform, font);
                ReservePanelView reserves = BuildReservePanels(
                    canvasObject.transform,
                    font,
                    out RectTransform player1Panel,
                    out RectTransform player2Panel,
                    out Transform player1Cards,
                    out Transform player2Cards,
                    out Text player1Header,
                    out Text player2Header);
                BuildTerritoryLabels(canvasObject.transform, font);
                RectTransform audioRect = BuildAudioControls(
                    canvasObject.transform,
                    font,
                    out Slider bgm,
                    out Slider sfx);
                GameObject legend = BuildEffectLegend(canvasObject.transform, font);
                BuildResultOverlay(
                    canvasObject.transform,
                    font,
                    out GameObject resultOverlay,
                    out Text resultText,
                    out Button resultButton);

                Assign(reserves, "player1PanelRect", player1Panel);
                Assign(reserves, "player2PanelRect", player2Panel);
                Assign(reserves, "player1CardsRoot", player1Cards);
                Assign(reserves, "player2CardsRoot", player2Cards);
                Assign(reserves, "player1Header", player1Header);
                Assign(reserves, "player2Header", player2Header);

                Assign(hud, "statusLabel", status);
                Assign(hud, "messageLabel", message);
                Assign(hud, "resetButton", reset);
                Assign(hud, "randomizePowerButton", randomize);
                Assign(hud, "fuseButton", fuse);
                Assign(hud, "reserveDeployButton", reserveDeploy);
                Assign(hud, "audioControlsRect", audioRect);
                Assign(hud, "bgmSlider", bgm);
                Assign(hud, "sfxSlider", sfx);
                Assign(hud, "reservePanelView", reserves);
                Assign(hud, "effectLegend", legend);
                Assign(hud, "uiFont", font);
                Assign(hud, "resultOverlay", resultOverlay);
                Assign(hud, "resultLabel", resultText);
                Assign(hud, "resultButton", resultButton);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Text BuildStatusArea(
            Transform parent,
            Font font,
            out Text message)
        {
            RectTransform stack = CreateRect(
                "Status Stack",
                parent,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(24f, -24f),
                new Vector2(540f, 120f));
            VerticalLayoutGroup layout = stack.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            Text status = CreatePanelText(
                "Turn Status", stack, font, 28, TextAnchor.MiddleLeft, 64f);
            status.fontStyle = FontStyle.Bold;
            status.transform.parent.GetComponent<Image>().color = NavyColor;
            message = CreateText(
                "Fusion Message", stack, font, 24, TextAnchor.MiddleLeft);
            // 合体の成否メッセージ。明色の背景に黄色は読みにくいので黒で出す。
            message.color = Color.black;
            message.gameObject.AddComponent<LayoutElement>().preferredHeight = 48f;
            return status;
        }

        private static void BuildOperationBar(
            Transform parent,
            Font font,
            out Button randomize,
            out Button fuse,
            out Button reserveDeploy)
        {
            RectTransform bar = CreateRect(
                "Operation Bar",
                parent,
                Vector2.one,
                Vector2.one,
                new Vector2(-24f, -24f),
                new Vector2(574f, 64f));
            HorizontalLayoutGroup layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = false;
            layout.childAlignment = TextAnchor.UpperRight;

            reserveDeploy = CreateButton("Reserve Deploy Button", bar, font, "リザーブ配置", 190f);
            fuse = CreateButton("Fuse Button", bar, font, "合体", 140f);
            randomize = CreateButton(
                "Randomize Power Button", bar, font, "パワーランダム化", 220f);
            reserveDeploy.interactable = false;
            fuse.interactable = false;
            randomize.interactable = false;
        }

        private static Button BuildResetButton(Transform parent, Font font)
        {
            Button reset = CreateButton(
                "Reset Button",
                parent,
                font,
                "リセット",
                180f,
                ButtonVisualStyle.DangerOutline);
            Object.DestroyImmediate(reset.GetComponent<LayoutElement>());
            SetAnchoredRect(
                reset.GetComponent<RectTransform>(),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(-24f, 24f),
                new Vector2(180f, 64f));
            return reset;
        }

        private static ReservePanelView BuildReservePanels(
            Transform parent,
            Font font,
            out RectTransform player1Panel,
            out RectTransform player2Panel,
            out Transform player1Cards,
            out Transform player2Cards,
            out Text player1Header,
            out Text player2Header)
        {
            RectTransform root = CreateStretchRect("Reserve Panels", parent);
            ReservePanelView view = root.gameObject.AddComponent<ReservePanelView>();
            player2Panel = CreateReservePanel(
                "Player 2 Reserve Panel",
                root,
                font,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-24f, -128f),
                Color.white,
                out player2Header,
                out player2Cards);
            player1Panel = CreateReservePanel(
                "Player 1 Reserve Panel",
                root,
                font,
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(-24f, 128f),
                Color.white,
                out player1Header,
                out player1Cards);
            return view;
        }

        private static RectTransform CreateReservePanel(
            string name,
            Transform parent,
            Font font,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 position,
            Color headerColor,
            out Text header,
            out Transform cardsRoot)
        {
            RectTransform panel = CreateRect(
                name, parent, anchor, pivot, position, new Vector2(620f, 170f));
            panel.gameObject.AddComponent<Image>().color = PanelColor;
            VerticalLayoutGroup vertical = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            vertical.padding = new RectOffset(16, 16, 8, 8);
            vertical.spacing = 8f;
            vertical.childControlHeight = true;
            vertical.childControlWidth = true;
            vertical.childForceExpandHeight = false;
            vertical.childForceExpandWidth = true;
            ContentSizeFitter fitter = panel.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            header = CreateText("Header", panel, font, 22, TextAnchor.MiddleLeft);
            header.color = headerColor;
            header.fontStyle = FontStyle.Bold;
            header.gameObject.AddComponent<LayoutElement>().preferredHeight = 32f;

            RectTransform cards = CreateRect(
                "Cards", panel, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(588f, 104f));
            HorizontalLayoutGroup horizontal = cards.gameObject.AddComponent<HorizontalLayoutGroup>();
            horizontal.spacing = 8f;
            horizontal.childControlHeight = true;
            horizontal.childControlWidth = true;
            horizontal.childForceExpandHeight = false;
            horizontal.childForceExpandWidth = false;
            cards.gameObject.AddComponent<LayoutElement>().preferredHeight = 104f;
            cardsRoot = cards;
            return panel;
        }

        private static void BuildTerritoryLabels(Transform parent, Font font)
        {
            CreateAnchoredPanelText(
                "Player 2 Territory Label",
                parent,
                font,
                "▼ プレイヤー2の陣地",
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -12f));
            CreateAnchoredPanelText(
                "Player 1 Territory Label",
                parent,
                font,
                "▲ プレイヤー1の陣地",
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 12f));
        }

        private static RectTransform BuildAudioControls(
            Transform parent,
            Font font,
            out Slider bgm,
            out Slider sfx)
        {
            RectTransform panel = CreateRect(
                "Audio Volume Controls",
                parent,
                Vector2.zero,
                Vector2.zero,
                new Vector2(24f, 24f),
                new Vector2(300f, 104f));
            VerticalLayoutGroup vertical = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            vertical.spacing = 4f;
            vertical.childControlHeight = true;
            vertical.childControlWidth = true;
            vertical.childForceExpandHeight = false;
            vertical.childForceExpandWidth = true;
            bgm = CreateSliderRow("BGM", panel, font, out _);
            sfx = CreateSliderRow("SFX", panel, font, out _);
            bgm.gameObject.name = "BGM Slider";
            sfx.gameObject.name = "SFX Slider";
            return panel;
        }

        private static Slider CreateSliderRow(
            string labelText,
            Transform parent,
            Font font,
            out Text label)
        {
            RectTransform row = CreateRect(
                labelText + " Row", parent, Vector2.zero, Vector2.zero,
                Vector2.zero, new Vector2(300f, 48f));
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;
            label = CreateText(labelText + " Label", row, font, 22, TextAnchor.MiddleLeft);
            label.text = labelText;
            label.color = ButtonTextColor;
            label.fontStyle = FontStyle.Bold;
            label.gameObject.AddComponent<LayoutElement>().preferredWidth = 64f;

            GameObject sliderObject = CreateObject(
                labelText + " Slider",
                row,
                typeof(RectTransform),
                typeof(Slider),
                typeof(LayoutElement));
            LayoutElement sliderLayout = sliderObject.GetComponent<LayoutElement>();
            sliderLayout.preferredWidth = 220f;
            sliderLayout.preferredHeight = 40f;
            Slider slider = sliderObject.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;

            RectTransform background = CreateRect(
                "Background",
                sliderObject.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(220f, 8f));
            Image backgroundImage = background.gameObject.AddComponent<Image>();
            backgroundImage.color = new Color32(183, 189, 198, 255);

            RectTransform slideArea = CreateStretchRect("Handle Slide Area", sliderObject.transform);
            slideArea.offsetMin = new Vector2(11f, 0f);
            slideArea.offsetMax = new Vector2(-11f, 0f);
            GameObject handleObject = CreateObject(
                "Handle",
                slideArea,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            RectTransform handle = handleObject.GetComponent<RectTransform>();
            handle.anchorMin = new Vector2(0f, 0f);
            handle.anchorMax = new Vector2(0f, 1f);
            handle.pivot = new Vector2(0.5f, 0.5f);
            handle.sizeDelta = new Vector2(22f, 0f);
            Image handleImage = handleObject.GetComponent<Image>();
            handleImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(
                "UI/Skin/Knob.psd");
            handleImage.color = Color.white;
            Outline handleOutline = handleObject.AddComponent<Outline>();
            handleOutline.effectColor = BorderColor;
            handleOutline.effectDistance = new Vector2(1f, -1f);
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            return slider;
        }

        private static GameObject BuildEffectLegend(Transform parent, Font font)
        {
            RectTransform panel = CreateRect(
                "Cell Effect Legend",
                parent,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(24f, 0f),
                new Vector2(400f, 282f));
            panel.gameObject.AddComponent<Image>().color = LegendSurfaceColor;
            Outline panelOutline = panel.gameObject.AddComponent<Outline>();
            panelOutline.effectColor = BorderColor;
            panelOutline.effectDistance = new Vector2(1f, -1f);
            VerticalLayoutGroup vertical = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            vertical.padding = new RectOffset(20, 20, 16, 16);
            vertical.spacing = 6f;
            vertical.childControlHeight = true;
            vertical.childControlWidth = true;
            vertical.childForceExpandHeight = false;
            vertical.childForceExpandWidth = true;
            CreateLegendRow(
                panel, font, "Selected", "選択中（琥珀の枠）",
                LegendIconStyle.Frame, new Color32(255, 179, 0, 255));
            CreateLegendRow(
                panel, font, "Movable", "移動可能（白い点）",
                LegendIconStyle.Dot, Color.white);
            CreateLegendRow(
                panel, font, "Combat", "戦闘可能（赤い枠）",
                LegendIconStyle.Frame, new Color32(244, 67, 54, 255));
            CreateLegendRow(
                panel, font, "Fusion", "合体候補（青い枠）",
                LegendIconStyle.Frame, new Color32(33, 150, 243, 255));
            CreateLegendRow(
                panel, font, "Permanent", "永続効果（紫の塗り）",
                LegendIconStyle.Fill, new Color32(156, 39, 176, 255));
            CreateLegendRow(
                panel, font, "While Occupied", "滞在中効果（シアンの塗り）",
                LegendIconStyle.Fill, new Color32(0, 188, 212, 255));
            panel.gameObject.SetActive(false);
            return panel.gameObject;
        }

        private static void CreateLegendRow(
            Transform parent,
            Font font,
            string name,
            string text,
            LegendIconStyle iconStyle,
            Color color)
        {
            RectTransform row = CreateRect(
                name + " Legend Row", parent, Vector2.zero, Vector2.zero,
                Vector2.zero, new Vector2(360f, 36f));
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;
            CreateLegendIcon(row, iconStyle, color);
            Text label = CreateText("Legend Label", row, font, 22, TextAnchor.MiddleLeft);
            label.text = text;
            label.color = ButtonTextColor;
            label.fontStyle = FontStyle.Bold;
            label.gameObject.AddComponent<LayoutElement>().preferredWidth = 310f;
        }

        private static void CreateLegendIcon(
            Transform parent,
            LegendIconStyle iconStyle,
            Color color)
        {
            RectTransform icon = CreateRect(
                "Legend Icon",
                parent,
                Vector2.zero,
                Vector2.zero,
                Vector2.zero,
                new Vector2(28f, 28f));
            LayoutElement iconLayout = icon.gameObject.AddComponent<LayoutElement>();
            iconLayout.preferredWidth = 28f;
            iconLayout.preferredHeight = 28f;

            if (iconStyle == LegendIconStyle.Frame)
            {
                CreateBorder(icon, color, 3f);
                return;
            }

            RectTransform mark = CreateRect(
                "Mark",
                icon,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                iconStyle == LegendIconStyle.Dot
                    ? new Vector2(18f, 18f)
                    : new Vector2(24f, 24f));
            Image markImage = mark.gameObject.AddComponent<Image>();
            markImage.color = color;
            if (iconStyle == LegendIconStyle.Dot)
            {
                markImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(
                    "UI/Skin/Knob.psd");
                Outline dotOutline = mark.gameObject.AddComponent<Outline>();
                dotOutline.effectColor = BorderColor;
                dotOutline.effectDistance = new Vector2(1f, -1f);
            }
        }

        private static void BuildResultOverlay(
            Transform parent,
            Font font,
            out GameObject overlay,
            out Text resultText,
            out Button resultButton)
        {
            RectTransform overlayRect = CreateStretchRect("Result Overlay", parent);
            Image overlayImage = overlayRect.gameObject.AddComponent<Image>();
            overlayImage.color = new Color32(24, 27, 34, 220);
            overlayImage.raycastTarget = true;
            overlay = overlayRect.gameObject;

            RectTransform panel = CreateRect(
                "Result Panel",
                overlayRect,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(720f, 400f));
            panel.gameObject.AddComponent<Image>().color = new Color32(42, 47, 57, 255);
            VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(40, 40, 60, 60);
            layout.spacing = 36f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true;

            resultText = CreateText("Result Text", panel, font, 48, TextAnchor.MiddleCenter);
            resultText.fontStyle = FontStyle.Bold;
            resultText.gameObject.AddComponent<LayoutElement>().preferredHeight = 160f;
            resultButton = CreateButton(
                "Return To Title Button", panel, font, "スタート画面に戻る", 360f);
            resultButton.gameObject.GetComponent<LayoutElement>().preferredHeight = 72f;
            overlay.SetActive(false);
        }

        private static Text CreatePanelText(
            string name,
            Transform parent,
            Font font,
            int fontSize,
            TextAnchor alignment,
            float height)
        {
            GameObject panel = CreateObject(
                name,
                parent,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LayoutElement));
            panel.GetComponent<Image>().color = PanelColor;
            panel.GetComponent<LayoutElement>().preferredHeight = height;
            Text text = CreateText(name + " Text", panel.transform, font, fontSize, alignment);
            RectTransform rect = text.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(12f, 6f);
            rect.offsetMax = new Vector2(-12f, -6f);
            return text;
        }

        private static void CreateAnchoredPanelText(
            string name,
            Transform parent,
            Font font,
            string text,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 position)
        {
            RectTransform panel = CreateRect(
                name, parent, anchor, pivot, position, new Vector2(320f, 42f));
            panel.gameObject.AddComponent<Image>().color = PanelColor;
            Text label = CreateText(name + " Text", panel, font, 22, TextAnchor.MiddleCenter);
            label.text = text;
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
        }

        private static Button CreateButton(
            string name,
            Transform parent,
            Font font,
            string label,
            float width,
            ButtonVisualStyle style = ButtonVisualStyle.Standard)
        {
            GameObject buttonObject = CreateObject(
                name,
                parent,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(LayoutElement));
            Image image = buttonObject.GetComponent<Image>();
            image.color = style == ButtonVisualStyle.DangerOutline
                ? new Color32(255, 255, 255, 8)
                : ButtonColor;
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color32(230, 235, 243, 255);
            colors.pressedColor = new Color32(205, 214, 226, 255);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color32(175, 183, 195, 255);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            LayoutElement element = buttonObject.GetComponent<LayoutElement>();
            element.preferredWidth = width;
            element.preferredHeight = 64f;
            Text text = CreateText("Label", buttonObject.transform, font, 22, TextAnchor.MiddleCenter);
            text.text = label;
            text.color = style == ButtonVisualStyle.DangerOutline
                ? DangerColor
                : ButtonTextColor;
            text.fontStyle = FontStyle.Bold;
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            CreateBorder(
                buttonObject.transform,
                style == ButtonVisualStyle.DangerOutline
                    ? DangerColor
                    : BorderColor,
                3f);
            return button;
        }

        private static void CreateBorder(
            Transform parent,
            Color color,
            float thickness)
        {
            CreateBorderEdge(
                "Top Border", parent,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -thickness * 0.5f),
                new Vector2(0f, thickness), color);
            CreateBorderEdge(
                "Bottom Border", parent,
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0f, thickness * 0.5f),
                new Vector2(0f, thickness), color);
            CreateBorderEdge(
                "Left Border", parent,
                new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(thickness * 0.5f, 0f),
                new Vector2(thickness, 0f), color);
            CreateBorderEdge(
                "Right Border", parent,
                new Vector2(1f, 0f), new Vector2(1f, 1f),
                new Vector2(-thickness * 0.5f, 0f),
                new Vector2(thickness, 0f), color);
        }

        private static void CreateBorderEdge(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 position,
            Vector2 size,
            Color color)
        {
            GameObject edgeObject = CreateObject(
                name,
                parent,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            RectTransform edgeRect = edgeObject.GetComponent<RectTransform>();
            edgeRect.anchorMin = anchorMin;
            edgeRect.anchorMax = anchorMax;
            edgeRect.pivot = new Vector2(0.5f, 0.5f);
            edgeRect.anchoredPosition = position;
            edgeRect.sizeDelta = size;
            Image edgeImage = edgeObject.GetComponent<Image>();
            edgeImage.color = color;
            edgeImage.raycastTarget = false;
        }

        private static Text CreateText(
            string name,
            Transform parent,
            Font font,
            int fontSize,
            TextAnchor alignment)
        {
            GameObject textObject = CreateObject(
                name,
                parent,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            Text text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform CreateRect(
            string name,
            Transform parent,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 position,
            Vector2 size)
        {
            GameObject gameObject = CreateObject(name, parent, typeof(RectTransform));
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

        private static RectTransform CreateStretchRect(string name, Transform parent)
        {
            RectTransform rect = CreateRect(
                name, parent, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static GameObject CreateObject(
            string name,
            Transform parent,
            params System.Type[] components)
        {
            GameObject gameObject = new GameObject(name, components);
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private enum ButtonVisualStyle
        {
            Standard,
            DangerOutline
        }

        private enum LegendIconStyle
        {
            Frame,
            Dot,
            Fill
        }

        private static void Assign(Object target, string propertyName, Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new MissingFieldException(target.GetType().Name, propertyName);
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureSampleSceneEventSystem()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventObject = new GameObject(
                    "EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                eventSystem = eventObject.GetComponent<EventSystem>();
            }

            InputSystemUIInputModule inputModule =
                eventSystem.GetComponent<InputSystemUIInputModule>();
            if (inputModule == null)
            {
                inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }

            inputModule.AssignDefaultActions();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}

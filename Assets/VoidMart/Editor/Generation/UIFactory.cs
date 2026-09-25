using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VoidMart.Data;
using VoidMart.UI;

namespace VoidMart.EditorTools
{
    /// <summary>
    /// Builds the entire interface in code: HUD, joystick, modals, the puzzle overlay, the splash
    /// screen and world-space price tags.  Layout follows the spec's HUD map - level and XP
    /// top-left, currencies top-right, capacity gauge centre-bottom.
    /// </summary>
    public static class UIFactory
    {
        // ------------------------------------------------------------- primitives

        public static RectTransform NewRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return rect;
        }

        public static RectTransform FullScreen(string name, Transform parent)
        {
            var rect = NewRect(name, parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        public static Image NewImage(string name, Transform parent, Sprite sprite, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size, bool sliced = false)
        {
            var rect = NewRect(name, parent, anchorMin, anchorMax, pivot, position, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.raycastTarget = false;
            return image;
        }

        public static VMText NewText(string name, Transform parent, GameAssets assets, string content, float size,
            Color color, TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 rectSize)
        {
            var rect = NewRect(name, parent, anchorMin, anchorMax, pivot, position, rectSize);
            var text = rect.gameObject.AddComponent<VMText>();
            text.Setup(assets.displayFont, content, size, color, alignment);
            text.raycastTarget = false;
            text.SetShadow(new Vector2(0f, -3f), new Color(0f, 0f, 0f, 0.35f));
            return text;
        }

        public static Button NewButton(string name, Transform parent, GameAssets assets, string spriteKey, Color tint,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var image = NewImage(name, parent, assets.GetSprite(spriteKey), tint,
                anchorMin, anchorMax, pivot, position, size, true);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            var colors = button.colors;
            colors.highlightedColor = new Color(1.06f, 1.06f, 1.06f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.9f, 1f);
            colors.disabledColor = new Color(0.65f, 0.65f, 0.7f, 0.7f);
            colors.fadeDuration = 0.06f;
            button.colors = colors;
            return button;
        }

        static Image AddIcon(Transform parent, GameAssets assets, string iconKey, Color tint, Vector2 position, float size)
        {
            return NewImage("Icon " + iconKey, parent, assets.GetSprite(iconKey), tint,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(size, size));
        }

        // ------------------------------------------------------------ game canvas

        public class GameUi
        {
            public GameObject Root;
            public Canvas Canvas;
            public HUDController Hud;
            public VirtualJoystick Joystick;
            public UpgradeModal Upgrades;
            public SettingsPanel Settings;
            public PuzzleOverlayUI Puzzle;
            public ToastView Toast;
            public FloatingTextSpawner Floaters;
            public IrisWipe Iris;
        }

        public static GameUi BuildGameCanvas(GameConfig config, GameAssets assets)
        {
            var theme = config.theme;
            var ui = new GameUi();

            var root = new GameObject("UI Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            ui.Root = root;
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;
            ui.Canvas = canvas;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(config.ui.referenceWidth, config.ui.referenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = config.ui.matchWidthOrHeight;

            // ---- input layer (must sit under the buttons in the hierarchy) -------
            var joystickArea = FullScreen("Joystick Area", root.transform);
            joystickArea.anchorMax = new Vector2(1f, 0.62f);
            joystickArea.offsetMin = Vector2.zero;
            joystickArea.offsetMax = Vector2.zero;
            var joystickCatcher = joystickArea.gameObject.AddComponent<Image>();
            joystickCatcher.color = new Color(0f, 0f, 0f, 0f);
            joystickCatcher.raycastTarget = true;

            var joystickGroup = joystickArea.gameObject.AddComponent<CanvasGroup>();
            joystickGroup.alpha = config.ui.joystickOpacity;
            joystickGroup.blocksRaycasts = true;

            float baseSize = config.ui.joystickRadius * 2f;
            var joystickBase = NewImage("Joystick Base", joystickArea, assets.GetSprite("ui_joystick_base"), Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -config.ui.referenceHeight * 0.12f), new Vector2(baseSize, baseSize));
            var knob = NewImage("Joystick Knob", joystickArea, assets.GetSprite("ui_joystick_knob"), Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                joystickBase.rectTransform.anchoredPosition,
                new Vector2(config.ui.joystickKnobRadius * 2f, config.ui.joystickKnobRadius * 2f));

            var joystick = joystickArea.gameObject.AddComponent<VirtualJoystick>();
            joystick.Configure(config, joystickBase.rectTransform, knob.rectTransform, joystickGroup);
            ui.Joystick = joystick;

            // ---- safe-area HUD ---------------------------------------------------
            var safe = FullScreen("Safe Area", root.transform);
            safe.gameObject.AddComponent<SafeAreaFitter>();

            var hudObject = new GameObject("HUD", typeof(RectTransform));
            hudObject.transform.SetParent(safe, false);
            var hudRect = (RectTransform)hudObject.transform;
            hudRect.anchorMin = Vector2.zero;
            hudRect.anchorMax = Vector2.one;
            hudRect.offsetMin = Vector2.zero;
            hudRect.offsetMax = Vector2.zero;
            var hud = hudObject.AddComponent<HUDController>();
            var refs = new HUDController.Refs();

            // Level + XP (top-left)
            var levelGroup = NewRect("Level Group", hudRect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(28f, -28f), new Vector2(300f, 96f));
            NewImage("Level BG", levelGroup, assets.GetSprite("ui_pill"), new Color(theme.voidIndigo.r, theme.voidIndigo.g, theme.voidIndigo.b, 0.85f),
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, true);
            AddIcon(levelGroup, assets, "icon_star", theme.sunsetAmber, new Vector2(-112f, 0f), 54f);
            refs.levelLabel = NewText("Level", levelGroup, assets, "LV 1", 44f, theme.paper, TextAnchor.MiddleLeft,
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(28f, 0f), new Vector2(-96f, -18f));

            var xpBack = NewImage("XP BG", hudRect, assets.GetSprite("ui_bar_bg"), new Color(0f, 0f, 0f, 0.35f),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(34f, -132f), new Vector2(288f, 26f), true);
            refs.xpFill = NewImage("XP Fill", xpBack.transform, assets.GetSprite("ui_bar_fill"), theme.neonCyan,
                Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero, true);
            refs.xpFill.rectTransform.offsetMin = new Vector2(3f, 3f);
            refs.xpFill.rectTransform.offsetMax = new Vector2(-3f, -3f);
            refs.xpFill.type = Image.Type.Filled;
            refs.xpFill.fillMethod = Image.FillMethod.Horizontal;
            refs.xpFill.fillAmount = 0f;

            // Currencies (top-right)
            var cashGroup = NewRect("Cash Group", hudRect, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-28f, -28f), new Vector2(330f, 96f));
            NewImage("Cash BG", cashGroup, assets.GetSprite("ui_pill"), new Color(theme.voidIndigo.r, theme.voidIndigo.g, theme.voidIndigo.b, 0.85f),
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, true);
            AddIcon(cashGroup, assets, "icon_cash", theme.mint, new Vector2(-124f, 0f), 56f);
            refs.cashLabel = NewText("Cash", cashGroup, assets, "$0", 44f, theme.paper, TextAnchor.MiddleRight,
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-12f, 0f), new Vector2(-110f, -18f));
            refs.cashGroup = cashGroup;

            var gemGroup = NewRect("Gem Group", hudRect, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-28f, -136f), new Vector2(250f, 82f));
            NewImage("Gem BG", gemGroup, assets.GetSprite("ui_pill"), new Color(theme.voidIndigo.r, theme.voidIndigo.g, theme.voidIndigo.b, 0.75f),
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, true);
            AddIcon(gemGroup, assets, "icon_gem", theme.neonCyan, new Vector2(-88f, 0f), 46f);
            refs.gemLabel = NewText("Gems", gemGroup, assets, "0", 38f, theme.paper, TextAnchor.MiddleRight,
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-12f, 0f), new Vector2(-90f, -16f));
            refs.gemGroup = gemGroup;

            // Area banner (top-centre)
            refs.areaLabel = NewText("Area", hudRect, assets, "THE STRIP", 34f, new Color(1f, 1f, 1f, 0.72f), TextAnchor.UpperCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(460f, 48f));

            // Capacity gauge (bottom-centre)
            var capacityGroup = NewRect("Capacity", hudRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 42f), new Vector2(210f, 210f));
            refs.capacityGlow = NewImage("Glow", capacityGroup, assets.GetSprite("ui_glow"), new Color(1f, 1f, 1f, 0f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300f, 300f));
            NewImage("Track", capacityGroup, assets.GetSprite("ui_ring"), new Color(0f, 0f, 0f, 0.35f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200f, 200f));
            refs.capacityFill = NewImage("Fill", capacityGroup, assets.GetSprite("ui_ring"), theme.neonCyan,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200f, 200f));
            refs.capacityFill.type = Image.Type.Filled;
            refs.capacityFill.fillMethod = Image.FillMethod.Radial360;
            refs.capacityFill.fillOrigin = (int)Image.Origin360.Top;
            refs.capacityFill.fillClockwise = true;
            refs.capacityFill.fillAmount = 0f;
            NewImage("Hole", capacityGroup, assets.GetSprite("ui_hole_badge"), Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(132f, 132f));
            refs.capacityLabel = NewText("Capacity", capacityGroup, assets, "0%", 40f, theme.paper, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -2f), new Vector2(140f, 60f));
            refs.capacityGroup = capacityGroup;

            // Buttons (bottom-right)
            var upgradeButton = NewButton("Upgrades", hudRect, assets, "ui_button_primary", Color.white,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-28f, 42f), new Vector2(160f, 160f));
            AddIcon(upgradeButton.transform, assets, "icon_gear", Color.white, new Vector2(0f, 6f), 84f);
            refs.upgradeButton = upgradeButton.gameObject;

            var settingsButton = NewButton("Settings", hudRect, assets, "ui_button_neutral", Color.white,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-28f, 222f), new Vector2(120f, 120f));
            AddIcon(settingsButton.transform, assets, "icon_music", Color.white, new Vector2(0f, 4f), 60f);

            // Fever offer (right edge)
            var feverButton = NewButton("Fever", hudRect, assets, "ui_button_warn", Color.white,
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-28f, 120f), new Vector2(150f, 150f));
            AddIcon(feverButton.transform, assets, "icon_bolt", Color.white, new Vector2(0f, 10f), 76f);
            NewText("Fever Label", feverButton.transform, assets, "3X", 30f, theme.voidInk, TextAnchor.LowerCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(140f, 34f));
            refs.feverButton = feverButton.gameObject;
            feverButton.gameObject.SetActive(false);
            feverButton.onClick.AddListener(hud.OnFeverPressed);

            hud.Bind(config, refs);
            ui.Hud = hud;

            // ---- floating labels -------------------------------------------------
            var floatLayer = FullScreen("Floating Text", safe);
            var floatLabels = new VMText[12];
            for (int i = 0; i < floatLabels.Length; i++)
            {
                floatLabels[i] = NewText("Float " + i, floatLayer, assets, "+$0", 44f, theme.mint, TextAnchor.MiddleCenter,
                    new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300f, 70f));
            }
            var floaters = floatLayer.gameObject.AddComponent<FloatingTextSpawner>();
            floaters.Bind(config, floatLayer, canvas, floatLabels);
            ui.Floaters = floaters;

            // ---- toast -----------------------------------------------------------
            var toastPanel = NewRect("Toast", safe, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -210f), new Vector2(760f, 108f));
            var toastGroup = toastPanel.gameObject.AddComponent<CanvasGroup>();
            var toastBg = NewImage("BG", toastPanel, assets.GetSprite("ui_pill"), theme.electricViolet,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, true);
            var toastLabel = NewText("Label", toastPanel, assets, "", 40f, theme.voidInk, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-40f, -20f));
            var toast = toastPanel.gameObject.AddComponent<ToastView>();
            toast.Bind(config, toastPanel, toastBg, toastLabel, toastGroup);
            ui.Toast = toast;

            // ---- modals ----------------------------------------------------------
            ui.Upgrades = BuildUpgradeModal(config, assets, root.transform, theme);
            ui.Settings = BuildSettingsModal(config, assets, root.transform, theme);
            ui.Puzzle = BuildPuzzleOverlay(config, assets, root.transform, theme);

            upgradeButton.onClick.AddListener(ui.Upgrades.Open);
            settingsButton.onClick.AddListener(ui.Settings.Open);

            // ---- iris wipe (always last, drawn on top) ---------------------------
            var irisRoot = FullScreen("Iris Wipe", root.transform);
            var irisGroup = irisRoot.gameObject.AddComponent<CanvasGroup>();
            irisGroup.blocksRaycasts = false;
            irisGroup.interactable = false;
            irisGroup.alpha = 0f;
            var irisImage = NewImage("Mask", irisRoot, assets.GetSprite("ui_white"), Color.white,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            irisImage.rectTransform.offsetMin = Vector2.zero;
            irisImage.rectTransform.offsetMax = Vector2.zero;
            irisImage.material = assets.GetMaterial("mat_iris");
            var iris = irisRoot.gameObject.AddComponent<IrisWipe>();
            iris.Bind(config, canvas, irisImage, irisGroup);
            ui.Iris = iris;

            return ui;
        }

        // ------------------------------------------------------------- modals

        static (RectTransform panel, CanvasGroup group, GameObject blocker) BuildModalShell(
            string name, Transform parent, GameAssets assets, ThemeConfig theme, Vector2 size, Vector2 anchoredPosition, Vector2 pivot)
        {
            var host = FullScreen(name, parent);
            var blocker = NewImage("Blocker", host, assets.GetSprite("ui_white"), new Color(0f, 0f, 0f, 0.55f),
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            blocker.rectTransform.offsetMin = Vector2.zero;
            blocker.rectTransform.offsetMax = Vector2.zero;
            blocker.raycastTarget = true;

            var group = host.gameObject.AddComponent<CanvasGroup>();
            var panel = NewRect("Panel", host, new Vector2(0.5f, pivot.y), new Vector2(0.5f, pivot.y), new Vector2(0.5f, pivot.y),
                anchoredPosition, size);
            NewImage("BG", panel, assets.GetSprite("ui_sheet"), theme.paper,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, true);
            return (panel, group, blocker.gameObject);
        }

        static UpgradeModal BuildUpgradeModal(GameConfig config, GameAssets assets, Transform parent, ThemeConfig theme)
        {
            var shell = BuildModalShell("Upgrade Modal", parent, assets, theme,
                new Vector2(config.ui.referenceWidth - 80f, config.ui.referenceHeight * 0.62f),
                new Vector2(0f, 40f), new Vector2(0.5f, 0f));

            var header = NewText("Header", shell.panel, assets, "UPGRADES", 62f, theme.voidIndigo, TextAnchor.UpperCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(-80f, 78f));

            var close = NewButton("Close", shell.panel, assets, "ui_button_warn", Color.white,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -18f), new Vector2(96f, 96f));
            AddIcon(close.transform, assets, "icon_close", Color.white, new Vector2(0f, 4f), 48f);

            var viewport = NewRect("Viewport", shell.panel, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -56f), new Vector2(-56f, -168f));
            var viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.04f);
            viewportImage.raycastTarget = true;
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            var content = NewRect("Content", viewport, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                Vector2.zero, new Vector2(0f, 0f));
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 14f;
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = shell.panel.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRectMovementType();
            scroll.scrollSensitivity = 40f;

            var template = BuildUpgradeRow(config, assets, content, theme);
            template.SetActive(false);

            var modal = shell.panel.gameObject.AddComponent<UpgradeModal>();
            modal.BindModal(config, shell.panel, shell.group, ModalTransition.SlideUp, shell.blocker);
            modal.BindContent(content, template, header, close);
            return modal;
        }

        static ScrollRect.MovementType ScrollRectMovementType() => ScrollRect.MovementType.Elastic;

        static GameObject BuildUpgradeRow(GameConfig config, GameAssets assets, Transform parent, ThemeConfig theme)
        {
            var row = NewRect("Upgrade Row", parent, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                Vector2.zero, new Vector2(0f, 150f));
            var element = row.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = 150f;
            element.minHeight = 150f;

            var background = NewImage("BG", row, assets.GetSprite("ui_card"), theme.electricViolet,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, true);

            var icon = NewImage("Icon", row, assets.GetSprite("icon_bolt"), Color.white,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(86f, 0f), new Vector2(96f, 96f));

            var title = NewText("Title", row, assets, "Upgrade", 42f, theme.paper, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(150f, -24f), new Vector2(520f, 50f));
            var detail = NewText("Detail", row, assets, "LV 0", 30f, new Color(1f, 1f, 1f, 0.75f), TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(150f, -78f), new Vector2(520f, 42f));

            var levelBack = NewImage("Level BG", row, assets.GetSprite("ui_bar_bg"), new Color(0f, 0f, 0f, 0.3f),
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(150f, 22f), new Vector2(430f, 16f), true);
            var levelFill = NewImage("Level Fill", levelBack.transform, assets.GetSprite("ui_bar_fill"), theme.neonCyan,
                Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero, true);
            levelFill.rectTransform.offsetMin = new Vector2(2f, 2f);
            levelFill.rectTransform.offsetMax = new Vector2(-2f, -2f);
            levelFill.type = Image.Type.Filled;
            levelFill.fillMethod = Image.FillMethod.Horizontal;

            var buy = NewButton("Buy", row, assets, "ui_button_action", Color.white,
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-18f, 0f), new Vector2(230f, 116f));
            var cost = NewText("Cost", buy.transform, assets, "$0", 40f, theme.voidInk, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(-24f, -30f));

            var component = row.gameObject.AddComponent<UpgradeRow>();
            component.BindVisuals(icon, background, title, detail, cost, buy, levelFill);
            return row.gameObject;
        }

        static SettingsPanel BuildSettingsModal(GameConfig config, GameAssets assets, Transform parent, ThemeConfig theme)
        {
            var shell = BuildModalShell("Settings Modal", parent, assets, theme,
                new Vector2(880f, 1040f), Vector2.zero, new Vector2(0.5f, 0.5f));

            NewText("Header", shell.panel, assets, "SETTINGS", 60f, theme.voidIndigo, TextAnchor.UpperCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(-80f, 76f));

            var close = NewButton("Close", shell.panel, assets, "ui_button_warn", Color.white,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -18f), new Vector2(96f, 96f));
            AddIcon(close.transform, assets, "icon_close", Color.white, new Vector2(0f, 4f), 48f);

            var music = BuildSlider("Music", shell.panel, assets, theme, new Vector2(0f, -170f), "icon_music");
            var sfx = BuildSlider("SFX", shell.panel, assets, theme, new Vector2(0f, -290f), "icon_bolt");
            var haptics = BuildToggle("Haptics", shell.panel, assets, theme, new Vector2(0f, -410f));

            var noAds = NewButton("No Ads", shell.panel, assets, "ui_button_primary", Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -540f), new Vector2(680f, 130f));
            NewText("Label", noAds.transform, assets, "REMOVE ADS", 44f, Color.white, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(-40f, -30f));

            var restore = NewButton("Restore", shell.panel, assets, "ui_button_neutral", Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -690f), new Vector2(680f, 110f));
            NewText("Label", restore.transform, assets, "RESTORE PURCHASES", 34f, Color.white, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(-40f, -26f));

            var reset = NewButton("Reset", shell.panel, assets, "ui_button_warn", Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -824f), new Vector2(400f, 96f));
            NewText("Label", reset.transform, assets, "RESET SAVE", 32f, theme.voidInk, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(-30f, -24f));

            var version = NewText("Version", shell.panel, assets, "v" + config.buildVersion, 26f,
                new Color(theme.voidIndigo.r, theme.voidIndigo.g, theme.voidIndigo.b, 0.6f), TextAnchor.LowerCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(400f, 40f));

            var panel = shell.panel.gameObject.AddComponent<SettingsPanel>();
            panel.BindModal(config, shell.panel, shell.group, ModalTransition.Fade, shell.blocker);
            panel.BindControls(music, sfx, haptics, noAds, restore, close, reset, version);
            return panel;
        }

        static Slider BuildSlider(string name, Transform parent, GameAssets assets, ThemeConfig theme, Vector2 position, string iconKey)
        {
            var row = NewRect(name, parent, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), position, new Vector2(700f, 96f));
            AddIcon(row, assets, iconKey, theme.electricViolet, new Vector2(-300f, 0f), 62f);

            var track = NewImage("Track", row, assets.GetSprite("ui_bar_bg"), new Color(0f, 0f, 0f, 0.2f),
                new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(40f, 0f), new Vector2(-140f, 30f), true);

            var fillArea = NewRect("Fill Area", track.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            fillArea.offsetMin = new Vector2(4f, 4f);
            fillArea.offsetMax = new Vector2(-4f, -4f);
            var fill = NewImage("Fill", fillArea, assets.GetSprite("ui_bar_fill"), theme.mint,
                Vector2.zero, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, true);

            var handleArea = NewRect("Handle Area", track.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            handleArea.offsetMin = new Vector2(14f, 0f);
            handleArea.offsetMax = new Vector2(-14f, 0f);
            var handle = NewImage("Handle", handleArea, assets.GetSprite("ui_circle"), theme.paper,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(56f, 56f));
            handle.raycastTarget = true;

            var slider = track.gameObject.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.8f;
            track.raycastTarget = true;
            return slider;
        }

        static Toggle BuildToggle(string name, Transform parent, GameAssets assets, ThemeConfig theme, Vector2 position)
        {
            var row = NewRect(name, parent, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), position, new Vector2(700f, 96f));
            NewText("Label", row, assets, "HAPTICS", 40f, theme.voidIndigo, TextAnchor.MiddleLeft,
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-40f, 0f), new Vector2(-180f, -20f));

            var box = NewImage("Box", row, assets.GetSprite("ui_pill"), new Color(0f, 0f, 0f, 0.15f),
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-10f, 0f), new Vector2(150f, 74f), true);
            box.raycastTarget = true;
            var check = NewImage("Check", box.transform, assets.GetSprite("icon_check"), theme.mint,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(62f, 62f));

            var toggle = box.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = box;
            toggle.graphic = check;
            toggle.isOn = true;
            return toggle;
        }

        static PuzzleOverlayUI BuildPuzzleOverlay(GameConfig config, GameAssets assets, Transform parent, ThemeConfig theme)
        {
            var shell = BuildModalShell("Puzzle Overlay", parent, assets, theme,
                new Vector2(config.ui.referenceWidth - 60f, config.ui.referenceHeight * 0.78f), Vector2.zero, new Vector2(0.5f, 0.5f));

            var header = NewText("Header", shell.panel, assets, "UNJAM IT!", 68f, theme.voidIndigo, TextAnchor.UpperCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(-80f, 86f));

            var timerBack = NewImage("Timer BG", shell.panel, assets.GetSprite("ui_bar_bg"), new Color(0f, 0f, 0f, 0.2f),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(760f, 36f), true);
            var timerFill = NewImage("Timer Fill", timerBack.transform, assets.GetSprite("ui_bar_fill"), theme.neonCyan,
                Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero, true);
            timerFill.rectTransform.offsetMin = new Vector2(4f, 4f);
            timerFill.rectTransform.offsetMax = new Vector2(-4f, -4f);
            timerFill.type = Image.Type.Filled;
            timerFill.fillMethod = Image.FillMethod.Horizontal;

            var timerLabel = NewText("Timer", shell.panel, assets, "5.0s", 36f, theme.voidIndigo, TextAnchor.UpperCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -176f), new Vector2(300f, 46f));

            var board = NewRect("Board", shell.panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 90f), new Vector2(820f, 820f));

            var cellTemplate = NewImage("Cell Template", board, assets.GetSprite("ui_puzzle_cell"), theme.voidIndigo,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(90f, 90f), true);
            cellTemplate.gameObject.SetActive(false);

            var tray = NewRect("Tray", shell.panel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 60f), new Vector2(880f, 260f));
            var trayLayout = tray.gameObject.AddComponent<HorizontalLayoutGroup>();
            trayLayout.childAlignment = TextAnchor.MiddleCenter;
            trayLayout.spacing = 30f;
            trayLayout.childControlHeight = false;
            trayLayout.childControlWidth = false;

            var traySlot = NewRect("Tray Slot", tray, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(250f, 250f));
            traySlot.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.05f);
            traySlot.gameObject.GetComponent<Image>().raycastTarget = true;
            traySlot.gameObject.SetActive(false);

            var blockTemplate = NewImage("Block Template", traySlot, assets.GetSprite("ui_puzzle_cell"), theme.neonCyan,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(70f, 70f), true);
            blockTemplate.gameObject.SetActive(false);

            var skip = NewButton("Skip", shell.panel, assets, "ui_button_neutral", Color.white,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -18f), new Vector2(180f, 96f));
            AddIcon(skip.transform, assets, "icon_play", Color.white, new Vector2(-40f, 4f), 44f);
            NewText("Label", skip.transform, assets, "FIX", 34f, Color.white, TextAnchor.MiddleRight,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-18f, 4f), new Vector2(-60f, -24f));

            var overlay = shell.panel.gameObject.AddComponent<PuzzleOverlayUI>();
            overlay.BindModal(config, shell.panel, shell.group, ModalTransition.ScalePop, shell.blocker);
            overlay.BindBoard(board, cellTemplate, tray, traySlot, blockTemplate, timerFill, header, timerLabel, skip, 90f);
            return overlay;
        }

        // ------------------------------------------------------- world-space label

        public static ZoneLabel BuildWorldLabel(GameConfig config, GameAssets assets, Transform parent, string title, string price)
        {
            var theme = config.theme;
            var canvasObject = new GameObject("Label Canvas", typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(parent, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            var rect = (RectTransform)canvasObject.transform;
            rect.sizeDelta = new Vector2(320f, 180f);
            rect.localScale = Vector3.one * 0.01f;
            rect.localPosition = Vector3.zero;

            NewImage("BG", rect, assets.GetSprite("ui_pill"), new Color(theme.voidIndigo.r, theme.voidIndigo.g, theme.voidIndigo.b, 0.9f),
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, true);

            var titleText = NewText("Title", rect, assets, title, 38f, theme.paper, TextAnchor.UpperCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(-20f, 46f));
            var priceText = NewText("Price", rect, assets, price, 52f, theme.sunsetAmber, TextAnchor.UpperCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -62f), new Vector2(-20f, 62f));

            var barBack = NewImage("Bar BG", rect, assets.GetSprite("ui_bar_bg"), new Color(0f, 0f, 0f, 0.35f),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(260f, 22f), true);
            var fill = NewImage("Bar Fill", barBack.transform, assets.GetSprite("ui_bar_fill"), theme.mint,
                Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero, true);
            fill.rectTransform.offsetMin = new Vector2(3f, 3f);
            fill.rectTransform.offsetMax = new Vector2(-3f, -3f);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = 0f;

            var label = canvasObject.AddComponent<ZoneLabel>();
            label.BindVisuals(titleText, priceText, fill, rect);
            return label;
        }

        // -------------------------------------------------------------- boot ui

        public static Bootstrapper BuildBootCanvas(GameConfig config, GameAssets assets, string gameSceneName)
        {
            var theme = config.theme;
            var root = new GameObject("Boot Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(config.ui.referenceWidth, config.ui.referenceHeight);
            scaler.matchWidthOrHeight = 0.5f;

            var splash = FullScreen("Splash", root.transform);
            var group = splash.gameObject.AddComponent<CanvasGroup>();

            var background = NewImage("BG", splash, assets.GetSprite("ui_white"), theme.voidInk,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            background.rectTransform.offsetMin = Vector2.zero;
            background.rectTransform.offsetMax = Vector2.zero;

            NewImage("Glow", splash, assets.GetSprite("ui_glow"), new Color(theme.electricViolet.r, theme.electricViolet.g, theme.electricViolet.b, 0.55f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(1400f, 1400f));

            var logo = NewImage("Wordmark", splash, assets.GetSprite("ui_wordmark"), Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 160f), new Vector2(880f, 310f));

            NewImage("Hole", splash, assets.GetSprite("ui_hole_badge"), Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -140f), new Vector2(260f, 260f));

            var status = NewText("Status", splash, assets, "WAKING THE VOID", 34f, new Color(1f, 1f, 1f, 0.7f), TextAnchor.LowerCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 190f), new Vector2(800f, 50f));

            var barBack = NewImage("Progress BG", splash, assets.GetSprite("ui_bar_bg"), new Color(1f, 1f, 1f, 0.15f),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 140f), new Vector2(620f, 26f), true);
            var progress = NewImage("Progress Fill", barBack.transform, assets.GetSprite("ui_bar_fill"), theme.neonCyan,
                Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero, true);
            progress.rectTransform.offsetMin = new Vector2(3f, 3f);
            progress.rectTransform.offsetMax = new Vector2(-3f, -3f);
            progress.type = Image.Type.Filled;
            progress.fillMethod = Image.FillMethod.Horizontal;
            progress.fillAmount = 0f;

            NewText("Tagline", splash, assets, config.tagline, 40f, theme.neonCyan, TextAnchor.UpperCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(800f, 60f));

            var bootstrapper = root.AddComponent<Bootstrapper>();
            bootstrapper.Bind(config, group, logo.rectTransform, status, progress);
            var serialized = new SerializedObject(bootstrapper);
            serialized.FindProperty("m_GameSceneName").stringValue = gameSceneName;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return bootstrapper;
        }
    }
}

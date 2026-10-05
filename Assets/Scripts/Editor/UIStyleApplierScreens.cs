using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static partial class UIStyleApplier
{
    private static void StyleHud(Transform root)
    {
        Transform timer = DirectChild(root, "Timer");
        if (timer == null)
        {
            Transform timerPlate = DirectChild(root, StylePrefix + "TimerPlate");
            if (timerPlate != null) timer = DirectChild(timerPlate, "Timer");
        }
        if (timer != null) StyleTimer((RectTransform)timer);

        Transform exp = DirectChild(root, "ExpBarPanel");
        if (exp != null) StyleExpBar((RectTransform)exp);

        Transform health = DirectChild(root, "HealthBarContainer");
        if (health != null) StyleHealthBar((RectTransform)health);

        Transform currency = DirectChild(root, "CurrencyPanel");
        if (currency != null) StyleCurrency((RectTransform)currency);

        Transform minimap = DirectChild(root, "MinimapRoot");
        if (minimap != null) StyleMinimap((RectTransform)minimap);

        Transform shop = DirectChild(root, "ShopText");
        if (shop != null) StyleShopPrompt((RectTransform)shop);
    }

    private static void StyleTimer(RectTransform timer)
    {
        TMP_Text text = timer.GetComponent<TMP_Text>();
        if (text == null) return;

        bool nested = timer.parent != null && timer.parent.name == StylePrefix + "TimerPlate";
        RectTransform plate;
        if (nested)
        {
            plate = (RectTransform)timer.parent;
        }
        else
        {
            plate = GetOrCreateSibling(timer, StylePrefix + "TimerPlate");
            plate.anchorMin = plate.anchorMax = new Vector2(0.5f, 1f);
            plate.pivot = new Vector2(0.5f, 0.5f);
            plate.anchoredPosition = new Vector2(0f, timer.anchoredPosition.y + (0.5f - timer.pivot.y) * timer.rect.height);
            plate.sizeDelta = new Vector2(340f, 88f);
        }

        Image plateImage = GetOrAdd<Image>(plate.gameObject);
        plateImage.raycastTarget = false;
        style.ApplySprite(plateImage, style.plate, style.panel, style.skew, style.shadowOffset * 1.4f, null);
        plateImage.GetComponent<Shadow>().effectColor = style.primary;
        Touch(plateImage);
        Touch(plate);

        RectTransform icon = GetOrCreateChild(plate, StylePrefix + "Glyph", 0);
        icon.anchorMin = icon.anchorMax = new Vector2(0f, 0.5f);
        icon.pivot = new Vector2(0.5f, 0.5f);
        icon.anchoredPosition = new Vector2(52f, 0f);
        icon.sizeDelta = new Vector2(56f, 56f);
        Image iconImage = GetOrAdd<Image>(icon.gameObject);
        iconImage.raycastTarget = false;
        style.ApplySprite(iconImage, style.iconClock, Color.white, 0f, Vector2.zero, null);
        Touch(iconImage);

        if (!nested) timer.SetParent(plate, false);
        Stretch(timer, 0f, 0f, 0f, 0f);
        timer.SetAsLastSibling();
        Touch(timer);

        Apply(text, UITextRole.Number, style.paper, false);
        text.enableAutoSizing = false;
        text.fontSize = 56f;
        text.fontSizeMax = 56f;
        text.fontSizeMin = 28f;
        text.margin = new Vector4(62f, 0f, 0f, 0f);
        text.alignment = TextAlignmentOptions.Center;
        Finish(text);

        GameTimeUI timeUI = timer.GetComponent<GameTimeUI>();
        if (timeUI != null)
        {
            var serialized = new SerializedObject(timeUI);
            serialized.FindProperty("timeColor").colorValue = style.paper;
            serialized.FindProperty("overtimeColor").colorValue = style.yellow;
            SetReference(serialized, "plate", plateImage);
            SetColor(serialized, "overtimePlateColor", style.danger);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Touch(timeUI);
        }
    }

    private static void StyleExpBar(RectTransform panel)
    {
        Image frame = panel.GetComponent<Image>();
        panel.sizeDelta = new Vector2(panel.sizeDelta.x, 46f);
        if (frame != null)
        {
            style.ApplySprite(frame, style.plate, style.panel, style.skew, style.shadowOffset, null);
            Touch(frame);
        }
        Touch(panel);

        Transform background = DirectChild(panel, "ExpBarBackground");
        if (background != null)
        {
            Stretch((RectTransform)background, 8f, 8f, -8f, -8f);
            Image backgroundImage = background.GetComponent<Image>();
            if (backgroundImage != null)
            {
                backgroundImage.enabled = false;
                Touch(backgroundImage);
            }
            Touch(background);

            Transform fill = DirectChild(background, "ExpBarFill");
            if (fill != null)
            {
                RectTransform fillRect = (RectTransform)fill;
                fillRect.offsetMin = Vector2.zero;
                fillRect.offsetMax = Vector2.zero;
                Image fillImage = fill.GetComponent<Image>();
                if (fillImage != null)
                {
                    fillImage.raycastTarget = false;
                    style.ApplySprite(fillImage, style.fill, style.cyan, style.skew, Vector2.zero, style.plateStripesBold);
                    Touch(fillImage);
                }
                Touch(fillRect);
            }
        }

        Transform level = DirectChild(panel, "LevelText");
        if (level != null)
        {
            TMP_Text levelText = level.GetComponent<TMP_Text>();
            Apply(levelText, UITextRole.Label, style.paper, false);
            levelText.enableAutoSizing = false;
            levelText.fontSize = 26f;
            Finish(levelText);
        }
    }

    private static void StyleHealthBar(RectTransform container)
    {
        container.sizeDelta = new Vector2(container.sizeDelta.x, 38f);
        Touch(container);

        RectTransform frame = GetOrCreateChild(container, StylePrefix + "Frame", 0);
        Stretch(frame, 0f, 0f, 0f, 0f);
        Image frameImage = GetOrAdd<Image>(frame.gameObject);
        frameImage.raycastTarget = false;
        style.ApplySprite(frameImage, style.plate, style.panel, style.skew, style.shadowOffset, null);
        Touch(frameImage);

        Transform background = DirectChild(container, "HealthBarBackground");
        if (background != null)
        {
            Stretch((RectTransform)background, 8f, 8f, -8f, -8f);
            Image backgroundImage = background.GetComponent<Image>();
            if (backgroundImage != null)
            {
                backgroundImage.enabled = false;
                Touch(backgroundImage);
            }
            Touch(background);

            Transform fill = DirectChild(background, "HealthBarFill");
            if (fill != null)
            {
                RectTransform fillRect = (RectTransform)fill;
                fillRect.offsetMin = Vector2.zero;
                fillRect.offsetMax = Vector2.zero;
                Image fillImage = fill.GetComponent<Image>();
                if (fillImage != null)
                {
                    fillImage.raycastTarget = false;
                    style.ApplySprite(fillImage, style.fill, style.good, style.skew, Vector2.zero, style.plateStripesBold);
                    Touch(fillImage);
                }
                Touch(fillRect);
            }
        }

        HealthBarUI healthUI = container.GetComponent<HealthBarUI>();
        if (healthUI != null)
        {
            var serialized = new SerializedObject(healthUI);
            serialized.FindProperty("blinkColor").colorValue = style.paper;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Touch(healthUI);
        }
    }

    private static void StyleCurrency(RectTransform panel)
    {
        Image image = panel.GetComponent<Image>();
        if (image != null)
        {
            style.ApplySprite(image, style.plate, style.panel, style.skew, style.shadowOffset * 1.4f, null);
            image.GetComponent<Shadow>().effectColor = style.yellow;
            Touch(image);
        }

        RectTransform icon = GetOrCreateChild(panel, StylePrefix + "Glyph", 0);
        icon.anchorMin = icon.anchorMax = new Vector2(0f, 0.5f);
        icon.pivot = new Vector2(0.5f, 0.5f);
        icon.anchoredPosition = new Vector2(52f, 0f);
        icon.sizeDelta = new Vector2(58f, 58f);
        Image iconImage = GetOrAdd<Image>(icon.gameObject);
        iconImage.raycastTarget = false;
        style.ApplySprite(iconImage, style.iconCoin, style.yellow, 0f, Vector2.zero, null);
        Touch(iconImage);

        int counters = 0;
        foreach (Transform child in panel)
            if (child.GetComponent<TMP_Text>() != null) counters++;

        VerticalLayoutGroup rows = panel.GetComponent<VerticalLayoutGroup>();
        if (rows != null && counters > 1)
        {
            rows.spacing = 0f;
            rows.padding = new RectOffset(0, 0, 8, 8);
            rows.childAlignment = TextAnchor.MiddleCenter;
            rows.childControlWidth = true;
            rows.childControlHeight = true;
            rows.childForceExpandWidth = true;
            rows.childForceExpandHeight = true;
            Touch(rows);
        }

        foreach (Transform child in panel)
        {
            TMP_Text text = child.GetComponent<TMP_Text>();
            if (text == null) continue;

            Apply(text, UITextRole.Number, child.name.Contains("Diamond") ? style.cyan : style.yellow, true);
            text.fontSizeMax = counters > 1 ? 24f : 40f;
            text.fontSizeMin = counters > 1 ? 14f : 18f;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.margin = new Vector4(70f, 0f, 8f, 0f);
            text.alignment = TextAlignmentOptions.Center;
            Finish(text);
        }
    }

    private const float MinimapCircleRatio = 0.783f;
    private const float RingCircleRatio = 440f / 512f;

    private static void StyleMinimap(RectTransform minimap)
    {
        float visible = minimap.rect.width * MinimapCircleRatio + 10f;
        RectTransform ring = GetOrCreateChild(minimap, StylePrefix + "Ring", -1);
        ring.anchorMin = ring.anchorMax = new Vector2(0.5f, 0.5f);
        ring.pivot = new Vector2(0.5f, 0.5f);
        ring.anchoredPosition = Vector2.zero;
        ring.sizeDelta = Vector2.one * (visible / RingCircleRatio);
        Image ringImage = GetOrAdd<Image>(ring.gameObject);
        ringImage.raycastTarget = false;
        ringImage.maskable = false;
        style.ApplySprite(ringImage, style.ring, Color.white, 0f, Vector2.zero, null);
        Touch(ringImage);
        Touch(ring);
    }

    private static void StyleShopPrompt(RectTransform prompt)
    {
        prompt.sizeDelta = new Vector2(900f, 84f);
        Touch(prompt);

        Image image = prompt.GetComponent<Image>();
        if (image != null)
        {
            style.ApplySprite(image, style.plate, style.panel, style.skew, style.shadowOffset * 1.4f, null);
            image.GetComponent<Shadow>().effectColor = style.cyan;
            Touch(image);
        }

        TMP_Text text = DirectChildText(prompt);
        if (text != null)
        {
            Apply(text, UITextRole.Label, style.paper, true);
            text.fontSizeMax = 30f;
            text.fontSizeMin = 16f;
            text.margin = new Vector4(34f, 0f, 34f, 0f);
            Finish(text);
        }
    }

    private static void StyleLevelUp(Transform root)
    {
        Transform panel = DirectChild(root, "LevelUpPanel");
        if (panel == null) return;

        Transform titleTransform = DirectChild(panel, "LevelUpText");
        if (titleTransform != null)
        {
            RectTransform titleRect = (RectTransform)titleTransform;
            TMP_Text title = titleTransform.GetComponent<TMP_Text>();

            RectTransform burst = GetOrCreateSibling(titleRect, StylePrefix + "TitleBurst");
            burst.anchorMin = titleRect.anchorMin;
            burst.anchorMax = titleRect.anchorMax;
            burst.pivot = titleRect.pivot;
            burst.anchoredPosition = titleRect.anchoredPosition;
            burst.sizeDelta = new Vector2(900f, 270f);
            Image burstImage = GetOrAdd<Image>(burst.gameObject);
            burstImage.raycastTarget = false;
            style.ApplySprite(burstImage, style.burst, style.primary, 0f, style.panelShadowOffset, null);
            Touch(burstImage);
            Touch(burst);

            titleRect.sizeDelta = new Vector2(640f, 110f);
            Apply(title, UITextRole.Title, style.paper, true);
            Touch(titleRect);

            LevelUpManager manager = root.GetComponent<LevelUpManager>();
            if (manager != null)
            {
                var serialized = new SerializedObject(manager);
                SetReference(serialized, "titleBackdrop", burstImage);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Touch(manager);
            }
        }

        StyleNamedText(panel, "CooldownWarningText", UITextRole.Heading, style.yellow, true);
        StyleNamedText(panel, "Shop Instructions", UITextRole.Body, style.textDim, false);

        foreach (UpgradeButton card in panel.GetComponentsInChildren<UpgradeButton>(true))
        {
            Transform cardTransform = card.transform;
            StyleNamedText(cardTransform, "UpgradeNameText", UITextRole.Heading, style.paper, true, 20f);
            StyleNamedText(cardTransform, "DescriptionText", UITextRole.Body, style.textDim, false, 22f);
            StyleNamedText(cardTransform, "LabelText", UITextRole.Number, style.yellow, true, 20f);
            StyleNamedText(cardTransform, "ValuesText", UITextRole.Number, style.good, true, 22f);
            StyleNamedText(cardTransform, "CostText", UITextRole.Number, style.yellow, true);
        }
    }

    private static void StyleNamedText(Transform parent, string name, UITextRole role, Color color, bool autoSize, float sideMargin = 0f)
    {
        Transform child = DirectChild(parent, name);
        if (child == null) return;
        TMP_Text text = child.GetComponent<TMP_Text>();
        if (text == null) return;
        Apply(text, role, color, autoSize);
        if (sideMargin > 0f)
        {
            text.margin = new Vector4(sideMargin, text.margin.y, sideMargin, text.margin.w);
            Finish(text);
        }
    }

    private static void StyleTutorial(Transform root)
    {
        Transform panel = FindDeep(root, "TutorialPanel");
        if (panel == null) return;

        Transform message = DirectChild(panel, "MessageText");
        if (message != null)
        {
            TMP_Text text = message.GetComponent<TMP_Text>();
            if (text != null)
            {
                Apply(text, UITextRole.BodyDark, style.ink, false);
                text.fontSize = 26f;
                Finish(text);
            }
        }
    }

    private static void StyleMainMenu(Transform root)
    {
        Transform main = DirectChild(root, "MainMenuPanel");
        if (main == null) return;

        Transform title = DirectChild(main, "TitlePanel");
        if (title == null) return;

        RectTransform bolt = GetOrCreateChild(title, StylePrefix + "Bolt", 0);
        bolt.anchorMin = bolt.anchorMax = new Vector2(0f, 0.5f);
        bolt.pivot = new Vector2(0.5f, 0.5f);
        bolt.anchoredPosition = new Vector2(-18f, 34f);
        bolt.sizeDelta = new Vector2(150f, 250f);
        bolt.localRotation = Quaternion.Euler(0f, 0f, 14f);
        Image boltImage = GetOrAdd<Image>(bolt.gameObject);
        boltImage.raycastTarget = false;
        style.ApplySprite(boltImage, style.bolt, style.yellow, 0f, style.shadowOffset * 1.4f, null);
        Touch(boltImage);
        Touch(bolt);

        RectTransform spark = GetOrCreateChild(title, StylePrefix + "Spark", -1);
        spark.anchorMin = spark.anchorMax = new Vector2(1f, 1f);
        spark.pivot = new Vector2(0.5f, 0.5f);
        spark.anchoredPosition = new Vector2(-26f, -36f);
        spark.sizeDelta = new Vector2(92f, 92f);
        spark.localRotation = Quaternion.Euler(0f, 0f, 12f);
        Image sparkImage = GetOrAdd<Image>(spark.gameObject);
        sparkImage.raycastTarget = false;
        style.ApplySprite(sparkImage, style.spark, style.cyan, 0f, Vector2.zero, null);
        Touch(sparkImage);
        Touch(spark);
    }

    private static void StyleOverridePanels(Transform root)
    {
    }

    private static void StyleInitials(Transform root)
    {
        Transform cursor = FindDeep(root, "Cursor");
        if (cursor == null || root.GetComponentInChildren<InitialsEntryUI>(true) == null) return;

        Image image = cursor.GetComponent<Image>();
        if (image != null)
        {
            image.color = style.primary;
            Touch(image);
        }
    }

    private static void StyleSceneComponents(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (FloatingTextManager manager in root.GetComponentsInChildren<FloatingTextManager>(true))
            {
                var serialized = new SerializedObject(manager);
                serialized.FindProperty("damageColor").colorValue = style.paper;
                serialized.FindProperty("expColor").colorValue = style.cyan;
                serialized.FindProperty("coinColor").colorValue = style.yellow;
                serialized.FindProperty("diamondColor").colorValue = style.primary;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Touch(manager);
            }

            foreach (TutorialManager tutorial in root.GetComponentsInChildren<TutorialManager>(true))
            {
                var serialized = new SerializedObject(tutorial);
                SerializedProperty target = serialized.FindProperty("targetTimer");
                RectTransform current = target != null ? target.objectReferenceValue as RectTransform : null;
                if (current != null && current.parent != null && current.parent.name == StylePrefix + "TimerPlate")
                {
                    target.objectReferenceValue = current.parent;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    Touch(tutorial);
                }
            }

            foreach (GlitchTextUI glitch in root.GetComponentsInChildren<GlitchTextUI>(true))
            {
                var serialized = new SerializedObject(glitch);
                serialized.FindProperty("flickerColorA").colorValue = style.cyan;
                serialized.FindProperty("flickerColorB").colorValue = style.primary;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Touch(glitch);
            }
        }
    }

    private static void SetReference(SerializedObject serialized, string property, UnityEngine.Object value)
    {
        SerializedProperty found = serialized.FindProperty(property);
        if (found != null) found.objectReferenceValue = value;
    }

    private static void SetColor(SerializedObject serialized, string property, Color value)
    {
        SerializedProperty found = serialized.FindProperty(property);
        if (found != null) found.colorValue = value;
    }

    public static bool Validate(UIStyle targetStyle, StringBuilder targetReport)
    {
        style = targetStyle;
        report = targetReport;
        bool passed = true;

        foreach (string path in PrefabPaths)
        {
            if (!File.Exists(path)) continue;
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                passed &= ValidateRoots(path, new[] { root.transform }, false);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        foreach (string path in ScenePaths)
        {
            if (!File.Exists(path)) continue;
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            Canvas.ForceUpdateCanvases();
            var roots = new List<Transform>();
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
                    if (canvas.isRootCanvas && canvas.renderMode != RenderMode.WorldSpace) roots.Add(canvas.transform);
            passed &= ValidateRoots(path, roots, true);
        }

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        return passed;
    }

    private static bool ValidateRoots(string path, IEnumerable<Transform> roots, bool checkOverflow)
    {
        int texts = 0;
        int legacyFonts = 0;
        int legacySprites = 0;
        var overflowing = new List<string>();
        var offenders = new List<string>();

        foreach (Transform root in roots)
        {
            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                texts++;
                if (text.font != style.font)
                {
                    legacyFonts++;
                    if (offenders.Count < 8) offenders.Add(HierarchyPath(text.transform));
                }

                if (!checkOverflow || string.IsNullOrEmpty(text.text)) continue;
                text.ForceMeshUpdate(true, true);
                if (text.isTextOverflowing && overflowing.Count < 40)
                    overflowing.Add($"{HierarchyPath(text.transform)} (\"{Shorten(text.text)}\")");
            }

            foreach (Image image in root.GetComponentsInChildren<Image>(true))
            {
                string key = SpriteKey(image);
                if (Array.IndexOf(LegacyPanelSprites, key) < 0 && Array.IndexOf(LegacyButtonSprites, key) < 0 && Array.IndexOf(LegacyLogoSprites, key) < 0) continue;
                legacySprites++;
                if (offenders.Count < 8) offenders.Add(HierarchyPath(image.transform));
            }
        }

        report.AppendLine($"VALIDACIÓN {path}: {texts} textos, {legacyFonts} con fuente antigua, {legacySprites} sprites antiguos, {overflowing.Count} textos que no caben.");
        foreach (string offender in offenders) report.AppendLine("  ANTIGUO: " + offender);
        foreach (string entry in overflowing) report.AppendLine("  NO CABE: " + entry);
        return legacyFonts == 0 && legacySprites == 0;
    }

    private static string Shorten(string value)
    {
        value = value.Replace("\n", " ");
        return value.Length <= 32 ? value : value.Substring(0, 32) + "…";
    }

    private static string HierarchyPath(Transform transform)
    {
        var parts = new List<string>();
        Transform current = transform;
        while (current != null)
        {
            parts.Add(current.name);
            current = current.parent;
        }
        parts.Reverse();
        return string.Join("/", parts);
    }
}

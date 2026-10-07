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
    private static readonly string[] ObsoleteObjects =
    {
        "StyleBolt", "StyleSpark", "StyleBand", "StyleMinimapShadow", "StyleEye", "StylePaper",
        "StyleDepthFar", "StyleDepthMid", "StyleDepthNear", "StyleTitleDepth",
    };
    private const float ReferenceHeight = 1080f;
    private const float OverrideHudScale = 1.1f;
    private const float OverrideHudTop = 171f;
    private const float OverrideHudLeft = 280f;
    private static readonly string[] OverlayPanels = { "LevelUpPanel", "PausePanel", "GameOverPanel", "AudioPanel", "InitialsEntryUI" };

    private static Color Lilac => Color.Lerp(style.anomaly, style.paper, 0.5f);

    private static void Cleanup(Transform root)
    {
        var doomed = new List<GameObject>();
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (Array.IndexOf(ObsoleteObjects, child.name) >= 0) doomed.Add(child.gameObject);
        foreach (GameObject go in doomed) UnityEngine.Object.DestroyImmediate(go);

        foreach (Image image in root.GetComponentsInChildren<Image>(true))
        {
            if (PrefabUtility.IsPartOfPrefabInstance(image)) continue;
            Shadow[] shadows = image.GetComponents<Shadow>();
            for (int i = 1; i < shadows.Length; i++) UnityEngine.Object.DestroyImmediate(shadows[i]);
        }
    }

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

        Transform levelUp = DirectChild(root, "LevelUpPanel");
        if (currency != null && levelUp != null && currency.GetSiblingIndex() < levelUp.GetSiblingIndex())
        {
            currency.SetSiblingIndex(levelUp.GetSiblingIndex());
            Touch(currency);
        }

        if (timer != null)
        {
            foreach (string overlay in OverlayPanels)
            {
                Transform panel = DirectChild(root, overlay);
                if (panel != null && !PrefabUtility.IsPartOfPrefabInstance(panel.gameObject)) Touch(GetOrAdd<UIOverlay>(panel.gameObject));
            }
        }
    }

    private static void HudPlate(Image image, Color lip)
    {
        style.ApplySprite(image, style.plate, style.panel, style.skew, style.shadowOffset, null);
        UIStyle.SetLip(image, lip);
        Touch(image);
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
        HudPlate(plateImage, style.secondary);
        Touch(GetOrAdd<CanvasGroup>(plate.gameObject));
        Touch(GetOrAdd<UIHideUnderOverlay>(plate.gameObject));
        Touch(plate);

        RectTransform icon = GetOrCreateChild(plate, StylePrefix + "Glyph", 0);
        icon.anchorMin = icon.anchorMax = new Vector2(0f, 0.5f);
        icon.pivot = new Vector2(0.5f, 0.5f);
        icon.anchoredPosition = new Vector2(52f, 0f);
        icon.sizeDelta = new Vector2(52f, 52f);
        Image iconImage = GetOrAdd<Image>(icon.gameObject);
        iconImage.raycastTarget = false;
        style.ApplySprite(iconImage, style.iconClock, style.cyan, 0f, Vector2.zero, null);
        Touch(iconImage);

        if (!nested) timer.SetParent(plate, false);
        Stretch(timer, 0f, 0f, 0f, 0f);
        timer.SetAsLastSibling();
        Touch(timer);

        Apply(text, UITextRole.Number, style.paper, false);
        text.enableAutoSizing = false;
        text.fontSize = 52f;
        text.fontSizeMax = 52f;
        text.fontSizeMin = 26f;
        text.margin = new Vector4(70f, 0f, 24f, 0f);
        text.alignment = TextAlignmentOptions.Center;
        Finish(text);

        GameTimeUI timeUI = timer.GetComponent<GameTimeUI>();
        if (timeUI != null)
        {
            var serialized = new SerializedObject(timeUI);
            serialized.FindProperty("timeColor").colorValue = style.paper;
            serialized.FindProperty("overtimeColor").colorValue = style.paper;
            SetReference(serialized, "plate", plateImage);
            SetColor(serialized, "overtimePlateColor", style.danger);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Touch(timeUI);
        }
    }

    private static void BarFrame(Image frame)
    {
        style.ApplySprite(frame, style.capsule, style.panel, 0f, style.shadowOffset * 0.67f, null);
        UIStyle.SetLip(frame, style.secondary);
        Touch(frame);
    }

    private static void BarFill(Transform background, string fillName, Color color)
    {
        Stretch((RectTransform)background, 8f, 8f, -8f, -8f);
        Image backgroundImage = background.GetComponent<Image>();
        if (backgroundImage != null)
        {
            backgroundImage.enabled = false;
            Touch(backgroundImage);
        }
        Touch(background);

        Transform fill = DirectChild(background, fillName);
        if (fill == null) return;

        RectTransform fillRect = (RectTransform)fill;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        Image fillImage = fill.GetComponent<Image>();
        if (fillImage != null)
        {
            fillImage.raycastTarget = false;
            style.ApplySprite(fillImage, style.fill, color, 0f, Vector2.zero, style.plateStripesBold);
            Touch(fillImage);
        }
        Touch(fillRect);
    }

    private static void StyleExpBar(RectTransform panel)
    {
        Image frame = panel.GetComponent<Image>();
        panel.sizeDelta = new Vector2(panel.sizeDelta.x, 46f);
        if (frame != null) BarFrame(frame);
        Touch(panel);

        Transform background = DirectChild(panel, "ExpBarBackground");
        if (background != null) BarFill(background, "ExpBarFill", style.cyan);

        Transform level = DirectChild(panel, "LevelText");
        if (level != null)
        {
            TMP_Text levelText = level.GetComponent<TMP_Text>();
            Apply(levelText, UITextRole.Label, style.paper, false);
            levelText.enableAutoSizing = false;
            levelText.fontSize = 24f;
            levelText.margin = Vector4.zero;
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
        BarFrame(frameImage);

        Transform background = DirectChild(container, "HealthBarBackground");
        if (background != null) BarFill(background, "HealthBarFill", style.good);

        MannersFaceUI face = BuildFace(container, new Vector2(0f, 0.5f), new Vector2(-30f, 0f), 54f, false);
        face.transform.SetAsLastSibling();

        HealthBarUI healthUI = container.GetComponent<HealthBarUI>();
        if (healthUI != null)
        {
            var serialized = new SerializedObject(healthUI);
            serialized.FindProperty("blinkColor").colorValue = style.paper;
            SetBool(serialized, "useStyleColors", true);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Touch(healthUI);
        }
    }

    private static MannersFaceUI BuildFace(Transform parent, Vector2 anchor, Vector2 position, float size, bool menuMode)
    {
        RectTransform face = GetOrCreateChild(parent, StylePrefix + "Face", -1);
        face.anchorMin = face.anchorMax = anchor;
        face.pivot = new Vector2(0.5f, 0.5f);
        face.anchoredPosition = position;
        face.sizeDelta = new Vector2(size, size);
        Image screen = GetOrAdd<Image>(face.gameObject);
        screen.raycastTarget = false;
        style.ApplySprite(screen, style.faceScreen, Color.white, 0f, Vector2.zero, null);
        Touch(screen);
        Touch(face);

        RectTransform eyes = GetOrCreateChild(face, StylePrefix + "Eyes", -1);
        Stretch(eyes, 0f, 0f, 0f, 0f);
        Image eyesImage = GetOrAdd<Image>(eyes.gameObject);
        eyesImage.raycastTarget = false;
        style.ApplySprite(eyesImage, style.faceCalm, style.cyan, 0f, Vector2.zero, null);
        Touch(eyesImage);

        MannersFaceUI component = GetOrAdd<MannersFaceUI>(face.gameObject);
        var serialized = new SerializedObject(component);
        SetReference(serialized, "eyes", eyesImage);
        SetBool(serialized, "reactToGame", !menuMode);
        SetBool(serialized, "maskSlips", menuMode);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        Touch(component);
        return component;
    }

    private static void StyleCurrency(RectTransform panel)
    {
        Image image = panel.GetComponent<Image>();
        if (image != null) HudPlate(image, style.secondary);

        RectTransform icon = GetOrCreateChild(panel, StylePrefix + "Glyph", 0);
        icon.anchorMin = icon.anchorMax = new Vector2(0f, 0.5f);
        icon.pivot = new Vector2(0.5f, 0.5f);
        icon.anchoredPosition = new Vector2(50f, 0f);
        icon.sizeDelta = new Vector2(54f, 54f);
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

            if (counters == 1)
            {
                Stretch(text.rectTransform, 0f, 0f, 0f, 0f);
                Touch(text.rectTransform);
            }

            Apply(text, UITextRole.Number, child.name.Contains("Diamond") ? Lilac : style.yellow, true);
            text.fontSizeMax = counters > 1 ? 24f : 38f;
            text.fontSizeMin = counters > 1 ? 14f : 18f;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.margin = new Vector4(78f, 0f, 30f, 0f);
            text.alignment = TextAlignmentOptions.Center;
            Finish(text);
        }
    }

    private const float MinimapCircleRatio = 0.783f;
    private const float RingCircleRatio = 220f / 256f;

    private static void StyleMinimap(RectTransform minimap)
    {
        float visible = minimap.rect.width * MinimapCircleRatio + 10f;
        RectTransform ring = GetOrCreateChild(minimap, StylePrefix + "Ring", -1);
        ring.anchorMin = ring.anchorMax = new Vector2(0.5f, 0.5f);
        ring.pivot = new Vector2(0.5f, 0.5f);
        ring.anchoredPosition = Vector2.zero;
        ring.sizeDelta = Vector2.one * Snap(visible / RingCircleRatio);
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
        if (image != null) HudPlate(image, style.danger);

        TMP_Text text = DirectChildText(prompt);
        if (text != null)
        {
            Apply(text, UITextRole.Label, style.paper, true);
            text.fontSizeMax = 28f;
            text.fontSizeMin = 16f;
            text.margin = new Vector4(48f, 0f, 48f, 0f);
            Finish(text);
        }
    }

    private static void StyleLevelUp(Transform root)
    {
        Transform panel = DirectChild(root, "LevelUpPanel");
        if (panel == null) return;

        Image banner = null;
        Image promptPlate = null;

        Transform titleTransform = DirectChild(panel, "LevelUpText");
        if (titleTransform != null)
        {
            RectTransform titleRect = (RectTransform)titleTransform;
            TMP_Text title = titleTransform.GetComponent<TMP_Text>();

            RectTransform burst = GetOrCreateSibling(titleRect, StylePrefix + "TitleBurst");
            burst.anchorMin = burst.anchorMax = new Vector2(0.5f, 1f);
            burst.pivot = new Vector2(0.5f, 0.5f);
            burst.anchoredPosition = new Vector2(0f, -118f);
            burst.sizeDelta = new Vector2(480f, 112f);
            banner = GetOrAdd<Image>(burst.gameObject);
            banner.raycastTarget = false;
            style.ApplySprite(banner, style.burst, style.primary, 0f, style.panelShadowOffset, null);
            Touch(banner);
            Touch(burst);

            titleRect.anchorMin = titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = new Vector2(0f, -118f);
            titleRect.sizeDelta = new Vector2(350f, 74f);
            title.margin = Vector4.zero;
            Apply(title, UITextRole.Title, style.paper, true);
            title.fontSizeMax = 58f;
            title.fontSizeMin = 28f;
            Finish(title);
            Touch(titleRect);
        }

        Transform promptTransform = DirectChild(panel, "Shop Instructions");
        if (promptTransform != null)
        {
            RectTransform promptRect = (RectTransform)promptTransform;
            if (promptRect.anchorMin.y < 0.99f)
            {
                float bottom = promptRect.anchoredPosition.y - promptRect.pivot.y * promptRect.rect.height;
                float fromTop = ReferenceHeight - (bottom + promptRect.rect.height * 0.5f);
                promptRect.anchorMin = promptRect.anchorMax = new Vector2(0.5f, 1f);
                promptRect.pivot = new Vector2(0.5f, 0.5f);
                promptRect.anchoredPosition = new Vector2(0f, -Snap(fromTop));
            }
            promptRect.sizeDelta = new Vector2(1000f, 64f);
            Touch(promptRect);

            RectTransform plate = GetOrCreateSibling(promptRect, StylePrefix + "PromptPlate");
            plate.anchorMin = plate.anchorMax = promptRect.anchorMin;
            plate.pivot = promptRect.pivot;
            plate.anchoredPosition = promptRect.anchoredPosition;
            plate.sizeDelta = new Vector2(1040f, 64f);
            promptPlate = GetOrAdd<Image>(plate.gameObject);
            promptPlate.raycastTarget = false;
            style.ApplySprite(promptPlate, style.capsule, style.panel, 0f, style.shadowOffset, null);
            UIStyle.SetLip(promptPlate, style.danger);
            Touch(promptPlate);
            Touch(plate);

            RectTransform glyph = GetOrCreateChild(plate, StylePrefix + "Glyph", 0);
            glyph.anchorMin = glyph.anchorMax = new Vector2(0f, 0.5f);
            glyph.pivot = new Vector2(0.5f, 0.5f);
            glyph.anchoredPosition = new Vector2(50f, 0f);
            glyph.sizeDelta = new Vector2(40f, 40f);
            Image glyphImage = GetOrAdd<Image>(glyph.gameObject);
            glyphImage.raycastTarget = false;
            style.ApplySprite(glyphImage, style.triangle, style.alert, 0f, Vector2.zero, null);
            Touch(glyphImage);

            TMP_Text prompt = promptTransform.GetComponent<TMP_Text>();
            Apply(prompt, UITextRole.Label, style.paper, false);
            prompt.enableAutoSizing = true;
            prompt.fontSizeMax = 26f;
            prompt.fontSizeMin = 16f;
            prompt.textWrappingMode = TextWrappingModes.Normal;
            prompt.overflowMode = TextOverflowModes.Truncate;
            prompt.alignment = TextAlignmentOptions.Center;
            prompt.margin = new Vector4(84f, 4f, 44f, 4f);
            Finish(prompt);
        }

        Transform cooldownTransform = DirectChild(panel, "CooldownWarningText");
        if (cooldownTransform != null)
        {
            RectTransform cooldown = (RectTransform)cooldownTransform;
            cooldown.anchorMin = cooldown.anchorMax = new Vector2(0.5f, 0f);
            cooldown.pivot = new Vector2(0.5f, 0.5f);
            cooldown.anchoredPosition = new Vector2(0f, 192f);
            cooldown.sizeDelta = new Vector2(900f, 60f);
            Touch(cooldown);
            Apply(cooldownTransform.GetComponent<TMP_Text>(), UITextRole.Heading, style.yellow, true);
        }

        LevelUpManager manager = root.GetComponent<LevelUpManager>();
        if (manager != null)
        {
            var serialized = new SerializedObject(manager);
            SetReference(serialized, "titleBackdrop", banner);
            SetReference(serialized, "closeInstructionPlate", promptPlate != null ? promptPlate.gameObject : null);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Touch(manager);
        }

        foreach (UpgradeButton card in panel.GetComponentsInChildren<UpgradeButton>(true))
        {
            Transform cardTransform = card.transform;
            StyleNamedText(cardTransform, "UpgradeNameText", UITextRole.Heading, style.paper, true, 22f);
            Transform cardName = DirectChild(cardTransform, "UpgradeNameText");
            if (cardName != null)
            {
                TMP_Text nameText = cardName.GetComponent<TMP_Text>();
                nameText.textWrappingMode = TextWrappingModes.Normal;
                nameText.fontSizeMax = 26f;
                nameText.fontSizeMin = 17f;
                nameText.lineSpacing = -22f;
                Finish(nameText);
            }
            StyleNamedText(cardTransform, "DescriptionText", UITextRole.Body, style.textDim, false, 24f);
            StyleNamedText(cardTransform, "LabelText", UITextRole.Number, style.yellow, true, 22f);
            StyleNamedText(cardTransform, "CostText", UITextRole.Number, style.yellow, true);

            Transform values = DirectChild(cardTransform, "ValuesText");
            if (values != null)
            {
                TMP_Text valuesText = values.GetComponent<TMP_Text>();
                Apply(valuesText, UITextRole.Number, style.good, true);
                valuesText.fontSizeMax = 30f;
                valuesText.fontSizeMin = 15f;
                valuesText.textWrappingMode = TextWrappingModes.NoWrap;
                valuesText.lineSpacing = -18f;
                valuesText.margin = new Vector4(22f, 0f, 22f, 0f);
                Finish(valuesText);
            }
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

        foreach (Button button in panel.GetComponentsInChildren<Button>(true))
        {
            TMP_Text label = DirectChildText(button.transform);
            if (label == null) continue;

            label.textWrappingMode = TextWrappingModes.Normal;
            label.fontSizeMax = Mathf.Min(label.fontSizeMax, 30f);
            label.fontSizeMin = 16f;
            label.lineSpacing = -20f;
            Finish(label);
        }

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

        Image backdrop = panel.GetComponent<Image>();
        if (backdrop != null)
        {
            bool blocks = backdrop.raycastTarget;
            HideGraphic(backdrop);
            backdrop.raycastTarget = blocks;
            Touch(backdrop);
        }

        Vector2 center = new Vector2(0.5f, 0.5f);
        Vector2 size = ((RectTransform)panel).sizeDelta;
        float w = size.x;
        float h = size.y;

        RectTransform echo = GetOrCreateChild(panel, StylePrefix + "ShardEcho", 0);
        Shard(echo, center, Vector2.zero, size, style.cyan, 3f, 1f, 4.4f, 0.12f, new[]
        {
            new Vector2(-50f, -24f),
            new Vector2(w + 62f, -28f),
            new Vector2(w + 50f, h + 12f),
            new Vector2(-42f, h + 36f),
        });

        RectTransform accent = GetOrCreateChild(panel, StylePrefix + "ShardAccent", 1);
        Shard(accent, center, Vector2.zero, size, style.cyan, 0f, 1f, 0f, 0.07f, new[]
        {
            new Vector2(-30f, -6f),
            new Vector2(w + 36f, -44f),
            new Vector2(w + 28f, h * 0.52f),
            new Vector2(w + 112f, h * 0.74f),
            new Vector2(w + 20f, h * 0.8f),
            new Vector2(w + 16f, h - 10f),
            new Vector2(-32f, h + 14f),
            new Vector2(-30f, h * 0.36f),
            new Vector2(-118f, -10f),
            new Vector2(-22f, 4f),
        });

        RectTransform ink = GetOrCreateChild(panel, StylePrefix + "ShardInk", 2);
        Shard(ink, center, Vector2.zero, size, style.ink, 0f, 0.45f, 2.1f, 0.03f, new[]
        {
            new Vector2(2f, -12f),
            new Vector2(w + 16f, -20f),
            new Vector2(w + 12f, h * 0.6f),
            new Vector2(w + 70f, h * 0.8f),
            new Vector2(w + 8f, h * 0.86f),
            new Vector2(w + 6f, h + 8f),
            new Vector2(-10f, h + 2f),
            new Vector2(-8f, h * 0.325f),
            new Vector2(-98f, 4f),
            new Vector2(0f, 20f),
        });

        RectTransform paper = GetOrCreateChild(panel, StylePrefix + "ShardPaper", 3);
        Shard(paper, center, Vector2.zero, size, style.cream, 0f, 0f, 0f, 0f, new[]
        {
            new Vector2(10f, 6f),
            new Vector2(w, 0f),
            new Vector2(w - 6f, h),
            new Vector2(0f, h - 12f),
            new Vector2(3f, h * 0.28f),
            new Vector2(-62f, 22f),
            new Vector2(8f, 40f),
        });
    }

    private static void StyleSilhouette(Image image)
    {
        if (image == null) return;

        UISilhouette silhouette = GetOrAdd<UISilhouette>(image.gameObject);
        var serialized = new SerializedObject(silhouette);
        serialized.FindProperty("color").colorValue = style.ink;
        serialized.FindProperty("outline").floatValue = style.silhouetteOutline;
        serialized.FindProperty("offset").vector2Value = style.silhouetteOffset;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        Touch(silhouette);
    }

    private static void StyleMainMenu(Transform root)
    {
        Transform main = DirectChild(root, "MainMenuPanel");
        if (main == null) return;

        Transform title = UIEditorHierarchy.Find(main, "TitlePanel");
        if (title == null) return;

        Transform ribbon = DirectChild(title, StylePrefix + "Ribbon");
        float y = ribbon != null ? ((RectTransform)ribbon).anchoredPosition.y : 0f;
        MannersFaceUI face = BuildFace(title, new Vector2(0f, 0.5f), new Vector2(34f, y), 168f, true);
        face.transform.SetAsLastSibling();

        Transform eye = ribbon != null ? DirectChild(ribbon, StylePrefix + "Eye") : null;
        if (eye != null)
        {
            eye.gameObject.SetActive(false);
            Touch(eye.gameObject);
        }
    }

    private static void StyleOverridePanels(Transform root)
    {
        foreach (OverrideHudPanel hud in root.GetComponentsInChildren<OverrideHudPanel>(true))
        {
            RectTransform panel = (RectTransform)hud.transform;
            Image body = panel.GetComponent<Image>();
            if (body != null)
            {
                style.ApplySprite(body, style.plate, style.panel, style.skewSoft, style.shadowOffset, null);
                UIStyle.SetLip(body, style.anomaly);
                Touch(body);
            }

            RectTransform tab = GetOrCreateChild(panel, StylePrefix + "Tab", 0);
            tab.anchorMin = tab.anchorMax = new Vector2(0f, 1f);
            tab.pivot = new Vector2(0f, 0.5f);
            tab.anchoredPosition = new Vector2(-10f, -4f);
            tab.sizeDelta = new Vector2(156f, 30f);
            Image tabImage = GetOrAdd<Image>(tab.gameObject);
            tabImage.raycastTarget = false;
            style.ApplySprite(tabImage, style.plate, style.anomaly, style.skew, Vector2.zero, style.plateStripes);
            Touch(tabImage);
            Touch(tab);

            Transform title = DirectChild(panel, "Title");
            if (title != null)
            {
                RectTransform titleRect = (RectTransform)title;
                titleRect.anchorMin = titleRect.anchorMax = tab.anchorMin;
                titleRect.pivot = tab.pivot;
                titleRect.anchoredPosition = tab.anchoredPosition;
                titleRect.sizeDelta = tab.sizeDelta;
                Touch(titleRect);

                TMP_Text titleText = title.GetComponent<TMP_Text>();
                Apply(titleText, UITextRole.Label, style.paper, true);
                titleText.fontSizeMax = 15f;
                titleText.fontSizeMin = 9f;
                titleText.margin = new Vector4(12f, 0f, 12f, 0f);
                titleText.alignment = TextAlignmentOptions.Center;
                Finish(titleText);
            }

            foreach (TMP_Text sign in panel.GetComponentsInChildren<TMP_Text>(true))
            {
                if (!sign.name.EndsWith("_Txt")) continue;

                sign.enableAutoSizing = false;
                sign.fontSize = 20f;
                sign.textWrappingMode = TextWrappingModes.NoWrap;
                sign.overflowMode = TextOverflowModes.Overflow;
                Finish(sign);
            }

            if (!PrefabUtility.IsPartOfPrefabInstance(panel.gameObject)) continue;
            if (panel.anchorMin.y < 0.99f || panel.pivot.y < 0.99f || panel.anchorMin.x < 0.99f || panel.pivot.x > 0.01f) continue;

            panel.localScale = Vector3.one * OverrideHudScale;
            panel.anchoredPosition = new Vector2(-OverrideHudLeft, -OverrideHudTop);
            Touch(panel);
        }
    }

    private static void StyleInitials(Transform root)
    {
        InitialsEntryUI initials = root.GetComponentInChildren<InitialsEntryUI>(true);
        if (initials == null) return;
        if (!PrefabUtility.IsPartOfPrefabInstance(initials.gameObject)) Touch(GetOrAdd<UIOverlay>(initials.gameObject));

        Transform cursor = FindDeep(root, "Cursor");
        if (cursor == null) return;

        Image image = cursor.GetComponent<Image>();
        if (image != null)
        {
            image.color = style.cyan;
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
                serialized.FindProperty("diamondColor").colorValue = Lilac;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Touch(manager);
            }

            foreach (TutorialManager tutorial in root.GetComponentsInChildren<TutorialManager>(true))
            {
                var serialized = new SerializedObject(tutorial);
                StyleSilhouette(serialized.FindProperty("robotImage").objectReferenceValue as Image);
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
                serialized.FindProperty("flickerColorB").colorValue = style.alert;
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

    private static void SetBool(SerializedObject serialized, string property, bool value)
    {
        SerializedProperty found = serialized.FindProperty(property);
        if (found != null) found.boolValue = value;
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

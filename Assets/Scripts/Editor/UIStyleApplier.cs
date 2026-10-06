using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static partial class UIStyleApplier
{
    public static readonly string[] ScenePaths =
    {
        "Assets/Scenes/MainMenu.unity",
        "Assets/Scenes/Final Levels/LEVEL 1/LEVEL 1.unity",
        "Assets/Scenes/Sandbox.unity",
    };

    public static readonly string[] PrefabPaths =
    {
        "Assets/Prefabs/UI/OverrideHudPanel.prefab",
        "Assets/Prefabs/UI/OverrideHintsPanel.prefab",
        "Assets/Prefabs/UI/InitialsEntryUI.prefab",
        "Assets/Prefabs/UI/FloatingText.prefab",
    };

    private const string StylePrefix = "Style";
    private const float CardSlotInset = 24f;
    private const float LargePanelMinSize = 260f;

    private static readonly string[] LegacyPanelSprites = { "FrameMap", "Interface windows", "PanelParaBotones" };
    private static readonly string[] LegacyButtonSprites = { "ButtonMap9" };
    private static readonly string[] LegacyLogoSprites = { "Logo1", "Logo2", "Logo3" };

    private static UIStyle style;
    private static StringBuilder report;
    private static int imageCount;
    private static int textCount;
    private static int buttonCount;
    private static int createdCount;

    public static void ApplyAll(UIStyle targetStyle, StringBuilder targetReport)
    {
        style = targetStyle;
        report = targetReport;

        foreach (string path in PrefabPaths)
        {
            if (!File.Exists(path)) continue;
            ResetCounters();
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                StyleRoot(root.transform);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            report.AppendLine($"ESTILO {path}: {imageCount} imágenes, {textCount} textos, {buttonCount} botones, {createdCount} objetos nuevos.");
        }

        AssetDatabase.SaveAssets();

        foreach (string path in ScenePaths)
        {
            if (!File.Exists(path)) continue;
            ResetCounters();
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            RevertStyleOverrides(scene);
            Canvas.ForceUpdateCanvases();

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
                {
                    if (!canvas.isRootCanvas && canvas.transform.parent != null && canvas.transform.parent.GetComponentInParent<Canvas>(true) != null) continue;
                    if (canvas.renderMode == RenderMode.WorldSpace) continue;
                    StyleRoot(canvas.transform);
                }
            }

            StyleSceneComponents(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            report.AppendLine($"ESTILO {path}: {imageCount} imágenes, {textCount} textos, {buttonCount} botones, {createdCount} objetos nuevos.");
        }

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    private static readonly string[] StyleOverrideProperties =
    {
        "m_Sprite", "m_Color", "m_Material", "m_Type", "m_sharedMaterial", "m_fontAsset", "m_fontColor", "m_fontColor32", "m_fontStyle", "m_fontMaterial"
    };

    private static void RevertStyleOverrides(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            {
                GameObject go = transform.gameObject;
                if (!PrefabUtility.IsAnyPrefabInstanceRoot(go)) continue;
                if (Array.IndexOf(PrefabPaths, PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go)) < 0) continue;

                PropertyModification[] modifications = PrefabUtility.GetPropertyModifications(go);
                if (modifications == null) continue;

                var kept = new List<PropertyModification>();
                foreach (PropertyModification modification in modifications)
                {
                    bool graphic = modification.target is Image || modification.target is TMP_Text;
                    bool styled = false;
                    if (graphic)
                    {
                        foreach (string property in StyleOverrideProperties)
                        {
                            if (modification.propertyPath == property || modification.propertyPath.StartsWith(property + ".")) styled = true;
                        }
                    }
                    if (!styled) kept.Add(modification);
                }

                if (kept.Count != modifications.Length)
                    PrefabUtility.SetPropertyModifications(go, kept.ToArray());
            }
        }
    }

    private static void ResetCounters()
    {
        imageCount = 0;
        textCount = 0;
        buttonCount = 0;
        createdCount = 0;
    }

    private static void StyleRoot(Transform root)
    {
        Cleanup(root);

        foreach (Image image in root.GetComponentsInChildren<Image>(true))
            StyleImage(image);

        foreach (Slider slider in root.GetComponentsInChildren<Slider>(true))
            StyleSlider(slider);

        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            StyleText(text);

        StyleHud(root);
        StyleLevelUp(root);
        StyleTutorial(root);
        StyleMainMenu(root);
        StyleOverridePanels(root);
        StyleInitials(root);
        AddScreenIntros(root);
    }

    private static void StyleImage(Image image)
    {
        GameObject go = image.gameObject;
        string name = go.name;
        if (IsStyleObject(go)) return;
        if (name == "HoverHitArea" || name == "MapImage") return;

        Slider slider = go.GetComponentInParent<Slider>(true);
        if (slider != null) return;

        string key = SpriteKey(image);
        Button button = go.GetComponent<Button>();
        if (button != null)
        {
            StyleButton(button, image, key);
            return;
        }

        if (name.StartsWith("EmptySquare"))
        {
            style.ApplySprite(image, style.plate, style.secondary, style.skew, Vector2.zero, null);
            Touch(image);
            return;
        }

        if (name == "IconBackdrop")
        {
            bool result = go.transform.parent != null && go.transform.parent.name.EndsWith("3");
            style.ApplySprite(image, null, result ? Color.Lerp(style.cream, style.yellow, 0.6f) : style.cream, style.skew, Vector2.zero, null);
            Touch(image);
            return;
        }

        if (name == "Key")
        {
            style.ApplySprite(image, style.plateChamfer, style.cream, 0f, style.shadowOffset, null);
            UIStyle.SetLip(image, style.secondary);
            Touch(image);
            return;
        }

        if (name == "Frame" && go.transform.parent != null && go.transform.parent.name == "SelectorPanel")
        {
            image.enabled = false;
            image.sprite = null;
            Touch(image);
            return;
        }

        if (IsRibbonHost(go.transform))
        {
            BuildRibbon(image.rectTransform);
            return;
        }

        if (Array.IndexOf(LegacyLogoSprites, key) >= 0)
        {
            image.sprite = null;
            image.enabled = false;
            Touch(image);
            return;
        }

        if (IsScrim(image))
        {
            StyleScrim(image);
            return;
        }

        if (key == "builtin" && image.color.a > 0.01f && IsButtonContainer(go))
        {
            HideGraphic(image);
            Touch(image);
            return;
        }

        if (Array.IndexOf(LegacyPanelSprites, key) >= 0 || IsStylePanel(image.sprite) || image.sprite == style.plateChamfer || (image.sprite == style.plate && (!IsSkewed(image) || IsButtonContainer(go))))
        {
            StylePanel(image);
            return;
        }

        if (key == "arrow" || image.sprite == style.arrowDown)
        {
            image.sprite = style.arrowDown;
            image.color = style.cyan;
            Touch(image);
            return;
        }

        if (IsPicture(image, key))
            AddFrame(image.rectTransform);
    }

    private static bool IsSkewed(Image image)
    {
        UISkew skew = image.GetComponent<UISkew>();
        return skew != null;
    }

    private static bool IsPicture(Image image, string key)
    {
        if (image.sprite == null) return false;
        string name = image.gameObject.name;
        if (name == "QR" || key == "Link al MannerItch" || key == "CityPic" || key == "DeserrtPic" || key == "pLAYER") return true;
        return false;
    }

    private static void AddFrame(RectTransform target)
    {
        RectTransform frame = GetOrCreateChild(target, StylePrefix + "Frame", -1);
        Stretch(frame, -5f, -5f, 5f, 5f);
        Image frameImage = GetOrAdd<Image>(frame.gameObject);
        frameImage.raycastTarget = false;
        style.ApplySprite(frameImage, style.frame, Color.white, 0f, Vector2.zero, null);
        Touch(frameImage);
    }

    private static bool IsScrim(Image image)
    {
        if (!IsFullStretch(image.rectTransform)) return false;
        if (image.color.a < 0.3f) return false;

        string name = image.gameObject.name;
        Transform parent = image.transform.parent;
        bool underCanvas = parent != null && parent.GetComponent<Canvas>() != null;
        return underCanvas || name == "Background";
    }

    private static void StyleScrim(Image image)
    {
        string name = image.gameObject.name;
        Color color = style.scrim;

        if (name == "MainMenuPanel") color.a = 0.45f;
        else if (name == "Panel") color.a = 0.6f;
        else if (name == "GameOverPanel")
        {
            color = Color.Lerp(style.ink, style.danger, 0.3f);
            color.a = 0.94f;
        }

        image.sprite = null;
        image.type = Image.Type.Simple;
        image.color = color;
        image.material = style.scrimStripes;
        Touch(image);
        imageCount++;
    }

    private static void StylePanel(Image image)
    {
        GameObject go = image.gameObject;
        string name = go.name;
        Vector2 size = Size(image.rectTransform);
        imageCount++;

        if (IsButtonContainer(go))
        {
            if (go.GetComponent<GridLayoutGroup>() != null)
            {
                style.ApplySprite(image, style.panelPaper, Color.white, 0f, Vector2.zero, null);
            }
            else if (go.GetComponent<HorizontalLayoutGroup>() != null && HasPanelParent(go.transform, "MainMenuPanel"))
            {
                Color dock = style.panel;
                dock.a = 0.94f;
                style.ApplySprite(image, style.plate, dock, style.skewSoft, style.panelShadowOffset, null);
                UIStyle.SetLip(image, style.secondary);
            }
            else
            {
                HideGraphic(image);
            }
            Touch(image);
            return;
        }

        if (name == "Image" && go.transform.parent != null && HasDirectChild<Button>(go.transform.parent))
        {
            HideGraphic(image);
            Touch(image);
            return;
        }

        Image ancestor = PanelAncestor(go.transform);
        if (ancestor != null && Contains(ancestor.rectTransform, image.rectTransform))
        {
            bool onPaper = ancestor.sprite == style.panelPaper;
            style.ApplySprite(image, style.plateChamfer, onPaper ? Color.Lerp(style.cream, style.secondary, 0.25f) : style.panelAlt, 0f, Vector2.zero, null);
            Touch(image);
            return;
        }

        bool large = Mathf.Min(size.x, size.y) >= LargePanelMinSize;
        style.ApplySprite(image, large ? style.panelDark : style.panelSmall, Color.white, 0f, large ? style.panelShadowOffset : style.shadowOffset, large ? style.screenScan : null);
        Touch(image);
    }

    private static void HideGraphic(Image image)
    {
        image.sprite = null;
        image.type = Image.Type.Simple;
        image.color = new Color(1f, 1f, 1f, 0f);
        image.material = null;
        image.raycastTarget = false;
        Shadow shadow = image.GetComponent<Shadow>();
        if (shadow != null) shadow.enabled = false;
    }

    private static bool IsButtonContainer(GameObject go)
    {
        if (go.GetComponent<LayoutGroup>() == null) return false;
        foreach (Transform child in go.transform)
            if (child.GetComponent<Button>() != null) return true;
        return false;
    }

    private static void StyleButton(Button button, Image image, string key)
    {
        string name = button.name;
        buttonCount++;

        if (name.StartsWith("UpgradeButton") || (name.StartsWith("Map") && name.EndsWith("Button")))
        {
            StyleCard(button, image);
            return;
        }

        TMP_Text label = DirectChildText(button.transform);
        bool isPicture = image.sprite != null && Array.IndexOf(LegacyButtonSprites, key) < 0 && Array.IndexOf(LegacyPanelSprites, key) < 0
            && key != "builtin" && image.sprite != style.plate && image.sprite != style.plateChamfer;

        if (label == null || isPicture)
        {
            if (key == "Icon_06") image.sprite = style.iconSpeaker;

            ColorBlock iconColors = ColorBlock.defaultColorBlock;
            iconColors.normalColor = Color.white;
            iconColors.highlightedColor = new Color(0.62f, 0.86f, 1f, 1f);
            iconColors.selectedColor = iconColors.highlightedColor;
            iconColors.pressedColor = new Color(0.4f, 0.62f, 0.8f, 1f);
            iconColors.disabledColor = style.disabled;
            iconColors.fadeDuration = 0.08f;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = iconColors;
            ConfigureHover(button.gameObject, false);

            if (isPicture && Mathf.Min(Size(image.rectTransform).x, Size(image.rectTransform).y) >= 100f)
                AddFrame(image.rectTransform);

            Touch(image);
            Touch(button);
            return;
        }

        UIPlateRole role = ButtonRole(button, label);
        bool gridKey = button.transform.parent != null && button.transform.parent.GetComponent<GridLayoutGroup>() != null;
        if (gridKey)
        {
            style.ApplySprite(image, style.plateChamfer, Color.white, 0f, style.shadowOffset * 0.67f, null);
            UIStyle.SetLip(image, style.lipTint);
        }
        else
        {
            style.ApplySprite(image, style.plate, Color.white, Size(image.rectTransform).y > 96f ? style.skewSoft : style.skew, style.shadowOffset, style.plateStripes);
        }
        image.raycastTarget = true;
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = style.ButtonColors(role);
        ConfigureHover(button.gameObject, true);
        Touch(image);
        Touch(button);
    }

    private static void ConfigureHover(GameObject go, bool plate)
    {
        MenuButtonHover hover = GetOrAdd<MenuButtonHover>(go);
        var serialized = new SerializedObject(hover);
        serialized.FindProperty("hoverScale").floatValue = style.hoverScale;
        serialized.FindProperty("scaleDuration").floatValue = style.hoverDuration;
        serialized.FindProperty("hoverRotation").floatValue = plate ? style.hoverRotation : 0f;
        serialized.FindProperty("pressScale").floatValue = style.pressScale;
        serialized.FindProperty("hoverShadow").floatValue = plate ? style.hoverShadow : 1f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        Touch(hover);
    }

    private static UIPlateRole ButtonRole(Button button, TMP_Text label)
    {
        var key = new StringBuilder();
        key.Append(button.name).Append(' ');
        if (label != null) key.Append(label.text).Append(' ');
        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            key.Append(button.onClick.GetPersistentMethodName(i)).Append(' ');
        string text = key.ToString().ToLowerInvariant();

        if (ContainsAny(text, "salir", "exit", "mainmenu", "menu principal", "menú principal", "skip", "saltar", "revert", "button_del"))
            return UIPlateRole.Danger;
        if (ContainsAny(text, "sobrecarga"))
            return UIPlateRole.Anomaly;
        if (ContainsAny(text, "volver", "regresar", "return", "back", "cancel", "nobutton", "button_spc"))
            return UIPlateRole.Neutral;
        if (ContainsAny(text, "play", "jugar", "reanudar", "resume", "next", "comprar", "purchase", "equipar", "apply", "aplicar", "retry", "keep", "mantener", "button_end"))
            return UIPlateRole.Primary;
        return UIPlateRole.Secondary;
    }

    private static bool ContainsAny(string text, params string[] words)
    {
        foreach (string word in words)
            if (text.Contains(word)) return true;
        return false;
    }

    private static void StyleCard(Button button, Image image)
    {
        bool locked = Luminance(image.color) < 0.2f && image.sprite != style.panelDark;
        bool wasLocked = image.sprite == style.panelDark && image.color.r < 0.7f;
        style.ApplySprite(image, style.panelDark, locked || wasLocked ? new Color(0.5f, 0.5f, 0.58f, 1f) : Color.white, 0f, style.panelShadowOffset, style.screenScan);
        image.raycastTarget = true;

        ColorBlock colors = ColorBlock.defaultColorBlock;
        colors.normalColor = new Color(0.9f, 0.9f, 0.95f, 1f);
        colors.highlightedColor = Color.white;
        colors.selectedColor = Color.white;
        colors.pressedColor = new Color(0.78f, 0.78f, 0.86f, 1f);
        colors.disabledColor = style.disabled;
        colors.fadeDuration = 0.08f;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = colors;
        Touch(image);
        Touch(button);

        Transform card = button.transform;
        Transform icon = DirectChild(card, "Icon");
        if (icon != null)
        {
            RectTransform iconRect = (RectTransform)icon;
            RectTransform slot = GetOrCreateSibling(iconRect, StylePrefix + "Slot");
            slot.anchorMin = iconRect.anchorMin;
            slot.anchorMax = iconRect.anchorMax;
            slot.pivot = iconRect.pivot;
            Vector2 slotSize = iconRect.sizeDelta + new Vector2(32f, 16f);
            if (iconRect.anchorMin.y > 0.99f && Mathf.Approximately(iconRect.pivot.y, 0.5f))
            {
                iconRect.anchoredPosition = new Vector2(iconRect.anchoredPosition.x, -Snap(CardSlotInset + slotSize.y * 0.5f));
                Touch(iconRect);
            }
            slot.anchoredPosition = iconRect.anchoredPosition;
            slot.sizeDelta = slotSize;
            Image slotImage = GetOrAdd<Image>(slot.gameObject);
            slotImage.raycastTarget = false;
            style.ApplySprite(slotImage, style.plate, style.cream, style.skew, style.shadowOffset * 0.67f, null);
            Touch(slotImage);
            Touch(slot);

            UpgradeButton upgrade = button.GetComponent<UpgradeButton>();
            if (upgrade != null)
            {
                var serializedUpgrade = new SerializedObject(upgrade);
                serializedUpgrade.FindProperty("iconBackdrop").objectReferenceValue = slot.gameObject;
                serializedUpgrade.ApplyModifiedPropertiesWithoutUndo();
                Touch(upgrade);
            }
        }

        Transform overlay = DirectChild(card, "FillOverlay");
        if (overlay != null)
        {
            Image overlayImage = overlay.GetComponent<Image>();
            if (overlayImage != null)
            {
                Color fillColor = style.cyan;
                fillColor.a = 0.5f;
                style.ApplySprite(overlayImage, style.fill, fillColor, 0f, Vector2.zero, style.plateStripesBold);
                Touch(overlayImage);
            }
        }

        HoldToSelectButton hold = button.GetComponent<HoldToSelectButton>();
        if (hold != null)
        {
            var serialized = new SerializedObject(hold);
            Color premium = Lilac;
            premium.a = 0.6f;
            serialized.FindProperty("premiumFillColor").colorValue = premium;
            SerializedProperty inset = serialized.FindProperty("fillInset");
            if (inset != null) inset.vector2Value = new Vector2(13f, 13f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Touch(hold);
        }

        foreach (Transform child in card)
        {
            Image picture = child.GetComponent<Image>();
            if (picture == null || picture.sprite == null) continue;
            string pictureKey = SpriteKey(picture);
            if (pictureKey == "CityPic" || pictureKey == "DeserrtPic") AddFrame(picture.rectTransform);
        }
    }

    private static void StyleSlider(Slider slider)
    {
        Transform background = slider.transform.Find("Background");
        Transform fillArea = slider.transform.Find("Fill Area");
        RectTransform fill = slider.fillRect;
        RectTransform handle = slider.handleRect;

        if (background != null)
        {
            RectTransform rect = (RectTransform)background;
            rect.anchorMin = new Vector2(0f, 0.12f);
            rect.anchorMax = new Vector2(1f, 0.88f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = background.GetComponent<Image>();
            if (image != null)
            {
                style.ApplySprite(image, style.capsule, style.panel, 0f, Vector2.zero, null);
                Touch(image);
            }
            Touch(rect);
        }

        if (fillArea != null)
        {
            RectTransform rect = (RectTransform)fillArea;
            rect.anchorMin = new Vector2(0f, 0.12f);
            rect.anchorMax = new Vector2(1f, 0.88f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(-14f, -14f);
            Touch(rect);
        }

        if (fill != null)
        {
            fill.sizeDelta = Vector2.zero;
            Image image = fill.GetComponent<Image>();
            if (image != null)
            {
                style.ApplySprite(image, style.fill, style.cyan, 0f, Vector2.zero, style.plateStripesBold);
                Touch(image);
            }
            Touch(fill);
        }

        if (handle != null)
        {
            handle.sizeDelta = new Vector2(30f, 10f);
            Image image = handle.GetComponent<Image>();
            if (image != null)
            {
                style.ApplySprite(image, style.capsule, style.paper, 0f, style.shadowOffset * 0.67f, null);
                UIStyle.SetLip(image, style.secondary);
                Touch(image);
            }
            Touch(handle);
        }

        ColorBlock colors = ColorBlock.defaultColorBlock;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.62f, 0.86f, 1f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(0.4f, 0.62f, 0.8f, 1f);
        colors.disabledColor = style.disabled;
        slider.colors = colors;
        Touch(slider);
        imageCount += 3;
    }

    private static void StyleText(TMP_Text text)
    {
        GameObject go = text.gameObject;
        if (IsStyleObject(go)) return;

        Transform parent = text.transform.parent;
        Button parentButton = parent != null ? parent.GetComponent<Button>() : null;
        bool inPlateButton = parentButton != null && parentButton.targetGraphic is Image plate && (plate.sprite == style.plate || plate.sprite == style.plateChamfer);
        Image backdrop = PanelAncestor(text.transform);
        bool onPaper = (backdrop != null && backdrop.sprite == style.panelPaper) || (parent != null && parent.name == "Key");
        float size = MaxSize(text);
        string name = go.name;
        Color mapped = MapColor(text.color, style.paper);
        bool accent = !Near(mapped, style.paper) && !Near(mapped, style.textDim) && !Near(mapped, style.yellow) && !Near(mapped, style.cyan);
        textCount++;

        FloatingText floating = go.GetComponent<FloatingText>();
        if (floating != null)
        {
            Apply(text, UITextRole.Number, text.color, false);
            var serializedFloating = new SerializedObject(floating);
            serializedFloating.FindProperty("popScale").floatValue = 1.2f;
            serializedFloating.ApplyModifiedPropertiesWithoutUndo();
            Touch(floating);
            return;
        }

        if (inPlateButton)
        {
            Apply(text, UITextRole.Label, style.paper, true);
            return;
        }

        if (onPaper)
        {
            if (size >= 34f || name.Contains("Title"))
            {
                Apply(text, UITextRole.Heading, style.secondary, true);
                text.fontSharedMaterial = style.textPlain;
            }
            else
            {
                Apply(text, UITextRole.BodyDark, style.ink, false);
            }
            if (parent != null && parent.name == "Key")
                text.fontStyle = FontStyles.UpperCase;
            Finish(text);
            return;
        }

        if (name == "UnknownText")
        {
            Apply(text, UITextRole.Heading, style.cyan, false);
            return;
        }

        if (name.EndsWith("_Txt"))
        {
            Apply(text, UITextRole.Label, style.paper, false);
            return;
        }

        if (size >= 60f)
        {
            Apply(text, UITextRole.Title, accent ? mapped : style.paper, true);
            return;
        }

        bool besideSlider = parent != null && HasDirectChild<Slider>(parent);
        string content = text.text ?? string.Empty;
        bool shortLine = content.Trim().Length <= 30 && !content.Contains("\n");
        bool keyRow = parent != null && parent.name.EndsWith("KeyPanel");
        bool heading = !besideSlider && !keyRow && (name.Contains("Title") || size >= 30f && shortLine);

        if (besideSlider)
        {
            Apply(text, UITextRole.Body, style.paper, true);
            text.fontSizeMax = Mathf.Min(size, 32f);
            text.fontSizeMin = 16f;
            Finish(text);
            return;
        }

        if (heading)
        {
            Apply(text, UITextRole.Heading, accent ? mapped : style.cyan, true);
            return;
        }

        Color bodyColor = mapped;
        Apply(text, UITextRole.Body, bodyColor, false);
    }

    private static void Apply(TMP_Text text, UITextRole role, Color color, bool autoSize)
    {
        float size = MaxSize(text);
        float alpha = text.color.a;
        style.ApplyFont(text, role);
        color.a = alpha;
        text.color = color;
        text.fontStyle = UIStyle.TextStyle(role);
        text.characterSpacing = style.TextSpacing(role);
        text.extraPadding = role != UITextRole.BodyDark;

        Vector2 rect = text.rectTransform.rect.size;
        if (autoSize && rect.x > 20f && rect.y > 12f)
        {
            text.enableAutoSizing = true;
            text.fontSizeMax = size;
            text.fontSizeMin = Mathf.Max(10f, Mathf.Round(size * 0.5f));

            string content = text.text ?? string.Empty;
            if (!content.Contains("\n") && (role == UITextRole.Label || rect.y < size * 2.6f))
                text.textWrappingMode = TextWrappingModes.NoWrap;
        }

        if (role == UITextRole.Label && rect.x > 60f)
        {
            float margin = Mathf.Round(Mathf.Clamp(rect.y * 0.3f, 8f, 26f));
            UISkew skew = text.transform.parent != null ? text.transform.parent.GetComponent<UISkew>() : null;
            float lean = skew != null ? Mathf.Round(Mathf.Min(Mathf.Abs(skew.Amount) * rect.y * 0.5f, rect.x * 0.1f)) : 0f;
            text.margin = new Vector4(margin + lean, 0f, margin + lean, 0f);
        }

        Finish(text);
    }

    private static void Finish(TMP_Text text)
    {
        text.ForceMeshUpdate(true, true);
        Touch(text);
    }

    private static float Snap(float value)
    {
        return Mathf.Round(value * 10f) / 10f;
    }

    private static float MaxSize(TMP_Text text)
    {
        return text.enableAutoSizing ? text.fontSizeMax : text.fontSize;
    }

    private static Color MapColor(Color color, Color fallback)
    {
        Color[] palette = { style.yellow, style.good, style.alert, style.cyan, style.paper, style.textDim, style.ink, Lilac };
        foreach (Color entry in palette)
            if (Near(color, entry)) return WithAlpha(entry, color.a);

        if (Near(color, FirstVersionDim)) return WithAlpha(style.textDim, color.a);

        Color.RGBToHSV(color, out float h, out float s, out float v);
        if (s < 0.2f)
            return WithAlpha(v > 0.9f ? fallback : v > 0.55f ? style.textDim : fallback, color.a);
        if (h >= 0.1f && h < 0.2f) return WithAlpha(style.yellow, color.a);
        if (h >= 0.2f && h < 0.45f) return WithAlpha(style.good, color.a);
        if (h >= 0.45f && h < 0.62f) return WithAlpha(style.cyan, color.a);
        if (h >= 0.62f && h < 0.97f) return WithAlpha(Lilac, color.a);
        return WithAlpha(style.alert, color.a);
    }

    private static readonly Color FirstVersionDim = new Color32(0xCF, 0xC4, 0xF2, 0xFF);

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private static bool Near(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) < 0.09f;
    }

    private static float Luminance(Color color)
    {
        return color.r * 0.299f + color.g * 0.587f + color.b * 0.114f;
    }

    private static bool IsRibbonHost(Transform transform)
    {
        string name = transform.name;
        if (name == "TitlePanel" || name == "GOPanel") return true;
        return name == "Title" && transform.GetComponent<Image>() != null && DirectChildText(transform) != null;
    }

    private static void BuildRibbon(RectTransform host)
    {
        Image hostImage = host.GetComponent<Image>();
        if (hostImage != null)
        {
            HideGraphic(hostImage);
            Touch(hostImage);
        }

        var texts = new List<TMP_Text>();
        foreach (Transform child in host)
        {
            if (IsStyleObject(child.gameObject)) continue;
            TMP_Text text = child.GetComponent<TMP_Text>();
            if (text != null) texts.Add(text);
        }
        if (texts.Count == 0) return;

        texts.Sort((a, b) => MaxSize(b).CompareTo(MaxSize(a)));
        TMP_Text title = texts[0];
        TMP_Text subtitle = texts.Count > 1 ? texts[1] : null;
        Rect hostRect = host.rect;
        bool gameOver = host.name == "GOPanel";
        imageCount++;

        Rect titleBounds = RelativeRect(host, title.rectTransform);
        float titleSize = MaxSize(title);
        Vector2 center = new Vector2(0.5f, 0.5f);
        Vector2 ribbonPosition = new Vector2(0f, Snap(titleBounds.center.y - hostRect.center.y));
        Vector2 ribbonSize = new Vector2(Snap(hostRect.width - 50f), Snap(Mathf.Max(titleSize * 1.5f, 110f)));
        float w = ribbonSize.x;
        float h = ribbonSize.y;
        float lean = Mathf.Round(style.skew * h * 0.5f);

        RectTransform accent = GetOrCreateChild(host, StylePrefix + "ShardAccent", 0);
        Shard(accent, center, ribbonPosition, ribbonSize, gameOver ? style.yellow : style.primary, 0f, 1f, 0f, 0.06f, new[]
        {
            new Vector2(-lean - 30f, -2f),
            new Vector2(w - lean + 4f, -8f),
            new Vector2(w - lean * 0.2f + 8f, h * 0.4f),
            new Vector2(w + lean + 40f, h * 0.86f),
            new Vector2(w + lean + 4f, h + 16f),
            new Vector2(lean - 22f, h + 24f),
            new Vector2(lean * 0.5f - 24f, h * 0.8f),
            new Vector2(-lean - 42f, h * 0.5f),
            new Vector2(-lean * 0.5f - 26f, h * 0.3f),
        });

        RectTransform ink = GetOrCreateChild(host, StylePrefix + "ShardInk", 1);
        Shard(ink, center, ribbonPosition, ribbonSize, style.ink, 0f, 0.45f, 2.1f, 0f, new[]
        {
            new Vector2(-lean - 10f, -12f),
            new Vector2(w - lean + 14f, -16f),
            new Vector2(w - lean * 0.4f + 10f, h * 0.3f),
            new Vector2(w + lean + 26f, h * 0.64f),
            new Vector2(w + lean * 0.56f + 10f, h * 0.78f),
            new Vector2(w + lean + 12f, h + 8f),
            new Vector2(lean - 6f, h + 12f),
            new Vector2(lean * 0.44f - 10f, h * 0.72f),
            new Vector2(-lean - 28f, h * 0.36f),
            new Vector2(-lean * 0.6f - 10f, h * 0.2f),
        });

        RectTransform ribbon = GetOrCreateChild(host, StylePrefix + "Ribbon", 2);
        ribbon.anchorMin = ribbon.anchorMax = center;
        ribbon.pivot = center;
        ribbon.anchoredPosition = ribbonPosition;
        ribbon.sizeDelta = ribbonSize;
        Image ribbonImage = GetOrAdd<Image>(ribbon.gameObject);
        ribbonImage.raycastTarget = false;
        Color ribbonColor = gameOver ? style.danger : style.secondary;
        style.ApplySprite(ribbonImage, style.plate, ribbonColor, style.skew, Vector2.zero, gameOver ? style.plateHazard : style.plateStripes);
        Touch(ribbonImage);
        Touch(ribbon);

        if (subtitle != null)
        {
            Rect subBounds = RelativeRect(host, subtitle.rectTransform);
            float subSize = MaxSize(subtitle);
            RectTransform sub = GetOrCreateChild(host, StylePrefix + "SubRibbon", 3);
            sub.anchorMin = sub.anchorMax = new Vector2(0.5f, 0.5f);
            sub.pivot = new Vector2(0.5f, 0.5f);
            sub.anchoredPosition = new Vector2(0f, Snap(subBounds.center.y - hostRect.center.y));
            sub.sizeDelta = new Vector2(Snap(hostRect.width * 0.66f), Snap(Mathf.Max(subSize * 1.45f, 52f)));
            Image subImage = GetOrAdd<Image>(sub.gameObject);
            subImage.raycastTarget = false;
            style.ApplySprite(subImage, style.capsule, style.panel, 0f, style.shadowOffset * 0.67f, null);
            UIStyle.SetLip(subImage, style.secondary);
            Touch(subImage);
            Touch(sub);

            RectTransform chevron = GetOrCreateChild(sub, StylePrefix + "Glyph", -1);
            chevron.anchorMin = chevron.anchorMax = new Vector2(0f, 0.5f);
            chevron.pivot = new Vector2(0.5f, 0.5f);
            chevron.anchoredPosition = new Vector2(34f, 0f);
            chevron.sizeDelta = new Vector2(26f, 26f);
            Image chevronImage = GetOrAdd<Image>(chevron.gameObject);
            chevronImage.raycastTarget = false;
            style.ApplySprite(chevronImage, style.chevron, style.cyan, 0f, Vector2.zero, null);
            Touch(chevronImage);

            subtitle.rectTransform.anchorMin = subtitle.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            subtitle.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            subtitle.rectTransform.anchoredPosition = sub.anchoredPosition;
            subtitle.rectTransform.sizeDelta = sub.sizeDelta - new Vector2(24f, 8f);
            Apply(subtitle, UITextRole.Heading, style.textDim, true);
            subtitle.margin = new Vector4(48f, 0f, 48f, 0f);
            subtitle.alignment = TextAlignmentOptions.Center;
            Touch(subtitle.rectTransform);
            Finish(subtitle);
        }

        title.rectTransform.anchorMin = title.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        title.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        bool faceTitle = HasPanelParent(host, "MainMenuPanel");
        float rightPad = lean + 46f;
        float leftPad = faceTitle ? (150f + rightPad) * 0.5f : rightPad;
        rightPad = leftPad;
        title.rectTransform.anchoredPosition = ribbon.anchoredPosition;
        title.rectTransform.sizeDelta = ribbon.sizeDelta - new Vector2(leftPad + rightPad, 14f);
        title.margin = Vector4.zero;
        Apply(title, UITextRole.Title, style.paper, true);
        title.alignment = TextAlignmentOptions.Center;
        Touch(title.rectTransform);
        Finish(title);

        RectTransform cursor = GetOrCreateChild(host, StylePrefix + "Cursor", -1);
        float textWidth = Mathf.Min(title.GetPreferredValues(title.text).x, title.rectTransform.sizeDelta.x);
        float height = Snap(Mathf.Clamp(title.fontSize * 0.62f, 24f, 64f));
        cursor.anchorMin = cursor.anchorMax = new Vector2(0.5f, 0.5f);
        cursor.pivot = new Vector2(0f, 0.5f);
        cursor.sizeDelta = new Vector2(Snap(height * 0.5f), height);
        Vector2 cursorPosition = new Vector2(Snap(title.rectTransform.anchoredPosition.x + textWidth * 0.5f + CursorGap), Snap(ribbon.anchoredPosition.y - title.fontSize * 0.36f + height * 0.5f));
        if ((cursor.anchoredPosition - cursorPosition).sqrMagnitude > 0.0001f) cursor.anchoredPosition = cursorPosition;
        Image cursorImage = GetOrAdd<Image>(cursor.gameObject);
        cursorImage.raycastTarget = false;
        style.ApplySprite(cursorImage, style.cursor, style.paper, 0f, Vector2.zero, null);
        UITextCursor follow = GetOrAdd<UITextCursor>(cursor.gameObject);
        var serializedCursor = new SerializedObject(follow);
        serializedCursor.FindProperty("target").objectReferenceValue = title;
        serializedCursor.FindProperty("gap").floatValue = CursorGap;
        serializedCursor.ApplyModifiedPropertiesWithoutUndo();
        Touch(follow);
        Touch(cursorImage);
        Touch(cursor);
    }

    private const float CursorGap = 1f;

    private static bool HasPanelParent(Transform target, string panelName)
    {
        Transform parent = target.parent;
        if (parent != null && parent.name == "ContentFrame") parent = parent.parent;
        return parent != null && parent.name == panelName;
    }

    private static void Shard(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size, Color color, float stroke, float motion, float phase, float enterDelay, Vector2[] pixels)
    {
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        UIShard shard = GetOrAdd<UIShard>(rect.gameObject);
        shard.raycastTarget = false;
        shard.color = color;

        var serialized = new SerializedObject(shard);
        SerializedProperty points = serialized.FindProperty("points");
        points.arraySize = pixels.Length;
        for (int i = 0; i < pixels.Length; i++)
            points.GetArrayElementAtIndex(i).vector2Value = new Vector2(pixels[i].x / size.x, pixels[i].y / size.y);
        serialized.FindProperty("stroke").floatValue = stroke;
        serialized.FindProperty("motion").floatValue = motion;
        serialized.FindProperty("phase").floatValue = phase;
        serialized.FindProperty("enterDelay").floatValue = enterDelay;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        Touch(shard);
        Touch(rect);
    }

    private static void AddScreenIntros(Transform root)
    {
        if (root.GetComponent<Canvas>() == null)
        {
            if (root.GetComponent<InitialsEntryUI>() != null) EnsureIntro(root.gameObject);
            return;
        }

        foreach (Transform child in root)
        {
            Image image = child.GetComponent<Image>();
            if (image == null || !IsFullStretch((RectTransform)child)) continue;
            if (image.material != style.scrimStripes) continue;
            if (child.name == "LevelUpPanel" || child.name == "Panel") continue;
            EnsureIntro(child.gameObject);
        }
    }

    private static void EnsureIntro(GameObject go)
    {
        UIScreenIntro intro = go.GetComponent<UIScreenIntro>();
        if (intro == null)
        {
            intro = go.AddComponent<UIScreenIntro>();
            Touch(intro);
            Touch(go);
        }
        AuthorIntroBlocks(intro);
    }

    internal static bool AuthorIntroBlocks(UIScreenIntro intro)
    {
        Transform frame = intro.transform.Find("ContentFrame");
        if (frame == null) return false;
        var serialized = new SerializedObject(intro);
        SerializedProperty blocks = serialized.FindProperty("blocks");
        bool valid = blocks.arraySize > 0;
        for (int i = 0; i < blocks.arraySize && valid; i++)
        {
            RectTransform block = blocks.GetArrayElementAtIndex(i).objectReferenceValue as RectTransform;
            valid = block != null && block.IsChildOf(frame) && block.GetComponent<AspectRatioFitter>() == null;
        }
        if (valid) return false;
        var targets = new List<RectTransform>();
        foreach (Transform child in frame)
        {
            RectTransform rect = child as RectTransform;
            if (rect != null && !IsFullStretch(rect)) targets.Add(rect);
        }
        blocks.arraySize = targets.Count;
        for (int i = 0; i < targets.Count; i++) blocks.GetArrayElementAtIndex(i).objectReferenceValue = targets[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
        if (PrefabUtility.IsPartOfPrefabInstance(intro)) PrefabUtility.RecordPrefabInstancePropertyModifications(intro);
        EditorUtility.SetDirty(intro);
        return true;
    }

    private static string SpriteKey(Image image)
    {
        Sprite sprite = image.sprite;
        if (sprite == null) return string.Empty;
        string path = AssetDatabase.GetAssetPath(sprite);
        if (string.IsNullOrEmpty(path) || path.StartsWith("Resources/unity_builtin")) return "builtin";
        return Path.GetFileNameWithoutExtension(path);
    }

    private static bool IsStylePanel(Sprite sprite)
    {
        return sprite != null && (sprite == style.panelDark || sprite == style.panelSmall || sprite == style.panelPaper);
    }

    private static bool IsStyleObject(GameObject go)
    {
        return go.name.StartsWith(StylePrefix, StringComparison.Ordinal);
    }

    private static Image PanelAncestor(Transform transform)
    {
        Transform current = transform.parent;
        while (current != null)
        {
            Image image = current.GetComponent<Image>();
            if (image != null && image.enabled && IsStylePanel(image.sprite)) return image;
            current = current.parent;
        }
        return null;
    }

    private static bool Contains(RectTransform outer, RectTransform inner)
    {
        Rect bounds = RelativeRect(outer, inner);
        Rect rect = outer.rect;
        const float tolerance = 12f;
        return bounds.xMin >= rect.xMin - tolerance && bounds.xMax <= rect.xMax + tolerance
            && bounds.yMin >= rect.yMin - tolerance && bounds.yMax <= rect.yMax + tolerance;
    }

    private static readonly Vector3[] Corners = new Vector3[4];

    private static Rect RelativeRect(RectTransform space, RectTransform target)
    {
        target.GetWorldCorners(Corners);
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        for (int i = 0; i < 4; i++)
        {
            Vector3 local = space.InverseTransformPoint(Corners[i]);
            min = Vector2.Min(min, local);
            max = Vector2.Max(max, local);
        }
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    private static bool IsFullStretch(RectTransform rect)
    {
        return rect.anchorMin.sqrMagnitude < 0.0001f && (rect.anchorMax - Vector2.one).sqrMagnitude < 0.0001f && rect.sizeDelta.sqrMagnitude < 4f;
    }

    private static Vector2 Size(RectTransform rect)
    {
        Vector2 size = rect.rect.size;
        if (size.x <= 0.01f || size.y <= 0.01f) size = rect.sizeDelta;
        return new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.y));
    }

    private static TMP_Text DirectChildText(Transform parent)
    {
        foreach (Transform child in parent)
        {
            if (IsStyleObject(child.gameObject)) continue;
            TMP_Text text = child.GetComponent<TMP_Text>();
            if (text != null) return text;
        }
        return null;
    }

    private static Transform DirectChild(Transform parent, string name)
    {
        return UIEditorHierarchy.Find(parent, name);
    }

    private static bool HasDirectChild<T>(Transform parent) where T : Component
    {
        foreach (Transform child in parent)
            if (child.GetComponent<T>() != null) return true;
        return false;
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform child in root)
        {
            Transform found = FindDeep(child, name);
            if (found != null) return found;
        }
        return null;
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        if (component == null) component = go.AddComponent<T>();
        return component;
    }

    private static RectTransform GetOrCreateChild(Transform parent, string name, int siblingIndex)
    {
        Transform existing = DirectChild(parent, name);
        RectTransform rect;
        if (existing != null)
        {
            rect = (RectTransform)existing;
        }
        else
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            rect = (RectTransform)go.transform;
            createdCount++;

            if (parent.GetComponent<LayoutGroup>() != null)
                go.AddComponent<LayoutElement>().ignoreLayout = true;
        }

        if (siblingIndex < 0) rect.SetAsLastSibling();
        else rect.SetSiblingIndex(siblingIndex);
        return rect;
    }

    private static RectTransform GetOrCreateSibling(RectTransform target, string name)
    {
        Transform parent = target.parent;
        Transform existing = DirectChild(parent, name);
        RectTransform rect;
        if (existing != null)
        {
            rect = (RectTransform)existing;
        }
        else
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = target.gameObject.layer;
            go.transform.SetParent(parent, false);
            rect = (RectTransform)go.transform;
            createdCount++;

            if (parent.GetComponent<LayoutGroup>() != null)
                go.AddComponent<LayoutElement>().ignoreLayout = true;
        }

        int index = target.GetSiblingIndex();
        if (rect.GetSiblingIndex() < index) index--;
        rect.SetSiblingIndex(index);
        return rect;
    }

    private static void Stretch(RectTransform rect, float left, float bottom, float right, float top)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(right, top);
    }

    private static void Touch(Object target)
    {
        if (target == null) return;
        EditorUtility.SetDirty(target);
        if (PrefabUtility.IsPartOfPrefabInstance(target))
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
    }
}

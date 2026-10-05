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
            style.ApplySprite(image, style.plateChamfer, style.panelAlt, 0f, Vector2.zero, null);
            Touch(image);
            return;
        }

        if (name == "IconBackdrop")
        {
            image.sprite = null;
            image.type = Image.Type.Simple;
            image.color = Luminance(image.color) > 0.93f ? style.paper : Color.Lerp(style.paper, style.yellow, 0.55f);
            Touch(image);
            return;
        }

        if (name == "Key")
        {
            style.ApplySprite(image, style.plateChamfer, style.paper, 0f, style.shadowOffset, null);
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

        if (Array.IndexOf(LegacyPanelSprites, key) >= 0 || IsStylePanel(image.sprite) || (image.sprite == style.plate && image.GetComponent<UISkew>() == null))
        {
            StylePanel(image);
            return;
        }

        if (key == "arrow" || image.sprite == style.arrowDown)
        {
            image.sprite = style.arrowDown;
            image.color = style.yellow;
            Touch(image);
            return;
        }

        if (IsPicture(image, key))
            AddFrame(image.rectTransform);
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

        if (name == "MainMenuPanel") color.a = 0.4f;
        else if (name == "Panel") color.a = 0.55f;
        else if (name == "GameOverPanel")
        {
            color = Color.Lerp(style.ink, style.danger, 0.38f);
            color.a = 0.93f;
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

        if (name == "TutorialPanel")
        {
            style.ApplySprite(image, style.panelPaper, Color.white, 0f, style.panelShadowOffset, null);
            Touch(image);
            return;
        }

        if (IsButtonContainer(go))
        {
            if (go.GetComponent<GridLayoutGroup>() != null)
            {
                style.ApplySprite(image, style.panelPaper, Color.white, 0f, Vector2.zero, null);
            }
            else if (go.GetComponent<HorizontalLayoutGroup>() != null && go.transform.parent != null && go.transform.parent.name == "MainMenuPanel")
            {
                Color dock = style.panel;
                dock.a = 0.94f;
                style.ApplySprite(image, style.plate, dock, style.skew, style.panelShadowOffset, null);
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
            style.ApplySprite(image, style.plate, onPaper ? Color.Lerp(style.paper, style.secondary, 0.22f) : style.panelAlt, 0f, Vector2.zero, null);
            Touch(image);
            return;
        }

        bool large = Mathf.Min(size.x, size.y) >= LargePanelMinSize;
        style.ApplySprite(image, large ? style.panelDark : style.panelSmall, Color.white, 0f, large ? style.panelShadowOffset : style.shadowOffset, null);
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
            && key != "builtin" && image.sprite != style.plate;

        if (label == null || isPicture)
        {
            if (key == "Icon_06") image.sprite = style.iconSpeaker;

            ColorBlock iconColors = ColorBlock.defaultColorBlock;
            iconColors.normalColor = Color.white;
            iconColors.highlightedColor = new Color(1f, 0.93f, 0.6f, 1f);
            iconColors.selectedColor = iconColors.highlightedColor;
            iconColors.pressedColor = new Color(0.85f, 0.75f, 0.45f, 1f);
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
        style.ApplySprite(image, style.plate, Color.white, style.skew, style.shadowOffset, style.plateStripes);
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
        style.ApplySprite(image, style.panelDark, locked || wasLocked ? new Color(0.5f, 0.5f, 0.58f, 1f) : Color.white, 0f, style.panelShadowOffset, null);
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
            slot.anchoredPosition = iconRect.anchoredPosition;
            slot.sizeDelta = iconRect.sizeDelta + new Vector2(36f, 36f);
            Image slotImage = GetOrAdd<Image>(slot.gameObject);
            slotImage.raycastTarget = false;
            style.ApplySprite(slotImage, style.plateChamfer, style.paper, 0f, style.shadowOffset * 0.8f, null);
            Touch(slotImage);
            Touch(slot);

            RectTransform band = GetOrCreateSibling(slot, StylePrefix + "Band");
            band.anchorMin = iconRect.anchorMin;
            band.anchorMax = iconRect.anchorMax;
            band.pivot = iconRect.pivot;
            band.anchoredPosition = iconRect.anchoredPosition;
            band.sizeDelta = new Vector2(Size((RectTransform)card).x * 0.78f, 76f);
            Image bandImage = GetOrAdd<Image>(band.gameObject);
            bandImage.raycastTarget = false;
            style.ApplySprite(bandImage, style.plate, style.secondary, style.skew, Vector2.zero, style.plateStripes);
            Touch(bandImage);
            Touch(band);
        }

        Transform overlay = DirectChild(card, "FillOverlay");
        if (overlay != null)
        {
            Image overlayImage = overlay.GetComponent<Image>();
            if (overlayImage != null)
            {
                Color fillColor = style.yellow;
                fillColor.a = 0.55f;
                style.ApplySprite(overlayImage, style.fill, fillColor, 0f, Vector2.zero, style.plateStripesBold);
                Touch(overlayImage);
            }
        }

        HoldToSelectButton hold = button.GetComponent<HoldToSelectButton>();
        if (hold != null)
        {
            var serialized = new SerializedObject(hold);
            Color premium = style.paper;
            premium.a = 0.7f;
            serialized.FindProperty("premiumFillColor").colorValue = premium;
            SerializedProperty inset = serialized.FindProperty("fillInset");
            if (inset != null) inset.vector2Value = new Vector2(11f, 11f);
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
                style.ApplySprite(image, style.plate, style.panel, style.skew, Vector2.zero, null);
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
                style.ApplySprite(image, style.fill, style.cyan, style.skew, Vector2.zero, style.plateStripesBold);
                Touch(image);
            }
            Touch(fill);
        }

        if (handle != null)
        {
            handle.sizeDelta = new Vector2(34f, 8f);
            Image image = handle.GetComponent<Image>();
            if (image != null)
            {
                style.ApplySprite(image, style.plate, style.yellow, style.skew, style.shadowOffset, null);
                Touch(image);
            }
            Touch(handle);
        }

        ColorBlock colors = ColorBlock.defaultColorBlock;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 0.82f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(0.85f, 0.85f, 0.7f, 1f);
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
        bool inPlateButton = parentButton != null && parentButton.targetGraphic is Image plate && plate.sprite == style.plate;
        Image backdrop = PanelAncestor(text.transform);
        bool onPaper = (backdrop != null && backdrop.sprite == style.panelPaper) || (parent != null && parent.name == "Key");
        float size = MaxSize(text);
        string name = go.name;
        Color mapped = MapColor(text.color, style.paper);
        bool accent = !Near(mapped, style.paper) && !Near(mapped, style.textDim);
        textCount++;

        if (go.GetComponent<FloatingText>() != null)
        {
            Apply(text, UITextRole.Number, text.color, false);
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
                Apply(text, UITextRole.Heading, accent ? mapped : style.primary, true);
            else
                Apply(text, UITextRole.BodyDark, accent && !Near(mapped, style.yellow) ? mapped : style.ink, false);
            if (parent != null && parent.name == "Key")
                text.fontStyle = FontStyles.Italic | FontStyles.UpperCase;
            Touch(text);
            return;
        }

        if (name == "UnknownText")
        {
            Apply(text, UITextRole.Heading, style.yellow, false);
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
            Apply(text, UITextRole.Heading, accent ? mapped : style.yellow, true);
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
        text.characterSpacing = role == UITextRole.Body || role == UITextRole.BodyDark ? 0f : 3f;
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
            text.margin = new Vector4(margin, 0f, margin, 0f);
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
        Color[] palette = { style.yellow, style.good, style.primary, style.danger, style.cyan, style.paper, style.textDim, style.ink, style.secondary };
        foreach (Color entry in palette)
            if (Near(color, entry)) return WithAlpha(entry, color.a);

        Color.RGBToHSV(color, out float h, out float s, out float v);
        if (s < 0.2f)
            return WithAlpha(v > 0.9f ? fallback : v > 0.55f ? style.textDim : fallback, color.a);
        if (h >= 0.1f && h < 0.2f) return WithAlpha(style.yellow, color.a);
        if (h >= 0.2f && h < 0.45f) return WithAlpha(style.good, color.a);
        if (h >= 0.45f && h < 0.62f) return WithAlpha(style.cyan, color.a);
        if (h >= 0.62f && h < 0.78f) return WithAlpha(style.secondary, color.a);
        if (h >= 0.78f && h < 0.97f) return WithAlpha(style.primary, color.a);
        return WithAlpha(style.danger, color.a);
    }

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
        RectTransform ribbon = GetOrCreateChild(host, StylePrefix + "Ribbon", 0);
        ribbon.anchorMin = ribbon.anchorMax = new Vector2(0.5f, 0.5f);
        ribbon.pivot = new Vector2(0.5f, 0.5f);
        ribbon.anchoredPosition = new Vector2(0f, Snap(titleBounds.center.y - hostRect.center.y));
        ribbon.sizeDelta = new Vector2(Snap(hostRect.width - 50f), Snap(Mathf.Max(titleSize * 1.5f, 110f)));
        Image ribbonImage = GetOrAdd<Image>(ribbon.gameObject);
        ribbonImage.raycastTarget = false;
        style.ApplySprite(ribbonImage, style.plate, gameOver ? style.danger : style.primary, style.skew, style.panelShadowOffset, style.plateStripesBold);
        AccentLayer(ribbonImage, style.yellow, new Vector2(8f, -8f), new Vector2(5f, -5f));
        Touch(ribbonImage);
        Touch(ribbon);

        if (subtitle != null)
        {
            Rect subBounds = RelativeRect(host, subtitle.rectTransform);
            float subSize = MaxSize(subtitle);
            RectTransform sub = GetOrCreateChild(host, StylePrefix + "SubRibbon", 1);
            sub.anchorMin = sub.anchorMax = new Vector2(0.5f, 0.5f);
            sub.pivot = new Vector2(0.5f, 0.5f);
            sub.anchoredPosition = new Vector2(Snap(hostRect.width * 0.05f), Snap(subBounds.center.y - hostRect.center.y));
            sub.sizeDelta = new Vector2(Snap(hostRect.width * 0.66f), Snap(Mathf.Max(subSize * 1.55f, 54f)));
            Image subImage = GetOrAdd<Image>(sub.gameObject);
            subImage.raycastTarget = false;
            style.ApplySprite(subImage, style.plate, style.panel, style.skew, style.shadowOffset, null);
            Touch(subImage);
            Touch(sub);

            subtitle.rectTransform.anchorMin = subtitle.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            subtitle.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            subtitle.rectTransform.anchoredPosition = sub.anchoredPosition;
            subtitle.rectTransform.sizeDelta = sub.sizeDelta - new Vector2(70f, 10f);
            subtitle.margin = Vector4.zero;
            Apply(subtitle, UITextRole.Heading, style.yellow, true);
            subtitle.alignment = TextAlignmentOptions.Center;
            Touch(subtitle.rectTransform);
            Finish(subtitle);
        }

        title.rectTransform.anchorMin = title.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        title.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        title.rectTransform.anchoredPosition = ribbon.anchoredPosition;
        title.rectTransform.sizeDelta = ribbon.sizeDelta - new Vector2(90f, 14f);
        title.margin = Vector4.zero;
        Apply(title, UITextRole.Title, style.paper, true);
        title.alignment = TextAlignmentOptions.Center;
        Touch(title.rectTransform);
        Finish(title);
    }

    private static void AccentLayer(Image image, Color accent, Vector2 accentOffset, Vector2 inkOffset)
    {
        Shadow[] shadows = image.GetComponents<Shadow>();
        Shadow first = shadows.Length > 0 ? shadows[0] : image.gameObject.AddComponent<Shadow>();
        Shadow second = shadows.Length > 1 ? shadows[1] : image.gameObject.AddComponent<Shadow>();

        first.enabled = true;
        first.effectColor = accent;
        first.effectDistance = accentOffset;
        first.useGraphicAlpha = true;
        second.enabled = true;
        second.effectColor = style.ink;
        second.effectDistance = inkOffset;
        second.useGraphicAlpha = true;
        Touch(first);
        Touch(second);
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
        if (go.GetComponent<UIScreenIntro>() != null) return;
        Touch(go.AddComponent<UIScreenIntro>());
        Touch(go);
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
        foreach (Transform child in parent)
            if (child.name == name) return child;
        return null;
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

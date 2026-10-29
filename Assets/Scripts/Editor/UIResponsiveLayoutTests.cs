using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class UIResponsiveLayoutTests
{
    private static readonly Vector2Int[] Resolutions =
    {
        new Vector2Int(1920, 1080),
        new Vector2Int(3440, 1440),
        new Vector2Int(3840, 1080),
        new Vector2Int(1600, 1200),
        new Vector2Int(1920, 1200)
    };

    private static IEnumerable<string> Scenes => UIAuthoringTools.Scenes;
    private static IEnumerable<string> GameplayScenes => UIAuthoringTools.Scenes.Skip(1);
    private static IEnumerable<string> StyledGameplayScenes => UIAuthoringTools.Scenes.Skip(1).Take(2);

    [TearDown]
    public void ClosePreviewScene() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

    [TestCaseSource(nameof(Scenes))]
    public void HeadersAndFramesKeepTheirAlignmentAcrossLanguagesAndAspectRatios(string path)
    {
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        RectTransform[] frames = Components<RectTransform>(scene).Where(rect => rect.name == "ContentFrame").ToArray();
        Assert.IsNotEmpty(frames, path + ": faltan las áreas de contenido de la UI.");
        foreach (RectTransform frame in frames) EnableAncestors(frame);
        RectTransform[] headers = Components<RectTransform>(scene)
            .Where(rect => rect.Find("StyleRibbon") != null && DirectTexts(rect).Any()).ToArray();
        if (path != UIAuthoringTools.Scenes[3]) Assert.IsNotEmpty(headers, path + ": faltan los títulos de las pantallas.");

        using (var viewport = new LayoutViewport(scene))
        {
            foreach (GameLanguage language in new[] { GameLanguage.English, GameLanguage.Spanish })
            {
                foreach (LocalizedText text in Components<LocalizedText>(scene)) text.ApplyLanguage(language);
                foreach (Vector2Int resolution in Resolutions)
                {
                    viewport.Resize(resolution);
                    string location = path + " / " + language + " / " + resolution;
                    foreach (RectTransform frame in frames)
                    {
                        Rect bounds = ScreenBounds(frame, viewport.Camera);
                        AssertInsideViewport(bounds, resolution, location + " / " + HierarchyPath(frame));
                        Assert.That(bounds.center.x, Is.EqualTo(resolution.x * .5f).Within(2f), location + ": el contenido se desplazó horizontalmente.");
                        Assert.That(bounds.center.y, Is.EqualTo(resolution.y * .5f).Within(2f), location + ": el contenido se desplazó verticalmente.");
                        Assert.That(bounds.width / bounds.height, Is.EqualTo(16f / 9f).Within(.005f), location + ": cambió la proporción de la composición.");
                    }
                    foreach (RectTransform header in headers) AssertHeader(header, viewport.Camera, resolution, location);
                }
            }
        }
    }

    [TestCaseSource(nameof(GameplayScenes))]
    public void HudRemainsVisibleAndKeepsItsCornerMarginsAtDifferentAspectRatios(string path)
    {
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        RectTransform minimap = Components<RectTransform>(scene).FirstOrDefault(rect => rect.name == "MinimapRoot");
        CurrencyUI currency = Components<CurrencyUI>(scene).FirstOrDefault();
        PlayerStatsHUD stats = Components<PlayerStatsHUD>(scene).FirstOrDefault();
        Assert.IsNotNull(minimap, path + ": falta el minimapa.");
        Assert.IsNotNull(currency, path + ": falta la información de monedas.");
        Assert.IsNotNull(stats, path + ": faltan las estadísticas del HUD.");
        RectTransform[] targets = { minimap, currency.transform as RectTransform, stats.transform as RectTransform };
        foreach (RectTransform target in targets) EnableAncestors(target);

        using (var viewport = new LayoutViewport(scene))
        {
            var referenceMargins = new Vector2[targets.Length];
            for (int size = 0; size < Resolutions.Length; size++)
            {
                Vector2Int resolution = Resolutions[size];
                viewport.Resize(resolution);
                for (int i = 0; i < targets.Length; i++)
                {
                    RectTransform target = targets[i];
                    string location = path + " / " + resolution + " / " + target.name;
                    Rect bounds = ScreenBounds(target, viewport.Camera);
                    AssertInsideViewport(bounds, resolution, location);
                    float scale = target.GetComponentInParent<Canvas>().rootCanvas.scaleFactor;
                    float edge = i == 1 ? resolution.x - bounds.xMax : bounds.xMin;
                    var margin = new Vector2(edge, resolution.y - bounds.yMax) / scale;
                    if (size == 0) referenceMargins[i] = margin;
                    else
                    {
                        Assert.That(margin.x, Is.EqualTo(referenceMargins[i].x).Within(2f), location + ": cambió el margen horizontal del HUD.");
                        Assert.That(margin.y, Is.EqualTo(referenceMargins[i].y).Within(2f), location + ": cambió el margen vertical del HUD.");
                    }
                    foreach (TMP_Text text in target.GetComponentsInChildren<TMP_Text>())
                    {
                        text.ForceMeshUpdate();
                        Assert.IsFalse(text.isTextOverflowing, location + " / " + text.name + ": el texto del HUD se desborda.");
                    }
                }
            }
        }
    }

    [TestCaseSource(nameof(Scenes))]
    public void LanguageSelectorKeepsItsPlacementAndClearsMenuButtonsAcrossAspectRatios(string path)
    {
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        Transform canvas = scene.GetRootGameObjects().First(root => root.name == "Canvas").transform;
        bool options = path == UIAuthoringTools.Scenes[0];
        Transform panel = UIEditorHierarchy.Find(canvas, options ? "OptionsPanel" : "PausePanel");
        Assert.IsNotNull(panel, path + ": falta la pantalla con selector de idioma.");
        LanguageSelector selector = panel.GetComponentInChildren<LanguageSelector>(true);
        Assert.IsNotNull(selector, path + ": falta el selector de idioma.");
        EnableAncestors(selector.transform);
        var selectorRect = (RectTransform)selector.transform;
        var frame = selectorRect.parent as RectTransform;
        Assert.IsNotNull(frame, path + ": el selector necesita un área de contenido.");
        Vector2 relativeCenter = new Vector2(.67f, .46f);
        Vector2 referenceSize = Vector2.zero;

        using (var viewport = new LayoutViewport(scene))
        {
            for (int size = 0; size < Resolutions.Length; size++)
            {
                Vector2Int resolution = Resolutions[size];
                viewport.Resize(resolution);
                string location = path + " / " + resolution + " / " + selector.name;
                Rect bounds = ScreenBounds(selectorRect, viewport.Camera);
                Rect parentBounds = ScreenBounds(frame, viewport.Camera);
                AssertInsideViewport(bounds, resolution, location);
                Assert.That(bounds.xMin, Is.GreaterThanOrEqualTo(parentBounds.xMin - 2f), location + ": sale del área de contenido por la izquierda.");
                Assert.That(bounds.xMax, Is.LessThanOrEqualTo(parentBounds.xMax + 2f), location + ": sale del área de contenido por la derecha.");
                Assert.That(bounds.yMin, Is.GreaterThanOrEqualTo(parentBounds.yMin - 2f), location + ": sale del área de contenido por abajo.");
                Assert.That(bounds.yMax, Is.LessThanOrEqualTo(parentBounds.yMax + 2f), location + ": sale del área de contenido por arriba.");
                float scale = selector.GetComponentInParent<Canvas>().rootCanvas.scaleFactor;
                if (size == 0)
                {
                    if (!options) relativeCenter = new Vector2((bounds.center.x - parentBounds.xMin) / parentBounds.width, (bounds.center.y - parentBounds.yMin) / parentBounds.height);
                    referenceSize = options ? new Vector2(560f, 112f) : bounds.size / scale;
                }
                var expectedCenter = parentBounds.min + Vector2.Scale(parentBounds.size, relativeCenter);
                Assert.That(bounds.center.x, Is.EqualTo(expectedCenter.x).Within(2f), location + ": cambió la posición horizontal del selector.");
                Assert.That(bounds.center.y, Is.EqualTo(expectedCenter.y).Within(2f), location + ": cambió la posición vertical del selector.");
                Assert.That(bounds.width / scale, Is.EqualTo(referenceSize.x).Within(2f), location + ": cambió el ancho del selector.");
                Assert.That(bounds.height / scale, Is.EqualTo(referenceSize.y).Within(2f), location + ": cambió la altura del selector.");
                foreach (Button button in panel.GetComponentsInChildren<Button>())
                {
                    if (button.transform.IsChildOf(selector.transform)) continue;
                    Rect buttonBounds = ScreenBounds((RectTransform)button.transform, viewport.Camera);
                    float width = Mathf.Min(bounds.xMax, buttonBounds.xMax) - Mathf.Max(bounds.xMin, buttonBounds.xMin);
                    float height = Mathf.Min(bounds.yMax, buttonBounds.yMax) - Mathf.Max(bounds.yMin, buttonBounds.yMin);
                    Assert.IsFalse(width > 2f && height > 2f, location + ": se encima con el botón " + button.name + ".");
                }
            }
        }
    }

    [TestCaseSource(nameof(StyledGameplayScenes))]
    public void OverrideHudTitleRendersEveryLetterInBothLanguagesAtDifferentAspectRatios(string path)
    {
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        OverrideHudPanel hud = Components<OverrideHudPanel>(scene).FirstOrDefault();
        Assert.IsNotNull(hud, path + ": falta el HUD de sobrecargas.");
        EnableAncestors(hud.transform);
        Transform title = UIEditorHierarchy.Find(hud.transform, "Title");
        Assert.IsNotNull(title, path + ": falta el título del HUD de sobrecargas.");
        LocalizedText translation = title.GetComponent<LocalizedText>();
        TMP_Text text = title.GetComponent<TMP_Text>();
        Assert.IsNotNull(translation, path + ": el título del HUD necesita ambos idiomas.");
        Assert.IsNotNull(text, path + ": falta el TMP del título del HUD.");

        using (var viewport = new LayoutViewport(scene))
        {
            foreach (GameLanguage language in new[] { GameLanguage.English, GameLanguage.Spanish })
            {
                translation.ApplyLanguage(language);
                string expected = translation.Content.Get(language);
                foreach (Vector2Int resolution in Resolutions)
                {
                    viewport.Resize(resolution);
                    text.ForceMeshUpdate(true, true);
                    string location = path + " / " + language + " / " + resolution + " / " + HierarchyPath(title);
                    AssertInsideViewport(ScreenBounds(text.rectTransform, viewport.Camera), resolution, location);
                    Assert.IsFalse(text.isTextOverflowing, location + ": el título se desborda.");
                    string rendered = new string(text.textInfo.characterInfo.Take(text.textInfo.characterCount).Where(glyph => glyph.isVisible).Select(glyph => glyph.character).ToArray());
                    Assert.AreEqual(expected.ToUpperInvariant(), rendered.ToUpperInvariant(), location + ": el título está truncado o contiene una elipsis.");
                }
            }
        }
    }

    private static void AssertHeader(RectTransform header, Camera camera, Vector2Int resolution, string location)
    {
        RectTransform ribbon = header.Find("StyleRibbon") as RectTransform;
        RectTransform subRibbon = header.Find("StyleSubRibbon") as RectTransform;
        Rect titleBounds = ScreenBounds(ribbon, camera);
        string label = location + " / " + HierarchyPath(header);
        AssertInsideViewport(titleBounds, resolution, label + " / StyleRibbon");
        if (header.parent != null && header.parent.name == "ContentFrame")
            Assert.That(titleBounds.center.x, Is.EqualTo(ScreenBounds((RectTransform)header.parent, camera).center.x).Within(2f), label + ": el título no está centrado en su pantalla.");
        if (subRibbon != null)
        {
            Rect subtitleBounds = ScreenBounds(subRibbon, camera);
            AssertInsideViewport(subtitleBounds, resolution, label + " / StyleSubRibbon");
            Assert.That(subtitleBounds.center.x, Is.EqualTo(titleBounds.center.x).Within(2f), label + ": el subtítulo está desplazado respecto al título.");
        }
        foreach (TMP_Text text in DirectTexts(header))
        {
            text.ForceMeshUpdate();
            Rect bounds = ScreenBounds(text.rectTransform, camera);
            Assert.That(bounds.center.x, Is.EqualTo(titleBounds.center.x).Within(2f), label + " / " + text.name + ": la caja del texto no está centrada.");
            Assert.IsFalse(text.isTextOverflowing, label + " / " + text.name + ": el texto se desborda.");
            AssertGlyphsCentered(text, camera, titleBounds.center.x, label);
        }
    }

    private static void AssertGlyphsCentered(TMP_Text text, Camera camera, float center, string location)
    {
        float left = float.PositiveInfinity;
        float right = float.NegativeInfinity;
        for (int i = 0; i < text.textInfo.characterCount; i++)
        {
            TMP_CharacterInfo glyph = text.textInfo.characterInfo[i];
            if (!glyph.isVisible) continue;
            left = Mathf.Min(left, RectTransformUtility.WorldToScreenPoint(camera, text.transform.TransformPoint(glyph.bottomLeft)).x);
            right = Mathf.Max(right, RectTransformUtility.WorldToScreenPoint(camera, text.transform.TransformPoint(glyph.topRight)).x);
        }
        if (float.IsPositiveInfinity(left)) return;
        float scale = text.canvas != null ? text.canvas.rootCanvas.scaleFactor : 1f;
        Assert.That((left + right) * .5f, Is.EqualTo(center).Within(Mathf.Max(2f, text.fontSize * scale * .12f)), location + " / " + text.name + ": los glifos del texto no están visualmente centrados.");
    }

    private static IEnumerable<TMP_Text> DirectTexts(Transform parent)
    {
        foreach (Transform child in parent)
        {
            TMP_Text text = child.GetComponent<TMP_Text>();
            if (text != null && !child.name.Contains("Ghost") && !child.name.StartsWith("Style", StringComparison.Ordinal)) yield return text;
        }
    }

    private static IEnumerable<T> Components<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true));

    private static void EnableAncestors(Transform target)
    {
        while (target != null) { target.gameObject.SetActive(true); target = target.parent; }
    }

    private static string HierarchyPath(Transform target) => target.parent == null ? target.name : HierarchyPath(target.parent) + "/" + target.name;

    private static Rect ScreenBounds(RectTransform rect, Camera camera)
    {
        var corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        Vector2 minimum = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 maximum = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        foreach (Vector3 corner in corners)
        {
            Vector2 point = RectTransformUtility.WorldToScreenPoint(camera, corner);
            minimum = Vector2.Min(minimum, point);
            maximum = Vector2.Max(maximum, point);
        }
        return Rect.MinMaxRect(minimum.x, minimum.y, maximum.x, maximum.y);
    }

    private static void AssertInsideViewport(Rect bounds, Vector2Int resolution, string location)
    {
        Assert.That(bounds.width, Is.GreaterThan(0f), location + ": ancho nulo.");
        Assert.That(bounds.height, Is.GreaterThan(0f), location + ": altura nula.");
        Assert.That(bounds.xMin, Is.GreaterThanOrEqualTo(-2f), location + ": sale por la izquierda.");
        Assert.That(bounds.yMin, Is.GreaterThanOrEqualTo(-2f), location + ": sale por abajo.");
        Assert.That(bounds.xMax, Is.LessThanOrEqualTo(resolution.x + 2f), location + ": sale por la derecha.");
        Assert.That(bounds.yMax, Is.LessThanOrEqualTo(resolution.y + 2f), location + ": sale por arriba.");
    }

    private sealed class LayoutViewport : IDisposable
    {
        private static readonly MethodInfo UpdateFitter = typeof(AspectRatioFitter).GetMethod("OnRectTransformDimensionsChange", BindingFlags.Instance | BindingFlags.NonPublic);
        private readonly CanvasState[] canvases;
        private readonly GameObject cameraObject;
        private RenderTexture texture;
        public Camera Camera { get; }

        public LayoutViewport(Scene scene)
        {
            canvases = Components<Canvas>(scene).Where(canvas => canvas.isRootCanvas && canvas.renderMode != RenderMode.WorldSpace).Select(canvas => new CanvasState(canvas)).ToArray();
            cameraObject = new GameObject("UI responsive test camera") { hideFlags = HideFlags.HideAndDontSave };
            cameraObject.transform.position = new Vector3(12000f, -20000f, 7000f);
            Camera = cameraObject.AddComponent<Camera>();
            Camera.orthographic = true;
            Camera.clearFlags = CameraClearFlags.SolidColor;
            Camera.nearClipPlane = .1f;
            Camera.farClipPlane = 200f;
            Camera.cullingMask = 1 << 5;
            var data = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = false;
            data.renderShadows = false;
            foreach (CanvasState state in canvases)
            {
                state.Canvas.renderMode = RenderMode.ScreenSpaceCamera;
                state.Canvas.worldCamera = Camera;
                state.Canvas.planeDistance = 50f;
            }
        }

        public void Resize(Vector2Int resolution)
        {
            ReleaseTexture();
            texture = new RenderTexture(resolution.x, resolution.y, 24, RenderTextureFormat.ARGB32);
            Camera.targetTexture = texture;
            for (int pass = 0; pass < 3; pass++)
            {
                Canvas.ForceUpdateCanvases();
                foreach (CanvasState state in canvases)
                {
                    if (!state.Canvas.isActiveAndEnabled) continue;
                    LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)state.Canvas.transform);
                    foreach (AspectRatioFitter fitter in state.Canvas.GetComponentsInChildren<AspectRatioFitter>())
                        if (fitter.isActiveAndEnabled) UpdateFitter.Invoke(fitter, null);
                }
            }
            Canvas.ForceUpdateCanvases();
            Camera.Render();
            Canvas.ForceUpdateCanvases();
        }

        public void Dispose()
        {
            foreach (CanvasState state in canvases) state.Restore();
            ReleaseTexture();
            Object.DestroyImmediate(cameraObject);
        }

        private void ReleaseTexture()
        {
            if (texture == null) return;
            Camera.targetTexture = null;
            texture.Release();
            Object.DestroyImmediate(texture);
            texture = null;
        }
    }

    private readonly struct CanvasState
    {
        public readonly Canvas Canvas;
        private readonly RenderMode mode;
        private readonly Camera camera;
        private readonly float distance;

        public CanvasState(Canvas canvas) { Canvas = canvas; mode = canvas.renderMode; camera = canvas.worldCamera; distance = canvas.planeDistance; }
        public void Restore() { if (Canvas == null) return; Canvas.renderMode = mode; Canvas.worldCamera = camera; Canvas.planeDistance = distance; }
    }
}

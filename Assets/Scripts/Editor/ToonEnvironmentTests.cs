using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class ToonEnvironmentTests
{
    private const int Size = 512;

    [Test]
    public void ToonShaderCompilesWithTheExpectedPasses()
    {
        Shader shader = Shader.Find(ToonEnvironmentStyle.ShaderName);
        Assert.IsNotNull(shader, "El shader toon de entorno no está en el proyecto.");
        Assert.IsFalse(ShaderUtil.ShaderHasError(shader), "El shader toon tiene errores de compilación.");
        var lightModes = new HashSet<string>();
        var tag = new ShaderTagId("LightMode");
        for (int i = 0; i < shader.passCount; i++) lightModes.Add(shader.FindPassTagValue(i, tag).name.ToUpperInvariant());
        CollectionAssert.IsSubsetOf(new[] { "UNIVERSALFORWARD", "TOONOUTLINE", "SHADOWCASTER", "DEPTHONLY", "DEPTHNORMALS" }, lightModes);
    }

    [Test]
    public void ToonShaderIsSrpBatcherCompatible()
    {
        Shader shader = Shader.Find(ToonEnvironmentStyle.ShaderName);
        bool? compatible = ToonEnvironmentTools.IsSrpBatcherCompatible(shader);
        if (compatible == null) Assert.Ignore("Esta versión del editor no expone la comprobación del SRP Batcher.");
        Assert.IsTrue(compatible.Value, "Todas las propiedades del material deben estar en el CBUFFER UnityPerMaterial de todas las pasadas.");
    }

    [Test]
    public void EcoDisablesOutlinesAndOtherPresetsKeepThem()
    {
        Assert.IsFalse(GameGraphicsSettings.CreatePreset(GameGraphicsSettings.GraphicsPreset.Eco).outlines);
        Assert.IsTrue(GameGraphicsSettings.CreatePreset(GameGraphicsSettings.GraphicsPreset.Balanced).outlines);
        Assert.IsTrue(GameGraphicsSettings.CreatePreset(GameGraphicsSettings.GraphicsPreset.High).outlines);
        var saved = new GameGraphicsSettings.SettingsData { outlines = false };
        Assert.IsFalse(GameGraphicsSettings.Sanitize(saved).outlines);
        var legacyJson = JsonUtility.FromJson<GameGraphicsSettings.SettingsData>("{\"frameRate\":60}");
        Assert.IsTrue(legacyJson.outlines, "Las preferencias guardadas antes de este cambio deben conservar los contornos activos.");
    }

    [Test]
    public void OutlineToggleDisablesThePassOnManagedMaterialsAndRestoresIt()
    {
        var material = new Material(Shader.Find(ToonEnvironmentStyle.ShaderName));
        var style = ScriptableObject.CreateInstance<ToonEnvironmentStyle>();
        style.environmentMaterials.Add(material);
        style.environmentMaterials.Add(null);
        try
        {
            ToonEnvironmentStyle.ApplyOutlines(false, style);
            Assert.IsFalse(material.GetShaderPassEnabled(ToonEnvironmentStyle.OutlinePassLightMode));
            Assert.AreEqual(1f, Shader.GetGlobalFloat(ToonEnvironmentStyle.OutlineDisabledId));
            ToonEnvironmentStyle.ApplyOutlines(true, style);
            Assert.IsTrue(material.GetShaderPassEnabled(ToonEnvironmentStyle.OutlinePassLightMode));
            Assert.AreEqual(0f, Shader.GetGlobalFloat(ToonEnvironmentStyle.OutlineDisabledId));
        }
        finally
        {
            ToonEnvironmentStyle.ApplyOutlines(true, null);
            Object.DestroyImmediate(style);
            Object.DestroyImmediate(material);
        }
    }

    [Test]
    public void RenderedCubeShowsTwoTonesOutlineEmissionAndFade()
    {
        bool previousFog = RenderSettings.fog;
        Light previousSun = RenderSettings.sun;
        bool previousAsync = ShaderUtil.allowAsyncCompilation;
        ShaderUtil.allowAsyncCompilation = false;
        RenderSettings.fog = false;
        Shader toonShader = Shader.Find(ToonEnvironmentStyle.ShaderName);
        WarmUpVariants(toonShader);
        var root = new GameObject("Toon render test");
        var material = new Material(toonShader);
        var white = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        white.SetPixels32(new[] { new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255) });
        white.Apply();
        var target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32);
        var readback = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        try
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(root.transform);
            cube.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
            var renderer = cube.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;

            var lightObject = new GameObject("Toon test light");
            lightObject.transform.SetParent(root.transform);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = Color.white;
            light.intensity = 1f;
            light.shadows = LightShadows.None;
            light.transform.rotation = Quaternion.LookRotation(Vector3.left);
            RenderSettings.sun = light;
            foreach (Light other in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (other != light && other.type == LightType.Directional) other.intensity = 0f;

            var cameraObject = new GameObject("Toon test camera");
            cameraObject.transform.SetParent(root.transform);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 2f;
            camera.aspect = 1f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 50f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.magenta;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.useOcclusionCulling = false;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.transform.rotation = Quaternion.identity;
            camera.targetTexture = target;

            material.SetTexture(ToonEnvironmentStyle.BaseMapId, white);
            material.SetColor(ToonEnvironmentStyle.BaseColorId, Color.white);
            material.SetColor(ToonEnvironmentStyle.ShadeColorId, new Color(0.5f, 0.5f, 0.5f, 1f));
            material.SetFloat(ToonEnvironmentStyle.ShadeThresholdId, 0.5f);
            material.SetFloat(ToonEnvironmentStyle.ShadeSoftnessId, 0.01f);
            material.SetFloat(ToonEnvironmentStyle.LightColorInfluenceId, 0f);
            material.SetColor(ToonEnvironmentStyle.OutlineColorId, Color.black);
            material.SetFloat(ToonEnvironmentStyle.OutlineWidthId, 8f);
            material.EnableKeyword("_RECEIVE_SHADOWS_OFF");
            material.DisableKeyword("_EMISSION");
            ToonEnvironmentStyle.ApplyOutlines(true, null);

            Color[] pixels = Render(camera, target, readback);
            Color shade = Sample(pixels, 0.42f, 0.5f);
            Color lit = Sample(pixels, 0.58f, 0.5f);
            Color background = Sample(pixels, 0.9f, 0.9f);
            Assert.Greater(Luminance(lit), 0.9f, "La cara que mira a la luz debe mostrar la textura sin oscurecer.");
            Assert.That(Luminance(shade), Is.EqualTo(0.5f).Within(0.08f), "La cara opuesta debe mostrar el color de sombra plano.");
            Assert.Greater(CountDarkPixels(pixels, 0.66f, 0.74f, 0.5f), 0, "Debe haber contorno negro justo fuera de la silueta.");
            Assert.Greater(background.r, 0.9f, "El fondo debe seguir siendo magenta.");
            Assert.Greater(background.b, 0.9f, "El fondo debe seguir siendo magenta.");

            ToonEnvironmentStyle.ApplyOutlines(false, null);
            pixels = Render(camera, target, readback);
            Assert.AreEqual(0, CountDarkPixels(pixels, 0.66f, 0.74f, 0.5f), "Con los contornos apagados la silueta no debe tener línea.");
            ToonEnvironmentStyle.ApplyOutlines(true, null);

            material.EnableKeyword("_EMISSION");
            material.SetTexture(ToonEnvironmentStyle.EmissionMapId, white);
            material.SetColor(ToonEnvironmentStyle.EmissionColorId, new Color(0f, 2f, 0f, 1f));
            Render(camera, target, readback);
            pixels = Render(camera, target, readback);
            Color emissive = Sample(pixels, 0.42f, 0.5f);
            Assert.Greater(emissive.g, 0.9f, "La emisión debe iluminar incluso la cara en sombra.");
            Assert.Greater(emissive.g - emissive.r, 0.3f, "La emisión debe aportar su propio color.");
            material.DisableKeyword("_EMISSION");

            material.SetFloat("_Surface", 1f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.renderQueue = 3000;
            material.SetColor(ToonEnvironmentStyle.BaseColorId, new Color(1f, 1f, 1f, 0.25f));
            pixels = Render(camera, target, readback);
            Color faded = Sample(pixels, 0.58f, 0.5f);
            float expectedGreen = Mathf.LinearToGammaSpace(0.25f);
            Assert.That(faded.g, Is.EqualTo(expectedGreen).Within(0.08f), "El edificio desvanecido debe mezclarse al 25 % con el fondo magenta (el BuildingFader usa _Surface y _BaseColor.a).");
            Assert.That(faded.r, Is.GreaterThan(0.9f));
        }
        finally
        {
            ShaderUtil.allowAsyncCompilation = previousAsync;
            RenderSettings.fog = previousFog;
            RenderSettings.sun = previousSun;
            ToonEnvironmentStyle.ApplyOutlines(true, null);
            RenderTexture.active = null;
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(material);
            Object.DestroyImmediate(white);
            Object.DestroyImmediate(readback);
            target.Release();
            Object.DestroyImmediate(target);
        }
    }

    private static void WarmUpVariants(Shader shader)
    {
        var collection = new ShaderVariantCollection();
        try
        {
            collection.Add(new ShaderVariantCollection.ShaderVariant(shader, PassType.ScriptableRenderPipeline, "_RECEIVE_SHADOWS_OFF"));
            collection.Add(new ShaderVariantCollection.ShaderVariant(shader, PassType.ScriptableRenderPipeline, "_RECEIVE_SHADOWS_OFF", "_EMISSION"));
            collection.Add(new ShaderVariantCollection.ShaderVariant(shader, PassType.ScriptableRenderPipeline));
            collection.WarmUp();
        }
        finally
        {
            Object.DestroyImmediate(collection);
        }
    }

    private static Color[] Render(Camera camera, RenderTexture target, Texture2D readback)
    {
        camera.Render();
        RenderTexture.active = target;
        readback.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
        readback.Apply();
        RenderTexture.active = null;
        return readback.GetPixels();
    }

    private static Color Sample(Color[] pixels, float x, float y)
    {
        int px = Mathf.Clamp(Mathf.RoundToInt(x * (Size - 1)), 0, Size - 1);
        int py = Mathf.Clamp(Mathf.RoundToInt(y * (Size - 1)), 0, Size - 1);
        return pixels[py * Size + px];
    }

    private static int CountDarkPixels(Color[] pixels, float xStart, float xEnd, float y)
    {
        int row = Mathf.Clamp(Mathf.RoundToInt(y * (Size - 1)), 0, Size - 1);
        int from = Mathf.Clamp(Mathf.RoundToInt(xStart * (Size - 1)), 0, Size - 1);
        int to = Mathf.Clamp(Mathf.RoundToInt(xEnd * (Size - 1)), 0, Size - 1);
        int count = 0;
        for (int x = from; x <= to; x++)
            if (Luminance(pixels[row * Size + x]) < 0.15f) count++;
        return count;
    }

    private static float Luminance(Color color) => 0.299f * color.r + 0.587f * color.g + 0.114f * color.b;
}

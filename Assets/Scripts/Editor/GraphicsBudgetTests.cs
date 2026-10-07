using NUnit.Framework;
using System.Reflection;
using UnityEngine;

public class GraphicsBudgetTests
{
    [TestCase(60, 60, 1)]
    [TestCase(120, 60, 2)]
    [TestCase(144, 60, 3)]
    [TestCase(165, 60, 3)]
    [TestCase(240, 60, 4)]
    [TestCase(360, 60, 0)]
    [TestCase(59.94, 60, 1)]
    [TestCase(119.88, 60, 2)]
    [TestCase(120, 30, 4)]
    [TestCase(120, 45, 3)]
    [TestCase(0, 60, 0)]
    [TestCase(double.NaN, 60, 0)]
    [TestCase(double.PositiveInfinity, 60, 0)]
    public void VSyncNeverExceedsTheSelectedBudget(double hz, int fps, int divisor)
    {
        int actual = GameGraphicsSettings.CalculateVSyncCount(hz, fps);
        Assert.AreEqual(divisor, actual);
        if (actual != 0) Assert.LessOrEqual(hz / actual, fps);
    }

    [TestCase(3024, 1964, 1920, 1080)]
    [TestCase(3456, 2234, 1920, 1080)]
    [TestCase(3840, 2160, 1920, 1080)]
    [TestCase(3440, 1440, 1920, 1080)]
    [TestCase(1366, 768, 1365, 768)]
    [TestCase(0, 0, 1920, 1080)]
    public void FullHdIsTheDefaultOutput(int width, int height, int expectedWidth, int expectedHeight)
    {
        Assert.AreEqual(new Vector2Int(expectedWidth, expectedHeight), GameGraphicsSettings.CalculateResolution(width, height, 1080));
    }

    [TestCase(1920, 1080, 1f)]
    [TestCase(3840, 2160, .5f)]
    [TestCase(7680, 4320, .25f)]
    [TestCase(1280, 720, 1f)]
    public void LargeGameViewsDoNotSilentlySupersample(int width, int height, float expected)
    {
        Assert.That(GameGraphicsSettings.CalculateRenderScale(width, height, 1080, 1), Is.EqualTo(expected).Within(.00001f));
        Assert.That(GameGraphicsSettings.CalculateRenderScale(width, height, 1080, .75f), Is.EqualTo(expected * .75f).Within(.00001f));
    }

    [Test]
    public void RetinaAspectFitsBothAxesAndNativeIsExplicit()
    {
        float scale = GameGraphicsSettings.CalculateRenderScale(3024, 1964, 1080, 1);
        Assert.LessOrEqual(3024 * scale, 1920.01f);
        Assert.LessOrEqual(1964 * scale, 1080.01f);
        Assert.AreEqual(1, GameGraphicsSettings.CalculateRenderScale(3024, 1964, 0, 1));
        Assert.AreEqual(new Vector2Int(3024, 1964), GameGraphicsSettings.CalculateResolution(3024, 1964, 0));
    }

    [Test]
    public void FlipbooksShareAuthoredMaterialsAndReleaseOnlyTheirOverrides()
    {
        var texture = new Texture2D(2, 2);
        var replacement = new Texture2D(2, 2);
        var source = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        source.mainTexture = texture;
        Material owned = null;
        try
        {
            for (int i = 0; i < 1000; i++)
            {
                Assert.AreSame(source, FlipbookMaterialUtility.Resolve(source, texture, true, out owned));
                Assert.IsNull(owned);
            }
            var variant = FlipbookMaterialUtility.Resolve(source, replacement, true, out owned);
            Assert.AreSame(variant, owned);
            Assert.AreNotSame(source, variant);
            Assert.AreSame(texture, source.mainTexture);
            Assert.AreSame(replacement, variant.mainTexture);
            FlipbookMaterialUtility.Release(ref owned);
            Assert.IsTrue(variant == null);
            Assert.IsTrue(source != null);
            Assert.IsNotNull(FlipbookMaterialUtility.Resolve(null, texture, false, out owned));
            FlipbookMaterialUtility.Release(ref owned);
            Assert.IsNull(owned);
        }
        finally
        {
            FlipbookMaterialUtility.Release(ref owned);
            Object.DestroyImmediate(source);
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(replacement);
        }
    }

    [Test]
    public void OneBuildingSharesItsFadeMaterialAcrossMeshPieces()
    {
        var root = new GameObject("Fade material regression");
        root.SetActive(false);
        var source = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        var first = new GameObject("First piece").AddComponent<MeshRenderer>();
        var second = new GameObject("Second piece").AddComponent<MeshRenderer>();
        first.transform.SetParent(root.transform);
        second.transform.SetParent(root.transform);
        first.sharedMaterial = second.sharedMaterial = source;
        Material owned = null;
        var fader = root.AddComponent<BuildingFader>();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        try
        {
            typeof(BuildingFader).GetMethod("Awake", flags).Invoke(fader, null);
            fader.SetOccluded(true);
            fader.Tick(1, 4, .3f);
            owned = first.sharedMaterial;
            Assert.AreNotSame(source, owned);
            Assert.AreSame(owned, second.sharedMaterial);
            Assert.That(owned.color.a, Is.EqualTo(.3f).Within(.001f));
            Assert.AreEqual(1, source.color.a);
            fader.SetOccluded(false);
            fader.Tick(1, 4, .3f);
            Assert.AreSame(source, first.sharedMaterial);
            Assert.AreSame(source, second.sharedMaterial);
        }
        finally
        {
            // Inactive EditMode fixtures do not receive native OnDestroy.
            typeof(BuildingFader).GetMethod("OnDestroy", flags).Invoke(fader, null);
            Object.DestroyImmediate(root);
        }
        Assert.IsTrue(owned == null);
        Assert.IsTrue(source != null);
        Object.DestroyImmediate(source);
    }
}

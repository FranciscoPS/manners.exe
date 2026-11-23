using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public class ProductionSceneTests
{
    private SceneSetup[] previousSetup;

    [SetUp]
    public void RememberScenes()
    {
        previousSetup = EditorSceneManager.GetSceneManagerSetup();
    }

    [TearDown]
    public void RestoreScenes()
    {
        bool restorable = previousSetup.Length > 0 && previousSetup.All(item => !string.IsNullOrEmpty(item.path)) && previousSetup.Count(item => item.isActive) == 1;
        if (restorable) EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
        else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    [Test]
    public void LevelOneRunsTheHelicopterCameraAndNewsWithProductionAssets()
    {
        EditorSceneManager.OpenScene(HelicopterBroadcastTools.ProductionScenePath, OpenSceneMode.Single);

        HelicopterCameraExtension camera = Object.FindFirstObjectByType<HelicopterCameraExtension>(FindObjectsInactive.Include);
        NewsBroadcastOverlay overlay = Object.FindFirstObjectByType<NewsBroadcastOverlay>(FindObjectsInactive.Include);

        Assert.IsNotNull(camera, "LEVEL 1 necesita la cámara de helicóptero en la cámara del jugador.");
        Assert.IsNotNull(overlay, "LEVEL 1 necesita el rótulo de noticias en el HUD.");
        Assert.AreEqual(HelicopterBroadcastTools.ProductionCameraConfigPath, AssetDatabase.GetAssetPath(camera.Config));
        var cameraFields = new SerializedObject(camera);
        Volume lens = cameraFields.FindProperty("lensVolume").objectReferenceValue as Volume;
        Assert.IsNotNull(lens, "La cámara de helicóptero necesita su Volume de lente.");
        Assert.AreEqual(HelicopterBroadcastTools.ProductionLensProfilePath, AssetDatabase.GetAssetPath(lens.sharedProfile));
        Assert.AreEqual(HelicopterBroadcastTools.ProductionLensProfilePath, AssetDatabase.GetAssetPath(camera.Config.lensProfile));
        Object news = new SerializedObject(overlay).FindProperty("config").objectReferenceValue;
        Assert.AreEqual(HelicopterBroadcastTools.ProductionNewsConfigPath, AssetDatabase.GetAssetPath(news));
    }

    [Test]
    public void LevelOneNeverReferencesSandboxAssets()
    {
        string[] dependencies = AssetDatabase.GetDependencies(HelicopterBroadcastTools.ProductionScenePath, false);

        string[] sandbox = dependencies.Where(path => path.StartsWith(GameAssetPaths.Sandbox + "/")).ToArray();

        Assert.IsEmpty(sandbox, "LEVEL 1 apunta a assets del sandbox: " + string.Join(", ", sandbox));
    }

    [Test]
    public void SandboxSyncPairsTheHelicopterCameraNewsAndLens()
    {
        SandboxDiffTool.DiffResult result = SandboxDiffTool.Compare(false);

        string[] labels = result.pairs.Select(pair => pair.label).ToArray();

        CollectionAssert.Contains(labels, "HelicopterCameraConfig");
        CollectionAssert.Contains(labels, "NewsBroadcastConfig");
        Assert.IsTrue(labels.Any(label => label.StartsWith("HelicopterLens/")), "La sincronización debe comparar los efectos de la lente.");
    }
}

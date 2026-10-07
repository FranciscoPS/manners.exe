using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class PerformanceSceneSmokeTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    [Serializable] private class Snapshot
    {
        public string scene, scenario, gpu, api;
        public int renderers, materials, activeRigidbodies, dynamicRigidbodies, enemies;
        public int updates, fixedUpdates, frameTarget, vSync;
        public int width, height, playerFrames;
        public double elapsedSeconds;
        public float renderScale;
    }

    [UnityTest]
    public IEnumerator BuildScenesKeepTheirGraphicsBudgetAndMinimapStopsInPause()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
        var snapshots = new List<Snapshot>();
        // Do not alter saved display preferences in a smoke test.
        var settingsType = typeof(GameGraphicsSettings);
        var original = GameGraphicsSettings.Current;
        var currentField = settingsType.GetField("current", BindingFlags.Static | BindingFlags.NonPublic);
        var renderMethod = settingsType.GetMethod("ApplyRendering", BindingFlags.Static | BindingFlags.NonPublic);
        currentField.SetValue(null, GameGraphicsSettings.CreatePreset(GameGraphicsSettings.GraphicsPreset.Balanced));
        renderMethod.Invoke(null, null);
        try
        {
            foreach (string scene in new[] { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/Final Levels/LEVEL 1/LEVEL 1.unity" })
            {
                yield return SceneManager.LoadSceneAsync(scene);
                yield return WaitRealtime(3);
                var player = Object.FindFirstObjectByType<PlayerHealth>();
                if (player != null) player.SetInvulnerable(true);
                if (scene.Contains("LEVEL 1"))
                {
                    // Initial tutorial can legitimately pause time. Dismiss it
                    // for this automated simulation without changing preferences.
                    var tutorial = Object.FindFirstObjectByType<TutorialManager>();
                    if (tutorial != null) tutorial.gameObject.SetActive(false);
                    Time.timeScale = 1;
                }
                yield return WaitRealtime(2);
                // The batch runner has no focused window. Simulate the focus
                // callback to verify 60/30, then explicitly test background 15.
                settingsType.GetMethod("SetFocus", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { true });
                var pipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
                Assert.IsNotNull(pipeline);
                Assert.AreEqual(scene.Contains("MainMenu") ? 30 : 60, Application.targetFrameRate);
                Assert.AreEqual(0, QualitySettings.vSyncCount);
                Assert.LessOrEqual(Screen.width * pipeline.renderScale, 1920.5f);
                Assert.LessOrEqual(Screen.height * pipeline.renderScale, 1080.5f);
                var main = Camera.main;
                Assert.IsNotNull(main);
                var data = main.GetUniversalAdditionalCameraData();
                int rendererIndex = (int)typeof(UniversalAdditionalCameraData).GetField("m_RendererIndex", Private).GetValue(data);
                Assert.AreEqual("PC_NoAO_Renderer", pipeline.rendererDataList[rendererIndex].name);

                var snapshot = Capture();
                snapshot.scenario = "initial";
                int startFrame = Time.frameCount;
                double start = Time.realtimeSinceStartupAsDouble;
                yield return WaitRealtime(5);
                snapshot.playerFrames = Time.frameCount - startFrame;
                snapshot.elapsedSeconds = Time.realtimeSinceStartupAsDouble - start;
                snapshots.Add(snapshot);

                if (scene.Contains("LEVEL 1"))
                {
                    var minimap = Object.FindFirstObjectByType<MinimapSystem>();
                    Assert.IsNotNull(minimap);
                    var camera = (Camera)typeof(MinimapSystem).GetField("minimapCamera", Private).GetValue(minimap);
                    Assert.IsNotNull(camera);
                    var minimapData = camera.GetUniversalAdditionalCameraData();
                    Assert.IsFalse(minimapData.renderShadows);
                    Assert.IsFalse(minimapData.renderPostProcessing);
                    Time.timeScale = 0;
                    yield return WaitRealtime(.2);
                    Assert.IsFalse(camera.enabled, "Minimap must stop rendering when paused.");
                    Assert.LessOrEqual(Application.targetFrameRate, 30);
                    Time.timeScale = 1;
                    var spawner = Object.FindFirstObjectByType<EnemySpawnManager>();
                    if (spawner != null) spawner.StopAllCoroutines();
                    var point = Object.FindFirstObjectByType<SpawnPoint>();
                    Assert.IsNotNull(point);
                    var configuration = Object.Instantiate(AssetDatabase.LoadAssetAtPath<EnemyConfiguration>(
                        AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:EnemyConfiguration")[0])));
                    configuration.maxHealth = 1000000;
                    try
                    {
                        for (int i = 0; i < 18; i++)
                        {
                            point.ForceSpawn(25, configuration);
                            yield return WaitRealtime(.05);
                        }
                        yield return WaitRealtime(2);
                        snapshot = Capture();
                        snapshot.scenario = "450 extra enemies at one spawn point";
                        Assert.GreaterOrEqual(snapshot.enemies, 450);
                        startFrame = Time.frameCount;
                        start = Time.realtimeSinceStartupAsDouble;
                        yield return WaitRealtime(5);
                        snapshot.playerFrames = Time.frameCount - startFrame;
                        snapshot.elapsedSeconds = Time.realtimeSinceStartupAsDouble - start;
                        snapshots.Add(snapshot);
                    }
                    finally { Object.Destroy(configuration); }
                    // Exercise the corrected fade material lifetime on real meshes.
                    var building = Object.FindFirstObjectByType<BuildingsScript>();
                    if (building != null) building.DestroyByHit(building.transform.position - Vector3.forward);
                    yield return WaitRealtime(3);
                    settingsType.GetMethod("SetFocus", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { false });
                    Assert.AreEqual(15, Application.targetFrameRate);
                }
            }
            Directory.CreateDirectory("Logs/PerformanceReview");
            File.WriteAllText("Logs/PerformanceReview/scene-smoke.json", "[" + string.Join(",", snapshots.Select(s => JsonUtility.ToJson(s, true))) + "]");
        }
        finally
        {
            Time.timeScale = 1;
            currentField.SetValue(null, original);
            renderMethod.Invoke(null, null);
        }
        yield return new ExitPlayMode();
    }

    [UnityTearDown]
    public IEnumerator CleanupPlayMode()
    {
        if (EditorApplication.isPlaying) yield return new ExitPlayMode();
    }

    private static IEnumerator WaitRealtime(double seconds)
    {
        double until = Time.realtimeSinceStartupAsDouble + seconds;
        while (Time.realtimeSinceStartupAsDouble < until) yield return null;
    }

    private static Snapshot Capture()
    {
        var renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r => r.enabled).ToArray();
        var bodies = Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None);
        return new Snapshot
        {
            scene = SceneManager.GetActiveScene().name, gpu = SystemInfo.graphicsDeviceName, api = SystemInfo.graphicsDeviceType.ToString(),
            renderers = renderers.Length, materials = renderers.SelectMany(r => r.sharedMaterials).Where(m => m != null).Distinct().Count(),
            activeRigidbodies = bodies.Length, dynamicRigidbodies = bodies.Count(r => !r.isKinematic), enemies = EnemyHealth.ActiveEnemyCount,
            updates = UpdateManager.Instance.GetUpdateableCount(), fixedUpdates = UpdateManager.Instance.GetFixedUpdateableCount(),
            frameTarget = Application.targetFrameRate, vSync = QualitySettings.vSyncCount,
            width = Screen.width, height = Screen.height,
            renderScale = ((UniversalRenderPipelineAsset)UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline).renderScale
        };
    }
}

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>
/// Native edit-mode regression checks for the performance fixes. Temporary,
/// inactive objects stay in a preview scene; gameplay singletons are never used.
/// Run from PerformanceValidation, without loading or saving a gameplay scene.
/// </summary>
public static class PerformanceRegressionChecks
{
    private const BindingFlags InstancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;

    public static List<string> Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Run performance regression checks outside Play Mode.");

        var passed = new List<string>();
        Scene preview = EditorSceneManager.NewPreviewScene();
        try
        {
            CheckGrid(preview, passed);
            CheckUpdateManager(preview, passed);
            CheckPooledMaterial(preview, passed);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(preview);
        }
        return passed;
    }

    private static GameObject CreateInactiveObject(Scene preview, string name)
    {
        var gameObject = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave };
        gameObject.SetActive(false);
        SceneManager.MoveGameObjectToScene(gameObject, preview);
        return gameObject;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("Performance regression: " + message);
    }

    private static void CheckGrid(Scene preview, List<string> passed)
    {
        int activeEnemiesBefore = EnemyHealth.ActiveEnemyCount;
        int registeredEnemiesBefore = EnemyHealth.ActiveEnemies.Count;
        var enemies = new List<EnemyHealth>();
        var random = new System.Random(39458);
        for (int i = 0; i < 256; i++)
        {
            GameObject gameObject = CreateInactiveObject(preview, "Grid check enemy " + i);
            gameObject.transform.position = new Vector3(
                (float)(random.NextDouble() * 40 - 20),
                (float)(random.NextDouble() * 4 - 2),
                (float)(random.NextDouble() * 40 - 20));
            enemies.Add(gameObject.AddComponent<EnemyHealth>());
        }
        enemies.Insert(13, null);
        // A native destroyed component accessed through a managed reference must
        // also be ignored, rather than attempting to read its Transform.
        EnemyHealth destroyedEnemy = CreateInactiveObject(preview, "Destroyed grid entry").AddComponent<EnemyHealth>();
        Object.DestroyImmediate(destroyedEnemy);
        enemies.Insert(49, destroyedEnemy);

        var grid = new EnemyProximityGrid();
        var expected = new List<EnemyHealth>();
        var actual = new List<EnemyHealth>();
        var actualSet = new HashSet<EnemyHealth>();
        foreach (float cellSize in new[] { 0.5f, 1.1f, 2.5f, 8f })
        {
            grid.Build(enemies, cellSize);
            for (int query = 0; query < 60; query++)
            {
                Vector3 center = new Vector3(
                    (float)(random.NextDouble() * 40 - 20), 0f,
                    (float)(random.NextDouble() * 40 - 20));
                float radius = query == 0 ? 0f : (float)(random.NextDouble() * 9);
                expected.Clear();
                foreach (EnemyHealth enemy in enemies)
                    if (enemy != null && (enemy.transform.position - center).sqrMagnitude <= radius * radius)
                        expected.Add(enemy);

                actual.Clear();
                grid.CollectWithin(center, radius, actual, true);
                Require(actual.Count == expected.Count, "ordered grid count differs from brute force");
                for (int i = 0; i < expected.Count; i++)
                    Require(actual[i] == expected[i], "ordered grid changes scatter candidate order");

                actual.Clear();
                grid.CollectWithin(center, radius, actual);
                actualSet.Clear();
                foreach (EnemyHealth enemy in actual) actualSet.Add(enemy);
                Require(actual.Count == expected.Count && actualSet.SetEquals(expected),
                    "unordered grid changes EMP membership or duplicates a neighbor");
            }
        }

        var subset = new List<EnemyHealth> { enemies[0], enemies[1] };
        grid.Build(subset, 2.5f);
        actual.Clear();
        grid.CollectWithin(Vector3.zero, 100f, actual, true);
        Require(actual.Count == 2 && actual[0] == subset[0] && actual[1] == subset[1],
            "grid rebuild retains old entries");
        Require(EnemyHealth.ActiveEnemyCount == activeEnemiesBefore &&
            EnemyHealth.ActiveEnemies.Count == registeredEnemiesBefore,
            "inactive grid fixtures affected the gameplay enemy registry");
        passed.Add("Native grid: 240 ordered/unordered queries match brute force, negative cells, destroyed/null entries and rebuild");
    }

    private sealed class Tick : IUpdateable, IFixedUpdateable, ILateUpdateable
    {
        public Action action;
        public int calls;
        public bool IsActive => true;
        private void Invoke() { calls++; action?.Invoke(); }
        public void OnUpdate(float deltaTime) => Invoke();
        public void OnFixedUpdate(float deltaTime) => Invoke();
        public void OnLateUpdate(float deltaTime) => Invoke();
    }

    private static void CheckUpdateManager(Scene preview, List<string> passed)
    {
        FieldInfo singleton = typeof(UpdateManager).GetField("instance", BindingFlags.Static | BindingFlags.NonPublic);
        Require(singleton != null, "cannot inspect UpdateManager singleton isolation");
        object originalSingleton = singleton.GetValue(null);

        GameObject gameObject = CreateInactiveObject(preview, "Update manager regression check");
        var manager = gameObject.AddComponent<UpdateManager>();
        foreach (string phase in new[] { "Update", "FixedUpdate", "LateUpdate" })
        {
            manager.ClearAll();
            MethodInfo method = typeof(UpdateManager).GetMethod(phase, InstancePrivate);
            Require(method != null, "missing update loop " + phase);

            void Add(Tick tick)
            {
                if (phase == "Update") manager.Register((IUpdateable)tick);
                else if (phase == "FixedUpdate") manager.Register((IFixedUpdateable)tick);
                else manager.Register((ILateUpdateable)tick);
            }
            void Remove(Tick tick)
            {
                if (phase == "Update") manager.Unregister((IUpdateable)tick);
                else if (phase == "FixedUpdate") manager.Unregister((IFixedUpdateable)tick);
                else manager.Unregister((ILateUpdateable)tick);
            }
            int Count() => phase == "Update" ? manager.GetUpdateableCount()
                : phase == "FixedUpdate" ? manager.GetFixedUpdateableCount() : manager.GetLateUpdateableCount();
            void RunLoop() => method.Invoke(manager, null);

            var first = new Tick();
            var pooled = new Tick();
            first.action = () => { Add(pooled); Add(pooled); };
            Add(first);
            RunLoop();
            Require(Count() == 2 && pooled.calls == 0, phase + ": deferred duplicate register");

            first.action = null;
            Add(pooled);
            RunLoop();
            Require(Count() == 2 && pooled.calls == 1, phase + ": deferred register desynchronizes HashSet");

            first.action = () => Remove(pooled);
            RunLoop();
            Require(Count() == 1, phase + ": deferred removal failed");
            first.action = null;
            Add(pooled);
            Require(Count() == 2, phase + ": pooled object cannot register after a deferred removal");

            first.action = () => { Remove(pooled); Add(pooled); };
            RunLoop();
            Require(Count() == 2, phase + ": remove then add must leave the callback registered");

            first.action = () => { Add(pooled); Remove(pooled); };
            RunLoop();
            Require(Count() == 1, phase + ": add then remove must leave the callback unregistered");

            manager.ClearAll();
            Require(Count() == 0, phase + ": ClearAll leaves callbacks behind");
            passed.Add("Native " + phase + ": deferred operations, duplicates, pool reuse and last-operation ordering");
        }
        Require(ReferenceEquals(originalSingleton, singleton.GetValue(null)),
            "update-loop fixtures created or replaced the gameplay singleton");
        Object.DestroyImmediate(gameObject);
    }

    private static void CheckPooledMaterial(Scene preview, List<string> passed)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        Require(shader != null, "URP Unlit shader is unavailable for native material check");
        string uniqueName = "PerformanceRegressionMaterial_" + Guid.NewGuid().ToString("N");
        var source = new Material(shader) { name = uniqueName, hideFlags = HideFlags.HideAndDontSave };
        Material instance = null;
        GameObject gameObject = CreateInactiveObject(preview, "Pooled material regression check");
        var renderer = gameObject.AddComponent<MeshRenderer>();
        var orb = gameObject.AddComponent<ExperienceOrb>();
        FieldInfo materialField = typeof(BaseCollectible).GetField("materialInstance", InstancePrivate);
        Require(materialField != null, "cannot inspect owned collectible material");
        try
        {
            renderer.sharedMaterial = source;
            orb.SetVisuals(null, source, Color.cyan, 1f);
            instance = renderer.sharedMaterial;
            Require(instance != null && instance != source, "orb modified the shared source material");
            int originalId = instance.GetInstanceID();
            int nativeMaterialCount = CountNamedMaterials(uniqueName);
            Color sourceColor = source.color;

            for (int i = 0; i < 512; i++)
            {
                Color color = (i & 1) == 0 ? Color.red : Color.green;
                float scale = (i % 3 + 1) * 0.5f;
                orb.SetVisuals(null, source, color, scale);
                orb.SetEmission(3f, 2f);
                Require(renderer.sharedMaterial.GetInstanceID() == originalId,
                    "reusing the same pooled orb created another native Material");
                Require(renderer.sharedMaterial.color == color && gameObject.transform.localScale == Vector3.one * scale,
                    "material reuse lost per-spawn color or scale");
            }

            Require(CountNamedMaterials(uniqueName) == nativeMaterialCount,
                "native Material count grew during pooled orb reuse");
            Require(source.color == sourceColor, "pooled orb changed the shared material asset");
            passed.Add("Native pooled orb: 512 reconfigurations preserve material identity/count, colors, scale and shared source");
        }
        finally
        {
            // Gameplay owns the material via Destroy (deferred in Play Mode).
            // Edit-mode fixtures release it explicitly with DestroyImmediate and
            // clear ownership first, so cleanup never calls runtime Destroy here.
            materialField.SetValue(orb, null);
            renderer.sharedMaterial = null;
            if (instance != null) Object.DestroyImmediate(instance);
            if (source != null) Object.DestroyImmediate(source);
            Object.DestroyImmediate(gameObject);
        }
    }

    private static int CountNamedMaterials(string prefix)
    {
        int count = 0;
        foreach (Material material in Resources.FindObjectsOfTypeAll<Material>())
            if (material.name.StartsWith(prefix, StringComparison.Ordinal)) count++;
        return count;
    }
}

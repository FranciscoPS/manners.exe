using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine;
using Object = UnityEngine.Object;

public class PerformanceRegressionTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [UnityTest]
    public IEnumerator ReconfiguringPooledPickupReusesAndReleasesOwnedMaterials()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
        var go = new GameObject("Pickup material regression");
        var renderer = go.AddComponent<MeshRenderer>();
        var source = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        var replacement = new Material(source);
        renderer.sharedMaterial = source;
        var pickup = go.AddComponent<PerformanceTestCollectible>();
        Material owned = renderer.sharedMaterial;
        try
        {
            Assert.AreNotSame(source, owned);
            for (int i = 0; i < 1000; i++)
                pickup.SetVisuals(null, source, Color.red, 1);
            Assert.AreSame(owned, renderer.sharedMaterial, "Pool reuse must not allocate another material on each spawn.");
            pickup.SetVisuals(null, replacement, Color.blue, 1);
            yield return null;
            Assert.IsTrue(owned == null, "Replacing a template must release the old instance.");
            owned = renderer.sharedMaterial;
            Object.DestroyImmediate(go);
            yield return null;
            Assert.IsTrue(owned == null, "Destroying a pickup must release its final instance.");
            Assert.IsTrue(source != null && replacement != null, "Source assets must survive.");
        }
        finally
        {
            if (go) Object.DestroyImmediate(go);
            Object.DestroyImmediate(source);
            Object.DestroyImmediate(replacement);
        }
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator DestroyedBuildingReleasesOnlyItsFadeMaterials()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
        var go = new GameObject("Building material regression");
        var visual = new GameObject("visual");
        visual.transform.SetParent(go.transform);
        var renderer = visual.AddComponent<MeshRenderer>();
        var source = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        renderer.sharedMaterial = source;
        var building = go.AddComponent<BuildingsScript>();
        typeof(BuildingsScript).GetField("visual", Private).SetValue(building, visual);
        typeof(BuildingsScript).GetField("shakeDuration", Private).SetValue(building, 0f);
        try
        {
            int before = Resources.FindObjectsOfTypeAll<Material>().Length;
            typeof(BuildingsScript).GetMethod("CreateFadeMaterials", Private).Invoke(building, new object[] { new Renderer[] { renderer } });
            Material owned = renderer.sharedMaterial;
            Assert.AreNotSame(source, owned);
            Assert.AreEqual(before + 1, Resources.FindObjectsOfTypeAll<Material>().Length, "Only one clone per slot, with no implicit Renderer.materials clone.");
            Object.DestroyImmediate(go);
            yield return null;
            Assert.IsTrue(owned == null);
            Assert.IsTrue(source != null);
        }
        finally
        {
            if (go) Object.DestroyImmediate(go);
            Object.DestroyImmediate(source);
        }
        yield return new ExitPlayMode();
    }

    [TestCase("Update", "updateables", "updateableSet")]
    [TestCase("FixedUpdate", "fixedUpdateables", "fixedUpdateableSet")]
    [TestCase("LateUpdate", "lateUpdateables", "lateUpdateableSet")]
    public void DeferredRegistrationKeepsMembershipConsistent(string phase, string listName, string setName)
    {
        var manager = new GameObject("Update registration regression").AddComponent<UpdateManager>();
        var member = new TickMember();
        Action register = () => Register(manager, member, phase);
        Action unregister = () => Unregister(manager, member, phase);
        var tick = typeof(UpdateManager).GetMethod(phase, Private);
        try
        {
            register();
            member.Action = () => { unregister(); register(); register(); };
            tick.Invoke(manager, null);
            AssertMembership(manager, listName, setName, 1);
            member.Action = () => { register(); unregister(); };
            tick.Invoke(manager, null);
            AssertMembership(manager, listName, setName, 0);
            register();
            member.Action = () => throw new MissingReferenceException("Simulated destroyed update target");
            tick.Invoke(manager, null);
            AssertMembership(manager, listName, setName, 0);
            // No stale set entry may prevent this object from being registered again.
            register();
            AssertMembership(manager, listName, setName, 1);
        }
        finally { Object.DestroyImmediate(manager.gameObject); }
    }

    private static void AssertMembership(UpdateManager manager, string list, string set, int expected)
    {
        Assert.AreEqual(expected, ((IList)typeof(UpdateManager).GetField(list, Private).GetValue(manager)).Count);
        var members = typeof(UpdateManager).GetField(set, Private).GetValue(manager);
        Assert.AreEqual(expected, members.GetType().GetProperty("Count").GetValue(members));
    }

    private static void Register(UpdateManager m, TickMember item, string phase)
    {
        if (phase == "Update") m.Register((IUpdateable)item);
        else if (phase == "FixedUpdate") m.Register((IFixedUpdateable)item);
        else m.Register((ILateUpdateable)item);
    }
    private static void Unregister(UpdateManager m, TickMember item, string phase)
    {
        if (phase == "Update") m.Unregister((IUpdateable)item);
        else if (phase == "FixedUpdate") m.Unregister((IFixedUpdateable)item);
        else m.Unregister((ILateUpdateable)item);
    }
    private class TickMember : IUpdateable, IFixedUpdateable, ILateUpdateable
    {
        public Action Action;
        public bool IsActive => true;
        public void OnUpdate(float dt) => Action?.Invoke();
        public void OnFixedUpdate(float dt) => Action?.Invoke();
        public void OnLateUpdate(float dt) => Action?.Invoke();
    }
}

public class PerformanceTestCollectible : BaseCollectible
{
    protected override void OnEnable() => InitializeRenderer();
    protected override void OnDisable() { }
    protected override void UpdateConfiguration() { }
    protected override void OnCollected(GameObject playerObject) { }
}
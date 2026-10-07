using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class WebBuildRegressionTests
{
    private GameObject root;
    private MusicManager manager;
    private GameObject clockRoot;
    private object previousClock;
    private static readonly FieldInfo ClockInstance = typeof(GameTimeManager).GetField("instance", BindingFlags.Static | BindingFlags.NonPublic);
    private readonly List<AudioClip> clips = new List<AudioClip>();
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("Music regression test");
        manager = root.AddComponent<MusicManager>();
        Invoke("SetupAudioSource");
        previousClock = ClockInstance.GetValue(null);
        clockRoot = new GameObject("Music regression clock");
        ClockInstance.SetValue(null, clockRoot.AddComponent<GameTimeManager>());
    }

    [TearDown]
    public void TearDown()
    {
        manager.StopMusic();
        Object.DestroyImmediate(root);
        ClockInstance.SetValue(null, previousClock);
        Object.DestroyImmediate(clockRoot);
        foreach (var clip in clips) Object.DestroyImmediate(clip);
        clips.Clear();
    }

    [Test]
    public void WebSequenceKeepsIntroThenLoopsAndBridgeOnOneSource()
    {
        var intro = Clip("intro", 2);
        var loop1 = Clip("loop1", 3);
        var bridge = Clip("bridge", 1);
        var loop2 = Clip("loop2", 3);
        var sequence = Sequence(intro, new List<MusicLoopSection>
        {
            new MusicLoopSection { loopClip = loop1, bridgeClip = bridge, repeatCount = 2 },
            new MusicLoopSection { loopClip = loop2 }
        });

        Assert.That(sequence.Step(), Is.True);
        AssertOnlyRegularClip(intro, false);
        // A not-yet-playing source must not count as a completed intro.
        Source.Stop();
        Assert.That(sequence.Step(), Is.True);
        AssertOnlyRegularClip(intro, false);
        Elapse(2.1);
        Assert.That(sequence.Step(), Is.True);
        AssertOnlyRegularClip(loop1, true);
        Elapse(3.1);
        Assert.That(sequence.Step(), Is.True);
        AssertOnlyRegularClip(loop1, true);
        Elapse(6.1);
        Assert.That(sequence.Step(), Is.True);
        AssertOnlyRegularClip(bridge, false);
        Elapse(1.1);
        Assert.That(sequence.Step(), Is.False);
        AssertOnlyRegularClip(loop2, true);
    }

    [Test]
    public void WebSequenceWaitsDuringExplicitPause()
    {
        var intro = Clip("intro", 2);
        var loop = Clip("loop", 3);
        var sequence = Sequence(intro, new List<MusicLoopSection>(), loop);
        Assert.That(sequence.Step(), Is.True);
        manager.PauseMusic();
        Elapse(10);
        Assert.That(sequence.Step(), Is.True);
        AssertOnlyRegularClip(intro, false);
        manager.ResumeMusic();
        Elapse(2.1);
        Assert.That(sequence.Step(), Is.False);
        AssertOnlyRegularClip(loop, true);
    }

    [Test]
    public void WebSequenceSupportsNoIntroAndNoBridge()
    {
        var first = Clip("first", 1);
        var last = Clip("last", 1);
        var sequence = Sequence(null, new List<MusicLoopSection>
        {
            new MusicLoopSection { loopClip = first, repeatCount = 1 },
            new MusicLoopSection { loopClip = last }
        });
        Assert.That(sequence.Step(), Is.True);
        AssertOnlyRegularClip(first, true);
        Elapse(1.1);
        Assert.That(sequence.Step(), Is.False);
        AssertOnlyRegularClip(last, true);
    }

    [Test]
    public void WebOvertimeBridgeAndLoopReuseOneSource()
    {
        var bridge = Clip("overtime bridge", 1);
        var loop = Clip("overtime loop", 3);
        var sequence = new Routine((IEnumerator)Invoke("WebOvertimeRoutine", new SceneMusicConfig
        {
            overtimeBridgeClip = bridge, overtimeLoopClip = loop
        }));
        Assert.That(sequence.Step(), Is.True);
        Assert.That(Get<AudioSource>("overtimeSource").clip, Is.SameAs(bridge));
        Elapse(1.1);
        Assert.That(sequence.Step(), Is.False);
        Assert.That(Get<AudioSource>("overtimeSource").clip, Is.SameAs(loop));
        Assert.That(Source.clip, Is.Null);
        Assert.That(Get<AudioSource>("introSource").clip, Is.Null);
    }

    [Test]
    public void WebAutomaticTransitionWaitsForGameTimeAndLoopBoundary()
    {
        var first = Clip("first", 3);
        var last = Clip("last", 3);
        var sequence = Sequence(null, new List<MusicLoopSection>
        {
            new MusicLoopSection { loopClip = first },
            new MusicLoopSection { loopClip = last, startAtSeconds = 10 }
        });
        Assert.That(sequence.Step(), Is.True);
        Elapse(8);
        Assert.That(sequence.Step(), Is.True);
        AssertOnlyRegularClip(first, true);
        var clock = clockRoot.GetComponent<GameTimeManager>();
        typeof(GameTimeManager).GetField("isGameActive", Private).SetValue(clock, true);
        typeof(GameTimeManager).GetField("gameStartTime", Private).SetValue(clock, Time.time - 11);
        Elapse(11);
        Assert.That(sequence.Step(), Is.True);
        AssertOnlyRegularClip(first, true);
        Elapse(12.1);
        Assert.That(sequence.Step(), Is.False);
        AssertOnlyRegularClip(last, true);
    }

    [Test]
    public void WebOvertimeWithoutBridgeUsesLastRegularLoopAsFallback()
    {
        var loop = Clip("regular fallback", 3);
        typeof(MusicManager).GetField("lastRegularLoop", Private).SetValue(manager, loop);
        var sequence = new Routine((IEnumerator)Invoke("WebOvertimeRoutine", new SceneMusicConfig()));
        Assert.That(sequence.Step(), Is.False);
        Assert.That(Get<AudioSource>("overtimeSource").clip, Is.SameAs(loop));
        Assert.That(Get<AudioSource>("overtimeSource").loop, Is.True);
        Assert.That(Source.clip, Is.Null);
    }

    [Test]
    public void AllEnemyVariantsHaveRenderersWithoutPrematureLodCulling()
    {
        int count = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Characters" }))
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            if (!prefab.GetComponent<EnemyController>()) continue;
            count++;
            Assert.That(prefab.GetComponentsInChildren<Renderer>(true), Is.Not.Empty, prefab.name);
            Assert.That(prefab.GetComponentsInChildren<LODGroup>(true), Is.Empty, prefab.name);
        }
        Assert.That(count, Is.EqualTo(12));
    }

    private AudioSource Source => Get<AudioSource>("loopSource");
    private T Get<T>(string name) => (T)typeof(MusicManager).GetField(name, Private).GetValue(manager);
    private object Invoke(string name, params object[] args) => typeof(MusicManager).GetMethod(name, Private).Invoke(manager, args);
    private void Elapse(double seconds) => typeof(MusicManager).GetField("webClipStartDsp", Private).SetValue(manager, AudioSettings.dspTime - seconds);

    private AudioClip Clip(string name, int seconds)
    {
        var clip = AudioClip.Create(name, 44100 * seconds, 1, 44100, false);
        clips.Add(clip);
        return clip;
    }

    private Routine Sequence(AudioClip intro, List<MusicLoopSection> sections, AudioClip legacy = null)
        => new Routine((IEnumerator)Invoke("WebMusicSequenceRoutine", intro, sections, legacy));

    private void AssertOnlyRegularClip(AudioClip clip, bool loop)
    {
        Assert.That(Source.clip, Is.SameAs(clip));
        Assert.That(Source.loop, Is.EqualTo(loop));
        Assert.That(Get<AudioSource>("introSource").clip, Is.Null);
        Assert.That(Get<AudioSource>("overtimeSource").clip, Is.Null);
    }

    // Drive nested Unity coroutine enumerators without real-time sleeps, then
    // place the DSP origin at each boundary to exercise the production path.
    private sealed class Routine
    {
        private readonly Stack<IEnumerator> stack = new Stack<IEnumerator>();
        public Routine(IEnumerator routine) => stack.Push(routine);
        public bool Step()
        {
            for (int guard = 0; guard < 100; guard++)
            {
                if (stack.Count == 0) return false;
                var current = stack.Peek();
                if (!current.MoveNext()) { stack.Pop(); continue; }
                if (current.Current is IEnumerator child) { stack.Push(child); continue; }
                return true;
            }
            throw new InvalidOperationException("Coroutine failed to yield.");
        }
    }
}

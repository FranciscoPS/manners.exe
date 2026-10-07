using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public class OverrideCombinationTests
{
    private const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    private const BindingFlags StaticFields = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
    private readonly List<Object> ownedObjects = new List<Object>();
    private readonly List<Action> restoreStatics = new List<Action>();
    private PlayerStatsManager stats;
    private UpgradeDatabase upgrades;
    private OverrideManager overrides;
    private Transform player;
    private Random.State randomState;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
        InitializeFixtures();
    }

    private void InitializeFixtures()
    {
        randomState = Random.state;
        stats = Component<PlayerStatsManager>();
        upgrades = Asset<UpgradeDatabase>();
        overrides = Component<OverrideManager>();
        OverrideStatic(typeof(PlayerStatsManager), "instance", stats);
        OverrideStatic(typeof(UpgradeDatabase), "instance", upgrades);
        OverrideStatic(typeof(OverrideManager), "instance", overrides);
        OverrideStatic(typeof(GameBalanceConfig), "instance", Asset<GameBalanceConfig>());
        OverrideStatic(typeof(UpdateManager), "instance", Component<UpdateManager>());
        OverrideStatic(typeof(EnemySeparationManager), "<Instance>k__BackingField", Component<EnemySeparationManager>());
        OverrideStatic(typeof(GameSessionStats), "instance", Component<GameSessionStats>());
        OverrideStatic(typeof(FloatingTextManager), "<Instance>k__BackingField", null);
        OverrideStatic(typeof(EnemySpawnManager), "<Instance>k__BackingField", null);
        OverrideStatic(typeof(DropSpawner), "<Instance>k__BackingField", Component<DropSpawner>());
        OverrideStatic(typeof(GameEvents), "OnEnemyDamaged", null);
        var factory = Component<SpawnFactory>();
        Set(factory, "poolManager", Component<PoolManager>());
        OverrideStatic(typeof(SpawnFactory), "instance", factory);

        var savedEnemies = new List<EnemyHealth>(EnemyHealth.ActiveEnemies);
        EnemyHealth.ActiveEnemies.Clear();
        restoreStatics.Add(() => { EnemyHealth.ActiveEnemies.Clear(); EnemyHealth.ActiveEnemies.AddRange(savedEnemies); });
        OverrideStatic(typeof(EnemyHealth), "<ActiveEnemyCount>k__BackingField", 0);
        var savedBuildings = new List<BuildingsScript>(BuildingsScript.ActiveBuildings);
        BuildingsScript.ActiveBuildings.Clear();
        restoreStatics.Add(() => { BuildingsScript.ActiveBuildings.Clear(); BuildingsScript.ActiveBuildings.AddRange(savedBuildings); });
        player = GameObject("Player fixture").transform;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        OverrideProcVisual.ClearAll();
        yield return null;
        for (int i = ownedObjects.Count - 1; i >= 0; i--)
            if (ownedObjects[i] != null) Object.DestroyImmediate(ownedObjects[i]);
        ownedObjects.Clear();
        for (int i = restoreStatics.Count - 1; i >= 0; i--) restoreStatics[i]();
        restoreStatics.Clear();
        Random.state = randomState;
        yield return null;
        yield return new ExitPlayMode();
    }

    [Test]
    public void MultishotAlwaysEmitsHalfItsBulletsRoundedUp()
    {
        Assert.AreEqual(1, OverrideCombatResolver.EmissionCount(), "Without Multishot there are no extra emissions.");
        Own(UpgradeType.MultiShot);
        Assert.AreEqual(0f, stats.GetMultiShotProbability(), "The fixture proves the combination ignores the projectile chance.");
        Assert.AreEqual(2, OverrideCombatResolver.EmissionCount(), "Level 1 fires four bullets, so it emits two.");
        Assert.AreEqual(2, OverrideCombatResolver.EmissionCount(1));
        Assert.AreEqual(1, OverrideCombatResolver.EmissionCount(0));
        Assert.AreEqual(1, OverrideCombatResolver.EmissionCount(-1));
        SetLevel(UpgradeType.MultiShot, 2);
        Assert.AreEqual(4, OverrideCombatResolver.EmissionCount(), "Seven bullets round up to four emissions.");
        Assert.AreEqual(3, OverrideCombatResolver.EmissionCount(2), "The config ceiling still limits the half.");
        SetLevel(UpgradeType.MultiShot, 3);
        Assert.AreEqual(5, OverrideCombatResolver.EmissionCount());
        SetLevel(UpgradeType.MultiShot, 100);
        Assert.AreEqual(9, OverrideCombatResolver.EmissionCount(20), "Extra emissions stay capped at eight.");
        SetLevel(UpgradeType.MultiShot, 0);
        Assert.AreEqual(1, OverrideCombatResolver.EmissionCount());
    }

    [Test]
    public void OwnedUpgradesTriggerOnEveryTickEvenAtZeroProjectileChance()
    {
        Own(UpgradeType.MultiShot);
        Own(UpgradeType.ExplosiveShot);
        Own(UpgradeType.Knockback);
        Assert.AreEqual(0f, stats.GetExplosiveShotProbability());
        Assert.AreEqual(0f, stats.GetKnockbackProbability());
        EnemyHealth target = Enemy(Vector3.zero);
        EnemyHealth neighbor = Enemy(Vector3.right * 2f);
        var combat = new OverrideCombatResolver();

        for (int i = 0; i < 5; i++)
        {
            Assert.AreEqual(2, OverrideCombatResolver.EmissionCount());
            Set(target.Controller, "isKnockedBack", false);
            Set(neighbor.Controller, "isKnockedBack", false);
            float neighborHealth = Health(neighbor);
            combat.BeginTick(OverrideAttackSource.Laser, 1f, Vector3.back, Vector3.forward);
            combat.Hit(target);
            combat.EndTick();
            Assert.AreEqual(neighborHealth - 1f, Health(neighbor), "Every owned tick explodes.");
            Assert.IsTrue(Get<bool>(target.Controller, "isKnockedBack"), "Every owned tick pushes.");
            Assert.IsTrue(Get<bool>(neighbor.Controller, "isKnockedBack"));
        }
    }

    [Test]
    public void LaserMultishotAlwaysFiresSeparatedBeamsAndReusesVisualsAfterUpgradeChanges()
    {
        Own(UpgradeType.MultiShot, 2);
        LaserBeamEffect effect = Laser(out LaserBeamConfig config);
        Random.InitState(7);
        for (int sweep = 0; sweep < 3; sweep++)
        {
            Invoke(effect, "BeginSweep");
            Assert.AreEqual(4, Get<int>(effect, "activeBeamCount"), "Every sweep fires half the Multishot bullets, rounded up.");
            AssertBeamsSeparated(Get<IList>(effect, "beamVisuals"), 4, config.minBeamSeparationAngle);
        }
        var visuals = Get<IList>(effect, "beamVisuals");
        Assert.AreEqual(4, visuals.Count);
        object extraVisual = visuals[1];

        SetLevel(UpgradeType.MultiShot, 0);
        Invoke(effect, "BeginSweep");
        Assert.AreEqual(1, Get<int>(effect, "activeBeamCount"));
        Assert.AreEqual(4, visuals.Count);
        Assert.IsFalse(Get<Transform>(visuals[1], "root").gameObject.activeSelf);
        SetLevel(UpgradeType.MultiShot, 1);
        Invoke(effect, "BeginSweep");
        Assert.AreSame(extraVisual, visuals[1], "Repeated sweeps must reuse the existing beam visuals.");

        var data = Asset<OverrideData>();
        data.effectConfig = config;
        Get<Dictionary<OverrideData, GameObject>>(overrides, "activeEffects").Add(data, effect.gameObject);
        effect.gameObject.SetActive(true);
        Assert.AreSame(config, overrides.GetActiveConfig<LaserBeamConfig>());
        effect.enabled = false;
        Assert.IsNull(overrides.GetActiveConfig<LaserBeamConfig>());
        Assert.IsFalse(Get<bool>(effect, "sweeping"));
        for (int i = 0; i < visuals.Count; i++)
            Assert.IsFalse(Get<Transform>(visuals[i], "root").gameObject.activeSelf);
    }

    [Test]
    public void LaserMultishotAimsEachBeamAtTheNearestSeparatedEnemy()
    {
        Own(UpgradeType.MultiShot, 2);
        LaserBeamEffect effect = Laser(out LaserBeamConfig config);
        Enemy(new Vector3(0f, 0f, 3f));
        Enemy(new Vector3(0.5f, 0f, 4f));
        Enemy(new Vector3(4f, 0f, 0f));
        Enemy(new Vector3(-5f, 0f, 0f));
        Invoke(effect, "BeginSweep");
        var visuals = Get<IList>(effect, "beamVisuals");
        AssertBeamAim(visuals[0], Vector3.forward, 3f, "The first beam keeps the nearest enemy.");
        AssertBeamAim(visuals[1], Vector3.right, 4f, "An enemy too close in angle to another beam is skipped.");
        AssertBeamAim(visuals[2], Vector3.left, 5f);
        Assert.AreEqual(config.extendDistance, Get<float>(visuals[2], "endDistance") - Get<float>(visuals[2], "startDistance"), 0.001f);
    }

    [Test]
    public void LaserBeamsWithoutEnoughTargetsSpreadToSeparatedGroundPoints()
    {
        Own(UpgradeType.MultiShot, 2);
        LaserBeamEffect effect = Laser(out LaserBeamConfig config);
        Enemy(new Vector3(0f, 0f, 3f));
        Random.InitState(11);
        Invoke(effect, "BeginSweep");
        var visuals = Get<IList>(effect, "beamVisuals");
        AssertBeamAim(visuals[0], Vector3.forward, 3f);
        AssertBeamsSeparated(visuals, 4, config.minBeamSeparationAngle);
        for (int i = 1; i < 4; i++)
        {
            float distance = Get<float>(visuals[i], "startDistance");
            Assert.That(distance, Is.InRange(config.range * 0.3f - 0.001f, config.range + 0.001f), "Fallback beams aim at ground points within range.");
        }
    }

    [Test]
    public void LaserHitsEveryTargetWhenDeathsRemoveEnemiesFromTheRegistry()
    {
        var config = Asset<LaserBeamConfig>();
        config.damage = 20f;
        var effect = Component<LaserBeamEffect>();
        effect.Configure(config);
        EnemyHealth first = Enemy(Vector3.forward, 10f);
        EnemyHealth second = Enemy(Vector3.forward * 2f, 10f);
        EnemyHealth third = Enemy(Vector3.forward * 3f, 10f);
        Invoke(effect, "DamageNear", Vector3.zero, Vector3.forward * 4f);
        Assert.IsFalse(first.gameObject.activeSelf);
        Assert.IsFalse(second.gameObject.activeSelf);
        Assert.IsFalse(third.gameObject.activeSelf);
        Assert.AreEqual(0, EnemyHealth.ActiveEnemies.Count);
    }

    [TestCase(OverrideAttackSource.Laser)]
    [TestCase(OverrideAttackSource.Cryo)]
    [TestCase(OverrideAttackSource.Emp)]
    public void EachTickHasExactlyOneExplosionAndNeverRecurses(OverrideAttackSource source)
    {
        Own(UpgradeType.ExplosiveShot);
        EnemyHealth first = Enemy(Vector3.zero);
        EnemyHealth second = Enemy(Vector3.right);
        EnemyHealth bystander = Enemy(Vector3.right * 2f);
        EnemyHealth outsideFirstExplosion = Enemy(Vector3.right * 4f);
        var combat = new OverrideCombatResolver();
        combat.BeginTick(source, 10f, Vector3.back, Vector3.forward);
        combat.Hit(first);
        combat.Hit(first);
        combat.Hit(second);
        SetLevel(UpgradeType.ExplosiveShot, 0);
        combat.EndTick();
        combat.EndTick();
        Assert.AreEqual(80f, Health(first), "Duplicate hits and EndTick calls must not duplicate damage.");
        Assert.AreEqual(80f, Health(second));
        Assert.AreEqual(90f, Health(bystander), "Multiple primary targets still produce just one explosion.");
        Assert.AreEqual(100f, Health(outsideFirstExplosion), "An explosion victim must not create another explosion.");

        combat.BeginTick(source, 10f, Vector3.back, Vector3.forward);
        combat.Hit(first);
        combat.EndTick();
        Assert.AreEqual(70f, Health(first));
        Assert.AreEqual(90f, Health(bystander), "A tick that starts without the upgrade must not explode.");

        SetLevel(UpgradeType.ExplosiveShot, 1);
        combat.BeginTick(source, 10f, Vector3.back, Vector3.forward);
        combat.Hit(first);
        combat.EndTick();
        Assert.AreEqual(50f, Health(first));
        Assert.AreEqual(70f, Health(second));
        Assert.AreEqual(80f, Health(bystander), "Every tick with the upgrade explodes.");
        Assert.AreEqual(100f, Health(outsideFirstExplosion));
    }

    [Test]
    public void EmptyOrCancelledTicksCannotReuseAnEarlierImpact()
    {
        Own(UpgradeType.ExplosiveShot);
        EnemyHealth enemy = Enemy(Vector3.zero);
        var combat = new OverrideCombatResolver();
        combat.BeginTick(OverrideAttackSource.Laser, 10f, Vector3.back, Vector3.forward);
        combat.Hit(enemy);
        combat.EndTick();
        Assert.AreEqual(80f, Health(enemy));
        combat.Hit(enemy);
        combat.BeginTick(OverrideAttackSource.Cryo, 10f, Vector3.back, Vector3.forward);
        combat.EndTick();
        Assert.AreEqual(80f, Health(enemy));
    }

    [Test]
    public void ExplosionAndKnockbackDependOnlyOnTheirOwnUpgradeAndCarryCryoSlow()
    {
        Own(UpgradeType.Knockback);
        CryoFieldConfig cryo = ActiveOverride<CryoFieldConfig>();
        EnemyHealth target = Enemy(Vector3.zero);
        EnemyHealth neighbor = Enemy(Vector3.right * 2f);
        var combat = new OverrideCombatResolver();
        combat.BeginTick(OverrideAttackSource.Cryo, 10f, Vector3.back, Vector3.forward);
        combat.Hit(target);
        combat.EndTick();
        Assert.IsTrue(Get<bool>(target.Controller, "isKnockedBack"));
        Assert.IsTrue(Get<bool>(neighbor.Controller, "isKnockedBack"), "Knockback chains without Explosive Shots.");
        Assert.AreEqual(100f, Health(neighbor));

        Set(target.Controller, "isKnockedBack", false);
        Set(neighbor.Controller, "isKnockedBack", false);
        SetLevel(UpgradeType.ExplosiveShot, 1);
        SetLevel(UpgradeType.Knockback, 0);
        combat.BeginTick(OverrideAttackSource.Cryo, 10f, Vector3.back, Vector3.forward);
        combat.Hit(target);
        combat.EndTick();
        Assert.AreEqual(90f, Health(neighbor), "Explosive Shots explodes without Knockback.");
        Assert.IsFalse(Get<bool>(target.Controller, "isKnockedBack"));
        Assert.IsFalse(Get<bool>(neighbor.Controller, "isKnockedBack"));
        Assert.AreEqual(cryo.slowMultiplier, Get<float>(neighbor.Controller, "slowMultiplier"));

        SetLevel(UpgradeType.Knockback, 1);
        combat.BeginTick(OverrideAttackSource.Cryo, 10f, Vector3.back, Vector3.forward);
        combat.Hit(target);
        combat.EndTick();
        Assert.IsTrue(Get<bool>(neighbor.Controller, "isKnockedBack"), "The explosion also carries the tick's push.");
    }

    [Test]
    public void LaserCarriesFrostAndUsesStrongestExistingControlBonus()
    {
        CryoFieldConfig cryo = ActiveOverride<CryoFieldConfig>();
        EmpPulseConfig emp = ActiveOverride<EmpPulseConfig>();
        EnemyHealth fresh = Enemy(Vector3.zero);
        EnemyHealth slowed = Enemy(Vector3.right);
        EnemyHealth frozen = Enemy(Vector3.left);
        slowed.Controller.ApplySlow(cryo.slowMultiplier, 5f);
        frozen.Controller.ApplySlow(0f, 5f);
        var combat = new OverrideCombatResolver();
        combat.BeginTick(OverrideAttackSource.Laser, 10f, Vector3.back, Vector3.forward);
        combat.Hit(fresh);
        combat.Hit(slowed);
        combat.Hit(frozen);
        combat.EndTick();
        Assert.IsTrue(fresh.Controller.IsSlowed);
        Assert.AreEqual(90f, Health(fresh), "The first hit applies frost after evaluating existing control.");
        Assert.AreEqual(100f - 10f * cryo.laserDamageMultiplier, Health(slowed));
        Assert.AreEqual(100f - 10f * Mathf.Max(cryo.laserDamageMultiplier, emp.laserDamageMultiplier), Health(frozen));
        Assert.IsTrue(frozen.Controller.IsFrozen, "Frost must not replace EMP's stronger freeze.");
    }

    [Test]
    public void EmpUsesPlayerDamageForExplosionsUntilLaserIsActive()
    {
        Own(UpgradeType.ExplosiveShot);
        EnemyHealth target = Enemy(Vector3.zero);
        var combat = new OverrideCombatResolver();
        combat.BeginTick(OverrideAttackSource.Emp, 0f, Vector3.back, Vector3.forward);
        combat.Hit(target);
        Assert.AreEqual(100f, Health(target), "Base EMP remains a control attack.");
        combat.EndTick();
        Assert.AreEqual(100f - stats.GetModifiedDamage(), Health(target));

        LaserBeamConfig laser = ActiveOverride<LaserBeamConfig>();
        laser.damage = 12f;
        laser.empDamageMultiplier = 0.5f;
        float previousHealth = Health(target);
        combat.BeginTick(OverrideAttackSource.Emp, 0f, Vector3.back, Vector3.forward);
        combat.Hit(target);
        combat.EndTick();
        Assert.AreEqual(previousHealth - 12f, Health(target), "With the laser active, EMP deals 6 direct damage and one 6-damage explosion.");
    }

    [TestCase(OverrideAttackSource.Laser)]
    [TestCase(OverrideAttackSource.Cryo)]
    public void ZeroDamageAttacksCannotGainPlayerDamageThroughExplosion(OverrideAttackSource source)
    {
        Own(UpgradeType.ExplosiveShot);
        EnemyHealth target = Enemy(Vector3.zero);
        EnemyHealth neighbor = Enemy(Vector3.right);
        var combat = new OverrideCombatResolver();
        combat.BeginTick(source, 0f, Vector3.back, Vector3.forward);
        combat.Hit(target);
        combat.EndTick();
        Assert.AreEqual(100f, Health(target));
        Assert.AreEqual(100f, Health(neighbor));
    }

    [Test]
    public void LethalExplosionStillHitsAllCollectedTargets()
    {
        Own(UpgradeType.ExplosiveShot);
        EnemyHealth seed = Enemy(Vector3.zero, 5f);
        EnemyHealth first = Enemy(Vector3.right, 5f);
        EnemyHealth second = Enemy(Vector3.left, 5f);
        EnemyHealth third = Enemy(Vector3.forward, 5f);
        var combat = new OverrideCombatResolver();
        combat.BeginTick(OverrideAttackSource.Cryo, 10f, Vector3.back, Vector3.forward);
        combat.Hit(seed);
        combat.EndTick();
        Assert.IsFalse(seed.gameObject.activeSelf);
        Assert.IsFalse(first.gameObject.activeSelf);
        Assert.IsFalse(second.gameObject.activeSelf);
        Assert.IsFalse(third.gameObject.activeSelf);
        Assert.AreEqual(0, EnemyHealth.ActiveEnemies.Count);
    }

    [Test]
    public void CryoMultishotHasHalfItsBulletsAsDelayedTicksWithoutRecursiveBursts()
    {
        Own(UpgradeType.MultiShot);
        var config = Asset<CryoFieldConfig>();
        config.tickInterval = 1f;
        config.repeatDelay = 0.2f;
        config.damagePerTick = 10f;
        var effect = Component<CryoFieldEffect>();
        effect.Configure(config);
        Set(effect, "player", player);
        Set(effect, "tickTimer", config.tickInterval);
        EnemyHealth enemy = Enemy(Vector3.forward, 1000f);

        effect.OnUpdate(0.5f);
        Assert.AreEqual(1000f, Health(enemy));
        Assert.IsTrue(enemy.Controller.IsSlowed, "The aura still refreshes slow between damage ticks.");
        effect.OnUpdate(0.5f);
        Assert.AreEqual(990f, Health(enemy));
        Assert.AreEqual(1, Get<int>(effect, "remainingExtraPulses"), "Level 1 fires four bullets, so the tick repeats once.");
        effect.OnUpdate(0.1f);
        Assert.AreEqual(990f, Health(enemy));
        effect.OnUpdate(0.11f);
        Assert.AreEqual(980f, Health(enemy));
        Assert.AreEqual(0, Get<int>(effect, "remainingExtraPulses"));
        effect.OnUpdate(0.5f);
        Assert.AreEqual(980f, Health(enemy), "Echoes must not start another burst.");
        effect.OnUpdate(0.51f);
        Assert.AreEqual(970f, Health(enemy));
        Assert.AreEqual(1, Get<int>(effect, "remainingExtraPulses"), "Every main tick starts the full echo sequence again.");
        Invoke(effect, "OnDisable");
        Assert.AreEqual(0, Get<int>(effect, "remainingExtraPulses"), "Disabling cancels pending echoes.");
    }

    [Test]
    public void EmpMultishotWaitsBetweenWavesAndCryoExtendsEachFreeze()
    {
        Own(UpgradeType.MultiShot);
        LaserBeamConfig laser = ActiveOverride<LaserBeamConfig>();
        laser.damage = 10f;
        laser.empDamageMultiplier = 0.5f;
        ActiveOverride<CryoFieldConfig>();
        var config = Asset<EmpPulseConfig>();
        config.interval = 10f;
        config.expandDuration = 0.1f;
        config.repeatDelay = 0.2f;
        config.maxChainHops = 0;
        config.visualPrefabOverride = GameObject("EMP visual fixture");
        var effect = Component<EmpPulseEffect>();
        effect.Configure(config);
        effect.gameObject.SetActive(true);
        effect.Activate(player);
        EnemyHealth enemy = Enemy(Vector3.forward, 1000f);

        effect.OnUpdate(9f);
        Assert.AreEqual(1000f, Health(enemy));
        effect.OnUpdate(1.01f);
        Assert.AreEqual(1000f, Health(enemy));
        effect.OnUpdate(0.05f);
        Assert.AreEqual(995f, Health(enemy));
        Assert.AreEqual(Time.time + config.freezeDuration * config.cryoFreezeDurationMultiplier,
            Get<float>(enemy.Controller, "slowEndTime"), 0.001f);
        effect.OnUpdate(0.06f);
        Assert.AreEqual(995f, Health(enemy), "The expanding wave hits each target only once.");
        effect.OnUpdate(0.1f);
        Assert.AreEqual(995f, Health(enemy));
        Assert.IsFalse(Get<bool>(effect, "expanding"));
        effect.OnUpdate(0.11f);
        effect.OnUpdate(0.11f);
        Assert.AreEqual(990f, Health(enemy));
        Assert.AreEqual(0, Get<int>(effect, "remainingExtraPulses"));
        effect.OnUpdate(1f);
        Assert.AreEqual(990f, Health(enemy), "After half the Multishot bullets in waves, the regular interval resumes.");
    }

    [Test]
    public void EmpChainSurvivesLethalLaserDamageButStopsAtConfiguredHopCount()
    {
        LaserBeamConfig laser = ActiveOverride<LaserBeamConfig>();
        laser.damage = 20f;
        laser.empDamageMultiplier = 0.5f;
        var config = Asset<EmpPulseConfig>();
        config.interval = 1f;
        config.expandDuration = 0.1f;
        config.radius = 1.1f;
        config.chainRadius = 2.1f;
        config.maxChainHops = 1;
        config.visualPrefabOverride = GameObject("EMP visual fixture");
        var effect = Component<EmpPulseEffect>();
        effect.Configure(config);
        effect.gameObject.SetActive(true);
        effect.Activate(player);
        EnemyHealth seed = Enemy(Vector3.forward, 5f);
        EnemyHealth firstHop = Enemy(Vector3.forward * 3f, 5f);
        EnemyHealth secondHop = Enemy(Vector3.forward * 5f, 100f);

        effect.OnUpdate(1.01f);
        effect.OnUpdate(0.11f);
        Assert.IsFalse(seed.gameObject.activeSelf);
        Assert.IsFalse(firstHop.gameObject.activeSelf, "A dead seed must retain its impact position for chaining.");
        Assert.AreEqual(100f, Health(secondHop));
        Assert.IsFalse(secondHop.Controller.IsFrozen, "One configured hop must not spread to the second neighbor.");
    }

    private static float Health(EnemyHealth enemy) => Get<float>(enemy, "currentHealth");

    private LaserBeamEffect Laser(out LaserBeamConfig config)
    {
        config = Asset<LaserBeamConfig>();
        config.fireShake = 0f;
        config.visualPrefabOverride = GameObject("Beam prefab fixture");
        config.visualPrefabOverride.AddComponent<LineRenderer>();
        var effect = Component<LaserBeamEffect>();
        effect.Configure(config);
        effect.Activate(player);
        return effect;
    }

    private static void AssertBeamAim(object beam, Vector3 direction, float distance, string message = null)
    {
        Assert.Less(Vector3.Angle(direction, Get<Vector3>(beam, "direction")), 0.01f, message);
        Assert.AreEqual(distance, Get<float>(beam, "startDistance"), 0.001f, message);
    }

    private static void AssertBeamsSeparated(IList beams, int count, float minAngle)
    {
        for (int a = 0; a < count; a++)
            for (int b = a + 1; b < count; b++)
                Assert.GreaterOrEqual(Vector3.Angle(Get<Vector3>(beams[a], "direction"), Get<Vector3>(beams[b], "direction")), minAngle - 0.01f,
                    $"Beams {a} and {b} must point in clearly different directions.");
    }

    private void Own(UpgradeType type, int level = 1)
    {
        var upgrade = Asset<UpgradeData>();
        upgrade.upgradeType = type;
        upgrade.baseValue = 0f;
        upgrade.multiplierPerLevel = 1f;
        upgrades.allUpgrades.Add(upgrade);
        SetLevel(type, level);
    }

    private void SetLevel(UpgradeType type, int level)
    {
        Get<Dictionary<UpgradeType, int>>(stats, "upgradeLevels")[type] = level;
    }

    private T ActiveOverride<T>() where T : OverrideEffectConfig
    {
        var config = Asset<T>();
        var data = Asset<OverrideData>();
        data.effectConfig = config;
        GameObject marker = GameObject("Active override fixture");
        marker.SetActive(true);
        Get<Dictionary<OverrideData, GameObject>>(overrides, "activeEffects").Add(data, marker);
        return config;
    }

    private EnemyHealth Enemy(Vector3 position, float health = 100f)
    {
        var go = GameObject("Enemy fixture");
        go.transform.position = position;
        go.AddComponent<EnemyController>();
        var enemy = go.AddComponent<EnemyHealth>();
        enemy.SetConfiguration(health, PoolManager.PoolType.BasicEnemy, null, 0, 0, 0f, 0f, 0, 0, 0f, 0, 0);
        go.SetActive(true);
        Assert.Contains(enemy, EnemyHealth.ActiveEnemies, "Real OnEnable must register the enemy before area and chain checks.");
        return enemy;
    }

    private GameObject GameObject(string name)
    {
        var go = new GameObject(name);
        go.SetActive(false);
        ownedObjects.Add(go);
        return go;
    }

    private T Component<T>() where T : Component => GameObject(typeof(T).Name + " fixture").AddComponent<T>();

    private T Asset<T>() where T : ScriptableObject
    {
        T asset = ScriptableObject.CreateInstance<T>();
        ownedObjects.Add(asset);
        return asset;
    }

    private void OverrideStatic(Type type, string fieldName, object replacement)
    {
        FieldInfo field = type.GetField(fieldName, StaticFields);
        Assert.IsNotNull(field, type.Name + "." + fieldName);
        object previous = field.GetValue(null);
        restoreStatics.Add(() => field.SetValue(null, previous));
        field.SetValue(null, replacement);
    }

    private static T Get<T>(object target, string field) => (T)target.GetType().GetField(field, InstanceFields).GetValue(target);
    private static void Set(object target, string field, object value) => target.GetType().GetField(field, InstanceFields).SetValue(target, value);
    private static void Invoke(object target, string method, params object[] args) => target.GetType().GetMethod(method, InstanceFields).Invoke(target, args);
}

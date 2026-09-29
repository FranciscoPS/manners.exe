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

public class SynergyCombinationTests
{
    private const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    private const BindingFlags StaticFields = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
    private readonly List<Object> ownedObjects = new List<Object>();
    private readonly List<Action> restoreStatics = new List<Action>();
    private PlayerStatsManager stats;
    private UpgradeDatabase upgrades;
    private SynergyManager synergies;
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
        // Inactive managers avoid Awake/bootstrap, scene subscriptions and real assets.
        stats = Component<PlayerStatsManager>();
        upgrades = Asset<UpgradeDatabase>();
        synergies = Component<SynergyManager>();
        OverrideStatic(typeof(PlayerStatsManager), "instance", stats);
        OverrideStatic(typeof(UpgradeDatabase), "instance", upgrades);
        OverrideStatic(typeof(SynergyManager), "instance", synergies);
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
        // Resolve the runtime rings' deferred destruction while the isolated
        // UpdateManager is still present for their OnDisable callbacks.
        SynergyProcVisual.ClearAll();
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
    public void MultishotReadsCurrentUpgradeAndBoundsEveryEmissionGroup()
    {
        UpgradeData upgrade = Upgrade(UpgradeType.MultiShot, 0f, 100);
        Assert.AreEqual(1, SynergyCombatResolver.RollEmissionCount());
        upgrade.baseValue = 100f;
        Assert.AreEqual(3, SynergyCombatResolver.RollEmissionCount());
        Assert.AreEqual(2, SynergyCombatResolver.RollEmissionCount(1));
        Assert.AreEqual(1, SynergyCombatResolver.RollEmissionCount(0));
        Assert.AreEqual(1, SynergyCombatResolver.RollEmissionCount(-1));
        upgrade.baseValue = 0f;
        Assert.AreEqual(1, SynergyCombatResolver.RollEmissionCount(), "A previous successful burst must not become shared state.");
    }

    [Test]
    public void MultishotMakesItsOwnDecisionAfterAProjectileRoll()
    {
        Upgrade(UpgradeType.MultiShot, 50f);
        int seed = 0;
        for (; seed < 1000; seed++)
        {
            Random.InitState(seed);
            if (Random.value < 0.5f && Random.value > 0.5f) break;
        }
        Assert.Less(seed, 1000);
        Random.InitState(seed);
        bool projectileMultishot = Random.value < stats.GetMultiShotProbability() / 100f;
        Assert.IsTrue(projectileMultishot);
        Assert.AreEqual(1, SynergyCombatResolver.RollEmissionCount(),
            "A successful projectile decision must not force the laser, EMP or cryo decision.");
    }

    [Test]
    public void LaserMultishotUsesDistinctDirectionsAndReusesVisualsAfterUpgradeChanges()
    {
        UpgradeData upgrade = Upgrade(UpgradeType.MultiShot, 100f);
        var config = Asset<LaserBeamConfig>();
        config.fireShake = 0f;
        config.visualPrefabOverride = GameObject("Beam prefab fixture");
        config.visualPrefabOverride.AddComponent<LineRenderer>();
        var effect = Component<LaserBeamEffect>();
        effect.Configure(config);
        effect.Activate(player);
        Invoke(effect, "BeginSweep", Vector3.forward * 5f);
        Assert.AreEqual(3, Get<int>(effect, "activeBeamCount"));
        var visuals = Get<IList>(effect, "beamVisuals");
        Assert.AreEqual(3, visuals.Count);
        Assert.Greater(Vector3.Angle(Get<Vector3>(visuals[0], "direction"), Get<Vector3>(visuals[1], "direction")), 1f);
        Assert.Greater(Vector3.Angle(Get<Vector3>(visuals[1], "direction"), Get<Vector3>(visuals[2], "direction")), 1f);
        object extraVisual = visuals[1];

        upgrade.baseValue = 0f;
        Invoke(effect, "BeginSweep", Vector3.forward * 5f);
        Assert.AreEqual(1, Get<int>(effect, "activeBeamCount"));
        Assert.AreEqual(3, visuals.Count);
        Assert.IsFalse(Get<Transform>(visuals[1], "root").gameObject.activeSelf);
        upgrade.baseValue = 100f;
        Invoke(effect, "BeginSweep", Vector3.forward * 5f);
        Assert.AreSame(extraVisual, visuals[1], "Repeated procs must reuse the existing beam visuals.");

        // Give this real effect an active registry entry, then disable its
        // behaviour while leaving its GameObject present in that registry.
        var data = Asset<SynergyData>();
        data.effectConfig = config;
        Get<Dictionary<SynergyData, GameObject>>(synergies, "activeEffects").Add(data, effect.gameObject);
        effect.gameObject.SetActive(true);
        Assert.AreSame(config, synergies.GetActiveConfig<LaserBeamConfig>());
        effect.enabled = false;
        Assert.IsNull(synergies.GetActiveConfig<LaserBeamConfig>());
        Assert.IsFalse(Get<bool>(effect, "sweeping"));
        for (int i = 0; i < visuals.Count; i++)
            Assert.IsFalse(Get<Transform>(visuals[i], "root").gameObject.activeSelf);
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

    [TestCase(SynergyAttackSource.Laser)]
    [TestCase(SynergyAttackSource.Cryo)]
    [TestCase(SynergyAttackSource.Emp)]
    public void EachTickHasAtMostOneExplosionAndNeverRecurses(SynergyAttackSource source)
    {
        UpgradeData explosive = Upgrade(UpgradeType.ExplosiveShot, 100f);
        EnemyHealth first = Enemy(Vector3.zero);
        EnemyHealth second = Enemy(Vector3.right);
        EnemyHealth bystander = Enemy(Vector3.right * 2f);
        EnemyHealth outsideFirstExplosion = Enemy(Vector3.right * 4f);
        var combat = new SynergyCombatResolver();
        combat.BeginTick(source, 10f, Vector3.back, Vector3.forward);
        combat.Hit(first);
        combat.Hit(first);
        combat.Hit(second);
        explosive.baseValue = 0f;
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
        Assert.AreEqual(90f, Health(bystander), "The next tick must use its new 0% probability.");
    }

    [Test]
    public void EmptyOrCancelledTicksCannotReuseAnEarlierImpact()
    {
        Upgrade(UpgradeType.ExplosiveShot, 100f);
        EnemyHealth enemy = Enemy(Vector3.zero);
        var combat = new SynergyCombatResolver();
        combat.BeginTick(SynergyAttackSource.Laser, 10f, Vector3.back, Vector3.forward);
        combat.Hit(enemy);
        combat.EndTick();
        Assert.AreEqual(80f, Health(enemy));
        combat.Hit(enemy);
        combat.BeginTick(SynergyAttackSource.Cryo, 10f, Vector3.back, Vector3.forward);
        combat.EndTick();
        Assert.AreEqual(80f, Health(enemy));
    }

    [Test]
    public void ExplosionAndKnockbackHaveIndependentRollsAndCarryCryoSlow()
    {
        UpgradeData explosive = Upgrade(UpgradeType.ExplosiveShot, 0f);
        UpgradeData knockback = Upgrade(UpgradeType.Knockback, 100f);
        CryoFieldConfig cryo = ActiveSynergy<CryoFieldConfig>();
        EnemyHealth target = Enemy(Vector3.zero);
        EnemyHealth neighbor = Enemy(Vector3.right * 2f);
        var combat = new SynergyCombatResolver();
        combat.BeginTick(SynergyAttackSource.Cryo, 10f, Vector3.back, Vector3.forward);
        combat.Hit(target);
        combat.EndTick();
        Assert.IsTrue(Get<bool>(target.Controller, "isKnockedBack"));
        Assert.IsTrue(Get<bool>(neighbor.Controller, "isKnockedBack"), "Knockback can chain when explosion fails.");
        Assert.AreEqual(100f, Health(neighbor));

        Set(target.Controller, "isKnockedBack", false);
        Set(neighbor.Controller, "isKnockedBack", false);
        explosive.baseValue = 100f;
        knockback.baseValue = 0f;
        combat.BeginTick(SynergyAttackSource.Cryo, 10f, Vector3.back, Vector3.forward);
        combat.Hit(target);
        combat.EndTick();
        Assert.AreEqual(90f, Health(neighbor), "Explosion can succeed when knockback fails.");
        Assert.IsFalse(Get<bool>(target.Controller, "isKnockedBack"));
        Assert.IsFalse(Get<bool>(neighbor.Controller, "isKnockedBack"));
        Assert.AreEqual(cryo.slowMultiplier, Get<float>(neighbor.Controller, "slowMultiplier"));

        knockback.baseValue = 100f;
        combat.BeginTick(SynergyAttackSource.Cryo, 10f, Vector3.back, Vector3.forward);
        combat.Hit(target);
        combat.EndTick();
        Assert.IsTrue(Get<bool>(neighbor.Controller, "isKnockedBack"), "Explosion also carries the tick's successful push.");
    }

    [Test]
    public void LaserCarriesFrostAndUsesStrongestExistingControlBonus()
    {
        CryoFieldConfig cryo = ActiveSynergy<CryoFieldConfig>();
        EmpPulseConfig emp = ActiveSynergy<EmpPulseConfig>();
        EnemyHealth fresh = Enemy(Vector3.zero);
        EnemyHealth slowed = Enemy(Vector3.right);
        EnemyHealth frozen = Enemy(Vector3.left);
        slowed.Controller.ApplySlow(cryo.slowMultiplier, 5f);
        frozen.Controller.ApplySlow(0f, 5f);
        var combat = new SynergyCombatResolver();
        combat.BeginTick(SynergyAttackSource.Laser, 10f, Vector3.back, Vector3.forward);
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
    public void EmpUsesPlayerDamageForExplosionsUntilLaserOverloadIsActive()
    {
        Upgrade(UpgradeType.ExplosiveShot, 100f);
        EnemyHealth target = Enemy(Vector3.zero);
        var combat = new SynergyCombatResolver();
        combat.BeginTick(SynergyAttackSource.Emp, 0f, Vector3.back, Vector3.forward);
        combat.Hit(target);
        Assert.AreEqual(100f, Health(target), "Base EMP remains a control attack.");
        combat.EndTick();
        Assert.AreEqual(100f - stats.GetModifiedDamage(), Health(target));

        LaserBeamConfig laser = ActiveSynergy<LaserBeamConfig>();
        laser.damage = 12f;
        laser.empDamageMultiplier = 0.5f;
        float previousHealth = Health(target);
        combat.BeginTick(SynergyAttackSource.Emp, 0f, Vector3.back, Vector3.forward);
        combat.Hit(target);
        combat.EndTick();
        Assert.AreEqual(previousHealth - 12f, Health(target), "Overloaded EMP deals 6 direct damage and one 6-damage explosion.");
    }

    [TestCase(SynergyAttackSource.Laser)]
    [TestCase(SynergyAttackSource.Cryo)]
    public void ZeroDamageAttacksCannotGainPlayerDamageThroughExplosion(SynergyAttackSource source)
    {
        Upgrade(UpgradeType.ExplosiveShot, 100f);
        EnemyHealth target = Enemy(Vector3.zero);
        EnemyHealth neighbor = Enemy(Vector3.right);
        var combat = new SynergyCombatResolver();
        combat.BeginTick(source, 0f, Vector3.back, Vector3.forward);
        combat.Hit(target);
        combat.EndTick();
        Assert.AreEqual(100f, Health(target));
        Assert.AreEqual(100f, Health(neighbor));
    }

    [Test]
    public void LethalExplosionStillHitsAllCollectedTargets()
    {
        Upgrade(UpgradeType.ExplosiveShot, 100f);
        EnemyHealth seed = Enemy(Vector3.zero, 5f);
        EnemyHealth first = Enemy(Vector3.right, 5f);
        EnemyHealth second = Enemy(Vector3.left, 5f);
        EnemyHealth third = Enemy(Vector3.forward, 5f);
        var combat = new SynergyCombatResolver();
        combat.BeginTick(SynergyAttackSource.Cryo, 10f, Vector3.back, Vector3.forward);
        combat.Hit(seed);
        combat.EndTick();
        Assert.IsFalse(seed.gameObject.activeSelf);
        Assert.IsFalse(first.gameObject.activeSelf);
        Assert.IsFalse(second.gameObject.activeSelf);
        Assert.IsFalse(third.gameObject.activeSelf);
        Assert.AreEqual(0, EnemyHealth.ActiveEnemies.Count);
    }

    [Test]
    public void CryoMultishotHasTwoDelayedEchoesWithoutRecursiveBursts()
    {
        Upgrade(UpgradeType.MultiShot, 100f, 100);
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
        Assert.AreEqual(2, Get<int>(effect, "remainingExtraPulses"));
        effect.OnUpdate(0.1f);
        Assert.AreEqual(990f, Health(enemy));
        effect.OnUpdate(0.11f);
        Assert.AreEqual(980f, Health(enemy));
        effect.OnUpdate(0.21f);
        Assert.AreEqual(970f, Health(enemy));
        Assert.AreEqual(0, Get<int>(effect, "remainingExtraPulses"));
        effect.OnUpdate(0.5f);
        Assert.AreEqual(970f, Health(enemy), "Echoes must not roll their own multishot, even at 100% chance.");
        effect.OnUpdate(0.51f);
        Assert.AreEqual(960f, Health(enemy));
        Invoke(effect, "OnDisable");
        Assert.AreEqual(0, Get<int>(effect, "remainingExtraPulses"), "Disabling cancels pending echoes.");
    }

    [Test]
    public void EmpMultishotWaitsBetweenWavesAndCryoExtendsEachFreeze()
    {
        Upgrade(UpgradeType.MultiShot, 100f, 100);
        LaserBeamConfig laser = ActiveSynergy<LaserBeamConfig>();
        laser.damage = 10f;
        laser.empDamageMultiplier = 0.5f;
        ActiveSynergy<CryoFieldConfig>();
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
        effect.OnUpdate(0.21f);
        effect.OnUpdate(0.11f);
        Assert.AreEqual(985f, Health(enemy));
        Assert.AreEqual(0, Get<int>(effect, "remainingExtraPulses"));
        effect.OnUpdate(1f);
        Assert.AreEqual(985f, Health(enemy), "After three waves, the regular interval resumes.");
    }

    [Test]
    public void EmpChainSurvivesLethalOverloadButStopsAtConfiguredHopCount()
    {
        LaserBeamConfig laser = ActiveSynergy<LaserBeamConfig>();
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

    private UpgradeData Upgrade(UpgradeType type, float probability, int level = 1)
    {
        var upgrade = Asset<UpgradeData>();
        upgrade.upgradeType = type;
        upgrade.baseValue = probability;
        upgrade.multiplierPerLevel = 1f;
        upgrades.allUpgrades.Add(upgrade);
        Get<Dictionary<UpgradeType, int>>(stats, "upgradeLevels")[type] = level;
        return upgrade;
    }

    private T ActiveSynergy<T>() where T : SynergyEffectConfig
    {
        var config = Asset<T>();
        var data = Asset<SynergyData>();
        data.effectConfig = config;
        GameObject marker = GameObject("Active synergy fixture");
        marker.SetActive(true);
        Get<Dictionary<SynergyData, GameObject>>(synergies, "activeEffects").Add(data, marker);
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

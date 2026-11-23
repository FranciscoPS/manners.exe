using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class FinalRushTests
{
    private const float Tolerance = 0.0001f;
    private const float TierSeconds = 10f;
    private const float ToughnessPerTier = 4f;
    private const float ResistancePerTier = 0.11f;
    private const float FastPlayerSpeed = 30f;
    private const float ChaseRatioStart = 0.6f;
    private const float ChaseRatioPerTier = 0.07f;
    private const float ChaseRatioMax = 1.25f;
    private const string LevelScenePath = "Assets/Scenes/Final Levels/LEVEL 1/LEVEL 1.unity";
    private const string TutorialKey = "TutorialCompleted_v1";
    private const int MinimumHorde = 10;
    private const float HordeWaitSeconds = 8f;
    private const int StormMultiplier = 4;
    private const float HeavyResistance = 0.88f;
    private const float ChainPushForce = 8.6f;
    private const float PushSeconds = 0.3f;

    private GameObject enemyObject;

    [TearDown]
    public void RestoreScaling()
    {
        FinalRushScaling.Clear();
        if (enemyObject != null) Object.DestroyImmediate(enemyObject);
    }

    [UnityTest, Timeout(240000)]
    public IEnumerator AnnihilationChestClearsTheOvertimeHordeEvenAtMaximumToughness()
    {
        bool hadTutorial = PlayerPrefs.HasKey(TutorialKey);
        int tutorialPreference = PlayerPrefs.GetInt(TutorialKey, 0);
        PlayerPrefs.SetInt(TutorialKey, 1);
        EditorSceneManager.OpenScene(LevelScenePath, OpenSceneMode.Single);
        yield return new EnterPlayMode();
        try
        {
            yield return new WaitForSecondsRealtime(0.25f);
            TutorialManager tutorial = Object.FindFirstObjectByType<TutorialManager>();
            if (tutorial != null && tutorial.IsTutorialPanelActive)
            {
                var skip = (Button)typeof(TutorialManager).GetField("skipAllButton", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(tutorial);
                skip.onClick.Invoke();
            }
            Object.FindFirstObjectByType<PlayerHealth>().SetInvulnerable(true);
            SandboxCommands.ForceFinalRush();
            float deadline = Time.realtimeSinceStartup + HordeWaitSeconds;
            while (CountAliveEnemies() < MinimumHorde && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.GreaterOrEqual(CountAliveEnemies(), MinimumHorde, "El overtime debe haber traído enemigos antes de abrir el cofre.");
            ChestItemData annihilation = ScriptableObject.CreateInstance<ChestItemData>();
            annihilation.effect = ChestItemEffect.KillAllEnemies;
            FinalRushScaling.Set(FinalRushMath.MaxToughness, 1f, 0f, 0f);

            ChestItemProvider.ApplyEffect(annihilation);

            Assert.AreEqual(0, CountAliveEnemies(), "El cofre de aniquilación debe matar a todos aunque el overtime haya escalado el aguante al máximo.");
            Object.Destroy(annihilation);
        }
        finally
        {
            FinalRushScaling.Clear();
            if (hadTutorial) PlayerPrefs.SetInt(TutorialKey, tutorialPreference); else PlayerPrefs.DeleteKey(TutorialKey);
            PlayerPrefs.Save();
        }
        yield return new ExitPlayMode();
    }

    [UnityTest, Timeout(240000)]
    public IEnumerator HitStormsKeepDamageNumbersUnderTheirCap()
    {
        bool hadTutorial = PlayerPrefs.HasKey(TutorialKey);
        int tutorialPreference = PlayerPrefs.GetInt(TutorialKey, 0);
        PlayerPrefs.SetInt(TutorialKey, 1);
        EditorSceneManager.OpenScene(LevelScenePath, OpenSceneMode.Single);
        yield return new EnterPlayMode();
        try
        {
            yield return new WaitForSecondsRealtime(0.25f);
            FloatingTextManager numbers = FloatingTextManager.Instance;
            Assert.IsNotNull(numbers, "LEVEL 1 necesita el FloatingTextManager.");
            Camera view = Camera.main;
            Vector3 center = view.transform.position + view.transform.forward * 10f;
            int storm = numbers.MaxDamageNumbers * StormMultiplier;

            for (int i = 0; i < storm; i++) numbers.ShowDamage(i + 1, center);
            yield return null;

            Assert.AreEqual(numbers.MaxDamageNumbers, numbers.ActiveTexts, "Una lluvia de golpes no debe pasar del tope de números de daño.");
            Assert.AreEqual(numbers.MaxDamageNumbers, Object.FindObjectsByType<FloatingText>(FindObjectsSortMode.None).Length);
            numbers.ShowCoins(5, center);
            Assert.AreEqual(numbers.MaxDamageNumbers + 1, numbers.ActiveTexts, "Las monedas se muestran aunque los números de daño estén al tope.");
        }
        finally
        {
            if (hadTutorial) PlayerPrefs.SetInt(TutorialKey, tutorialPreference); else PlayerPrefs.DeleteKey(TutorialKey);
            PlayerPrefs.Save();
        }
        yield return new ExitPlayMode();
    }

    private static int CountAliveEnemies()
    {
        int alive = 0;
        for (int i = 0; i < EnemyHealth.ActiveEnemies.Count; i++)
            if (EnemyHealth.ActiveEnemies[i] != null && EnemyHealth.ActiveEnemies[i].IsAlive) alive++;
        return alive;
    }

    [Test]
    public void ToughnessGrowsContinuouslyAndStaysFinite()
    {
        float start = FinalRushMath.Toughness(FinalRushMath.Tiers(0f, TierSeconds), ToughnessPerTier);
        float halfTier = FinalRushMath.Toughness(FinalRushMath.Tiers(5f, TierSeconds), ToughnessPerTier);
        float oneTier = FinalRushMath.Toughness(FinalRushMath.Tiers(10f, TierSeconds), ToughnessPerTier);
        float huge = FinalRushMath.Toughness(FinalRushMath.Tiers(9000f, TierSeconds), ToughnessPerTier);

        Assert.AreEqual(1f, start, Tolerance);
        Assert.AreEqual(2f, halfTier, Tolerance);
        Assert.AreEqual(4f, oneTier, Tolerance);
        Assert.AreEqual(FinalRushMath.MaxToughness, huge);
    }

    [Test]
    public void ControlResistanceRisesSteadilyUntilItsCeiling()
    {
        float start = FinalRushMath.ControlResistance(FinalRushMath.Tiers(0f, TierSeconds), ResistancePerTier, 1f);
        float midway = FinalRushMath.ControlResistance(FinalRushMath.Tiers(45f, TierSeconds), ResistancePerTier, 1f);
        float immune = FinalRushMath.ControlResistance(FinalRushMath.Tiers(120f, TierSeconds), ResistancePerTier, 1f);
        float capped = FinalRushMath.ControlResistance(FinalRushMath.Tiers(120f, TierSeconds), ResistancePerTier, 0.85f);

        Assert.AreEqual(0f, start, Tolerance);
        Assert.AreEqual(0.495f, midway, Tolerance);
        Assert.AreEqual(1f, immune, Tolerance);
        Assert.AreEqual(0.85f, capped, Tolerance);
    }

    [Test]
    public void SpeedAndContactDamageFloorsGrowWithOvertime()
    {
        float startSpeed = FinalRushMath.Speed(0f, 6f, 1.5f, 22f);
        float laterSpeed = FinalRushMath.Speed(4f, 6f, 1.5f, 22f);
        float maxSpeed = FinalRushMath.Speed(30f, 6f, 1.5f, 22f);
        float laterDamage = FinalRushMath.ContactDamage(9f, 12f, 18f);

        Assert.AreEqual(6f, startSpeed, Tolerance);
        Assert.AreEqual(12f, laterSpeed, Tolerance);
        Assert.AreEqual(22f, maxSpeed, Tolerance);
        Assert.AreEqual(174f, laterDamage, Tolerance);
    }

    [Test]
    public void ChaseSpeedEndsFasterThanThePlayerHoweverFastTheBuild()
    {
        float atStart = FinalRushMath.ChaseSpeed(0f, FastPlayerSpeed, ChaseRatioStart, ChaseRatioPerTier, ChaseRatioMax);
        float later = FinalRushMath.ChaseSpeed(10f, FastPlayerSpeed, ChaseRatioStart, ChaseRatioPerTier, ChaseRatioMax);
        float standingStill = FinalRushMath.ChaseSpeed(10f, 0f, ChaseRatioStart, ChaseRatioPerTier, ChaseRatioMax);

        Assert.AreEqual(FastPlayerSpeed * ChaseRatioStart, atStart, Tolerance);
        Assert.AreEqual(FastPlayerSpeed * ChaseRatioMax, later, Tolerance);
        Assert.Greater(later, FastPlayerSpeed);
        Assert.AreEqual(0f, standingStill, Tolerance);
    }

    [Test]
    public void OvertimeCapNeverPassesTheHardLimitEvenWhenTheNormalCapIsHigher()
    {
        int lowAtStart = FinalRushMath.Cap(0f, 300, 380, 30f, 450);
        int lowLater = FinalRushMath.Cap(999f, 300, 380, 30f, 450);
        int highAtStart = FinalRushMath.Cap(0f, 300, 900, 30f, 450);
        int highAtEnd = FinalRushMath.Cap(30f, 300, 900, 30f, 450);

        Assert.AreEqual(380, lowAtStart);
        Assert.AreEqual(380, lowLater);
        Assert.AreEqual(450, highAtStart);
        Assert.AreEqual(900, highAtEnd);
    }

    [Test]
    public void ToughEnemiesShowTheRealHitButLoseLessHealth()
    {
        EnemyHealth enemy = CreateEnemy(100f);
        FinalRushScaling.Set(4f, 0f, 0f, 0f);

        enemy.TakeDamage(40f);

        Assert.AreEqual(90f, enemy.CurrentHealth, Tolerance);
        Assert.IsTrue(enemy.IsAlive);
    }

    [Test]
    public void FullControlResistanceIgnoresFreezeSlowAndKnockback()
    {
        EnemyHealth enemy = CreateEnemy(100f);
        FinalRushScaling.Set(1f, 1f, 0f, 0f);

        enemy.Controller.ApplySlow(0f, 5f);
        enemy.Controller.ApplyKnockback(Vector3.forward, 20f, 0.5f);

        Assert.IsFalse(enemy.Controller.IsSlowed);
        Assert.IsTrue(enemy.Controller.WantsSeparation, "Un empujón resistido no debe dejarlo en retroceso.");
    }

    [Test]
    public void NegligibleResistedPushesLeaveTheEnemyChasing()
    {
        EnemyHealth resisting = CreateEnemy(100f);
        FinalRushScaling.Set(1f, HeavyResistance, 0f, 0f);

        bool pushed = resisting.Controller.ApplyKnockback(Vector3.forward, ChainPushForce, PushSeconds);

        Assert.IsFalse(pushed, "Un empujón de centímetros no debe sacar al enemigo de su NavMeshAgent.");
        Assert.IsTrue(resisting.Controller.WantsSeparation);
        FinalRushScaling.Clear();
        Assert.IsTrue(resisting.Controller.ApplyKnockback(Vector3.forward, ChainPushForce, PushSeconds), "Sin resistencia el mismo empujón sí se aplica.");
        Assert.IsFalse(resisting.Controller.WantsSeparation);
    }

    [Test]
    public void PartialControlResistanceShortensAndSoftensTheSlow()
    {
        EnemyHealth enemy = CreateEnemy(100f);
        FinalRushScaling.Set(1f, 0.5f, 0f, 0f);

        enemy.Controller.ApplySlow(0f, 4f);

        Assert.IsTrue(enemy.Controller.IsSlowed);
        Assert.IsFalse(enemy.Controller.IsFrozen, "A mitad de resistencia un congelamiento queda en ralentización.");
    }

    [Test]
    public void OvertimeFloorRaisesContactDamageOfEnemiesAlreadyInScene()
    {
        EnemyHealth enemy = CreateEnemy(100f);
        enemy.Controller.SetStats(3f, 10f, 0f);

        FinalRushScaling.Set(1f, 0f, 15f, 120f);

        Assert.AreEqual(120f, enemy.Controller.ContactDamage, Tolerance);
        Assert.AreEqual(0f, enemy.Controller.ControlResistance, Tolerance);
    }

    private EnemyHealth CreateEnemy(float health)
    {
        enemyObject = new GameObject("FinalRushEnemy");
        enemyObject.AddComponent<EnemyController>();
        EnemyHealth enemy = enemyObject.AddComponent<EnemyHealth>();
        enemy.SetConfiguration(health, PoolManager.PoolType.BasicEnemy, null, 0, 0, 1f, 0f, 0, 0, 0f, 0, 0);
        return enemy;
    }
}

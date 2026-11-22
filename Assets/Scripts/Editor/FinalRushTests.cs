using NUnit.Framework;
using UnityEngine;

public class FinalRushTests
{
    private const float Tolerance = 0.0001f;
    private const float NearInvincibleSeconds = 40f;
    private const float NearInvincibleDamage = 0.05f;
    private const float IndestructibleSeconds = 60f;

    [TearDown]
    public void RestoreArmor()
    {
        EnemyHealth.SetDamageTakenScale(1f);
    }

    [Test]
    public void ArmorFallsFromFullDamageToNearInvincibleAndThenIndestructible()
    {
        float start = Scale(0f);
        float twenty = Scale(20f);
        float forty = Scale(40f);
        float fifty = Scale(50f);
        float sixty = Scale(60f);
        float later = Scale(300f);

        Assert.AreEqual(1f, start, Tolerance);
        Assert.AreEqual(Mathf.Sqrt(NearInvincibleDamage), twenty, Tolerance);
        Assert.AreEqual(NearInvincibleDamage, forty, Tolerance);
        Assert.AreEqual(NearInvincibleDamage * 0.5f, fifty, Tolerance);
        Assert.AreEqual(0f, sixty);
        Assert.AreEqual(0f, later);
    }

    [Test]
    public void ArmorNeverGivesDamageBackAsOvertimeGoesOn()
    {
        float previous = 1f;

        for (float seconds = 0f; seconds <= 90f; seconds += 0.5f)
        {
            float scale = Scale(seconds);
            Assert.LessOrEqual(scale, previous + Tolerance, seconds + " s");
            previous = scale;
        }
    }

    [Test]
    public void WithoutIndestructibleSecondsEnemiesStayAtTheNearInvincibleDamage()
    {
        float scale = FinalRushMath.DamageTakenScale(500f, NearInvincibleSeconds, NearInvincibleDamage, 0f);

        Assert.AreEqual(NearInvincibleDamage, scale, Tolerance);
    }

    [Test]
    public void OvertimeCapNeverPassesTheHardLimitEvenWhenTheNormalCapIsHigher()
    {
        int lowAtStart = FinalRushMath.Cap(0f, 300, 380, 30f, 450);
        int lowLater = FinalRushMath.Cap(999f, 300, 380, 30f, 450);
        int highAtStart = FinalRushMath.Cap(0f, 300, 600, 30f, 450);
        int highAtEnd = FinalRushMath.Cap(30f, 300, 600, 30f, 450);

        Assert.AreEqual(380, lowAtStart);
        Assert.AreEqual(380, lowLater);
        Assert.AreEqual(450, highAtStart);
        Assert.AreEqual(600, highAtEnd);
    }

    [Test]
    public void ExponentialHealthStopsAtAFiniteCeiling()
    {
        float early = FinalRushMath.Health(200f, 4f, 2);
        float huge = FinalRushMath.Health(200f, 4f, 200);

        Assert.AreEqual(3200f, early, Tolerance);
        Assert.AreEqual(FinalRushMath.MaxHealth, huge);
    }

    [Test]
    public void IndestructibleEnemiesSurviveALethalHit()
    {
        var enemy = new GameObject("FinalRushEnemy").AddComponent<EnemyHealth>();
        try
        {
            enemy.SetConfiguration(10f, PoolManager.PoolType.BasicEnemy, null, 0, 0, 1f, 0f, 0, 0, 0f, 0, 0);
            EnemyHealth.SetDamageTakenScale(0f);

            enemy.TakeDamage(1000f);

            Assert.IsTrue(enemy.gameObject.activeSelf);
        }
        finally
        {
            Object.DestroyImmediate(enemy.gameObject);
        }
    }

    private static float Scale(float seconds)
    {
        return FinalRushMath.DamageTakenScale(seconds, NearInvincibleSeconds, NearInvincibleDamage, IndestructibleSeconds);
    }
}

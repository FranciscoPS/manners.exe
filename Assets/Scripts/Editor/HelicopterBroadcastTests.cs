using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class HelicopterBroadcastTests
{
    private const float ClassicDistance = 20f;
    private const float ClassicFieldOfView = 60f;
    private const float TelephotoFieldOfView = 36f;
    private const float Tolerance = 0.001f;

    [Test]
    public void TelephotoDistanceKeepsTheClassicVisibleHeight()
    {
        float classicHeight = HelicopterCameraMath.FramedHeight(ClassicDistance, ClassicFieldOfView);

        float telephotoDistance = HelicopterCameraMath.DistanceForFramedHeight(classicHeight, TelephotoFieldOfView);

        Assert.AreEqual(ClassicDistance, HelicopterCameraMath.DistanceForFramedHeight(classicHeight, ClassicFieldOfView), Tolerance);
        Assert.AreEqual(classicHeight, HelicopterCameraMath.FramedHeight(telephotoDistance, TelephotoFieldOfView), Tolerance);
        Assert.Greater(telephotoDistance, ClassicDistance);
    }

    [Test]
    public void OrbitStartsCenteredAndNeverExceedsItsAmplitude()
    {
        const float amplitude = 10f;
        const float period = 42f;
        float largest = 0f;

        for (float time = 0f; time < period * 3f; time += 0.05f)
            largest = Mathf.Max(largest, Mathf.Abs(HelicopterCameraMath.Orbit(time, amplitude, period)));

        Assert.AreEqual(0f, HelicopterCameraMath.Orbit(0f, amplitude, period), Tolerance);
        Assert.LessOrEqual(largest, amplitude + Tolerance);
        Assert.Greater(largest, amplitude * 0.5f);
    }

    [Test]
    public void ZoomPunchRisesHoldsAndReturnsToTheNormalShot()
    {
        const float attack = 0.4f;
        const float hold = 1f;
        const float release = 2f;

        float before = HelicopterCameraMath.PunchEnvelope(-0.1f, attack, hold, release);
        float rising = HelicopterCameraMath.PunchEnvelope(attack * 0.5f, attack, hold, release);
        float holding = HelicopterCameraMath.PunchEnvelope(attack + hold * 0.5f, attack, hold, release);
        float releasing = HelicopterCameraMath.PunchEnvelope(attack + hold + release * 0.5f, attack, hold, release);
        float after = HelicopterCameraMath.PunchEnvelope(attack + hold + release + 0.1f, attack, hold, release);

        Assert.AreEqual(0f, before, Tolerance);
        Assert.That(rising, Is.GreaterThan(0f).And.LessThan(1f));
        Assert.AreEqual(1f, holding, Tolerance);
        Assert.That(releasing, Is.GreaterThan(0f).And.LessThan(1f));
        Assert.AreEqual(0f, after, Tolerance);
    }

    [Test]
    public void CrowdOpensTheShotOnlyBetweenItsThresholds()
    {
        Assert.AreEqual(0f, HelicopterCameraMath.CrowdFactor(30, 60, 220), Tolerance);
        Assert.AreEqual(0.5f, HelicopterCameraMath.CrowdFactor(140, 60, 220), Tolerance);
        Assert.AreEqual(1f, HelicopterCameraMath.CrowdFactor(500, 60, 220), Tolerance);
    }

    [Test]
    public void FieldOfViewWidensWithTheCrowdAndNarrowsWithAPunch()
    {
        float calm = HelicopterCameraMath.ZoomedFieldOfView(TelephotoFieldOfView, 0.15f, 0f, 0f);

        float crowded = HelicopterCameraMath.ZoomedFieldOfView(TelephotoFieldOfView, 0.15f, 1f, 0f);
        float punched = HelicopterCameraMath.ZoomedFieldOfView(TelephotoFieldOfView, 0.15f, 0f, 0.1f);

        Assert.AreEqual(TelephotoFieldOfView, calm, Tolerance);
        Assert.AreEqual(TelephotoFieldOfView * 1.15f, crowded, Tolerance);
        Assert.AreEqual(TelephotoFieldOfView * 0.9f, punched, Tolerance);
    }

    [Test]
    public void DeadZoneStaysOpenWhileTheRobotKeepsMoving()
    {
        float zone = 1f;
        float idle = 0f;

        for (int frame = 0; frame < 300; frame++)
        {
            idle = HelicopterCameraMath.IdleTime(idle, 7f, 0.5f, 1f / 60f);
            zone = HelicopterCameraMath.RecenterZone(zone, idle, 0.8f, 0.6f, 1f / 60f);
        }

        Assert.AreEqual(0f, idle, Tolerance);
        Assert.AreEqual(1f, zone, Tolerance);
    }

    [Test]
    public void OperatorRecentersAfterTheRobotStopsAndReopensWhenItMoves()
    {
        float zone = 1f;
        float idle = 0f;

        for (int frame = 0; frame < 120; frame++)
        {
            idle = HelicopterCameraMath.IdleTime(idle, 0f, 0.5f, 1f / 60f);
            zone = HelicopterCameraMath.RecenterZone(zone, idle, 0.8f, 0.6f, 1f / 60f);
        }
        float afterStopping = zone;
        idle = HelicopterCameraMath.IdleTime(idle, 7f, 0.5f, 0.3f);
        zone = HelicopterCameraMath.RecenterZone(zone, idle, 0.8f, 0.6f, 0.3f);

        Assert.AreEqual(0f, afterStopping, Tolerance);
        Assert.AreEqual(0.5f, zone, Tolerance);
    }

    [Test]
    public void RecenterDelayOfZeroKeepsTheDeadZoneAlwaysOpen()
    {
        float zone = HelicopterCameraMath.RecenterZone(1f, 60f, 0f, 0.6f, 1f);

        Assert.AreEqual(1f, zone, Tolerance);
    }

    [Test]
    public void AltitudeBreathingStaysWithinItsFraction()
    {
        const float amplitude = 0.03f;

        for (float time = 0f; time < 60f; time += 0.1f)
        {
            float scale = HelicopterCameraMath.AltitudeScale(time, amplitude, 13f);
            Assert.That(scale, Is.InRange(1f - amplitude - Tolerance, 1f + amplitude + Tolerance));
        }
    }

    [Test]
    public void GimbalDriftStaysWithinItsAmplitude()
    {
        const float amplitude = 0.35f;

        for (float time = 0f; time < 120f; time += 0.25f)
        {
            Vector2 drift = HelicopterCameraMath.Drift(time, 0.18f, amplitude, 7.1f);
            Assert.LessOrEqual(Mathf.Abs(drift.x), amplitude + Tolerance);
            Assert.LessOrEqual(Mathf.Abs(drift.y), amplitude + Tolerance);
        }
    }

    [Test]
    public void UrgentHeadlineInterruptsButAnEqualOneWaitsForTheCooldown()
    {
        var schedule = new NewsBulletinSchedule();

        bool first = schedule.TryStart(1, 6f, 12f);
        schedule.Advance(2f);
        bool equal = schedule.TryStart(1, 6f, 12f);
        bool urgent = schedule.TryStart(3, 6f, 12f);

        Assert.IsTrue(first);
        Assert.IsFalse(equal);
        Assert.IsTrue(urgent);
    }

    [Test]
    public void AnyHeadlineCanAirOnceTheCooldownHasPassed()
    {
        var schedule = new NewsBulletinSchedule();
        schedule.TryStart(3, 6f, 12f);

        schedule.Advance(10f);
        bool duringCooldown = schedule.TryStart(0, 6f, 12f);
        schedule.Advance(8.01f);
        bool afterCooldown = schedule.TryStart(0, 6f, 12f);

        Assert.IsFalse(duringCooldown);
        Assert.IsTrue(afterCooldown);
    }

    [Test]
    public void ScheduleReportsTheEndOfAHeadlineExactlyOnce()
    {
        var schedule = new NewsBulletinSchedule();
        schedule.TryStart(0, 6f, 12f);

        bool midway = schedule.Advance(3f);
        bool ended = schedule.Advance(3.5f);
        bool afterwards = schedule.Advance(1f);

        Assert.IsFalse(midway);
        Assert.IsTrue(ended);
        Assert.IsFalse(afterwards);
        Assert.IsFalse(schedule.IsShowing);
    }

    [Test]
    public void HeadlinesOfAStoryRotateInOrderAndWrapAround()
    {
        var schedule = new NewsBulletinSchedule();
        var picked = new List<int>();

        for (int i = 0; i < 4; i++) picked.Add(schedule.NextHeadlineIndex(NewsStory.LevelUp, 3));
        int otherStory = schedule.NextHeadlineIndex(NewsStory.ChestSpawned, 3);

        CollectionAssert.AreEqual(new[] { 0, 1, 2, 0 }, picked);
        Assert.AreEqual(0, otherStory);
        Assert.AreEqual(-1, schedule.NextHeadlineIndex(NewsStory.Overtime, 0));
    }

    [Test]
    public void ChannelClockAdvancesWithTheMatchAndWrapsAtMidnight()
    {
        Assert.AreEqual(478, NewsBroadcastConfig.ClockMinutes(478, 59f));
        Assert.AreEqual(493, NewsBroadcastConfig.ClockMinutes(478, 15f * 60f));
        Assert.AreEqual(1, NewsBroadcastConfig.ClockMinutes(1439, 120f));
    }

    [Test]
    public void DefaultNewsCoversEveryStoryInBothLanguages()
    {
        NewsBroadcastConfig news = ScriptableObject.CreateInstance<NewsBroadcastConfig>();
        try
        {
            HelicopterBroadcastTools.ApplyDefaultNews(news);

            Assert.IsTrue(news.liveLabel.HasBothLanguages);
            Assert.Greater(news.tickerItems.Length, 0);
            foreach (LocalizedString item in news.tickerItems) Assert.IsTrue(item.HasBothLanguages);
            for (int story = 0; story < NewsBroadcastConfig.StoryCount; story++)
            {
                NewsStoryPool pool = news.Pool((NewsStory)story);
                Assert.Greater(pool.headlines.Length, 0, ((NewsStory)story).ToString());
                foreach (NewsHeadline headline in pool.headlines)
                {
                    Assert.IsTrue(headline.headline.HasBothLanguages, headline.headline.Get(GameLanguage.English));
                    Assert.IsTrue(headline.subline.HasBothLanguages, headline.subline.Get(GameLanguage.English));
                }
            }
        }
        finally
        {
            Object.DestroyImmediate(news);
        }
    }

    [Test]
    public void DistanceStepsSpreadFromTheClosestToTheFarthestWithNormalInTheMiddle()
    {
        var scales = new float[5];

        for (int step = 0; step < scales.Length; step++) scales[step] = HelicopterCameraMath.DistanceScale(step, 5, 0.8f, 1.3f);

        Assert.AreEqual(0.8f, scales[0], Tolerance);
        Assert.AreEqual(0.9f, scales[1], Tolerance);
        Assert.AreEqual(1f, scales[2], Tolerance);
        Assert.AreEqual(1.15f, scales[3], Tolerance);
        Assert.AreEqual(1.3f, scales[4], Tolerance);
    }

    [Test]
    public void MissingDistanceRangeKeepsTheNormalFraming()
    {
        float closest = HelicopterCameraMath.DistanceScale(0, 5, 0f, 0f);

        float farthest = HelicopterCameraMath.DistanceScale(4, 5, 0f, 0f);

        Assert.AreEqual(1f, closest, Tolerance);
        Assert.AreEqual(1f, farthest, Tolerance);
    }

    [Test]
    public void FollowSpeedSlowsDownOrSpeedsUpAroundNormal()
    {
        Assert.AreEqual(1.6f, HelicopterCameraMath.FollowScale(0, 3, 1.6f, 0.5f), Tolerance);
        Assert.AreEqual(1f, HelicopterCameraMath.FollowScale(1, 3, 1.6f, 0.5f), Tolerance);
        Assert.AreEqual(0.5f, HelicopterCameraMath.FollowScale(2, 3, 1.6f, 0.5f), Tolerance);
        Assert.AreEqual(1f, HelicopterCameraMath.FollowScale(0, 3, 0f, 0f), Tolerance);
    }

    [Test]
    public void ZoomPunchShrinksTheShotByTheSameFractionAtAnyDistance()
    {
        float classicHeight = HelicopterCameraMath.FramedHeight(ClassicDistance, ClassicFieldOfView);
        float punched = HelicopterCameraMath.ZoomedFieldOfView(TelephotoFieldOfView, 0f, 0f, 0.1f);

        float near = HelicopterCameraMath.DistanceForFramedHeight(classicHeight * 0.8f, TelephotoFieldOfView);
        float far = HelicopterCameraMath.DistanceForFramedHeight(classicHeight * 1.3f, TelephotoFieldOfView);
        float nearRatio = HelicopterCameraMath.FramedHeight(near, punched) / HelicopterCameraMath.FramedHeight(near, TelephotoFieldOfView);
        float farRatio = HelicopterCameraMath.FramedHeight(far, punched) / HelicopterCameraMath.FramedHeight(far, TelephotoFieldOfView);

        Assert.AreEqual(nearRatio, farRatio, Tolerance);
        Assert.Less(nearRatio, 1f);
        Assert.Greater(far, near);
    }

    [Test]
    public void TickerShowsBatchesOfTheConfiguredSize()
    {
        Assert.AreEqual(8, NewsBroadcastConfig.TickerBatchSize(8, 36));
        Assert.AreEqual(5, NewsBroadcastConfig.TickerBatchSize(8, 5));
        Assert.AreEqual(36, NewsBroadcastConfig.TickerBatchSize(0, 36));
        Assert.AreEqual(0, NewsBroadcastConfig.TickerBatchSize(8, 0));
    }

    [Test]
    public void MergingDefaultTickerAddsOnlyTheMissingItems()
    {
        NewsBroadcastConfig news = ScriptableObject.CreateInstance<NewsBroadcastConfig>();
        NewsBroadcastConfig defaults = ScriptableObject.CreateInstance<NewsBroadcastConfig>();
        try
        {
            HelicopterBroadcastTools.ApplyDefaultNews(defaults);
            news.tickerItems = new[] { defaults.tickerItems[0], new LocalizedString("Custom line", "Línea propia") };
            news.tickerItemsPerLoop = 0;

            int added = HelicopterBroadcastTools.MergeDefaultTicker(news);
            int again = HelicopterBroadcastTools.MergeDefaultTicker(news);

            Assert.AreEqual(defaults.tickerItems.Length - 1, added);
            Assert.AreEqual(0, again);
            Assert.AreEqual(defaults.tickerItems.Length + 1, news.tickerItems.Length);
            Assert.AreEqual("Custom line", news.tickerItems[1].Get(GameLanguage.English));
            Assert.AreEqual(defaults.tickerItemsPerLoop, news.tickerItemsPerLoop);
        }
        finally
        {
            Object.DestroyImmediate(news);
            Object.DestroyImmediate(defaults);
        }
    }

    [Test]
    public void StoryCountMatchesTheStoryEnum()
    {
        Assert.AreEqual(NewsBroadcastConfig.StoryCount, System.Enum.GetValues(typeof(NewsStory)).Length);
    }
}

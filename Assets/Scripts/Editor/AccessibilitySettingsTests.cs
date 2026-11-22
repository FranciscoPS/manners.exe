using NUnit.Framework;
using UnityEngine;

public class AccessibilitySettingsTests
{
    private string savedPrefs;
    private bool hadPrefs;

    [SetUp]
    public void SavePrefs()
    {
        hadPrefs = PlayerPrefs.HasKey(GameAccessibilitySettings.PrefsKey);
        savedPrefs = hadPrefs ? PlayerPrefs.GetString(GameAccessibilitySettings.PrefsKey) : null;
    }

    [TearDown]
    public void RestorePrefs()
    {
        if (hadPrefs) PlayerPrefs.SetString(GameAccessibilitySettings.PrefsKey, savedPrefs);
        else PlayerPrefs.DeleteKey(GameAccessibilitySettings.PrefsKey);
        PlayerPrefs.Save();
        GameAccessibilitySettings.Reload();
    }

    [Test]
    public void DefaultsKeepEveryCameraEffectOnAtTheNormalDistance()
    {
        var settings = new GameAccessibilitySettings.SettingsData();

        GameAccessibilitySettings.CameraPreset preset = GameAccessibilitySettings.PresetOf(settings);

        Assert.AreEqual(GameAccessibilitySettings.CameraPreset.Helicopter, preset);
        Assert.IsTrue(settings.cameraPans && settings.cameraSway && settings.autoZoom && settings.impactShake && settings.deadZone && settings.tvFilter);
        Assert.IsFalse(settings.lockOnRobot);
        Assert.AreEqual(GameAccessibilitySettings.NormalFollowSpeed, settings.followSpeed);
        Assert.AreEqual(GameAccessibilitySettings.NormalDistance, settings.cameraDistance);
    }

    [Test]
    public void FixedPresetTurnsEveryMovementOffAndLocksTheCameraOnTheRobot()
    {
        var settings = new GameAccessibilitySettings.SettingsData { cameraDistance = 4, tvFilter = false };

        GameAccessibilitySettings.SettingsData fixedCamera = GameAccessibilitySettings.WithPreset(settings, GameAccessibilitySettings.CameraPreset.Fixed);

        Assert.IsFalse(fixedCamera.cameraPans || fixedCamera.cameraSway || fixedCamera.autoZoom || fixedCamera.impactShake || fixedCamera.deadZone);
        Assert.IsTrue(fixedCamera.lockOnRobot);
        Assert.AreEqual(4, fixedCamera.cameraDistance);
        Assert.IsFalse(fixedCamera.tvFilter);
        Assert.AreEqual(GameAccessibilitySettings.CameraPreset.Fixed, GameAccessibilitySettings.PresetOf(fixedCamera));
    }

    [Test]
    public void ChangingOneOptionAfterAPresetReportsACustomProfile()
    {
        GameAccessibilitySettings.SettingsData gentle = GameAccessibilitySettings.WithPreset(new GameAccessibilitySettings.SettingsData(), GameAccessibilitySettings.CameraPreset.Gentle);

        gentle.impactShake = false;

        Assert.AreEqual(GameAccessibilitySettings.CameraPreset.Custom, GameAccessibilitySettings.PresetOf(gentle));
    }

    [Test]
    public void SanitizeKeepsStepsInsideTheirRanges()
    {
        var settings = new GameAccessibilitySettings.SettingsData { followSpeed = 9, cameraDistance = -3 };

        GameAccessibilitySettings.SettingsData clean = GameAccessibilitySettings.Sanitize(settings);

        Assert.AreEqual(GameAccessibilitySettings.FollowSpeedSteps - 1, clean.followSpeed);
        Assert.AreEqual(0, clean.cameraDistance);
    }

    [Test]
    public void AppliedSettingsSurviveAReload()
    {
        var settings = new GameAccessibilitySettings.SettingsData { cameraPans = false, lockOnRobot = true, cameraDistance = 3 };
        int changes = 0;
        System.Action count = () => changes++;
        GameAccessibilitySettings.Changed += count;

        GameAccessibilitySettings.Apply(settings);
        GameAccessibilitySettings.Reload();
        GameAccessibilitySettings.SettingsData loaded = GameAccessibilitySettings.Current;
        GameAccessibilitySettings.Changed -= count;

        Assert.IsFalse(loaded.cameraPans);
        Assert.IsTrue(loaded.lockOnRobot);
        Assert.AreEqual(3, loaded.cameraDistance);
        Assert.AreEqual(2, changes);
    }
}

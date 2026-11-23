using NUnit.Framework;
using UnityEngine;

public class AccessibilitySettingsTests
{
    private const float SavedDistance = 0.3f;

    private string savedPrefs;
    private bool hadPrefs;
    private string savedPreviousPrefs;
    private bool hadPreviousPrefs;

    [SetUp]
    public void SavePrefs()
    {
        hadPrefs = PlayerPrefs.HasKey(GameAccessibilitySettings.PrefsKey);
        savedPrefs = hadPrefs ? PlayerPrefs.GetString(GameAccessibilitySettings.PrefsKey) : null;
        hadPreviousPrefs = PlayerPrefs.HasKey(GameAccessibilitySettings.PreviousPrefsKey);
        savedPreviousPrefs = hadPreviousPrefs ? PlayerPrefs.GetString(GameAccessibilitySettings.PreviousPrefsKey) : null;
    }

    [TearDown]
    public void RestorePrefs()
    {
        if (hadPrefs) PlayerPrefs.SetString(GameAccessibilitySettings.PrefsKey, savedPrefs);
        else PlayerPrefs.DeleteKey(GameAccessibilitySettings.PrefsKey);
        if (hadPreviousPrefs) PlayerPrefs.SetString(GameAccessibilitySettings.PreviousPrefsKey, savedPreviousPrefs);
        else PlayerPrefs.DeleteKey(GameAccessibilitySettings.PreviousPrefsKey);
        PlayerPrefs.Save();
        GameAccessibilitySettings.Reload();
    }

    [Test]
    public void DefaultsStartAsHelicopterWithAutoZoomOffAndTheCameraFar()
    {
        var settings = new GameAccessibilitySettings.SettingsData();

        GameAccessibilitySettings.CameraPreset preset = GameAccessibilitySettings.PresetOf(settings);

        Assert.AreEqual(GameAccessibilitySettings.CameraPreset.Helicopter, preset);
        Assert.IsTrue(settings.cameraPans && settings.cameraSway && settings.impactShake && settings.deadZone);
        Assert.IsFalse(settings.autoZoom, "El zoom automático empieza apagado.");
        Assert.IsFalse(settings.lockOnRobot);
        Assert.AreEqual(GameAccessibilitySettings.NormalFollowSpeed, settings.followSpeed);
        Assert.AreEqual(GameAccessibilitySettings.DefaultCameraDistance, settings.cameraDistance);
        Assert.Greater(settings.cameraDistance, GameAccessibilitySettings.NormalCameraDistance);
        Assert.IsFalse(GameAccessibilitySettings.Defaults.autoZoom, PlayerSettingsDefaults.ResourceName + " también deja el zoom automático apagado.");
    }

    [Test]
    public void NoPresetTurnsAutoZoomOn()
    {
        var zoomed = new GameAccessibilitySettings.SettingsData { autoZoom = true };

        GameAccessibilitySettings.SettingsData helicopter = GameAccessibilitySettings.WithPreset(zoomed, GameAccessibilitySettings.CameraPreset.Helicopter);
        GameAccessibilitySettings.SettingsData gentle = GameAccessibilitySettings.WithPreset(zoomed, GameAccessibilitySettings.CameraPreset.Gentle);
        GameAccessibilitySettings.SettingsData fixedCamera = GameAccessibilitySettings.WithPreset(zoomed, GameAccessibilitySettings.CameraPreset.Fixed);

        Assert.IsFalse(helicopter.autoZoom || gentle.autoZoom || fixedCamera.autoZoom);
        Assert.AreEqual(GameAccessibilitySettings.CameraPreset.Custom, GameAccessibilitySettings.PresetOf(zoomed), "Encenderlo a mano deja el perfil en Personalizado.");
    }

    [Test]
    public void SettingsSavedBeforeTurnOffAutoZoomAndKeepTheRest()
    {
        var old = new GameAccessibilitySettings.SettingsData { autoZoom = true, cameraPans = false, cameraDistance = SavedDistance };
        PlayerPrefs.DeleteKey(GameAccessibilitySettings.PrefsKey);
        PlayerPrefs.SetString(GameAccessibilitySettings.PreviousPrefsKey, JsonUtility.ToJson(old));

        GameAccessibilitySettings.Reload();
        GameAccessibilitySettings.SettingsData loaded = GameAccessibilitySettings.Current;

        Assert.IsFalse(loaded.autoZoom, "Las preferencias guardadas con el zoom encendido por el valor anterior quedan apagadas.");
        Assert.IsFalse(loaded.cameraPans);
        Assert.AreEqual(SavedDistance, loaded.cameraDistance, 0.0001f);
    }

    [Test]
    public void FixedPresetTurnsEveryMovementOffAndLocksTheCameraOnTheRobot()
    {
        var settings = new GameAccessibilitySettings.SettingsData { cameraDistance = 0.2f };

        GameAccessibilitySettings.SettingsData fixedCamera = GameAccessibilitySettings.WithPreset(settings, GameAccessibilitySettings.CameraPreset.Fixed);

        Assert.IsFalse(fixedCamera.cameraPans || fixedCamera.cameraSway || fixedCamera.autoZoom || fixedCamera.impactShake || fixedCamera.deadZone);
        Assert.IsTrue(fixedCamera.lockOnRobot);
        Assert.AreEqual(0.2f, fixedCamera.cameraDistance);
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
    public void SanitizeKeepsStepsAndTheDistanceSliderInsideTheirRanges()
    {
        var tooFar = new GameAccessibilitySettings.SettingsData { followSpeed = 9, cameraDistance = 7f };
        var tooClose = new GameAccessibilitySettings.SettingsData { followSpeed = -2, cameraDistance = -3f };
        var broken = new GameAccessibilitySettings.SettingsData { cameraDistance = float.NaN };

        GameAccessibilitySettings.SettingsData far = GameAccessibilitySettings.Sanitize(tooFar);
        GameAccessibilitySettings.SettingsData close = GameAccessibilitySettings.Sanitize(tooClose);
        GameAccessibilitySettings.SettingsData repaired = GameAccessibilitySettings.Sanitize(broken);

        Assert.AreEqual(GameAccessibilitySettings.FollowSpeedSteps - 1, far.followSpeed);
        Assert.AreEqual(1f, far.cameraDistance);
        Assert.AreEqual(0, close.followSpeed);
        Assert.AreEqual(0f, close.cameraDistance);
        Assert.AreEqual(GameAccessibilitySettings.DefaultCameraDistance, repaired.cameraDistance);
    }

    [Test]
    public void SameSettingsNoticesEveryOptionTheMenuCanChange()
    {
        var saved = new GameAccessibilitySettings.SettingsData();
        GameAccessibilitySettings.SettingsData moved = saved.Clone();
        GameAccessibilitySettings.SettingsData locked = saved.Clone();

        moved.cameraDistance = 0.4f;
        locked.lockOnRobot = true;

        Assert.IsTrue(GameAccessibilitySettings.SameSettings(saved, saved.Clone()));
        Assert.IsFalse(GameAccessibilitySettings.SameSettings(saved, moved));
        Assert.IsFalse(GameAccessibilitySettings.SameSettings(saved, locked));
    }

    [Test]
    public void ResetRestoresTheDefaultsFromThePlayerSettingsAsset()
    {
        PlayerSettingsDefaults asset = PlayerSettingsDefaults.Instance;
        GameAccessibilitySettings.Apply(new GameAccessibilitySettings.SettingsData { cameraPans = false, cameraDistance = 0.1f });

        GameAccessibilitySettings.ResetToDefaults();
        GameAccessibilitySettings.SettingsData restored = GameAccessibilitySettings.Current;

        Assert.IsNotNull(asset, "Falta " + PlayerSettingsDefaults.ResourceName + " en Resources.");
        Assert.IsTrue(GameAccessibilitySettings.SameSettings(GameAccessibilitySettings.Sanitize(asset.accessibility), restored));
    }

    [Test]
    public void AppliedSettingsSurviveAReload()
    {
        var settings = new GameAccessibilitySettings.SettingsData { cameraPans = false, lockOnRobot = true, cameraDistance = 0.3f };
        int changes = 0;
        System.Action count = () => changes++;
        GameAccessibilitySettings.Changed += count;

        GameAccessibilitySettings.Apply(settings);
        GameAccessibilitySettings.Reload();
        GameAccessibilitySettings.SettingsData loaded = GameAccessibilitySettings.Current;
        GameAccessibilitySettings.Changed -= count;

        Assert.IsFalse(loaded.cameraPans);
        Assert.IsTrue(loaded.lockOnRobot);
        Assert.AreEqual(0.3f, loaded.cameraDistance, 0.0001f);
        Assert.AreEqual(2, changes);
    }
}

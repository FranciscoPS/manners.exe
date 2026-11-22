using System;
using UnityEngine;

public static class GameAccessibilitySettings
{
    public enum CameraPreset { Helicopter, Gentle, Fixed, Custom }

    public const int FollowSpeedSteps = 3;
    public const int NormalFollowSpeed = 1;
    public const int DistanceSteps = 5;
    public const int NormalDistance = 2;

    [Serializable]
    public sealed class SettingsData
    {
        public bool cameraPans = true;
        public bool cameraSway = true;
        public bool autoZoom = true;
        public bool impactShake = true;
        public bool lockOnRobot;
        public bool deadZone = true;
        public int followSpeed = NormalFollowSpeed;
        public int cameraDistance = NormalDistance;
        public bool tvFilter = true;

        public SettingsData Clone() => (SettingsData)MemberwiseClone();
    }

    public const string PrefsKey = "AccessibilitySettings.v1";

    private static SettingsData current;

    public static event Action Changed;

    public static SettingsData Current => Load().Clone();

    public static bool ImpactShake => Load().impactShake;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        current = null;
        Changed = null;
    }

    public static void Apply(SettingsData settings)
    {
        current = Sanitize(settings);
        PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(current));
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    public static void ResetToDefaults() => Apply(new SettingsData());

    public static void Reload()
    {
        current = null;
        Load();
        Changed?.Invoke();
    }

    public static SettingsData Sanitize(SettingsData settings)
    {
        SettingsData result = settings != null ? settings.Clone() : new SettingsData();
        result.followSpeed = Mathf.Clamp(result.followSpeed, 0, FollowSpeedSteps - 1);
        result.cameraDistance = Mathf.Clamp(result.cameraDistance, 0, DistanceSteps - 1);
        return result;
    }

    public static SettingsData WithPreset(SettingsData settings, CameraPreset preset)
    {
        SettingsData result = Sanitize(settings);
        switch (preset)
        {
            case CameraPreset.Helicopter:
                SetMotion(result, true, true, true, true, false, true);
                break;
            case CameraPreset.Gentle:
                SetMotion(result, false, false, true, true, false, false);
                break;
            case CameraPreset.Fixed:
                SetMotion(result, false, false, false, false, true, false);
                break;
            default:
                return result;
        }
        result.followSpeed = NormalFollowSpeed;
        return result;
    }

    public static CameraPreset PresetOf(SettingsData settings)
    {
        for (int i = 0; i < (int)CameraPreset.Custom; i++)
        {
            var preset = (CameraPreset)i;
            if (SameMotion(settings, WithPreset(settings, preset))) return preset;
        }
        return CameraPreset.Custom;
    }

    private static SettingsData Load()
    {
        if (current != null) return current;

        var loaded = new SettingsData();
        if (PlayerPrefs.HasKey(PrefsKey))
        {
            try { JsonUtility.FromJsonOverwrite(PlayerPrefs.GetString(PrefsKey), loaded); }
            catch (ArgumentException) { loaded = new SettingsData(); }
        }
        current = Sanitize(loaded);
        return current;
    }

    private static void SetMotion(SettingsData settings, bool pans, bool sway, bool zoom, bool shake, bool lockOn, bool deadZone)
    {
        settings.cameraPans = pans;
        settings.cameraSway = sway;
        settings.autoZoom = zoom;
        settings.impactShake = shake;
        settings.lockOnRobot = lockOn;
        settings.deadZone = deadZone;
    }

    private static bool SameMotion(SettingsData a, SettingsData b)
    {
        return a.cameraPans == b.cameraPans && a.cameraSway == b.cameraSway && a.autoZoom == b.autoZoom
            && a.impactShake == b.impactShake && a.lockOnRobot == b.lockOnRobot && a.deadZone == b.deadZone
            && a.followSpeed == b.followSpeed;
    }
}

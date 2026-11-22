using System;
using UnityEngine;

public static class GameAccessibilitySettings
{
    public enum CameraPreset { Helicopter, Gentle, Fixed, Custom }

    public const int FollowSpeedSteps = 3;
    public const int NormalFollowSpeed = 1;
    public const float NormalCameraDistance = 0.5f;
    public const float DefaultCameraDistance = 0.75f;

    [Serializable]
    public sealed class SettingsData
    {
        [Tooltip("Paneos de cámara: el helicóptero rodea al robot de lado a lado.")]
        public bool cameraPans = true;
        [Tooltip("Movimiento de cámara: vaivén suave y cambios de altura.")]
        public bool cameraSway = true;
        [Tooltip("Zooms automáticos: acercamientos en momentos clave y plano abierto con hordas.")]
        public bool autoZoom = true;
        [Tooltip("Sacudidas de pantalla por explosiones, golpes y cofres.")]
        public bool impactShake = true;
        [Tooltip("Fijar cámara al robot: siempre al centro, sin retraso ni zona muerta.")]
        public bool lockOnRobot;
        [Tooltip("Zona muerta: el robot se mueve un poco antes de que la cámara lo siga.")]
        public bool deadZone = true;
        [Tooltip("Velocidad de seguimiento: 0 = lenta, 1 = normal, 2 = rápida.")]
        [Range(0, FollowSpeedSteps - 1)] public int followSpeed = NormalFollowSpeed;
        [Tooltip("Posición del slider de distancia: 0 = lo más cerca, 0.5 = encuadre normal, 1 = lo más lejos. El rango real está en HelicopterCameraConfig.")]
        [Range(0f, 1f)] public float cameraDistance = DefaultCameraDistance;

        public SettingsData Clone() => (SettingsData)MemberwiseClone();
    }

    public const string PrefsKey = "AccessibilitySettings.v2";

    private static SettingsData current;

    public static event Action Changed;

    public static SettingsData Current => Load().Clone();

    public static bool ImpactShake => Load().impactShake;

    public static SettingsData Defaults
    {
        get
        {
            PlayerSettingsDefaults asset = PlayerSettingsDefaults.Instance;
            return Sanitize(asset != null ? asset.accessibility : null);
        }
    }

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

    public static void ResetToDefaults() => Apply(Defaults);

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
        result.cameraDistance = float.IsNaN(result.cameraDistance) ? DefaultCameraDistance : Mathf.Clamp01(result.cameraDistance);
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

    public static bool SameSettings(SettingsData a, SettingsData b)
    {
        if (a == null || b == null) return a == b;
        return SameMotion(a, b) && Mathf.Approximately(a.cameraDistance, b.cameraDistance);
    }

    private static SettingsData Load()
    {
        if (current != null) return current;

        SettingsData loaded = Defaults;
        if (PlayerPrefs.HasKey(PrefsKey))
        {
            try { JsonUtility.FromJsonOverwrite(PlayerPrefs.GetString(PrefsKey), loaded); }
            catch (ArgumentException) { loaded = Defaults; }
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

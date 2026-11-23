using System;
using System.Globalization;
using UnityEngine;

public static class GameLocalization
{
    public const GameLanguage FallbackLanguage = GameLanguage.Spanish;
    private const string PreferenceKey = "GameLanguage";
    private static bool initialized;
    private static GameLanguage language = FallbackLanguage;
    private static readonly CultureInfo EnglishCulture = CultureInfo.GetCultureInfo("en-US");
    private static readonly CultureInfo SpanishCulture = CultureInfo.GetCultureInfo("es-MX");
    public static event Action LanguageChanged;
    public static GameLanguage Language { get { Initialize(); return language; } }
    public static CultureInfo Culture => Language == GameLanguage.Spanish ? SpanishCulture : EnglishCulture;
    public static string NativeName => Language == GameLanguage.Spanish ? "Español" : "English";

    public static GameLanguage DefaultLanguage
    {
        get
        {
            PlayerSettingsDefaults defaults = PlayerSettingsDefaults.Instance;
            return defaults != null ? defaults.language : FallbackLanguage;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        initialized = false;
        language = FallbackLanguage;
        LanguageChanged = null;
    }

    public static void Initialize()
    {
        if (initialized) return;
        language = SavedOrDefault(PlayerPrefs.GetInt(PreferenceKey, (int)DefaultLanguage));
        initialized = true;
    }

    public static GameLanguage SavedOrDefault(int saved)
    {
        if (saved == (int)GameLanguage.English) return GameLanguage.English;
        return saved == (int)GameLanguage.Spanish ? GameLanguage.Spanish : DefaultLanguage;
    }

    public static void SetLanguage(GameLanguage value)
    {
        Initialize();
        if (value != GameLanguage.English && value != GameLanguage.Spanish) return;
        if (language == value) return;
        language = value;
        PlayerPrefs.SetInt(PreferenceKey, (int)value);
        PlayerPrefs.Save();
        LanguageChanged?.Invoke();
    }

    public static void Step(int direction) => SetLanguage(Language == GameLanguage.English ? GameLanguage.Spanish : GameLanguage.English);
}

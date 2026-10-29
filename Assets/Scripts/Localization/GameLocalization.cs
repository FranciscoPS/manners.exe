using System;
using System.Globalization;
using UnityEngine;

public static class GameLocalization
{
    private const string PreferenceKey = "GameLanguage";
    private static bool initialized;
    private static GameLanguage language;
    private static readonly CultureInfo EnglishCulture = CultureInfo.GetCultureInfo("en-US");
    private static readonly CultureInfo SpanishCulture = CultureInfo.GetCultureInfo("es-MX");
    public static event Action LanguageChanged;
    public static GameLanguage Language { get { Initialize(GameLanguage.English); return language; } }
    public static CultureInfo Culture => Language == GameLanguage.Spanish ? SpanishCulture : EnglishCulture;
    public static string NativeName => Language == GameLanguage.Spanish ? "Español" : "English";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        initialized = false;
        language = GameLanguage.English;
        LanguageChanged = null;
    }

    public static void Initialize(GameLanguage defaultLanguage)
    {
        if (initialized) return;
        int saved = PlayerPrefs.GetInt(PreferenceKey, (int)defaultLanguage);
        language = saved == (int)GameLanguage.Spanish ? GameLanguage.Spanish : GameLanguage.English;
        initialized = true;
    }

    public static void SetLanguage(GameLanguage value)
    {
        Initialize(GameLanguage.English);
        if (value != GameLanguage.English && value != GameLanguage.Spanish) return;
        if (language == value) return;
        language = value;
        PlayerPrefs.SetInt(PreferenceKey, (int)value);
        PlayerPrefs.Save();
        LanguageChanged?.Invoke();
    }

    public static void Step(int direction) => SetLanguage(Language == GameLanguage.English ? GameLanguage.Spanish : GameLanguage.English);
}

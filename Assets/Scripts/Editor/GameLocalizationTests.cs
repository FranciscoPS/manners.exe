using NUnit.Framework;

public class GameLocalizationTests
{
    [Test]
    public void CodeFallbackIsSpanishWhenTheDefaultsAssetIsMissing()
    {
        GameLanguage fallback = GameLocalization.FallbackLanguage;

        Assert.AreEqual(GameLanguage.Spanish, fallback);
    }

    [Test]
    public void DefaultLanguageComesFromThePlayerSettingsDefaultsAsset()
    {
        PlayerSettingsDefaults defaults = PlayerSettingsDefaults.Instance;

        GameLanguage language = GameLocalization.DefaultLanguage;

        Assert.IsNotNull(defaults, "Falta " + PlayerSettingsDefaults.ResourceName + " en Resources.");
        Assert.AreEqual(defaults.language, language);
    }

    [Test]
    public void SavedChoiceWinsAndUnknownValuesFallBackToTheDefault()
    {
        GameLanguage english = GameLocalization.SavedOrDefault((int)GameLanguage.English);
        GameLanguage spanish = GameLocalization.SavedOrDefault((int)GameLanguage.Spanish);
        GameLanguage unknown = GameLocalization.SavedOrDefault(42);

        Assert.AreEqual(GameLanguage.English, english);
        Assert.AreEqual(GameLanguage.Spanish, spanish);
        Assert.AreEqual(GameLocalization.DefaultLanguage, unknown);
    }
}

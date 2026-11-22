using System;
using UnityEngine;

public enum NewsStory
{
    MatchStart,
    LevelUp,
    ChestSpawned,
    ShopVisited,
    OverrideActivated,
    LowHealth,
    Overtime
}

[Serializable]
public struct NewsHeadline
{
    [Tooltip("Titular grande, en la barra roja.")]
    public LocalizedString headline;
    [Tooltip("Bajada, en la barra clara debajo del titular.")]
    public LocalizedString subline;

    public NewsHeadline(LocalizedString headline, LocalizedString subline)
    {
        this.headline = headline;
        this.subline = subline;
    }
}

[Serializable]
public struct NewsStoryPool
{
    [Tooltip("Un titular más urgente interrumpe al que está en pantalla; uno igual o menos urgente se descarta durante la pausa entre titulares.")]
    [Range(0, 5)] public int priority;
    [Tooltip("Titulares que se turnan en orden cada vez que ocurre el evento.")]
    public NewsHeadline[] headlines;

    public NewsStoryPool(int priority, NewsHeadline[] headlines)
    {
        this.priority = priority;
        this.headlines = headlines;
    }
}

[CreateAssetMenu(fileName = "NewsBroadcastConfig", menuName = "Game/Noticiero en vivo")]
public sealed class NewsBroadcastConfig : ScriptableObject
{
    public const int StoryCount = 7;

    [Header("Canal")]
    [Tooltip("Nombre del canal en el logo. Es una marca: no se traduce.")]
    public string channelName = "BD NEWS";
    [Tooltip("Marca de transmisión en directo.")]
    public LocalizedString liveLabel = new LocalizedString("LIVE", "EN VIVO");
    [Tooltip("Hora del reloj del canal al empezar la partida, en minutos desde medianoche (7:58 = 478). Avanza con el tiempo de partida.")]
    [Range(0, 1439)] public int clockStartMinutes = 478;
    [Tooltip("Pulsos por segundo de la marca EN VIVO.")]
    [Min(0f)] public float livePulseSpeed = 0.8f;
    [Tooltip("Opacidad mínima de la placa EN VIVO al pulsar.")]
    [Range(0f, 1f)] public float livePulseMinAlpha = 0.55f;

    [Header("Rótulo")]
    [Tooltip("Segundos que el titular se queda en pantalla.")]
    [Min(1f)] public float headlineDuration = 6f;
    [Tooltip("Segundos mínimos entre el final de un titular y el siguiente (los más urgentes pueden interrumpir).")]
    [Min(0f)] public float headlineCooldown = 12f;
    [Tooltip("Segundos tras empezar la partida antes del primer titular.")]
    [Min(0f)] public float firstHeadlineDelay = 2f;
    [Tooltip("Segundos de la animación de entrada y de salida del rótulo.")]
    [Min(0.05f)] public float wipeDuration = 0.35f;
    [Tooltip("Cada cuántos niveles sale un titular de nivel.")]
    [Min(1)] public int levelUpEvery = 3;
    [Tooltip("Fracción de vida bajo la cual sale el titular de vida baja. Vuelve a armarse al recuperar un 10 % por encima.")]
    [Range(0.05f, 0.9f)] public float lowHealthThreshold = 0.3f;

    [Header("Cintillo")]
    [Tooltip("Velocidad del cintillo en unidades del Canvas por segundo.")]
    [Min(1f)] public float tickerSpeed = 110f;
    [Tooltip("Separador entre noticias del cintillo.")]
    public string tickerSeparator = "   -   ";
    [Tooltip("Noticias del cintillo, en orden.")]
    public LocalizedString[] tickerItems = new LocalizedString[0];

    [Header("Titulares por evento (nunca mencionar combinaciones de sobrecargas)")]
    public NewsStoryPool matchStart;
    public NewsStoryPool levelUp;
    public NewsStoryPool chestSpawned;
    public NewsStoryPool shopVisited;
    public NewsStoryPool overrideActivated;
    public NewsStoryPool lowHealth;
    public NewsStoryPool overtime;

    public NewsStoryPool Pool(NewsStory story)
    {
        switch (story)
        {
            case NewsStory.MatchStart: return matchStart;
            case NewsStory.LevelUp: return levelUp;
            case NewsStory.ChestSpawned: return chestSpawned;
            case NewsStory.ShopVisited: return shopVisited;
            case NewsStory.OverrideActivated: return overrideActivated;
            case NewsStory.LowHealth: return lowHealth;
            default: return overtime;
        }
    }

    public string FormatClock(float matchSeconds)
    {
        int minutes = ClockMinutes(clockStartMinutes, matchSeconds);
        return (minutes / 60).ToString("00") + ":" + (minutes % 60).ToString("00");
    }

    public static int ClockMinutes(int startMinutes, float matchSeconds)
    {
        int elapsed = Mathf.FloorToInt(Mathf.Max(0f, matchSeconds) / 60f);
        return ((startMinutes + elapsed) % 1440 + 1440) % 1440;
    }
}

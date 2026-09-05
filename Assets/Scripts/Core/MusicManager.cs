using UnityEngine;
using DG.Tweening;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;

[Serializable]
public class MusicLoopSection
{
    [Tooltip("Loop que se repite. El último loop de la lista suena hasta que se acaba el tiempo de partida (y para siempre si la escena no tiene overtime configurado).")]
    public AudioClip loopClip;
    [Tooltip("Puente que suena UNA vez DESPUÉS de este loop, antes del siguiente loop. Dejar vacío en el último loop.")]
    public AudioClip bridgeClip;
    [Tooltip("0 = automático (reparte el tiempo de partida entre los loops). >0 = veces exactas que se repite este loop antes del puente.")]
    public int repeatCount = 0;
    [Tooltip("0 = automático. >0 = segundos de partida en los que debe COMENZAR este loop (su puente previo termina justo antes). Los loops anteriores se reparten para llenar hasta aquí. Ej: 150 = empieza ~2:30.")]
    public float startAtSeconds = 0f;
}

[Serializable]
public class SceneMusicConfig
{
    public int sceneIndex;
    [Tooltip("Carpeta con los clips de esta escena (ej. Assets/Music/TrejoMusic/Ciudad). 'Tools > Manners > Música > Asignar loops por nombre' llena los campos de abajo leyendo los nombres de los archivos: '<prefijo> intro', '<prefijo> loopN', '<prefijo> puenteN' y '<prefijo> outro'. Si se deja vacío, se deduce de la carpeta de los clips ya asignados.")]
    public string clipFolder;
    [Tooltip("Opcional. Suena una vez al inicio y luego pasa a los loops.")]
    public AudioClip introClip;
    [Tooltip("Loops y puentes en orden: loop1, puente1, loop2, puente2, loop3... El último loop suena hasta que se acaba el tiempo de partida. Si está vacío se usa el 'loopClip' de abajo.")]
    public MusicLoopSection[] loopSections;
    [Tooltip("Puente que suena UNA vez en el instante en que se acaba el tiempo de partida (inicio del overtime); el loop que esté sonando se apaga con un fundido corto. Opcional.")]
    public AudioClip overtimeBridgeClip;
    [Tooltip("Loop que suena para siempre después del puente de overtime (o directo al acabarse el tiempo, si no hay puente). Si se deja vacío, tras el puente vuelve el último loop normal.")]
    public AudioClip overtimeLoopClip;
    [Tooltip("LEGADO: loop infinito simple. Solo se usa si 'loopSections' está vacío.")]
    public AudioClip loopClip;
}

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Music Settings")]
    [SerializeField] private AudioClip menuMusic;
    [Tooltip("Un entry por escena de gameplay. Puedes dejar introClip vacío si la escena solo tiene loop.")]
    [SerializeField] private SceneMusicConfig[] sceneMusicConfigs;
    [SerializeField][Range(0f, 1f)] private float musicVolume = 0.5f;
    [SerializeField] private bool playOnAwake = true;

    [Header("SFX Settings")]
    [SerializeField][Range(0f, 1f)] private float sfxVolume = 0.8f;

    [Header("UI SFX")]
    public AudioClip hoverSFX;
    public AudioClip clickSFX;

    [Header("Volume Reduction Settings")]
    [SerializeField][Range(0f, 1f)] private float reducedVolumeMultiplier = 0.3f;
    [SerializeField] private float volumeFadeDuration = 0.5f;

    [Header("Scene Options")]
    [Tooltip("Índice de la escena del menú principal. Si la escena activa coincide, la música no se reproducirá automáticamente.")]
    [SerializeField] private int mainMenuSceneIndex = 0;

    [Header("Music Sequence")]
    [Tooltip("Duración (segundos) usada para repartir las repeticiones de los loops. Si hay GameTimeManager se usa su duración de partida; si no, este valor (600 = 10 min).")]
    [SerializeField] private float fallbackMatchDurationSeconds = 600f;

    [Header("Overtime")]
    [Tooltip("Segundos del fundido con el que se apaga el loop en curso cuando se acaba el tiempo de partida. El puente de overtime entra de inmediato, encima del fundido, para que el corte se sienta como un bajón.")]
    [SerializeField] private float overtimeFadeSeconds = 1.2f;

    private AudioSource introSource;
    private AudioSource loopSource;
    private AudioSource overtimeSource;
    private AudioSource[] musicSources;
    private AudioSource[] allMusicSources;
    private readonly HashSet<AudioSource> fadingOutSources = new HashSet<AudioSource>();
    private Coroutine musicSequenceCoroutine;
    private Coroutine overtimeCoroutine;
    private SceneMusicConfig activeConfig;
    private AudioClip lastRegularLoop;
    private bool overtimeStarted;
    private AudioSource menuSource;
    private AudioSource sfxLoopSource;
    private AudioSource sfxOneShotSource;
    private float savedMusicVolume;
    private bool isVolumeReduced = false;

    private const string MUSIC_VOLUME_KEY = "MusicVolume";
    private const string SFX_VOLUME_KEY = "SFXVolume";
    private const string MASTER_VOLUME_KEY = "MasterVolume";
    private const double ScheduleLead = 0.5;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        SetupAudioSource();
        LoadVolumeSettings();

        SceneManager.sceneLoaded += OnSceneLoaded;
        GameEvents.OnMatchTimeExpired += HandleMatchTimeExpired;

        if (playOnAwake)
        {
            if (SceneManager.GetActiveScene().buildIndex == mainMenuSceneIndex)
                PlayMenuMusic();
            else
                PlayMusic();
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        GameEvents.OnMatchTimeExpired -= HandleMatchTimeExpired;
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        LoadVolumeSettings();

        if (scene.buildIndex == mainMenuSceneIndex)
        {
            StopMusic();
            PlayMenuMusic();
        }
        else
        {
            StopMenuMusic();
            PlayMusic();
        }
    }

    private void SetupAudioSource()
    {
        introSource = CreateMusicSource();
        loopSource = CreateMusicSource();
        overtimeSource = CreateMusicSource();

        musicSources = new AudioSource[] { introSource, loopSource };
        allMusicSources = new AudioSource[] { introSource, loopSource, overtimeSource };

        menuSource = gameObject.AddComponent<AudioSource>();
        menuSource.loop = true;
        menuSource.playOnAwake = false;
        menuSource.volume = musicVolume;
        menuSource.spatialBlend = 0f;
        menuSource.priority = 0;

        sfxLoopSource = gameObject.AddComponent<AudioSource>();
        sfxLoopSource.loop = true;
        sfxLoopSource.playOnAwake = false;
        sfxLoopSource.volume = sfxVolume;
        sfxLoopSource.spatialBlend = 0f;
        sfxLoopSource.priority = 128;

        sfxOneShotSource = gameObject.AddComponent<AudioSource>();
        sfxOneShotSource.loop = false;
        sfxOneShotSource.playOnAwake = false;
        sfxOneShotSource.volume = sfxVolume;
        sfxOneShotSource.spatialBlend = 0f;
        sfxOneShotSource.priority = 128;
    }

    private AudioSource CreateMusicSource()
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.loop = false;
        source.playOnAwake = false;
        source.volume = musicVolume;
        source.spatialBlend = 0f;
        source.priority = 0;
        return source;
    }

    private void LoadVolumeSettings()
    {
        float masterVolume = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 1f);
        AudioListener.volume = masterVolume;

        musicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, musicVolume);
        sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, sfxVolume);

        ApplyMusicVolumeImmediate(musicVolume);

        if (menuSource != null)
        {
            menuSource.DOKill();
            menuSource.volume = musicVolume;
        }

        if (sfxLoopSource != null)
        {
            sfxLoopSource.volume = sfxVolume;
        }

        if (sfxOneShotSource != null)
        {
            sfxOneShotSource.volume = sfxVolume;
        }

        isVolumeReduced = false;
        savedMusicVolume = musicVolume;
    }

    public void PlayMenuMusic()
    {
        if (menuMusic == null || menuSource == null) return;
        if (menuSource.isPlaying) return;
        menuSource.clip = menuMusic;
        menuSource.volume = musicVolume;
        menuSource.Play();
    }

    public void FadeOutMenuMusic(float duration)
    {
        if (menuSource == null || !menuSource.isPlaying) return;
        menuSource.DOKill();
        menuSource.DOFade(0f, duration).SetUpdate(true).OnComplete(() =>
        {
            menuSource.Stop();
            menuSource.volume = musicVolume;
        });
    }

    private void StopMenuMusic()
    {
        if (menuSource != null)
        {
            menuSource.DOKill();
            menuSource.Stop();
            menuSource.volume = musicVolume;
        }
    }

    private SceneMusicConfig GetCurrentSceneConfig()
    {
        if (sceneMusicConfigs == null) return null;
        int index = SceneManager.GetActiveScene().buildIndex;
        foreach (var config in sceneMusicConfigs)
            if (config.sceneIndex == index) return config;
        return null;
    }

    public void PlayMusic()
    {
        if (SceneManager.GetActiveScene().buildIndex == mainMenuSceneIndex)
            return;

        SceneMusicConfig config = GetCurrentSceneConfig();
        if (config == null)
            return;

        StopMusicSequence();
        StopOvertimeRoutine();
        StopAllMusicSources();

        var sections = new List<MusicLoopSection>();
        if (config.loopSections != null)
        {
            foreach (var s in config.loopSections)
                if (s != null && s.loopClip != null) sections.Add(s);
        }

        activeConfig = config;
        overtimeStarted = false;
        lastRegularLoop = sections.Count > 0 ? sections[sections.Count - 1].loopClip : config.loopClip;

        musicSequenceCoroutine = StartCoroutine(MusicSequenceRoutine(config.introClip, sections, config.loopClip));
    }

    private void HandleMatchTimeExpired()
    {
        if (activeConfig == null || overtimeStarted) return;
        if (activeConfig.overtimeBridgeClip == null && activeConfig.overtimeLoopClip == null) return;

        overtimeStarted = true;
        StopMusicSequence();
        overtimeCoroutine = StartCoroutine(OvertimeRoutine(activeConfig));
    }

    private IEnumerator OvertimeRoutine(SceneMusicConfig config)
    {
        FadeOutRegularSources();

        AudioClip bridge = config.overtimeBridgeClip;
        AudioClip loop = config.overtimeLoopClip != null ? config.overtimeLoopClip : lastRegularLoop;
        double startDsp = AudioSettings.dspTime + 0.05;

        if (bridge == null)
        {
            if (loop != null)
                PlayClipScheduled(overtimeSource, loop, true, startDsp);
            yield break;
        }

        PlayClipScheduled(overtimeSource, bridge, false, startDsp);
        if (loop == null) yield break;

        double loopStartDsp = startDsp + ClipLength(bridge);
        float fadeSettled = Time.unscaledTime + overtimeFadeSeconds + 0.1f;
        while (Time.unscaledTime < fadeSettled && AudioSettings.dspTime < loopStartDsp - ScheduleLead)
            yield return null;

        AudioSource loopTarget = FreeRegularSource();
        PlayClipScheduled(loopTarget, loop, true, Math.Max(loopStartDsp, AudioSettings.dspTime + 0.05));
    }

    private void FadeOutRegularSources()
    {
        for (int i = 0; i < musicSources.Length; i++)
        {
            AudioSource source = musicSources[i];
            source.DOKill();
            if (!source.isPlaying) continue;

            fadingOutSources.Add(source);
            source.DOFade(0f, overtimeFadeSeconds).SetUpdate(true).OnComplete(() =>
            {
                source.Stop();
                fadingOutSources.Remove(source);
                source.volume = CurrentMusicVolume();
            });
        }
    }

    private AudioSource FreeRegularSource()
    {
        for (int i = 0; i < musicSources.Length; i++)
        {
            if (!musicSources[i].isPlaying && !fadingOutSources.Contains(musicSources[i]))
                return musicSources[i];
        }

        AudioSource fallback = musicSources[0];
        fallback.DOKill();
        fallback.Stop();
        fadingOutSources.Remove(fallback);
        return fallback;
    }

    private static double ClipLength(AudioClip clip)
    {
        if (clip == null || clip.frequency <= 0) return 0.0;
        return (double)clip.samples / clip.frequency;
    }

    private float GetMatchDurationSeconds()
    {
        if (GameTimeManager.Instance != null)
            return GameTimeManager.Instance.MatchDurationSeconds;
        return fallbackMatchDurationSeconds;
    }

    private float CurrentMusicVolume()
    {
        return isVolumeReduced ? musicVolume * reducedVolumeMultiplier : musicVolume;
    }

    private float CurrentGameTime()
    {
        if (GameTimeManager.Instance != null)
            return GameTimeManager.Instance.GetGameTime();
        return 0f;
    }

    private double[] ComputeLoopStartTimes(AudioClip introClip, List<MusicLoopSection> sections)
    {
        int k = sections.Count;
        double[] start = new double[k];
        if (k == 0) return start;

        double introLen = ClipLength(introClip);
        double fixedTime = introLen;
        for (int i = 0; i < k - 1; i++)
            fixedTime += ClipLength(sections[i].bridgeClip);

        double matchDur = GetMatchDurationSeconds();
        double available = Mathf.Max(0f, (float)(matchDur - fixedTime));
        double sharePerLoop = available / k;

        bool[] known = new bool[k];
        start[0] = introLen;
        known[0] = true;
        for (int i = 1; i < k; i++)
        {
            if (sections[i].startAtSeconds > 0f)
            {
                start[i] = sections[i].startAtSeconds;
                known[i] = true;
            }
        }

        if (!known[k - 1])
        {
            start[k - 1] = introLen + (k - 1) * sharePerLoop;
            known[k - 1] = true;
        }

        int prevKnown = 0;
        for (int i = 1; i < k; i++)
        {
            if (known[i])
            {
                for (int j = prevKnown + 1; j < i; j++)
                    start[j] = start[prevKnown] + (start[i] - start[prevKnown]) * (j - prevKnown) / (double)(i - prevKnown);
                prevKnown = i;
            }
        }
        return start;
    }

    private IEnumerator MusicSequenceRoutine(AudioClip introClip, List<MusicLoopSection> sections, AudioClip legacyLoop)
    {
        int srcIndex = 0;
        AudioSource cur = musicSources[srcIndex];

        if (sections.Count == 0)
        {
            if (legacyLoop == null) yield break;
            if (introClip != null)
            {
                PlayClipNow(cur, introClip, false);
                double iLen = ClipLength(introClip);
                while (cur.isPlaying && (iLen - cur.time) > ScheduleLead) yield return null;
                double iEnd = AudioSettings.dspTime + Math.Max(0.05, iLen - cur.time);
                PlayClipScheduled(musicSources[1 - srcIndex], legacyLoop, true, iEnd);
            }
            else
            {
                PlayClipNow(cur, legacyLoop, true);
            }
            yield break;
        }

        double[] start = ComputeLoopStartTimes(introClip, sections);
        int k = sections.Count;

        if (introClip != null)
        {
            PlayClipNow(cur, introClip, false);
            double introLen = ClipLength(introClip);
            while (cur.isPlaying && (introLen - cur.time) > ScheduleLead)
                yield return null;
            double endDsp = AudioSettings.dspTime + Math.Max(0.05, introLen - cur.time);
            AudioSource other = musicSources[1 - srcIndex];
            PlayClipScheduled(other, sections[0].loopClip, true, endDsp);
            while (AudioSettings.dspTime < endDsp) yield return null;
            srcIndex = 1 - srcIndex;
            cur = other;
        }
        else
        {
            PlayClipNow(cur, sections[0].loopClip, true);
        }

        for (int i = 0; i < k - 1; i++)
        {
            MusicLoopSection s = sections[i];
            double clipLen = ClipLength(s.loopClip);

            if (s.repeatCount > 0)
            {
                int completed = 0;
                float lastT = cur.time;
                while (completed < s.repeatCount)
                {
                    yield return null;
                    float t = cur.time;
                    if (t < lastT - 0.05f) completed++;
                    lastT = t;
                }
            }
            else
            {
                double threshold = start[i + 1];
                while (CurrentGameTime() < threshold)
                    yield return null;
            }

            double remaining;
            while (true)
            {
                if (Time.timeScale == 0f || !cur.isPlaying) { yield return null; continue; }
                remaining = clipLen - cur.time;
                if (remaining >= ScheduleLead) break;
                yield return null;
            }

            double boundaryDsp = AudioSettings.dspTime + remaining;
            cur.loop = false;

            AudioClip bridge = s.bridgeClip;
            AudioClip nextLoop = sections[i + 1].loopClip;

            AudioSource bridgeSrc = musicSources[1 - srcIndex];
            if (bridge != null)
            {
                PlayClipScheduled(bridgeSrc, bridge, false, boundaryDsp);
                double bridgeEndDsp = boundaryDsp + ClipLength(bridge);
                while (AudioSettings.dspTime < boundaryDsp) yield return null;
                srcIndex = 1 - srcIndex;
                cur = bridgeSrc;

                AudioSource loopSrc = musicSources[1 - srcIndex];
                PlayClipScheduled(loopSrc, nextLoop, true, bridgeEndDsp);
                while (AudioSettings.dspTime < bridgeEndDsp) yield return null;
                srcIndex = 1 - srcIndex;
                cur = loopSrc;
            }
            else
            {
                PlayClipScheduled(bridgeSrc, nextLoop, true, boundaryDsp);
                while (AudioSettings.dspTime < boundaryDsp) yield return null;
                srcIndex = 1 - srcIndex;
                cur = bridgeSrc;
            }
        }
    }

    private void PlayClipNow(AudioSource src, AudioClip clip, bool loop)
    {
        src.DOKill();
        fadingOutSources.Remove(src);
        src.Stop();
        src.clip = clip;
        src.loop = loop;
        src.volume = CurrentMusicVolume();
        src.Play();
    }

    private void PlayClipScheduled(AudioSource src, AudioClip clip, bool loop, double dspStart)
    {
        src.DOKill();
        fadingOutSources.Remove(src);
        src.Stop();
        src.clip = clip;
        src.loop = loop;
        src.volume = CurrentMusicVolume();
        src.PlayScheduled(dspStart);
    }

    private void StopMusicSequence()
    {
        if (musicSequenceCoroutine != null)
        {
            StopCoroutine(musicSequenceCoroutine);
            musicSequenceCoroutine = null;
        }
    }

    private void StopOvertimeRoutine()
    {
        if (overtimeCoroutine != null)
        {
            StopCoroutine(overtimeCoroutine);
            overtimeCoroutine = null;
        }
    }

    private void StopAllMusicSources()
    {
        if (allMusicSources == null) return;

        for (int i = 0; i < allMusicSources.Length; i++)
        {
            allMusicSources[i].DOKill();
            allMusicSources[i].Stop();
            allMusicSources[i].volume = CurrentMusicVolume();
        }

        fadingOutSources.Clear();
    }

    public void StopMusic()
    {
        StopMusicSequence();
        StopOvertimeRoutine();
        StopAllMusicSources();
        activeConfig = null;
        overtimeStarted = false;
    }

    public void PauseMusic()
    {
        if (allMusicSources == null) return;
        for (int i = 0; i < allMusicSources.Length; i++)
            allMusicSources[i].Pause();
    }

    public void ResumeMusic()
    {
        if (allMusicSources == null) return;
        for (int i = 0; i < allMusicSources.Length; i++)
            allMusicSources[i].UnPause();
    }

    private void ApplyMusicVolumeImmediate(float volume)
    {
        if (allMusicSources == null) return;

        for (int i = 0; i < allMusicSources.Length; i++)
        {
            AudioSource source = allMusicSources[i];
            if (fadingOutSources.Contains(source)) continue;
            source.DOKill();
            source.volume = volume;
        }
    }

    private void FadeMusicVolume(float target)
    {
        if (allMusicSources == null) return;

        for (int i = 0; i < allMusicSources.Length; i++)
        {
            AudioSource source = allMusicSources[i];
            if (fadingOutSources.Contains(source)) continue;
            source.DOKill();
            source.DOFade(target, volumeFadeDuration).SetUpdate(true);
        }
    }

    public void SetVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        ApplyMusicVolumeImmediate(musicVolume);
        if (menuSource != null) { menuSource.DOKill(); menuSource.volume = musicVolume; }
        isVolumeReduced = false;
        savedMusicVolume = musicVolume;
    }

    public float GetVolume()
    {
        return musicVolume;
    }

    public float GetSFXVolume()
    {
        return sfxVolume;
    }

    public bool IsPlaying()
    {
        if (allMusicSources == null) return false;

        for (int i = 0; i < allMusicSources.Length; i++)
        {
            if (allMusicSources[i] != null && allMusicSources[i].isPlaying)
                return true;
        }

        return false;
    }

    public void PlaySFXLoop(AudioClip sfx)
    {
        if (sfx == null || sfxLoopSource == null) return;

        sfxLoopSource.clip = sfx;
        sfxLoopSource.pitch = 1f;
        sfxLoopSource.Play();
    }

    public void PlaySFXLoop(AudioClip sfx, float volume, float pitch = 1f)
    {
        if (sfx == null || sfxLoopSource == null) return;

        sfxLoopSource.clip = sfx;
        sfxLoopSource.volume = Mathf.Clamp01(volume);
        sfxLoopSource.pitch = pitch;
        sfxLoopSource.Play();
    }

    public void StopSFXLoop()
    {
        if (sfxLoopSource != null && sfxLoopSource.isPlaying)
        {
            sfxLoopSource.Stop();
            sfxLoopSource.pitch = 1f;
        }
    }

    public void UpdateSFXLoopPitch(float pitch)
    {
        if (sfxLoopSource != null && sfxLoopSource.isPlaying)
        {
            sfxLoopSource.pitch = pitch;
        }
    }

    public void PlaySFXOneShot(AudioClip sfx)
    {
        if (sfx == null || sfxOneShotSource == null) return;

        sfxOneShotSource.PlayOneShot(sfx, sfxVolume);
    }

    public void PlayUISound(AudioClip clip)
    {
        if (clip == null || sfxOneShotSource == null) return;
        sfxOneShotSource.PlayOneShot(clip, sfxVolume);
    }

    public void PlaySFXOneShot(AudioClip sfx, float volume, float pitch = 1f)
    {
        if (sfx == null || sfxOneShotSource == null) return;

        sfxOneShotSource.pitch = pitch;
        sfxOneShotSource.PlayOneShot(sfx, Mathf.Clamp01(volume));
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (sfxLoopSource != null)
        {
            sfxLoopSource.volume = sfxVolume;
        }
        if (sfxOneShotSource != null)
        {
            sfxOneShotSource.volume = sfxVolume;
        }
    }

    public void ReduceVolume()
    {
        if (isVolumeReduced) return;
        isVolumeReduced = true;
        savedMusicVolume = musicVolume;
        FadeMusicVolume(savedMusicVolume * reducedVolumeMultiplier);
    }

    public void RestoreVolume()
    {
        if (!isVolumeReduced) return;
        isVolumeReduced = false;
        FadeMusicVolume(savedMusicVolume);
    }
}

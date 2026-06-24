using UnityEngine;
using DG.Tweening;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;

[Serializable]
public class MusicLoopSection
{
    [Tooltip("Loop que se repite. El último loop de la lista suena para siempre (incluido overtime).")]
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
    [Tooltip("Opcional. Suena una vez al inicio y luego pasa a los loops.")]
    public AudioClip introClip;
    [Tooltip("Loops y puentes en orden: loop1, puente1, loop2, puente2, loop3... El último loop suena para siempre. Si está vacío se usa el 'loopClip' de abajo.")]
    public MusicLoopSection[] loopSections;
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

    private AudioSource introSource;
    private AudioSource loopSource;
    private AudioSource[] musicSources;
    private Coroutine musicSequenceCoroutine;
    private AudioSource menuSource;
    private AudioSource sfxLoopSource;
    private AudioSource sfxOneShotSource;
    private float savedMusicVolume;
    private bool isVolumeReduced = false;

    private const string MUSIC_VOLUME_KEY = "MusicVolume";
    private const string SFX_VOLUME_KEY = "SFXVolume";
    private const string MASTER_VOLUME_KEY = "MasterVolume";

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
            if (!IsPlaying())
                PlayMusic();
        }
    }

    private void SetupAudioSource()
    {
        introSource = gameObject.AddComponent<AudioSource>();
        introSource.loop = false;
        introSource.playOnAwake = false;
        introSource.volume = musicVolume;
        introSource.spatialBlend = 0f;
        introSource.priority = 0;

        loopSource = gameObject.AddComponent<AudioSource>();
        loopSource.loop = false;
        loopSource.playOnAwake = false;
        loopSource.volume = musicVolume;
        loopSource.spatialBlend = 0f;
        loopSource.priority = 0;

        musicSources = new AudioSource[] { introSource, loopSource };

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

    private void LoadVolumeSettings()
    {

        float masterVolume = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 1f);
        AudioListener.volume = masterVolume;

        musicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, musicVolume);
        sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, sfxVolume);

        if (introSource != null)
        {
            introSource.DOKill();
            introSource.volume = musicVolume;
        }

        if (loopSource != null)
        {
            loopSource.DOKill();
            loopSource.volume = musicVolume;
        }

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
        introSource.Stop();
        loopSource.Stop();

        var sections = new List<MusicLoopSection>();
        if (config.loopSections != null)
        {
            foreach (var s in config.loopSections)
                if (s != null && s.loopClip != null) sections.Add(s);
        }

        musicSequenceCoroutine = StartCoroutine(MusicSequenceRoutine(config.introClip, sections, config.loopClip));
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
        const double LEAD = 0.5;

        int srcIndex = 0;
        AudioSource cur = musicSources[srcIndex];

        if (sections.Count == 0)
        {
            if (legacyLoop == null) yield break;
            if (introClip != null)
            {
                PlayClipNow(cur, introClip, false);
                double iLen = ClipLength(introClip);
                while (cur.isPlaying && (iLen - cur.time) > LEAD) yield return null;
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
            while (cur.isPlaying && (introLen - cur.time) > LEAD)
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
                if (remaining >= LEAD) break;
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
        src.Stop();
        src.clip = clip;
        src.loop = loop;
        src.volume = CurrentMusicVolume();
        src.Play();
    }

    private void PlayClipScheduled(AudioSource src, AudioClip clip, bool loop, double dspStart)
    {
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

    public void StopMusic()
    {
        StopMusicSequence();
        introSource.Stop();
        loopSource.Stop();
    }

    public void PauseMusic()
    {
        introSource.Pause();
        loopSource.Pause();
    }

    public void ResumeMusic()
    {
        introSource.UnPause();
        loopSource.UnPause();
    }

    public void SetVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (introSource != null) { introSource.DOKill(); introSource.volume = musicVolume; }
        if (loopSource  != null) { loopSource.DOKill();  loopSource.volume  = musicVolume; }
        if (menuSource  != null) { menuSource.DOKill();  menuSource.volume   = musicVolume; }
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
        return (introSource != null && introSource.isPlaying) ||
               (loopSource  != null && loopSource.isPlaying);
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
        float targetVolume = savedMusicVolume * reducedVolumeMultiplier;
        if (introSource != null) { introSource.DOKill(); introSource.DOFade(targetVolume, volumeFadeDuration).SetUpdate(true); }
        if (loopSource  != null) { loopSource.DOKill();  loopSource.DOFade(targetVolume,  volumeFadeDuration).SetUpdate(true); }
    }

    public void RestoreVolume()
    {
        if (!isVolumeReduced) return;
        isVolumeReduced = false;
        if (introSource != null) { introSource.DOKill(); introSource.DOFade(savedMusicVolume, volumeFadeDuration).SetUpdate(true); }
        if (loopSource  != null) { loopSource.DOKill();  loopSource.DOFade(savedMusicVolume,  volumeFadeDuration).SetUpdate(true); }
    }
}

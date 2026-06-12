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

        // Dos fuentes que se alternan (ping-pong) para encadenar intro/loops/puentes sin huecos.
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

        List<SeqItem> sequence = BuildMusicSequence(config);
        if (sequence.Count == 0)
            return;

        musicSequenceCoroutine = StartCoroutine(MusicSequenceRoutine(sequence));
    }

    private struct SeqItem
    {
        public AudioClip clip;
        public bool loopForever;
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

    /// <summary>
    /// Construye la secuencia ordenada: intro (1 vez), loop1 (N veces), puente1 (1 vez),
    /// loop2 (M veces), puente2 (1 vez), ..., loopN (para siempre, incluido overtime).
    /// Las repeticiones de cada loop se reparten por tiempo para llenar la partida.
    /// </summary>
    private List<SeqItem> BuildMusicSequence(SceneMusicConfig config)
    {
        var seq = new List<SeqItem>();

        if (config.introClip != null)
            seq.Add(new SeqItem { clip = config.introClip, loopForever = false });

        // Recoge los loops válidos.
        var sections = new List<MusicLoopSection>();
        if (config.loopSections != null)
        {
            foreach (var s in config.loopSections)
                if (s != null && s.loopClip != null) sections.Add(s);
        }

        // Sin secciones: cae al loop infinito legado.
        if (sections.Count == 0)
        {
            if (config.loopClip != null)
                seq.Add(new SeqItem { clip = config.loopClip, loopForever = true });
            return seq;
        }

        int k = sections.Count;

        // Tiempo fijo: intro + puentes (cada uno suena una sola vez; se ignora el puente del último loop).
        double fixedTime = ClipLength(config.introClip);
        for (int i = 0; i < k - 1; i++)
            fixedTime += ClipLength(sections[i].bridgeClip);

        double available = Mathf.Max(0f, GetMatchDurationSeconds() - (float)fixedTime);
        double sharePerLoop = available / k; // cada loop "posee" ~1/k de la partida

        for (int i = 0; i < k; i++)
        {
            MusicLoopSection s = sections[i];
            bool isLast = (i == k - 1);

            if (isLast)
            {
                // El último loop suena para siempre (cubre lo que reste + overtime). Su puente se ignora.
                seq.Add(new SeqItem { clip = s.loopClip, loopForever = true });
                break;
            }

            int reps;
            if (s.repeatCount > 0)
            {
                reps = s.repeatCount;
            }
            else
            {
                double len = ClipLength(s.loopClip);
                reps = len > 0.0 ? Mathf.Max(1, Mathf.RoundToInt((float)(sharePerLoop / len))) : 1;
            }

            for (int r = 0; r < reps; r++)
                seq.Add(new SeqItem { clip = s.loopClip, loopForever = false });

            // Puente para transicionar naturalmente al siguiente loop.
            if (s.bridgeClip != null)
                seq.Add(new SeqItem { clip = s.bridgeClip, loopForever = false });
        }

        return seq;
    }

    /// <summary>
    /// Reproduce la secuencia encadenando clips con PlayScheduled (sin huecos) alternando
    /// dos AudioSources. El último clip se marca como loop infinito.
    /// </summary>
    private IEnumerator MusicSequenceRoutine(List<SeqItem> items)
    {
        double startTime = AudioSettings.dspTime + 0.2;

        // Programa el primer clip.
        ScheduleClip(items[0], musicSources[0], startTime);
        double clipStart = startTime;

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].loopForever)
                yield break; // último clip: queda en loop infinito.

            double duration = ClipLength(items[i].clip);
            double nextStart = clipStart + duration;

            if (i + 1 < items.Count)
            {
                // Espera a que el clip actual EMPIECE; en ese momento el clip de hace dos
                // posiciones (que usó la fuente alterna) ya terminó, así que está libre.
                while (AudioSettings.dspTime < clipStart)
                    yield return null;

                AudioSource nextSrc = musicSources[(i + 1) % 2];
                ScheduleClip(items[i + 1], nextSrc, nextStart);
            }

            clipStart = nextStart;
        }
    }

    private void ScheduleClip(SeqItem item, AudioSource src, double dspStart)
    {
        src.Stop();
        src.clip = item.clip;
        src.loop = item.loopForever;
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

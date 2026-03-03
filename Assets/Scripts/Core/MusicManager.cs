using UnityEngine;
using DG.Tweening;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Music Settings")]
    [SerializeField] private AudioClip introClip;
    [SerializeField] private AudioClip loopClip;
    [SerializeField][Range(0f, 1f)] private float musicVolume = 0.5f;
    [SerializeField] private bool playOnAwake = true;

    [Header("SFX Settings")]
    [SerializeField][Range(0f, 1f)] private float sfxVolume = 0.8f;

    [Header("Volume Reduction Settings")]
    [SerializeField][Range(0f, 1f)] private float reducedVolumeMultiplier = 0.3f; // 30% del volumen original
    [SerializeField] private float volumeFadeDuration = 0.5f;

    [Header("Scene Options")]
    [Tooltip("Índice de la escena del menú principal. Si la escena activa coincide, la música no se reproducirá automáticamente.")]
    [SerializeField] private int mainMenuSceneIndex = 0;

    private AudioSource introSource;
    private AudioSource loopSource;
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

        // Suscribirse para reaplicar volúmenes en cada carga de escena
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Solo reproducir automáticamente si la escena actual NO es el main menu (configurable)
        if (playOnAwake && introClip != null && SceneManager.GetActiveScene().buildIndex != mainMenuSceneIndex)
        {
            PlayMusic();
        }
    }

    private void OnDestroy()
    {
        // Desuscribirse del evento para evitar referencias colgantes
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Reaplicar volúmenes guardados cuando se carga una nueva escena
        LoadVolumeSettings();

        // Si la música no está sonando, reanudarla (asegura que suene tras reload),
        // pero evitar reproducirla si la escena es el main menu.
        if (scene.buildIndex != mainMenuSceneIndex && !IsPlaying() && introClip != null)
        {
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
        loopSource.loop = true;
        loopSource.playOnAwake = false;
        loopSource.volume = musicVolume;
        loopSource.spatialBlend = 0f;
        loopSource.priority = 0;

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
        // Cargar volumen maestro
        float masterVolume = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 1f);
        AudioListener.volume = masterVolume;

        // Cargar volumen guardado desde PlayerPrefs
        musicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, musicVolume);
        sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, sfxVolume);

        // Aplicar volumen cargado a los AudioSources
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

        if (sfxLoopSource != null)
        {
            sfxLoopSource.volume = sfxVolume;
        }

        if (sfxOneShotSource != null)
        {
            sfxOneShotSource.volume = sfxVolume;
        }

        // Si cargamos ajustes explícitos, considerarlos como preferencia del usuario:
        isVolumeReduced = false;
        savedMusicVolume = musicVolume;
    }

    public void PlayMusic()
    {
        if (introClip == null || loopClip == null)
        {
            Debug.LogWarning("[MusicManager] Intro clip or loop clip not assigned!");
            return;
        }

        // Evitar reproducir en el main menu por configuración
        if (SceneManager.GetActiveScene().buildIndex == mainMenuSceneIndex)
            return;

        introSource.Stop();
        loopSource.Stop();

        double startTime = AudioSettings.dspTime + 0.1;
        double loopStartTime = startTime + (double)introClip.samples / introClip.frequency;

        introSource.clip = introClip;
        introSource.PlayScheduled(startTime);

        loopSource.clip = loopClip;
        loopSource.PlayScheduled(loopStartTime);
    }

    public void StopMusic()
    {
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
        if (loopSource != null)  { loopSource.DOKill();  loopSource.volume  = musicVolume; }
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

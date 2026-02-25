using UnityEngine;
using DG.Tweening;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Music Settings")]
    [SerializeField] private AudioClip gameplayMusic;
    [SerializeField][Range(0f, 1f)] private float musicVolume = 0.5f;
    [SerializeField] private bool playOnAwake = true;

    [Header("SFX Settings")]
    [SerializeField][Range(0f, 1f)] private float sfxVolume = 0.8f;

    [Header("Volume Reduction Settings")]
    [SerializeField][Range(0f, 1f)] private float reducedVolumeMultiplier = 0.3f; // 30% del volumen original
    [SerializeField] private float volumeFadeDuration = 0.5f;

    private AudioSource musicSource;
    private AudioSource sfxLoopSource;
    private AudioSource sfxOneShotSource;
    private float savedMusicVolume; // Para guardar el volumen original
    private bool isVolumeReduced = false;

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

        if (playOnAwake && gameplayMusic != null)
        {
            PlayMusic();
        }
    }

    private void SetupAudioSource()
    {
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = musicVolume;
        musicSource.spatialBlend = 0f;
        musicSource.priority = 0;

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

    public void PlayMusic()
    {
        if (gameplayMusic == null)
        {
            Debug.LogWarning("[MusicManager] No music clip assigned!");
            return;
        }

        // Siempre detener y reiniciar para asegurar que la música suene
        if (musicSource.isPlaying)
        {
            musicSource.Stop();
        }
        
        musicSource.clip = gameplayMusic;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource.isPlaying)
        {
            musicSource.Stop();
        }
    }

    public void PauseMusic()
    {
        if (musicSource.isPlaying)
        {
            musicSource.Pause();
        }
    }

    public void ResumeMusic()
    {
        if (!musicSource.isPlaying && musicSource.clip != null)
        {
            musicSource.UnPause();
        }
    }

    public void SetVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null)
        {
            musicSource.volume = musicVolume;
        }
    }

    public float GetVolume()
    {
        return musicVolume;
    }

    public bool IsPlaying()
    {
        return musicSource != null && musicSource.isPlaying;
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
        if (musicSource == null || isVolumeReduced) return;

        isVolumeReduced = true;
        savedMusicVolume = musicSource.volume;
        float targetVolume = savedMusicVolume * reducedVolumeMultiplier;

        musicSource.DOKill(); // Cancelar cualquier fade previo
        musicSource.DOFade(targetVolume, volumeFadeDuration).SetUpdate(true); // SetUpdate(true) para que funcione con Time.timeScale = 0
    }

    public void RestoreVolume()
    {
        if (musicSource == null || !isVolumeReduced) return;

        isVolumeReduced = false;
        
        musicSource.DOKill(); // Cancelar cualquier fade previo
        musicSource.DOFade(savedMusicVolume, volumeFadeDuration).SetUpdate(true); // SetUpdate(true) para que funcione con Time.timeScale = 0
    }
}

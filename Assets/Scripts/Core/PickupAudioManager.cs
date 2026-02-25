using UnityEngine;

public class PickupAudioManager : MonoBehaviour
{
    public static PickupAudioManager Instance { get; private set; }

    [Header("Audio Clips")]
    [SerializeField] private AudioClip coinSound;
    [SerializeField] private AudioClip diamondSound;
    [SerializeField] private AudioClip experienceOrbSound;

    [Header("Experience Orb Pitch Settings")]
    [SerializeField] private int smallOrbThreshold = 15;
    [SerializeField] private int mediumOrbThreshold = 30;
    [SerializeField] private float smallOrbPitchMin = 1.3f;
    [SerializeField] private float smallOrbPitchMax = 1.5f;
    [SerializeField] private float mediumOrbPitchMin = 1.0f;
    [SerializeField] private float mediumOrbPitchMax = 1.2f;
    [SerializeField] private float largeOrbPitchMin = 0.7f;
    [SerializeField] private float largeOrbPitchMax = 0.9f;

    [Header("Coin Pitch Settings")]
    [SerializeField] private float coinPitchMin = 0.95f;
    [SerializeField] private float coinPitchMax = 1.15f;

    [Header("Volume Settings")]
    [SerializeField] private float coinVolume = 0.7f;
    [SerializeField] private float diamondVolume = 0.6f;
    [SerializeField] private float orbVolume = 0.8f;

    [Header("Sound Limiting")]
    [SerializeField] private float diamondSoundCooldown = 0.05f;
    
    private AudioSource coinAudioSource;
    private AudioSource diamondAudioSource;
    private AudioSource orbAudioSource;
    private float lastDiamondSoundTime = -999f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null); // Convertir en root antes de DDOL
            DontDestroyOnLoad(gameObject);
            
            coinAudioSource = gameObject.AddComponent<AudioSource>();
            coinAudioSource.playOnAwake = false;
            coinAudioSource.spatialBlend = 0f;
            
            diamondAudioSource = gameObject.AddComponent<AudioSource>();
            diamondAudioSource.playOnAwake = false;
            diamondAudioSource.spatialBlend = 0f;
            diamondAudioSource.pitch = 1f;
            
            orbAudioSource = gameObject.AddComponent<AudioSource>();
            orbAudioSource.playOnAwake = false;
            orbAudioSource.spatialBlend = 0f;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void PlayCoinSound()
    {
        if (coinSound != null && coinAudioSource != null)
        {
            coinAudioSource.pitch = Random.Range(coinPitchMin, coinPitchMax);
            float finalVolume = coinVolume;
            if (MusicManager.Instance != null)
            {
                finalVolume *= MusicManager.Instance.GetSFXVolume();
            }
            coinAudioSource.PlayOneShot(coinSound, finalVolume);
        }
    }

    public void PlayDiamondSound()
    {
        if (diamondSound != null && diamondAudioSource != null)
        {
            if (Time.time - lastDiamondSoundTime < diamondSoundCooldown)
            {
                return;
            }
            
            lastDiamondSoundTime = Time.time;
            float finalVolume = diamondVolume;
            if (MusicManager.Instance != null)
            {
                finalVolume *= MusicManager.Instance.GetSFXVolume();
            }
            diamondAudioSource.PlayOneShot(diamondSound, finalVolume);
        }
    }

    public void PlayExperienceOrbSound(int experienceValue)
    {
        if (experienceOrbSound != null && orbAudioSource != null)
        {
            float pitch = CalculateOrbPitch(experienceValue);
            orbAudioSource.pitch = pitch;
            float finalVolume = orbVolume;
            if (MusicManager.Instance != null)
            {
                finalVolume *= MusicManager.Instance.GetSFXVolume();
            }
            orbAudioSource.PlayOneShot(experienceOrbSound, finalVolume);
        }
    }

    private float CalculateOrbPitch(int experienceValue)
    {
        if (experienceValue < smallOrbThreshold)
        {
            return Random.Range(smallOrbPitchMin, smallOrbPitchMax);
        }
        else if (experienceValue < mediumOrbThreshold)
        {
            return Random.Range(mediumOrbPitchMin, mediumOrbPitchMax);
        }
        else
        {
            return Random.Range(largeOrbPitchMin, largeOrbPitchMax);
        }
    }
}

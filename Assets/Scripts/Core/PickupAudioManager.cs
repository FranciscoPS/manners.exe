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

    [Header("Volume Settings")]
    [SerializeField] private float coinVolume = 1f;
    [SerializeField] private float diamondVolume = 1f;
    [SerializeField] private float orbVolume = 0.8f;

    private AudioSource audioSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayCoinSound()
    {
        if (coinSound != null && audioSource != null)
        {
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(coinSound, coinVolume);
        }
    }

    public void PlayDiamondSound()
    {
        if (diamondSound != null && audioSource != null)
        {
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(diamondSound, diamondVolume);
        }
    }

    public void PlayExperienceOrbSound(int experienceValue)
    {
        if (experienceOrbSound != null && audioSource != null)
        {
            float pitch = CalculateOrbPitch(experienceValue);
            audioSource.pitch = pitch;
            audioSource.PlayOneShot(experienceOrbSound, orbVolume);
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

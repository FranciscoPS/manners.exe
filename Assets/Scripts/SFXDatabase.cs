using UnityEngine;

[CreateAssetMenu(fileName = "SFXDatabase", menuName = "Game/SFX Database")]
public class SFXDatabase : ScriptableObject
{
    [Header("Upgrade Sounds")]
    public AudioClip holdUpgradeSFX;
    public AudioClip completeUpgradeSFX;
    public AudioClip levelUpSFX;

    [Header("Collectible Sounds")]
    public AudioClip coinCollectSFX;
    public AudioClip expOrbCollectSFX;
    public AudioClip diamondCollectSFX;

    [Header("Combat Sounds")]
    public AudioClip shootSFX;
    public AudioClip buildingDestroySFX;

    [Header("Volume Settings (0-1)")]
    [Range(0f, 1f)] public float upgradeVolume = 0.8f;
    [Range(0f, 1f)] public float collectibleVolume = 0.6f;
    [Range(0f, 1f)] public float shootVolume = 0.5f;
    [Range(0f, 1f)] public float buildingDestroyVolume = 0.4f;

    private static SFXDatabase instance;
    public static SFXDatabase Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<SFXDatabase>("SFXDatabase");
            }
            return instance;
        }
    }
}

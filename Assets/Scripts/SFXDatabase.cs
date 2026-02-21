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

    [Header("Player Sounds")]
    public AudioClip playerMoveSFX;
    public AudioClip playerDamageSFX;
    public AudioClip playerDeathSFX;

    [Header("Enemy Sounds")]
    public AudioClip enemyDeathSFX;

    [Header("Volume Settings (0-1)")]
    [Range(0f, 1f)] public float upgradeVolume = 0.8f;
    [Range(0f, 1f)] public float collectibleVolume = 0.6f;
    [Range(0f, 1f)] public float shootVolume = 0.5f;
    [Range(0f, 1f)] public float buildingDestroyVolume = 0.4f;
    [Range(0f, 1f)] public float playerMoveVolume = 0.3f;
    [Range(0f, 1f)] public float playerDamageVolume = 0.7f;
    [Range(0f, 1f)] public float playerDeathVolume = 0.8f;
    [Range(0f, 1f)] public float enemyDeathVolume = 0.5f;

    [Header("Pitch Variation (Min-Max)")]
    public Vector2 playerMovePitchRange = new Vector2(1.15f, 1.6f);
    public Vector2 playerDamagePitchRange = new Vector2(0.9f, 1.1f);
    public Vector2 enemyDeathPitchRange = new Vector2(0.85f, 1.15f);

    [Header("Movement Sound Settings")]
    [Range(0.1f, 1f)] public float moveSoundInterval = 0.3f; // Intervalo en segundos entre cada reproducción

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

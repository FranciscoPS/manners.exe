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

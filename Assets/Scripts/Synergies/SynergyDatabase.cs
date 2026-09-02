using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SynergyDatabase", menuName = "Game/Synergy Database")]
public class SynergyDatabase : ScriptableObject
{
    public List<SynergyData> allSynergies = new List<SynergyData>();

    private static SynergyDatabase instance;
    public static SynergyDatabase Instance
    {
        get
        {
            if (instance == null)
                instance = Resources.Load<SynergyDatabase>("SynergyDatabase");
            return instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    public static void OverrideInstance(SynergyDatabase database)
    {
        if (database != null)
            instance = database;
    }
}

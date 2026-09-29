using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "OverrideDatabase", menuName = "Game/Override Database")]
public class OverrideDatabase : ScriptableObject
{
    [FormerlySerializedAs("allSynergies")] public List<OverrideData> allOverrides = new List<OverrideData>();

    private static OverrideDatabase instance;
    public static OverrideDatabase Instance
    {
        get
        {
            if (instance == null)
                instance = Resources.Load<OverrideDatabase>("OverrideDatabase");
            return instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    public static void OverrideInstance(OverrideDatabase database)
    {
        if (database != null)
            instance = database;
    }
}

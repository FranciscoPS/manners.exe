using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Provee los \u00edtems de cofre disponibles y ejecuta sus efectos.
/// Carga autom\u00e1ticamente cualquier ChestItemData ubicado en una carpeta
/// "Resources/ChestItems". Si no hay ninguno, usa un Im\u00e1n Gigante por defecto
/// para que el sistema funcione sin configuraci\u00f3n manual.
/// </summary>
public static class ChestItemProvider
{
    private static List<ChestItemData> cachedItems;
    private static ChestItemData defaultGiantMagnet;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        cachedItems = null;
        defaultGiantMagnet = null;
    }

    /// <summary>Todos los \u00edtems de cofre disponibles (cacheados).</summary>
    public static List<ChestItemData> GetAvailableItems()
    {
        if (cachedItems != null && cachedItems.Count > 0)
            return cachedItems;

        cachedItems = new List<ChestItemData>();

        ChestItemData[] loaded = Resources.LoadAll<ChestItemData>("ChestItems");
        if (loaded != null && loaded.Length > 0)
        {
            cachedItems.AddRange(loaded);
        }

        if (cachedItems.Count == 0)
        {
            cachedItems.Add(GetDefaultGiantMagnet());
        }

        return cachedItems;
    }

    /// <summary>Devuelve hasta <paramref name="count"/> \u00edtems distintos al azar.</summary>
    public static List<ChestItemData> GetRandomItems(int count)
    {
        List<ChestItemData> pool = new List<ChestItemData>(GetAvailableItems());
        List<ChestItemData> result = new List<ChestItemData>();

        count = Mathf.Min(count, pool.Count);
        for (int i = 0; i < count; i++)
        {
            int idx = Random.Range(0, pool.Count);
            result.Add(pool[idx]);
            pool.RemoveAt(idx);
        }

        return result;
    }

    /// <summary>Ejecuta el efecto \u00fanico del \u00edtem elegido.</summary>
    public static void ApplyEffect(ChestItemData item)
    {
        if (item == null) return;

        switch (item.effect)
        {
            case ChestItemEffect.GiantMagnet:
                BaseCollectible.AttractAllToPlayer();
                break;
        }
    }

    private static ChestItemData GetDefaultGiantMagnet()
    {
        if (defaultGiantMagnet != null)
            return defaultGiantMagnet;

        defaultGiantMagnet = ScriptableObject.CreateInstance<ChestItemData>();
        defaultGiantMagnet.name = "ImanGigante";
        defaultGiantMagnet.itemName = "Im\u00e1n Gigante";
        defaultGiantMagnet.description = "Atrae al instante todos los orbes, monedas y objetos del mapa.";
        defaultGiantMagnet.effect = ChestItemEffect.GiantMagnet;
        defaultGiantMagnet.accentColor = new Color(0.4f, 0.8f, 1f);
        return defaultGiantMagnet;
    }
}

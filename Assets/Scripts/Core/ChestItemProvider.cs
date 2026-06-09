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
    private static List<ChestItemData> defaultItems;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        cachedItems = null;
        defaultItems = null;
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
            cachedItems.AddRange(GetDefaultItems());
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
                BaseCollectible.AttractAllToPlayer(2f);
                break;

            case ChestItemEffect.FullHeal:
                ApplyFullHeal();
                break;

            case ChestItemEffect.KillAllEnemies:
                ApplyKillAllEnemies();
                break;
        }
    }

    private static void ApplyFullHeal()
    {
        PlayerHealth ph = Object.FindFirstObjectByType<PlayerHealth>();
        if (ph != null)
            ph.Heal(ph.MaxHealth);
    }

    private static void ApplyKillAllEnemies()
    {
        // Copia la lista: morir desregistra al enemigo (modifica ActiveEnemies).
        List<EnemyHealth> enemies = new List<EnemyHealth>(EnemyHealth.ActiveEnemies);
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] != null)
                enemies[i].TakeDamage(999999f);
        }
    }

    private static List<ChestItemData> GetDefaultItems()
    {
        if (defaultItems != null)
            return defaultItems;

        defaultItems = new List<ChestItemData>
        {
            MakeItem("ImanGigante", "Imán Gigante",
                "Atrae hacia ti todos los orbes, monedas y objetos del mapa.",
                ChestItemEffect.GiantMagnet, new Color(0.4f, 0.8f, 1f)),

            MakeItem("CuracionTotal", "Curación Total",
                "Restaura toda tu vida al máximo.",
                ChestItemEffect.FullHeal, new Color(0.4f, 1f, 0.5f)),

            MakeItem("Aniquilacion", "Aniquilación",
                "Destruye a todos los enemigos en pantalla de golpe.",
                ChestItemEffect.KillAllEnemies, new Color(1f, 0.4f, 0.4f)),
        };

        return defaultItems;
    }

    private static ChestItemData MakeItem(string assetName, string itemName, string description, ChestItemEffect effect, Color accentColor)
    {
        ChestItemData item = ScriptableObject.CreateInstance<ChestItemData>();
        item.name = assetName;
        item.itemName = itemName;
        item.description = description;
        item.effect = effect;
        item.accentColor = accentColor;
        return item;
    }
}

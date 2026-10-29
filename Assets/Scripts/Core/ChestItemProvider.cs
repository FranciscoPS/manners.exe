using System.Collections.Generic;
using UnityEngine;

public static class ChestItemProvider
{
    private static List<ChestItemData> cachedItems;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        cachedItems = null;
    }

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

        if (cachedItems.Count == 0) Debug.LogError("Faltan los ChestItemData en Production/Resources/ChestItems.");

        return cachedItems;
    }

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

        List<EnemyHealth> enemies = new List<EnemyHealth>(EnemyHealth.ActiveEnemies);
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] != null)
                enemies[i].TakeDamage(999999f);
        }
    }

}

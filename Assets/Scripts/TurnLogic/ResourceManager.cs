using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Менеджер ресурсов по владельцам (ownerId = игрок, варвары, независимые).
/// Пул хранится по resourceId из ResourceStats. Всегда передавайте ownerId.
/// </summary>
public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    private Dictionary<int, Dictionary<string, float>> pools = new Dictionary<int, Dictionary<string, float>>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (transform.parent != null)
            transform.SetParent(null);

        DontDestroyOnLoad(gameObject);
    }

    private Dictionary<string, float> GetOrCreatePool(int ownerId)
    {
        if (!pools.TryGetValue(ownerId, out var pool))
        {
            pool = new Dictionary<string, float>();
            pools[ownerId] = pool;
        }
        return pool;
    }

    public float Get(int ownerId, string resourceId)
    {
        var pool = GetOrCreatePool(ownerId);
        return pool.TryGetValue(resourceId, out var amount) ? amount : 0f;
    }

    public void Add(int ownerId, string resourceId, float amount)
    {
        if (amount == 0 || string.IsNullOrEmpty(resourceId)) return;
        var pool = GetOrCreatePool(ownerId);
        if (pool.TryGetValue(resourceId, out var current))
            pool[resourceId] = current + amount;
        else
            pool[resourceId] = amount;
    }

    public bool CanAfford(int ownerId, string resourceId, float amount)
    {
        return Get(ownerId, resourceId) >= amount;
    }

    public bool Spend(int ownerId, string resourceId, float amount)
    {
        if (!CanAfford(ownerId, resourceId, amount)) return false;
        Add(ownerId, resourceId, -amount);
        return true;
    }

    /// <summary>
    /// Проверяет, хватает ли у ownerId ресурсов для списка затрат (build cost, unit cost).
    /// value в записи — количество к трате (положительное).
    /// </summary>
    public bool CanAffordCost(int ownerId, List<ResourceStatEntry> cost)
    {
        if (cost == null) return true;
        foreach (var e in cost)
        {
            if (e.resourceRef == null || string.IsNullOrEmpty(e.resourceRef.id)) continue;
            float need = Mathf.Abs(e.value);
            if (need <= 0f) continue;
            if (Get(ownerId, e.resourceRef.id) < need) return false;
        }
        return true;
    }

    /// <summary>
    /// Списывает ресурсы по списку затрат (build cost, unit cost, upkeep).
    /// Вызывать только если CanAffordCost вернул true.
    /// </summary>
    public void SpendCost(int ownerId, List<ResourceStatEntry> cost)
    {
        if (cost == null) return;
        foreach (var e in cost)
        {
            if (e.resourceRef == null || string.IsNullOrEmpty(e.resourceRef.id)) continue;
            float amount = Mathf.Abs(e.value);
            if (amount <= 0f) continue;
            Spend(ownerId, e.resourceRef.id, amount);
        }
    }
}



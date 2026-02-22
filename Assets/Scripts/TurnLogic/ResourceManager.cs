using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Менеджер ресурсов по владельцам (ownerId = игрок, варвары, независимые).
/// Пул хранится по resourceId из ResourceStats.
/// </summary>
public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    private Dictionary<int, Dictionary<string, float>> pools = new Dictionary<int, Dictionary<string, float>>();
    private const int DefaultOwnerId = 0;

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

    public float Get(string resourceId) => Get(DefaultOwnerId, resourceId);
    public void Add(string resourceId, float amount) => Add(DefaultOwnerId, resourceId, amount);
    public bool CanAfford(string resourceId, float amount) => CanAfford(DefaultOwnerId, resourceId, amount);
    public bool Spend(string resourceId, float amount) => Spend(DefaultOwnerId, resourceId, amount);
}



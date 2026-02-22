using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Типы ресурсов игрока.
/// </summary>
public enum ResourceType
{
    Gold,
    Food,
    Materials
}

/// <summary>
/// Пул ресурсов одного владельца (игрок, варвары, независимые).
/// </summary>
[System.Serializable]
public class ResourcePool
{
    public int gold;
    public int food;
    public int materials;
}

/// <summary>
/// Менеджер ресурсов по владельцам (ownerId = игрок, варвары, независимые).
/// </summary>
public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    private Dictionary<int, ResourcePool> pools = new Dictionary<int, ResourcePool>();
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

    private ResourcePool GetOrCreatePool(int ownerId)
    {
        if (!pools.TryGetValue(ownerId, out ResourcePool pool))
        {
            pool = new ResourcePool();
            pools[ownerId] = pool;
        }
        return pool;
    }

    public int Get(int ownerId, ResourceType type)
    {
        var pool = GetOrCreatePool(ownerId);
        return type switch
        {
            ResourceType.Gold => pool.gold,
            ResourceType.Food => pool.food,
            ResourceType.Materials => pool.materials,
            _ => 0
        };
    }

    public void Add(int ownerId, ResourceType type, int amount)
    {
        if (amount == 0) return;
        var pool = GetOrCreatePool(ownerId);
        switch (type)
        {
            case ResourceType.Gold: pool.gold += amount; break;
            case ResourceType.Food: pool.food += amount; break;
            case ResourceType.Materials: pool.materials += amount; break;
        }
    }

    public bool CanAfford(int ownerId, ResourceType type, int amount)
    {
        return Get(ownerId, type) >= amount;
    }

    public bool Spend(int ownerId, ResourceType type, int amount)
    {
        if (!CanAfford(ownerId, type, amount)) return false;
        Add(ownerId, type, -amount);
        return true;
    }

    /// <summary> Получить ресурс для владельца по умолчанию (обратная совместимость). </summary>
    public int Get(ResourceType type) => Get(DefaultOwnerId, type);

    /// <summary> Добавить ресурс владельцу по умолчанию (обратная совместимость). </summary>
    public void Add(ResourceType type, int amount) => Add(DefaultOwnerId, type, amount);

    public bool CanAfford(ResourceType type, int amount) => CanAfford(DefaultOwnerId, type, amount);

    public bool Spend(ResourceType type, int amount) => Spend(DefaultOwnerId, type, amount);

    /// <summary>
    /// Маппинг resourceId (string) в ResourceType для начисления дохода.
    /// </summary>
    public static bool TryGetResourceType(string resourceId, out ResourceType type)
    {
        if (string.IsNullOrEmpty(resourceId)) { type = ResourceType.Gold; return false; }
        switch (resourceId.ToLowerInvariant())
        {
            case "gold": type = ResourceType.Gold; return true;
            case "food": type = ResourceType.Food; return true;
            case "materials": type = ResourceType.Materials; return true;
            default: type = ResourceType.Gold; return false;
        }
    }
}



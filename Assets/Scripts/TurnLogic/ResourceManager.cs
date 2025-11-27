using UnityEngine;

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
/// Простой менеджер ресурсов игрока.
/// Хранит количество ресурсов и предоставляет базовые операции.
/// </summary>
public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    [Header("Текущие ресурсы")]
    [SerializeField] private int gold = 0;
    [SerializeField] private int food = 0;
    [SerializeField] private int materials = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public int Get(ResourceType type)
    {
        return type switch
        {
            ResourceType.Gold => gold,
            ResourceType.Food => food,
            ResourceType.Materials => materials,
            _ => 0
        };
    }

    public void Add(ResourceType type, int amount)
    {
        if (amount == 0)
            return;

        switch (type)
        {
            case ResourceType.Gold:
                gold += amount;
                break;
            case ResourceType.Food:
                food += amount;
                break;
            case ResourceType.Materials:
                materials += amount;
                break;
        }
    }

    public bool CanAfford(ResourceType type, int amount)
    {
        return Get(type) >= amount;
    }

    public bool Spend(ResourceType type, int amount)
    {
        if (!CanAfford(type, amount))
            return false;

        Add(type, -amount);
        return true;
    }
}



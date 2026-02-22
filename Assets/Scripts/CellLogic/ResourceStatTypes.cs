using System;

/// <summary>
/// Категория ресурса для применения модификаторов CellTypeStats.
/// none — для movementCost и прочих не-ресурсных статов.
/// </summary>
public enum ResourceStatType
{
    None,
    Plant,
    RawMaterial
}

/// <summary>
/// Одна запись ресурса: имя, тип, значение.
/// Используется в FeatureStats, BuildingStats, CellTypeStats.defaultResources.
/// </summary>
[Serializable]
public struct ResourceStatEntry
{
    public string name;
    public ResourceStatType type;
    public float value;

    public ResourceStatEntry(string name, ResourceStatType type, float value)
    {
        this.name = name ?? string.Empty;
        this.type = type;
        this.value = value;
    }
}

/// <summary>
/// Модификатор по типу ресурса для CellTypeStats.
/// modifier в диапазоне [-1, ∞), напр. -0.15 для -15%.
/// </summary>
[Serializable]
public struct ResourceStatModifier
{
    public ResourceStatType type;
    public float modifier;

    public ResourceStatModifier(ResourceStatType type, float modifier)
    {
        this.type = type;
        this.modifier = modifier;
    }
}

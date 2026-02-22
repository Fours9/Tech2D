using System;

/// <summary>
/// Категория ресурса для применения модификаторов.
/// None — для movementCost и прочих не-ресурсных статов.
/// </summary>
public enum ResourceStatType
{
    None,
    Plant,
    RawMaterial
}

/// <summary>
/// Одна запись ресурса: ссылка на ResourceStats и значение.
/// Используется в FeatureStats, BuildingStats, CellTypeStats.defaultResources.
/// </summary>
[Serializable]
public struct ResourceStatEntry
{
    public ResourceStats resourceRef;
    public float value;

    public ResourceStatEntry(ResourceStats resourceRef, float value)
    {
        this.resourceRef = resourceRef;
        this.value = value;
    }
}

/// <summary>
/// Модификатор для конкретного ресурса.
/// modifier в диапазоне [-1, ∞), напр. -0.15 для -15%.
/// Используется в CellTypeStats, FeatureStats, BuildingStats.
/// </summary>
[Serializable]
public struct ResourceStatModifier
{
    public ResourceStats resourceRef;  // к какому ресурсу применяется
    public float modifier;             // множитель в [-1, ∞), напр. -0.15

    public ResourceStatModifier(ResourceStats resourceRef, float modifier)
    {
        this.resourceRef = resourceRef;
        this.modifier = modifier;
    }
}

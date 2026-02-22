using System;
using UnityEngine;

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
/// Уровень применения бонуса: клетка, город или игрок.
/// </summary>
public enum BonusApplicationLevel
{
    Cell,
    City,
    Player
}

/// <summary>
/// Запись дохода по одному ресурсу для опроса клетки/города (id, количество, отображаемое имя, тип).
/// </summary>
[Serializable]
public struct ResourceIncomeEntry
{
    public string resourceId;
    public float amount;
    public string displayName;
    public ResourceStatType resourceStatType;
    public ResourceStats resourceStats;

    public ResourceIncomeEntry(string resourceId, float amount, string displayName, ResourceStatType resourceStatType, ResourceStats resourceStats = null)
    {
        this.resourceId = resourceId;
        this.amount = amount;
        this.displayName = displayName ?? "";
        this.resourceStatType = resourceStatType;
        this.resourceStats = resourceStats;
    }
}

/// <summary>
/// Бонус к ресурсу: цель — ИЛИ ссылка на ассет ресурса, ИЛИ тип (Plant/RawMaterial).
/// Может быть плоским (+5), процентным (+15%) или обоими. Формула: (amount + sumFlat) * (1 + sumPercent).
/// </summary>
[Serializable]
public struct ResourceBonus
{
    [Tooltip("Ссылка на ассет ресурса. Заполнять ИЛИ это поле, ИЛИ targetType (не оба).")]
    public ResourceStats targetResource;
    [Tooltip("Тип ресурсов (Plant, RawMaterial). Используется, если targetResource не задан.")]
    public ResourceStatType targetType;
    [Tooltip("Плоский бонус: добавляется к значению (напр. +5).")]
    public float flatValue;
    [Tooltip("Процентный бонус: 0.15 = +15%. Применяется после flat.")]
    public float percentValue;
    [Tooltip("Устаревшее: если percentValue == 0, используется как процент (0.15 = +15%).")]
    public float modifier;
    public BonusApplicationLevel applicationLevel;

    public ResourceBonus(ResourceStats targetResource, ResourceStatType targetType, float flatValue, float percentValue, BonusApplicationLevel applicationLevel)
    {
        this.targetResource = targetResource;
        this.targetType = targetType;
        this.flatValue = flatValue;
        this.percentValue = percentValue;
        this.modifier = 0f;
        this.applicationLevel = applicationLevel;
    }

    /// <summary> Эффективный процент для применения (percentValue или legacy modifier). </summary>
    public float EffectivePercent => percentValue != 0f ? percentValue : modifier;
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
/// Модификатор для ресурса: цель — ИЛИ ссылка на ассет ресурса (resourceRef), ИЛИ тип (targetType). modifier в [-1, ∞).
/// Используется в CellTypeStats, FeatureStats, BuildingStats.
/// </summary>
[Serializable]
public struct ResourceStatModifier
{
    [Tooltip("Ссылка на ассет ресурса. Заполнять ИЛИ это поле, ИЛИ targetType (не оба).")]
    public ResourceStats resourceRef;
    [Tooltip("Тип ресурсов (Plant, RawMaterial). Используется, если resourceRef не задан.")]
    public ResourceStatType targetType;
    public float modifier;              // множитель в [-1, ∞), напр. -0.15

    public ResourceStatModifier(ResourceStats resourceRef, float modifier)
    {
        this.resourceRef = resourceRef;
        this.targetType = ResourceStatType.None;
        this.modifier = modifier;
    }
}

using System.Collections.Generic;
using UnityEngine;
using CellNameSpace;

/// <summary>
/// Статический калькулятор агрегации статов клетки из CellTypeStats, FeatureStats, BuildingStats.
/// </summary>
public static class StatsCalculator
{
    /// <summary>
    /// Рассчитывает итоговые CellStats из трёх источников.
    /// Формула для ресурсов: (feature_value + cellType_default + building_delta) * (1 + totalModifier)
    /// totalModifier = сумма модификаторов из CellTypeStats, FeatureStats, BuildingStats по resourceRef.id
    /// Формула для movementCost: cellType_base + buildingStats.movementCostDelta
    /// </summary>
    public static CellNameSpace.CellStats Calculate(CellTypeStats cellTypeStats, FeatureStats featureStats, BuildingStats buildingStats)
    {
        var result = new CellNameSpace.CellStats();

        if (cellTypeStats == null)
        {
            result.movementCost = 1;
            result.isWalkable = true;
            return result;
        }

        result.isWalkable = cellTypeStats.isWalkable;

        // Собираем все уникальные id ресурсов из трёх источников
        var allResourceIds = new HashSet<string>();

        if (cellTypeStats.defaultResources != null)
        {
            foreach (var e in cellTypeStats.defaultResources)
            {
                if (e.resourceRef != null && !string.IsNullOrEmpty(e.resourceRef.id) && e.resourceRef.type != ResourceStatType.None)
                    allResourceIds.Add(e.resourceRef.id);
            }
        }

        if (featureStats != null && featureStats.resourceEntries != null)
        {
            foreach (var e in featureStats.resourceEntries)
            {
                if (e.resourceRef != null && !string.IsNullOrEmpty(e.resourceRef.id) && e.resourceRef.type != ResourceStatType.None)
                    allResourceIds.Add(e.resourceRef.id);
            }
        }

        if (buildingStats != null && buildingStats.resourceEntries != null)
        {
            foreach (var e in buildingStats.resourceEntries)
            {
                if (e.resourceRef != null && !string.IsNullOrEmpty(e.resourceRef.id) && e.resourceRef.type != ResourceStatType.None)
                    allResourceIds.Add(e.resourceRef.id);
            }
        }

        // Рассчитываем каждый ресурс: (feature + default) * (1 + modifier) + building_delta
        foreach (var resourceId in allResourceIds)
        {
            float featureValue = GetValueFromList(featureStats?.resourceEntries, resourceId);
            float cellTypeDefault = GetValueFromList(cellTypeStats.defaultResources, resourceId);
            float buildingDelta = GetValueFromList(buildingStats?.resourceEntries, resourceId);

            float totalModifier = GetModifierForResource(cellTypeStats, featureStats, buildingStats, resourceId);
            float baseValue = featureValue + cellTypeDefault;
            float value = baseValue * (1f + totalModifier) + buildingDelta;
            result.resources[resourceId] = value;
        }

        // movementCost: cellType_base + buildingStats.movementCostDelta
        int movementBase = cellTypeStats.movementCost;
        int movementDelta = buildingStats != null ? buildingStats.movementCostDelta : 0;
        result.movementCost = Mathf.Max(1, movementBase + movementDelta);

        return result;
    }

    private static float GetValueFromList(List<ResourceStatEntry> list, string resourceId)
    {
        if (list == null) return 0f;
        float sum = 0f;
        foreach (var e in list)
        {
            if (e.resourceRef != null && e.resourceRef.id == resourceId)
                sum += e.value;
        }
        return sum;
    }

    private static float GetModifierForResource(CellTypeStats cellTypeStats, FeatureStats featureStats, BuildingStats buildingStats, string resourceId)
    {
        float total = 0f;
        if (cellTypeStats?.resourceModifiers != null)
        {
            foreach (var m in cellTypeStats.resourceModifiers)
            {
                if (m.resourceRef != null && m.resourceRef.id == resourceId)
                    total += m.modifier;
            }
        }
        if (featureStats?.resourceModifiers != null)
        {
            foreach (var m in featureStats.resourceModifiers)
            {
                if (m.resourceRef != null && m.resourceRef.id == resourceId)
                    total += m.modifier;
            }
        }
        if (buildingStats?.resourceModifiers != null)
        {
            foreach (var m in buildingStats.resourceModifiers)
            {
                if (m.resourceRef != null && m.resourceRef.id == resourceId)
                    total += m.modifier;
            }
        }
        return total;
    }
}

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
    /// Формула для ресурсов: (feature_value + cellType_default) * (1 + cellType_modifier) + building_delta
    /// Формула для movementCost: cellType_base + building_delta (аддитивно)
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

        // Собираем модификаторы из CellTypeStats в словарь
        var modifiersByType = new Dictionary<ResourceStatType, float>();
        if (cellTypeStats.resourceModifiers != null)
        {
            foreach (var m in cellTypeStats.resourceModifiers)
            {
                modifiersByType[m.type] = m.modifier;
            }
        }

        // Собираем все уникальные имена ресурсов из трёх источников
        var allResourceNames = new HashSet<string>();

        if (cellTypeStats.defaultResources != null)
        {
            foreach (var e in cellTypeStats.defaultResources)
            {
                if (!string.IsNullOrEmpty(e.name) && e.type != ResourceStatType.None)
                    allResourceNames.Add(e.name);
            }
        }

        if (featureStats != null && featureStats.resourceEntries != null)
        {
            foreach (var e in featureStats.resourceEntries)
            {
                if (!string.IsNullOrEmpty(e.name) && e.type != ResourceStatType.None)
                    allResourceNames.Add(e.name);
            }
        }

        if (buildingStats != null && buildingStats.resourceEntries != null)
        {
            foreach (var e in buildingStats.resourceEntries)
            {
                if (!string.IsNullOrEmpty(e.name) && e.type != ResourceStatType.None)
                    allResourceNames.Add(e.name);
            }
        }

        // Рассчитываем каждый ресурс
        foreach (var name in allResourceNames)
        {
            float featureValue = GetValueFromList(featureStats?.resourceEntries, name);
            float cellTypeDefault = GetValueFromList(cellTypeStats.defaultResources, name);
            float buildingDelta = GetValueFromList(buildingStats?.resourceEntries, name);
            ResourceStatType resourceType = GetTypeFromLists(cellTypeStats.defaultResources, featureStats?.resourceEntries, buildingStats?.resourceEntries, name);
            float modifier = modifiersByType.TryGetValue(resourceType, out var m) ? m : 0f;

            float value = (featureValue + cellTypeDefault) * (1f + modifier) + buildingDelta;
            result.resources[name] = value;
        }

        // movementCost: type none, аддитивно
        int movementBase = cellTypeStats.movementCost;
        float movementDelta = GetMovementDeltaFromBuilding(buildingStats);
        result.movementCost = Mathf.Max(1, Mathf.RoundToInt(movementBase + movementDelta));

        return result;
    }

    private static float GetValueFromList(List<ResourceStatEntry> list, string name)
    {
        if (list == null) return 0f;
        float sum = 0f;
        foreach (var e in list)
        {
            if (e.name == name)
                sum += e.value;
        }
        return sum;
    }

    private static ResourceStatType GetTypeFromLists(List<ResourceStatEntry> defaultList, List<ResourceStatEntry> featureList, List<ResourceStatEntry> buildingList, string name)
    {
        if (defaultList != null)
        {
            foreach (var e in defaultList)
                if (e.name == name) return e.type;
        }
        if (featureList != null)
        {
            foreach (var e in featureList)
                if (e.name == name) return e.type;
        }
        if (buildingList != null)
        {
            foreach (var e in buildingList)
                if (e.name == name) return e.type;
        }
        return ResourceStatType.None;
    }

    private static float GetMovementDeltaFromBuilding(BuildingStats buildingStats)
    {
        if (buildingStats?.resourceEntries == null) return 0f;
        foreach (var e in buildingStats.resourceEntries)
        {
            if (e.type == ResourceStatType.None && (e.name == "movement" || e.name == "movementCost"))
                return e.value;
        }
        return 0f;
    }
}

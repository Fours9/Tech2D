using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Информация об игроке
/// </summary>
[System.Serializable]
public class PlayerInfo
{
    [Header("Идентификатор")]
    public int playerId = 0; // Уникальный ID игрока
    public string playerName = "Player"; // Имя игрока
    
    [Header("Визуал")]
    public Color playerColor = Color.white; // Цвет игрока для визуализации территории

    [Header("Бонусы (Cell/City/Player)")]
    public List<ResourceBonus> bonuses = new List<ResourceBonus>();

    /// <summary> Список бонусов игрока (Cell/City/Player). </summary>
    public List<ResourceBonus> Bonuses => bonuses;

    // Кеши копий статов (не общие — у каждого игрока свои)
    private Dictionary<string, BuildingStats> buildingCache;
    private Dictionary<string, FeatureStats> featureCache;
    private Dictionary<string, CellTypeStats> cellTypeCache;

    /// <summary>
    /// Конструктор для создания игрока
    /// </summary>
    public PlayerInfo(int id, string name, Color color)
    {
        playerId = id;
        playerName = name;
        playerColor = color;
    }
    
    /// <summary>
    /// Конструктор по умолчанию
    /// </summary>
    public PlayerInfo()
    {
        playerId = 0;
        playerName = "Player";
        playerColor = Color.white;
    }

    // --- BuildingStats ---
    public void InitializeBuildingCache(List<BuildingStats> templates)
    {
        buildingCache = new Dictionary<string, BuildingStats>();
        if (templates == null) return;
        foreach (var t in templates)
        {
            if (t == null || string.IsNullOrEmpty(t.id)) continue;
            var copy = ScriptableObject.Instantiate(t);
            buildingCache[t.id] = copy;
        }
    }
    public BuildingStats GetBuilding(string buildingId)
    {
        if (buildingCache == null) return null;
        return buildingCache.TryGetValue(buildingId, out var s) ? s : null;
    }
    public void UpdateBuilding(BuildingStats template)
    {
        if (template == null || buildingCache == null || string.IsNullOrEmpty(template.id)) return;
        var copy = ScriptableObject.Instantiate(template);
        buildingCache[template.id] = copy;
    }

    // --- FeatureStats ---
    public void InitializeFeatureCache(List<FeatureStats> templates)
    {
        featureCache = new Dictionary<string, FeatureStats>();
        if (templates == null) return;
        foreach (var t in templates)
        {
            if (t == null) continue;
            var copy = ScriptableObject.Instantiate(t);
            string key = !string.IsNullOrEmpty(t.id) ? t.id : (t.displayName ?? "unknown");
            featureCache[key] = copy;
        }
    }
    public FeatureStats GetFeature(string featureId)
    {
        if (featureCache == null) return null;
        return featureCache.TryGetValue(featureId, out var s) ? s : null;
    }
    public void UpdateFeature(FeatureStats template)
    {
        if (template == null || featureCache == null) return;
        string key = !string.IsNullOrEmpty(template.id) ? template.id : (template.displayName ?? "unknown");
        var copy = ScriptableObject.Instantiate(template);
        featureCache[key] = copy;
    }

    // --- CellTypeStats ---
    public void InitializeCellTypeCache(List<CellTypeStats> templates)
    {
        cellTypeCache = new Dictionary<string, CellTypeStats>();
        if (templates == null) return;
        foreach (var t in templates)
        {
            if (t == null) continue;
            var copy = ScriptableObject.Instantiate(t);
            string key = !string.IsNullOrEmpty(t.id) ? t.id : t.cellType.ToString();
            cellTypeCache[key] = copy;
        }
    }
    public CellTypeStats GetCellTypeStats(string cellTypeId)
    {
        if (cellTypeCache == null) return null;
        return cellTypeCache.TryGetValue(cellTypeId, out var s) ? s : null;
    }
    public void UpdateCellTypeStats(CellTypeStats template)
    {
        if (template == null || cellTypeCache == null) return;
        string key = !string.IsNullOrEmpty(template.id) ? template.id : template.cellType.ToString();
        var copy = ScriptableObject.Instantiate(template);
        cellTypeCache[key] = copy;
    }
}


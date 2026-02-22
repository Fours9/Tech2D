using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Единая точка хранения всех .asset статов (BuildingStats, FeatureStats, CellTypeStats) для первого копирования в Player.
/// В инспектор скидываются все ассеты — отдаёт списки в GameSetup для Initialize*Cache.
/// </summary>
public class FirstStatsManager : MonoBehaviour
{
    public static FirstStatsManager Instance { get; private set; }

    [Header("Building Stats (.asset)")]
    [SerializeField] private List<BuildingStats> buildingStatsList = new List<BuildingStats>();

    [Header("Feature Stats (.asset)")]
    [SerializeField] private List<FeatureStats> featureStatsList = new List<FeatureStats>();

    [Header("Cell Type Stats (.asset)")]
    [SerializeField] private List<CellTypeStats> cellTypeStatsList = new List<CellTypeStats>();

    [Header("Resource Stats (.asset)")]
    [SerializeField] private List<ResourceStats> resourceStatsList = new List<ResourceStats>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (transform.parent != null)
        {
            transform.SetParent(null);
        }

        DontDestroyOnLoad(gameObject);
    }

    /// <summary> Возвращает все BuildingStats для InitializeBuildingCache. </summary>
    public List<BuildingStats> GetAllBuildingStats()
    {
        return buildingStatsList != null ? new List<BuildingStats>(buildingStatsList) : new List<BuildingStats>();
    }

    /// <summary> Возвращает все FeatureStats для InitializeFeatureCache. </summary>
    public List<FeatureStats> GetAllFeatureStats()
    {
        return featureStatsList != null ? new List<FeatureStats>(featureStatsList) : new List<FeatureStats>();
    }

    /// <summary> Возвращает все CellTypeStats для InitializeCellTypeCache. </summary>
    public List<CellTypeStats> GetAllCellTypeStats()
    {
        return cellTypeStatsList != null ? new List<CellTypeStats>(cellTypeStatsList) : new List<CellTypeStats>();
    }

    /// <summary> Fallback для клеток без владельца. </summary>
    public BuildingStats GetBuildingStatsById(string id)
    {
        if (string.IsNullOrEmpty(id) || buildingStatsList == null) return null;
        foreach (var s in buildingStatsList)
        {
            if (s != null && s.id == id)
                return s;
        }
        return null;
    }

    /// <summary> Fallback для клеток без владельца. </summary>
    public FeatureStats GetFeatureStatsById(string id)
    {
        if (string.IsNullOrEmpty(id) || featureStatsList == null) return null;
        foreach (var s in featureStatsList)
        {
            if (s != null && s.id == id)
                return s;
        }
        return null;
    }

    /// <summary> Fallback для клеток без владельца. </summary>
    public ResourceStats GetResourceStatsById(string id)
    {
        if (string.IsNullOrEmpty(id) || resourceStatsList == null) return null;
        foreach (var s in resourceStatsList)
        {
            if (s != null && s.id == id)
                return s;
        }
        return null;
    }

    /// <summary> Возвращает все ResourceStats. </summary>
    public List<ResourceStats> GetAllResourceStats()
    {
        return resourceStatsList != null ? new List<ResourceStats>(resourceStatsList) : new List<ResourceStats>();
    }

    /// <summary> Fallback для клеток без владельца. </summary>
    public CellTypeStats GetCellTypeStatsById(string id)
    {
        if (string.IsNullOrEmpty(id) || cellTypeStatsList == null) return null;
        foreach (var s in cellTypeStatsList)
        {
            if (s != null && (s.id == id || string.IsNullOrEmpty(s.id) && s.cellType.ToString() == id))
                return s;
        }
        return null;
    }
}

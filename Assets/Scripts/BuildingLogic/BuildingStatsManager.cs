using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Централизованный менеджер для управления BuildingStats (ScriptableObject ассетами типов построек).
/// Обеспечивает доступ к статам построек по id.
/// </summary>
public class BuildingStatsManager : MonoBehaviour
{
    public static BuildingStatsManager Instance { get; private set; }

    [Header("Статы типов построек")]
    [SerializeField] private List<BuildingStats> buildingStatsList = new List<BuildingStats>();

    private Dictionary<string, BuildingStats> buildingStatsDictionary;

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
        InitializeDictionary();
    }

    private void InitializeDictionary()
    {
        buildingStatsDictionary = new Dictionary<string, BuildingStats>();

        if (buildingStatsList == null) return;

        foreach (var s in buildingStatsList)
        {
            if (s != null && !string.IsNullOrEmpty(s.id))
                buildingStatsDictionary[s.id] = s;
        }
    }

    /// <summary>
    /// Получает BuildingStats по id постройки
    /// </summary>
    public BuildingStats GetBuildingStatsById(string id)
    {
        if (buildingStatsDictionary == null)
            InitializeDictionary();

        if (string.IsNullOrEmpty(id)) return null;

        return buildingStatsDictionary.TryGetValue(id, out var stats) ? stats : null;
    }

    /// <summary>
    /// Возвращает все BuildingStats для UI и других целей
    /// </summary>
    public List<BuildingStats> GetAllBuildingStats()
    {
        return buildingStatsList != null ? new List<BuildingStats>(buildingStatsList) : new List<BuildingStats>();
    }

    /// <summary>
    /// Обновляет словарь (вызывать после изменения buildingStatsList в Inspector)
    /// </summary>
    public void RefreshBuildingStats()
    {
        InitializeDictionary();
    }
}




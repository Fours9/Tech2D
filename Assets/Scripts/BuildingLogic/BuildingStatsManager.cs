using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Централизованный менеджер для управления BuildingStats (ScriptableObject ассетами типов построек).
/// Обеспечивает доступ к статам построек по их типу (enum).
/// </summary>
public class BuildingStatsManager : MonoBehaviour
{
    public static BuildingStatsManager Instance { get; private set; }

    [System.Serializable]
    public class BuildingStatsMapping
    {
        public BuildingType buildingType;
        public BuildingStats buildingStats;
    }

    [Header("Статы типов построек")]
    [SerializeField] private List<BuildingStatsMapping> buildingStatsMappings = new List<BuildingStatsMapping>();

    private Dictionary<BuildingType, BuildingStats> buildingStatsDictionary;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        
        // Перемещаем GameObject в корень, если он дочерний (DontDestroyOnLoad работает только для корневых объектов)
        if (transform.parent != null)
        {
            transform.SetParent(null);
        }
        
        DontDestroyOnLoad(gameObject);
        InitializeDictionary();
    }

    /// <summary>
    /// Инициализирует словарь статов из списка маппингов
    /// </summary>
    private void InitializeDictionary()
    {
        buildingStatsDictionary = new Dictionary<BuildingType, BuildingStats>();

        foreach (var mapping in buildingStatsMappings)
        {
            if (mapping.buildingStats != null)
            {
                buildingStatsDictionary[mapping.buildingType] = mapping.buildingStats;
            }
        }
    }

    /// <summary>
    /// Получает BuildingStats для указанного типа постройки
    /// </summary>
    /// <param name="buildingType">Тип постройки</param>
    /// <returns>BuildingStats для данного типа, или null если не найден</returns>
    public BuildingStats GetBuildingStats(BuildingType buildingType)
    {
        if (buildingStatsDictionary == null)
        {
            InitializeDictionary();
        }

        if (buildingStatsDictionary.TryGetValue(buildingType, out BuildingStats stats))
        {
            return stats;
        }

        return null;
    }

    /// <summary>
    /// Получает список всех доступных типов построек (для UI и других целей)
    /// </summary>
    /// <returns>Список всех настроенных типов построек</returns>
    public List<BuildingType> GetAvailableBuildingTypes()
    {
        if (buildingStatsDictionary == null)
        {
            InitializeDictionary();
        }

        return new List<BuildingType>(buildingStatsDictionary.Keys);
    }

    /// <summary>
    /// Обновляет словарь статов (вызывать после изменения buildingStatsMappings в Inspector)
    /// </summary>
    public void RefreshBuildingStats()
    {
        InitializeDictionary();
    }
}




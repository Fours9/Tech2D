using UnityEngine;
using System.Collections.Generic;
using CellNameSpace;

/// <summary>
/// Централизованный менеджер для управления CellStats (ScriptableObject ассетами типов клеток).
/// Обеспечивает доступ к статам клеток по их типу (enum).
/// </summary>
public class CellStatsManager : MonoBehaviour
{
    public static CellStatsManager Instance { get; private set; }

    [System.Serializable]
    public class CellStatsMapping
    {
        public CellType cellType;
        public CellStats cellStats;
    }

    [Header("Статы типов клеток")]
    [SerializeField] private List<CellStatsMapping> cellStatsMappings = new List<CellStatsMapping>();

    private Dictionary<CellType, CellStats> cellStatsDictionary;

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
        cellStatsDictionary = new Dictionary<CellType, CellStats>();

        foreach (var mapping in cellStatsMappings)
        {
            if (mapping.cellStats != null)
            {
                cellStatsDictionary[mapping.cellType] = mapping.cellStats;
            }
        }
    }

    /// <summary>
    /// Получает CellStats для указанного типа клетки
    /// </summary>
    /// <param name="cellType">Тип клетки</param>
    /// <returns>CellStats для данного типа, или null если не найден</returns>
    public CellStats GetCellStats(CellType cellType)
    {
        if (cellStatsDictionary == null)
        {
            InitializeDictionary();
        }

        if (cellStatsDictionary.TryGetValue(cellType, out CellStats stats))
        {
            return stats;
        }

        return null;
    }

    /// <summary>
    /// Получает стоимость движения для типа клетки (из CellStats или fallback значения)
    /// </summary>
    public int GetMovementCost(CellType cellType)
    {
        CellStats stats = GetCellStats(cellType);
        if (stats != null)
        {
            return stats.movementCost;
        }

        // Fallback значения (старая логика)
        switch (cellType)
        {
            case CellType.field:
                return 1;
            case CellType.forest:
            case CellType.desert:
                return 2;
            case CellType.mountain:
                return 3;
            case CellType.deep_water:
            case CellType.shallow:
                return 1000;
            default:
                return 1;
        }
    }

    /// <summary>
    /// Проверяет, проходима ли клетка (из CellStats или fallback значения)
    /// </summary>
    public bool IsWalkable(CellType cellType)
    {
        CellStats stats = GetCellStats(cellType);
        if (stats != null)
        {
            return stats.isWalkable;
        }

        // Fallback значения
        return cellType != CellType.deep_water && cellType != CellType.shallow;
    }

    /// <summary>
    /// Обновляет словарь статов (вызывать после изменения cellStatsMappings в Inspector)
    /// </summary>
    public void RefreshCellStats()
    {
        InitializeDictionary();
    }
}




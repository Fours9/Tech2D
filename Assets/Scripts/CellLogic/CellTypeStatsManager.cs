using UnityEngine;
using System.Collections.Generic;
using CellNameSpace;

/// <summary>
/// Централизованный менеджер для управления CellTypeStats (ScriptableObject ассетами типов клеток).
/// Обеспечивает доступ к статам клеток по их типу (enum).
/// </summary>
public class CellTypeStatsManager : MonoBehaviour
{
    public static CellTypeStatsManager Instance { get; private set; }

    [System.Serializable]
    public class CellTypeStatsMapping
    {
        public CellType cellType;
        public CellTypeStats cellTypeStats;
    }

    [Header("Статы типов клеток")]
    [SerializeField] private List<CellTypeStatsMapping> cellTypeStatsMappings = new List<CellTypeStatsMapping>();

    private Dictionary<CellType, CellTypeStats> cellTypeStatsDictionary;

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
        cellTypeStatsDictionary = new Dictionary<CellType, CellTypeStats>();

        foreach (var mapping in cellTypeStatsMappings)
        {
            if (mapping.cellTypeStats != null)
            {
                cellTypeStatsDictionary[mapping.cellType] = mapping.cellTypeStats;
            }
        }
    }

    /// <summary>
    /// Получает CellTypeStats для указанного типа клетки
    /// </summary>
    /// <param name="cellType">Тип клетки</param>
    /// <returns>CellTypeStats для данного типа, или null если не найден</returns>
    public CellTypeStats GetCellTypeStats(CellType cellType)
    {
        if (cellTypeStatsDictionary == null)
        {
            InitializeDictionary();
        }

        if (cellTypeStatsDictionary.TryGetValue(cellType, out CellTypeStats stats))
        {
            return stats;
        }

        return null;
    }

    /// <summary>
    /// Получает стоимость движения для типа клетки (из CellTypeStats.movementCost или fallback)
    /// </summary>
    public int GetMovementCost(CellType cellType)
    {
        CellTypeStats stats = GetCellTypeStats(cellType);
        if (stats != null && stats.movementCost > 0)
            return Mathf.Max(1, stats.movementCost);

        // Fallback значения
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
    /// Проверяет, проходима ли клетка (из CellTypeStats или fallback значения)
    /// </summary>
    public bool IsWalkable(CellType cellType)
    {
        CellTypeStats stats = GetCellTypeStats(cellType);
        if (stats != null)
        {
            return stats.isWalkable;
        }

        // Fallback значения
        return cellType != CellType.deep_water && cellType != CellType.shallow;
    }

    /// <summary>
    /// Обновляет словарь статов (вызывать после изменения cellTypeStatsMappings в Inspector)
    /// </summary>
    public void RefreshCellTypeStats()
    {
        InitializeDictionary();
    }
}

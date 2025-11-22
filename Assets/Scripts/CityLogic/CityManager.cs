using UnityEngine;
using System.Collections.Generic;
using CellNameSpace;

/// <summary>
/// Менеджер для управления городами (создание, удаление и т.д.)
/// </summary>
public class CityManager : MonoBehaviour
{
    [Header("Настройки городов")]
    [SerializeField] private Sprite cityCenterSprite; // Спрайт центра города
    
    [Header("Ссылки (опционально)")]
    [SerializeField] private CellNameSpace.Grid grid; // Ссылка на Grid (найдет автоматически, если не указана)
    [SerializeField] private UnitManager unitManager; // Ссылка на UnitManager (найдет автоматически, если не указана)
    
    private Dictionary<Vector2Int, CityInfo> cities = new Dictionary<Vector2Int, CityInfo>(); // Словарь городов по позициям
    
    void Start()
    {
        // Находим Grid, если не указан
        if (grid == null)
        {
            grid = FindFirstObjectByType<CellNameSpace.Grid>();
            if (grid == null)
            {
                Debug.LogWarning("CityManager: Grid не найден. Создание городов может не работать.");
            }
        }
        
        // Находим UnitManager, если не указан
        if (unitManager == null)
        {
            unitManager = FindFirstObjectByType<UnitManager>();
            if (unitManager == null)
            {
                Debug.LogWarning("CityManager: UnitManager не найден.");
            }
        }
    }
    
    /// <summary>
    /// Создает город на позиции выбранного юнита
    /// </summary>
    /// <param name="unit">Юнит, который будет преобразован в город</param>
    /// <returns>true если город успешно создан, false иначе</returns>
    public bool CreateCityFromUnit(UnitInfo unit)
    {
        if (unit == null)
        {
            Debug.LogWarning("CityManager: UnitInfo равен null");
            return false;
        }
        
        if (!unit.IsPositionInitialized())
        {
            Debug.LogWarning("CityManager: Позиция юнита не инициализирована");
            return false;
        }
        
        int gridX = unit.GetGridX();
        int gridY = unit.GetGridY();
        Vector2Int cityPosition = new Vector2Int(gridX, gridY);
        
        // Проверяем, нет ли уже города на этой позиции
        if (cities.ContainsKey(cityPosition))
        {
            Debug.LogWarning($"CityManager: На позиции ({gridX}, {gridY}) уже есть город");
            return false;
        }
        
        // Получаем клетку по координатам
        CellInfo targetCell = GetCellAtPosition(gridX, gridY);
        if (targetCell == null)
        {
            Debug.LogWarning($"CityManager: Клетка на позиции ({gridX}, {gridY}) не найдена");
            return false;
        }
        
        // Устанавливаем спрайт города на слой построек
        if (cityCenterSprite != null)
        {
            targetCell.SetBuildingSprite(cityCenterSprite);
            Debug.Log($"CityManager: Спрайт города установлен на клетку ({gridX}, {gridY})");
        }
        else
        {
            Debug.LogWarning("CityManager: Спрайт центра города не назначен!");
        }
        
        // Создаем информацию о городе
        CityInfo cityInfo = new CityInfo
        {
            position = cityPosition,
            cell = targetCell,
            name = $"Город {cities.Count + 1}"
        };
        
        cities[cityPosition] = cityInfo;
        
        // Удаляем юнит
        if (unitManager != null)
        {
            unitManager.RemoveUnit(unit.gameObject);
            Debug.Log($"CityManager: Юнит удален");
        }
        else
        {
            Destroy(unit.gameObject);
        }
        
        // Снимаем выделение с юнита, если он был выбран
        if (UnitSelectionManager.Instance != null && UnitSelectionManager.Instance.GetSelectedUnit() == unit)
        {
            UnitSelectionManager.Instance.DeselectUnit();
        }
        
        Debug.Log($"CityManager: Город создан на позиции ({gridX}, {gridY})");
        return true;
    }
    
    /// <summary>
    /// Проверяет, есть ли город на указанной позиции
    /// </summary>
    public bool HasCityAt(Vector2Int position)
    {
        return cities.ContainsKey(position);
    }
    
    /// <summary>
    /// Получает информацию о городе на указанной позиции
    /// </summary>
    public CityInfo GetCityAt(Vector2Int position)
    {
        cities.TryGetValue(position, out CityInfo city);
        return city;
    }
    
    /// <summary>
    /// Получает все города
    /// </summary>
    public Dictionary<Vector2Int, CityInfo> GetAllCities()
    {
        return new Dictionary<Vector2Int, CityInfo>(cities);
    }
    
    /// <summary>
    /// Получает клетку по координатам сетки
    /// </summary>
    private CellInfo GetCellAtPosition(int gridX, int gridY)
    {
        // Используем метод Grid, если доступен (более эффективно)
        if (grid != null)
        {
            return grid.GetCellInfoAt(gridX, gridY);
        }
        
        // Иначе ищем через все CellInfo (менее эффективно, но работает)
        CellInfo[] allCells = FindObjectsByType<CellInfo>(FindObjectsSortMode.None);
        
        foreach (CellInfo cell in allCells)
        {
            if (cell.GetGridX() == gridX && cell.GetGridY() == gridY)
            {
                return cell;
            }
        }
        
        return null;
    }
}

/// <summary>
/// Информация о городе
/// </summary>
[System.Serializable]
public class CityInfo
{
    public Vector2Int position;
    public CellInfo cell;
    public string name;
}


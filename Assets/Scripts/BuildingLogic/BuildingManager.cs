using UnityEngine;
using System.Collections.Generic;
using CellNameSpace;

/// <summary>
/// Менеджер для управления постройками на клетках
/// </summary>
public class BuildingManager : MonoBehaviour
{
    [Header("Ссылки (опционально)")]
    [SerializeField] private CityManager cityManager; // Менеджер городов (найдет автоматически, если не указан)
    [SerializeField] private CellNameSpace.Grid grid; // Ссылка на Grid (найдет автоматически, если не указана)
    
    private Dictionary<Vector2Int, BuildingInfo> placedBuildings = new Dictionary<Vector2Int, BuildingInfo>(); // Словарь построек по позициям
    private BuildingInfo selectedBuilding = null; // Выбранная постройка для установки
    
    void Start()
    {
        // Находим CityManager, если не указан
        if (cityManager == null)
        {
            cityManager = FindFirstObjectByType<CityManager>();
            if (cityManager == null)
            {
                Debug.LogWarning("BuildingManager: CityManager не найден. Установка построек может не работать.");
            }
        }
        
        // Находим Grid, если не указан
        if (grid == null)
        {
            grid = FindFirstObjectByType<CellNameSpace.Grid>();
            if (grid == null)
            {
                Debug.LogWarning("BuildingManager: Grid не найден.");
            }
        }
    }
    
    /// <summary>
    /// Устанавливает постройку на указанную клетку
    /// </summary>
    /// <param name="cellPosition">Позиция клетки</param>
    /// <param name="building">Информация о постройке</param>
    /// <returns>true если постройка успешно установлена, false иначе</returns>
    public bool PlaceBuilding(Vector2Int cellPosition, BuildingInfo building)
    {
        if (building == null)
        {
            Debug.LogWarning("BuildingManager: BuildingInfo равен null");
            return false;
        }
        
        // Проверяем, что клетка принадлежит городу
        if (cityManager == null || !cityManager.IsCellOwnedByCity(cellPosition))
        {
            Debug.LogWarning($"BuildingManager: Клетка ({cellPosition.x}, {cellPosition.y}) не принадлежит городу!");
            return false;
        }
        
        // Проверяем, нет ли уже постройки на этой клетке
        if (placedBuildings.ContainsKey(cellPosition))
        {
            Debug.LogWarning($"BuildingManager: На клетке ({cellPosition.x}, {cellPosition.y}) уже есть постройка");
            return false;
        }
        
        // Получаем клетку
        CellInfo cell = GetCellAtPosition(cellPosition.x, cellPosition.y);
        if (cell == null)
        {
            Debug.LogWarning($"BuildingManager: Клетка на позиции ({cellPosition.x}, {cellPosition.y}) не найдена");
            return false;
        }
        
        // Проверяем, что это не центр города
        if (cityManager.HasCityAt(cellPosition))
        {
            Debug.LogWarning($"BuildingManager: Нельзя ставить постройку на центр города ({cellPosition.x}, {cellPosition.y})");
            return false;
        }
        
        // Устанавливаем данные о постройке
        BuildingStats buildingStats = building.buildingStats;
        if (buildingStats != null)
        {
            cell.SetBuildingStats(buildingStats);
            placedBuildings[cellPosition] = building;
            return true;
        }
        else
        {
            Debug.LogWarning($"BuildingManager: У постройки '{building.GetName()}' нет BuildingStats!");
            return false;
        }
    }
    
    /// <summary>
    /// Удаляет постройку с указанной клетки
    /// </summary>
    /// <param name="cellPosition">Позиция клетки</param>
    /// <returns>true если постройка успешно удалена, false иначе</returns>
    public bool RemoveBuilding(Vector2Int cellPosition)
    {
        if (!placedBuildings.ContainsKey(cellPosition))
        {
            Debug.LogWarning($"BuildingManager: На клетке ({cellPosition.x}, {cellPosition.y}) нет постройки");
            return false;
        }
        
        CellInfo cell = GetCellAtPosition(cellPosition.x, cellPosition.y);
        if (cell != null)
        {
            // Проверяем, не центр ли это города
            if (!cityManager.HasCityAt(cellPosition))
            {
                cell.SetBuildingStats(null);
            }
            // Если это центр города, оставляем BuildingStats города
        }
        
        placedBuildings.Remove(cellPosition);
        Debug.Log($"BuildingManager: Постройка удалена с клетки ({cellPosition.x}, {cellPosition.y})");
        return true;
    }
    
    /// <summary>
    /// Выбирает постройку для установки
    /// </summary>
    /// <param name="building">Информация о постройке</param>
    public void SelectBuilding(BuildingInfo building)
    {
        selectedBuilding = building;
        string buildingName = building != null ? building.GetName() : "null";
        Debug.Log($"BuildingManager: Выбрана постройка '{buildingName}'");
    }
    
    /// <summary>
    /// Получает выбранную постройку
    /// </summary>
    public BuildingInfo GetSelectedBuilding()
    {
        return selectedBuilding;
    }
    
    /// <summary>
    /// Проверяет, есть ли постройка на указанной клетке
    /// </summary>
    public bool HasBuildingAt(Vector2Int cellPosition)
    {
        return placedBuildings.ContainsKey(cellPosition);
    }
    
    /// <summary>
    /// Получает постройку на указанной клетке
    /// </summary>
    public BuildingInfo GetBuildingAt(Vector2Int cellPosition)
    {
        placedBuildings.TryGetValue(cellPosition, out BuildingInfo building);
        return building;
    }
    
    /// <summary>
    /// Получает словарь всех установленных построек (копия для чтения).
    /// </summary>
    public Dictionary<Vector2Int, BuildingInfo> GetAllPlacedBuildings()
    {
        return new Dictionary<Vector2Int, BuildingInfo>(placedBuildings);
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



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
            name = $"Город {cities.Count + 1}",
            expansionRadius = 1
        };
        
        // Добавляем центр города в список принадлежащих клеток
        cityInfo.ownedCells.Add(cityPosition);
        
        cities[cityPosition] = cityInfo;
        
        // Отмечаем центр города визуально
        MarkCellAsOwned(cityPosition, cityInfo);
        
        // Сразу расширяем город на радиус 1 (центр + все соседние клетки)
        if (grid != null)
        {
            int gridWidth = grid.GetGridWidth();
            int gridHeight = grid.GetGridHeight();
            
            // Получаем всех соседей центра города
            List<Vector2Int> neighbors = HexagonalGridHelper.GetNeighbors(
                gridX, gridY, gridWidth, gridHeight);
            
            // Добавляем всех соседей к городу
            foreach (Vector2Int neighborPos in neighbors)
            {
                // Проверяем, что клетка не принадлежит другому городу
                if (!IsCellOwnedByAnyCity(neighborPos, cityPosition))
                {
                    cityInfo.ownedCells.Add(neighborPos);
                    MarkCellAsOwned(neighborPos, cityInfo);
                }
            }
            
            cityInfo.expansionRadius = 1;
            Debug.Log($"CityManager: Город {cityInfo.name} создан с радиусом 1. Клеток: {cityInfo.ownedCells.Count}");
        }
        
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
    /// Расширяет город, добавляя соседние клетки
    /// </summary>
    /// <param name="cityPosition">Позиция центра города</param>
    /// <param name="targetCellPosition">Позиция клетки для добавления (опционально, если null - расширяет на один радиус)</param>
    /// <returns>true если расширение успешно, false иначе</returns>
    public bool ExpandCity(Vector2Int cityPosition, Vector2Int? targetCellPosition = null)
    {
        if (!cities.TryGetValue(cityPosition, out CityInfo city))
        {
            Debug.LogWarning($"CityManager: Город на позиции ({cityPosition.x}, {cityPosition.y}) не найден");
            return false;
        }
        
        if (grid == null)
        {
            Debug.LogError("CityManager: Grid не найден!");
            return false;
        }
        
        int gridWidth = grid.GetGridWidth();
        int gridHeight = grid.GetGridHeight();
        
        if (targetCellPosition.HasValue)
        {
            // Расширяем на конкретную клетку
            Vector2Int targetPos = targetCellPosition.Value;
            
            // Проверяем, что клетка не принадлежит другому городу
            if (IsCellOwnedByAnyCity(targetPos, cityPosition))
            {
                Debug.LogWarning($"CityManager: Клетка ({targetPos.x}, {targetPos.y}) уже принадлежит другому городу");
                return false;
            }
            
            // Проверяем, что клетка является соседом уже принадлежащих городу клеток
            if (!IsCellAdjacentToCity(city, targetPos, gridWidth, gridHeight))
            {
                Debug.LogWarning($"CityManager: Клетка ({targetPos.x}, {targetPos.y}) не является соседом города");
                return false;
            }
            
            // Добавляем клетку к городу
            city.ownedCells.Add(targetPos);
            MarkCellAsOwned(targetPos, city);
            Debug.Log($"CityManager: Клетка ({targetPos.x}, {targetPos.y}) добавлена к городу {city.name}");
            return true;
        }
        else
        {
            // Автоматическое расширение на один радиус
            List<Vector2Int> newCells = new List<Vector2Int>();
            
            // Получаем все соседние клетки для всех текущих клеток города
            foreach (Vector2Int ownedCellPos in city.ownedCells)
            {
                List<Vector2Int> neighbors = HexagonalGridHelper.GetNeighbors(
                    ownedCellPos.x, ownedCellPos.y, gridWidth, gridHeight);
                
                foreach (Vector2Int neighborPos in neighbors)
                {
                    // Проверяем, что клетка еще не принадлежит городу
                    if (!city.ownedCells.Contains(neighborPos))
                    {
                        // Проверяем, что клетка не принадлежит другому городу
                        if (!IsCellOwnedByAnyCity(neighborPos, cityPosition))
                        {
                            newCells.Add(neighborPos);
                        }
                    }
                }
            }
            
            // Добавляем все новые клетки
            foreach (Vector2Int newCellPos in newCells)
            {
                city.ownedCells.Add(newCellPos);
                MarkCellAsOwned(newCellPos, city);
            }
            
            city.expansionRadius++;
            Debug.Log($"CityManager: Город {city.name} расширен. Новых клеток: {newCells.Count}, радиус: {city.expansionRadius}");
            return newCells.Count > 0;
        }
    }
    
    /// <summary>
    /// Проверяет, принадлежит ли клетка какому-либо городу (кроме указанного)
    /// </summary>
    private bool IsCellOwnedByAnyCity(Vector2Int cellPosition, Vector2Int excludeCityPosition)
    {
        foreach (var kvp in cities)
        {
            if (kvp.Key == excludeCityPosition)
                continue;
            
            if (kvp.Value.ownedCells.Contains(cellPosition))
                return true;
        }
        return false;
    }
    
    /// <summary>
    /// Проверяет, является ли клетка соседом города
    /// </summary>
    private bool IsCellAdjacentToCity(CityInfo city, Vector2Int cellPosition, int gridWidth, int gridHeight)
    {
        List<Vector2Int> neighbors = HexagonalGridHelper.GetNeighbors(
            cellPosition.x, cellPosition.y, gridWidth, gridHeight);
        
        foreach (Vector2Int neighborPos in neighbors)
        {
            if (city.ownedCells.Contains(neighborPos))
                return true;
        }
        return false;
    }
    
    /// <summary>
    /// Отмечает клетку как принадлежащую городу (визуальная индикация)
    /// </summary>
    private void MarkCellAsOwned(Vector2Int cellPosition, CityInfo city)
    {
        CellInfo cell = GetCellAtPosition(cellPosition.x, cellPosition.y);
        if (cell != null)
        {
            cell.SetCityOwnership(city);
        }
    }
    
    /// <summary>
    /// Получает город, которому принадлежит клетка
    /// </summary>
    public CityInfo GetCityOwningCell(Vector2Int cellPosition)
    {
        foreach (var kvp in cities)
        {
            if (kvp.Value.ownedCells.Contains(cellPosition))
                return kvp.Value;
        }
        return null;
    }
    
    /// <summary>
    /// Проверяет, принадлежит ли клетка городу
    /// </summary>
    public bool IsCellOwnedByCity(Vector2Int cellPosition)
    {
        return GetCityOwningCell(cellPosition) != null;
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
    public Vector2Int position; // Позиция центра города
    public CellInfo cell; // Клетка центра города
    public string name;
    public HashSet<Vector2Int> ownedCells = new HashSet<Vector2Int>(); // Клетки, принадлежащие городу
    public int expansionRadius = 1; // Текущий радиус расширения (1 = только центр, 2 = центр + соседи, и т.д.)
}


using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CellNameSpace;

/// <summary>
/// Менеджер для управления городами (создание, удаление и т.д.)
/// </summary>
public class CityManager : MonoBehaviour
{
    [Header("Настройки городов")]
    [SerializeField] private BuildingStats cityCenterBuildingStats; // BuildingStats центра города (пользователь назначит в инспекторе)
    [Tooltip("Радиус видимости для тумана войны (в клетках) от каждой клетки города")]
    [SerializeField] private int defaultCityVisionRadius = 2; // Радиус видимости по умолчанию
    
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
        
        // Создаем информацию о городе; владелец = владелец юнита
        CityInfo cityInfo = new CityInfo
        {
            position = cityPosition,
            cell = targetCell,
            name = $"Город {cities.Count + 1}",
            expansionRadius = 1,
            visionRadius = defaultCityVisionRadius,
            ownerId = unit.GetOwnerId(),
            player = PlayerManager.Instance != null ? PlayerManager.Instance.GetPlayerByOwnerId(unit.GetOwnerId()) : null
        };
        
        // Добавляем центр города в список принадлежащих клеток
        cityInfo.ownedCells.Add(cityPosition);
        
        cities[cityPosition] = cityInfo;
        
        // Сначала отмечаем принадлежность (чтобы GetBuildingStats резолвил из City)
        MarkCellAsOwned(cityPosition, cityInfo);
        
        // Устанавливаем BuildingStats центра города
        if (cityCenterBuildingStats != null)
        {
            string bid = cityCenterBuildingStats.id;
            targetCell.SetBuildingId(bid);
        }
        else
        {
            Debug.LogWarning("CityManager: BuildingStats центра города не назначен!");
        }
        
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
        }
        
        // Удаляем юнит
        if (unitManager != null)
        {
            unitManager.RemoveUnit(unit.gameObject);
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
        
        // Автоматически выделяем только что созданный город
        if (CitySelectionManager.Instance != null)
        {
            CitySelectionManager.Instance.SelectCity(cityInfo);
        }
        
        // Обновляем видимость тумана войны после того, как все клетки города уже помечены
        // Используем корутину, чтобы дождаться следующего кадра, когда юнит будет полностью удален
        StartCoroutine(UpdateFogOfWarAfterCityCreation());
        
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
            // Обновляем видимость тумана войны после добавления клетки
            if (FogOfWarManager.Instance != null)
            {
                FogOfWarManager.Instance.UpdateVisibility();
            }
            
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
            // Обновляем видимость тумана войны после расширения города
            if (newCells.Count > 0 && FogOfWarManager.Instance != null)
            {
                FogOfWarManager.Instance.UpdateVisibility();
            }
            
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
        // Сначала используем кэш в CellInfo (оптимальный путь)
        if (grid != null)
        {
            CellInfo cell = grid.GetCellInfoAt(cellPosition.x, cellPosition.y);
            if (cell != null)
            {
                CityInfo owningCity = cell.GetOwningCity();
                if (owningCity != null)
                {
                    return owningCity;
                }
            }
        }
        
        // Fallback: если по какой-то причине кэш недоступен, используем старую логику
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
    /// Получает цвет игрока для города
    /// </summary>
    /// <param name="city">Город</param>
    /// <returns>Цвет игрока или белый по умолчанию</returns>
    public static Color GetCityColor(CityInfo city)
    {
        if (city != null && city.player != null)
        {
            return city.player.playerColor;
        }
        return Color.white; // Цвет по умолчанию, если игрок не назначен
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
    
    /// <summary>
    /// Обновляет туман войны в следующем кадре, после того как юнит полностью удален
    /// </summary>
    private IEnumerator UpdateFogOfWarAfterCityCreation()
    {
        // Ждем один кадр, чтобы Unity успел удалить юнит
        yield return null;
        
        // Теперь обновляем видимость тумана войны
        if (FogOfWarManager.Instance != null)
        {
            FogOfWarManager.Instance.UpdateVisibility();
        }
    }
}

/// <summary>
/// Результат опроса города: агрегированные ресурсы и бонусы уровня Player для передачи игроку.
/// </summary>
public class CityResourceIncomeResult
{
    public List<ResourceIncomeEntry> resources = new List<ResourceIncomeEntry>();
    public List<ResourceBonus> playerBonusesToPass = new List<ResourceBonus>();
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
    public int visionRadius = 2; // Радиус видимости для тумана войны (в клетках от каждой клетки города)
    public PlayerInfo player; // Игрок, которому принадлежит город (для UI, цвета)
    public int ownerId = 0; // Владелец города (игрок, варвары, независимые); при создании из settler = unit.ownerId
    public List<ResourceBonus> resourceBonuses = new List<ResourceBonus>(); // Бонусы уровня City (и Cell при опросе клеток)

    // Переопределения статов (город может переопределять постройки/фичи/типы клеток)
    private Dictionary<string, BuildingStats> buildingOverrides = new Dictionary<string, BuildingStats>();
    private Dictionary<string, FeatureStats> featureOverrides = new Dictionary<string, FeatureStats>();
    private Dictionary<string, CellTypeStats> cellTypeOverrides = new Dictionary<string, CellTypeStats>();

    public BuildingStats GetBuilding(string buildingId)
    {
        if (buildingOverrides != null && buildingOverrides.TryGetValue(buildingId, out var o)) return o;
        return player?.GetBuilding(buildingId);
    }
    public FeatureStats GetFeature(string featureId)
    {
        if (featureOverrides != null && featureOverrides.TryGetValue(featureId, out var o)) return o;
        return player?.GetFeature(featureId);
    }
    public CellTypeStats GetCellTypeStats(string cellTypeId)
    {
        if (cellTypeOverrides != null && cellTypeOverrides.TryGetValue(cellTypeId, out var o)) return o;
        return player?.GetCellTypeStats(cellTypeId);
    }

    /// <summary>
    /// Опрашивает клетки города, применяет бонусы по уровням, возвращает агрегированные ресурсы и player-бонусы.
    /// </summary>
    public CityResourceIncomeResult GetResourceIncome(CellNameSpace.Grid grid)
    {
        var result = new CityResourceIncomeResult();
        if (grid == null) return result;

        var aggregated = new Dictionary<string, ResourceIncomeEntry>(); // resourceId -> entry (we'll sum amount)
        var cityBonusesFromCells = new List<ResourceBonus>();

        foreach (Vector2Int pos in ownedCells)
        {
            CellInfo cell = grid.GetCellInfoAt(pos.x, pos.y);
            if (cell == null) continue;

            List<ResourceIncomeEntry> cellResources = cell.GetResourceIncomeList();
            List<ResourceBonus> cellBonuses = cell.GetBonusList();

            // Cell-level: apply to each resource from cell bonuses + player + city (Cell-level only)
            var cellLevelBonuses = new List<ResourceBonus>();
            foreach (var b in cellBonuses) if (b.applicationLevel == BonusApplicationLevel.Cell) cellLevelBonuses.Add(b);
            if (player?.Bonuses != null) foreach (var b in player.Bonuses) if (b.applicationLevel == BonusApplicationLevel.Cell) cellLevelBonuses.Add(b);
            foreach (var b in resourceBonuses) if (b.applicationLevel == BonusApplicationLevel.Cell) cellLevelBonuses.Add(b);

            foreach (var entry in cellResources)
            {
                float amount = ApplyBonusesToAmount(entry.amount, entry.resourceId, entry.resourceStatType, cellLevelBonuses);
                if (!aggregated.TryGetValue(entry.resourceId, out ResourceIncomeEntry existing))
                    aggregated[entry.resourceId] = new ResourceIncomeEntry(entry.resourceId, amount, entry.displayName, entry.resourceStatType, entry.resourceStats);
                else
                    aggregated[entry.resourceId] = new ResourceIncomeEntry(entry.resourceId, existing.amount + amount, existing.displayName, existing.resourceStatType, existing.resourceStats);
            }

            foreach (var b in cellBonuses)
            {
                if (b.applicationLevel == BonusApplicationLevel.City) cityBonusesFromCells.Add(b);
                if (b.applicationLevel == BonusApplicationLevel.Player) result.playerBonusesToPass.Add(b);
            }
        }

        // City-level: apply to aggregated (cityBonusesFromCells + city's own resourceBonuses with City level)
        var cityLevelBonuses = new List<ResourceBonus>(cityBonusesFromCells);
        foreach (var b in resourceBonuses) if (b.applicationLevel == BonusApplicationLevel.City) cityLevelBonuses.Add(b);

        result.resources = new List<ResourceIncomeEntry>();
        foreach (var kvp in aggregated)
        {
            float amount = ApplyBonusesToAmount(kvp.Value.amount, kvp.Key, kvp.Value.resourceStatType, cityLevelBonuses);
            result.resources.Add(new ResourceIncomeEntry(kvp.Value.resourceId, amount, kvp.Value.displayName, kvp.Value.resourceStatType, kvp.Value.resourceStats));
        }

        return result;
    }

    private static float ApplyBonusesToAmount(float amount, string resourceId, ResourceStatType resourceStatType, List<ResourceBonus> bonuses)
    {
        if (bonuses == null || bonuses.Count == 0) return amount;
        float sumFlat = 0f;
        float sumPercent = 0f;
        foreach (var b in bonuses)
        {
            bool match = b.targetResource != null ? (b.targetResource.id == resourceId) : b.targetType == resourceStatType;
            if (!match) continue;
            sumFlat += b.flatValue;
            sumPercent += b.EffectivePercent;
        }
        return (amount + sumFlat) * (1f + sumPercent);
    }
}


using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CellNameSpace;

/// <summary>
/// Менеджер для управления юнитами (спавн, удаление и т.д.)
/// </summary>
public class UnitManager : MonoBehaviour
{
    [Header("Настройки спавна")]
    [SerializeField] private GameObject unitPrefab; // Префаб юнита для спавна
    [SerializeField] private bool spawnDefaultUnitOnStart = true; // Спавнить ли дефолтного юнита при старте
    [SerializeField] private Vector2Int defaultSpawnPosition = new Vector2Int(0, 0); // Позиция спавна по умолчанию (gridX, gridY)
    [SerializeField] private bool findRandomSpawnPosition = false; // Если true, ищет случайную подходящую клетку
    
    [Header("Требования к клетке для спавна")]
    [SerializeField] private List<CellType> allowedCellTypes = new List<CellType> { CellType.field }; // Типы клеток, на которых можно спавнить юнита
    
    [Header("Ссылки (опционально)")]
    [SerializeField] private CellNameSpace.Grid grid; // Ссылка на Grid (найдет автоматически, если не указана)
    
    [Header("Настройки камеры")]
    [SerializeField] private bool moveCameraToUnitOnSpawn = true; // Перемещать ли камеру на юнита при спавне
    [SerializeField] private bool instantCameraMove = false; // Мгновенное перемещение камеры (иначе плавное)
    
    private List<GameObject> spawnedUnits = new List<GameObject>(); // Список заспавненных юнитов
    private CellNameSpace.CameraController cameraController; // Кэш CameraController
    
    void Start()
    {
        // Находим Grid, если не указан
        if (grid == null)
        {
            grid = FindFirstObjectByType<CellNameSpace.Grid>();
            if (grid == null)
            {
                Debug.LogWarning("UnitManager: Grid не найден. Спавн юнитов может не работать.");
            }
        }
        
        // Находим CameraController для перемещения камеры
        if (moveCameraToUnitOnSpawn)
        {
            cameraController = FindFirstObjectByType<CellNameSpace.CameraController>();
            if (cameraController == null)
            {
                Debug.LogWarning("UnitManager: CameraController не найден. Камера не будет перемещаться на юнита.");
            }
        }
        
        // Спавним дефолтного юнита при старте, если включено
        if (spawnDefaultUnitOnStart && unitPrefab != null)
        {
            // Ждем один кадр, чтобы Grid успел сгенерироваться
            StartCoroutine(SpawnDefaultUnitDelayed());
        }
    }
    
    /// <summary>
    /// Корутина для задержанного спавна дефолтного юнита (чтобы Grid успел сгенерироваться)
    /// </summary>
    private IEnumerator SpawnDefaultUnitDelayed()
    {
        // Ждем, пока Grid будет найден
        while (grid == null)
        {
            grid = FindFirstObjectByType<CellNameSpace.Grid>();
            if (grid != null)
                break;
            yield return null;
        }

        if (grid == null)
        {
            Debug.LogWarning("UnitManager: Не удалось найти Grid для спавна юнита");
            yield break;
        }

        // Ждем, пока Grid завершит генерацию типов клеток
        while (!grid.IsGenerationComplete)
        {
            yield return null;
        }

        SpawnUnit();
    }
    
    /// <summary>
    /// Спавнит юнита на указанной позиции или находит подходящую клетку
    /// </summary>
    public GameObject SpawnUnit(Vector2Int? gridPosition = null)
    {
        if (unitPrefab == null)
        {
            Debug.LogError("UnitManager: Префаб юнита не назначен!");
            return null;
        }
        
        if (grid == null)
        {
            Debug.LogError("UnitManager: Grid не найден!");
            return null;
        }
        
        Vector2Int spawnPos;
        
        // Определяем позицию спавна
        if (gridPosition.HasValue)
        {
            spawnPos = gridPosition.Value;
        }
        else if (findRandomSpawnPosition)
        {
            spawnPos = FindRandomSuitableCell();
            if (spawnPos.x < 0 || spawnPos.y < 0)
            {
                Debug.LogWarning("UnitManager: Не удалось найти подходящую клетку для спавна");
                return null;
            }
        }
        else
        {
            spawnPos = defaultSpawnPosition;
        }
        
        // Получаем клетку по координатам
        CellInfo targetCell = GetCellAtPosition(spawnPos.x, spawnPos.y);
        if (targetCell == null)
        {
            Debug.LogWarning($"UnitManager: Клетка на позиции ({spawnPos.x}, {spawnPos.y}) не найдена");
            return null;
        }
        
        // Проверяем, подходит ли тип клетки
        if (!allowedCellTypes.Contains(targetCell.GetCellType()))
        {
            Debug.LogWarning($"UnitManager: Клетка на позиции ({spawnPos.x}, {spawnPos.y}) имеет неподходящий тип {targetCell.GetCellType()}");
            return null;
        }
        
        // Спавним юнита на позиции клетки
        Vector3 spawnPosition = targetCell.transform.position;
        GameObject unit = Instantiate(unitPrefab, spawnPosition, Quaternion.identity);
        
        // Инициализируем UnitInfo
        UnitInfo unitInfo = unit.GetComponent<UnitInfo>();
        if (unitInfo != null)
        {
            unitInfo.SetGridPosition(spawnPos.x, spawnPos.y);
        }
        else
        {
            Debug.LogWarning($"UnitManager: UnitInfo не найден на префабе юнита {unitPrefab.name}");
        }
        
        spawnedUnits.Add(unit);
        Debug.Log($"UnitManager: Юнит заспавнен на позиции ({spawnPos.x}, {spawnPos.y})");
        
        // Перемещаем камеру на юнита, если включено
        if (moveCameraToUnitOnSpawn && cameraController != null)
        {
            cameraController.MoveToTarget(unit, instantCameraMove);
            Debug.Log($"UnitManager: Камера перемещена на юнита");
        }
        
        return unit;
    }
    
    /// <summary>
    /// Находит случайную подходящую клетку для спавна
    /// </summary>
    private Vector2Int FindRandomSuitableCell()
    {
        // Получаем все клетки в сцене
        CellInfo[] allCells = FindObjectsByType<CellInfo>(FindObjectsSortMode.None);
        
        if (allCells.Length == 0)
        {
            return new Vector2Int(-1, -1);
        }
        
        // Фильтруем подходящие клетки
        List<CellInfo> suitableCells = new List<CellInfo>();
        foreach (CellInfo cell in allCells)
        {
            if (allowedCellTypes.Contains(cell.GetCellType()))
            {
                suitableCells.Add(cell);
            }
        }
        
        if (suitableCells.Count == 0)
        {
            Debug.LogWarning("UnitManager: Не найдено подходящих клеток для спавна");
            return new Vector2Int(-1, -1);
        }
        
        // Выбираем случайную клетку
        CellInfo randomCell = suitableCells[Random.Range(0, suitableCells.Count)];
        return new Vector2Int(randomCell.GetGridX(), randomCell.GetGridY());
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
    /// Удаляет юнита
    /// </summary>
    public void RemoveUnit(GameObject unit)
    {
        if (spawnedUnits.Contains(unit))
        {
            spawnedUnits.Remove(unit);
        }
        Destroy(unit);
    }
    
    /// <summary>
    /// Удаляет всех юнитов
    /// </summary>
    public void RemoveAllUnits()
    {
        foreach (GameObject unit in spawnedUnits)
        {
            if (unit != null)
            {
                Destroy(unit);
            }
        }
        spawnedUnits.Clear();
    }
    
    /// <summary>
    /// Получить список всех заспавненных юнитов
    /// </summary>
    public List<GameObject> GetSpawnedUnits()
    {
        return new List<GameObject>(spawnedUnits);
    }
}


using UnityEngine;
using System.Collections.Generic;
using CellNameSpace;

/// <summary>
/// Контроллер для управления перемещением юнита
/// </summary>
public class UnitController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f; // Скорость перемещения
    
    private UnitInfo unitInfo;
    private bool isMoving = false;
    private List<CellInfo> currentPath = new List<CellInfo>(); // Текущий маршрут
    private int currentPathIndex = 0; // Индекс текущей клетки в маршруте
    private CellNameSpace.Grid grid; // Кэш Grid для поиска пути
    
    void Start()
    {
        unitInfo = GetComponent<UnitInfo>();
        if (unitInfo == null)
        {
            Debug.LogError($"UnitController: UnitInfo не найден на {gameObject.name}");
        }
        
        // Находим Grid для поиска пути
        grid = FindFirstObjectByType<CellNameSpace.Grid>();
    }

    void Update()
    {
        if (isMoving)
        {
            MoveAlongPath();
        }
    }
    
    /// <summary>
    /// Переместить юнита на указанную клетку с построением маршрута
    /// </summary>
    public void MoveToCell(CellInfo targetCell)
    {
        if (targetCell == null)
        {
            Debug.LogWarning("UnitController: Попытка переместиться на null клетку");
            return;
        }
        
        if (unitInfo == null)
        {
            Debug.LogError("UnitController: UnitInfo не найден");
            return;
        }
        
        if (grid == null)
        {
            grid = FindFirstObjectByType<CellNameSpace.Grid>();
            if (grid == null)
            {
                Debug.LogError("UnitController: Grid не найден, невозможно построить маршрут");
                return;
            }
        }
        
        // Получаем текущую клетку юнита
        CellInfo startCell = GetCurrentCell();
        if (startCell == null)
        {
            Debug.LogWarning("UnitController: Не удалось определить текущую клетку юнита");
            // Пытаемся переместиться напрямую
            MoveToPositionDirect(targetCell.transform.position);
            return;
        }
        
        // Если целевая клетка совпадает с текущей
        if (startCell == targetCell)
        {
            Debug.Log("UnitController: Юнит уже на целевой клетке");
            return;
        }
        
        // Строим маршрут
        List<CellInfo> path = Pathfinder.FindPath(startCell, targetCell, grid);
        
        if (path == null || path.Count == 0)
        {
            Debug.LogWarning($"UnitController: Не удалось построить маршрут к клетке ({targetCell.GetGridX()}, {targetCell.GetGridY()})");
            return;
        }
        
        // Убираем первую клетку из пути (это текущая клетка)
        if (path.Count > 1 && path[0] == startCell)
        {
            path.RemoveAt(0);
        }
        
        // Устанавливаем маршрут
        currentPath = path;
        currentPathIndex = 0;
        isMoving = true;
        
        Debug.Log($"UnitController: Построен маршрут из {path.Count} клеток к ({targetCell.GetGridX()}, {targetCell.GetGridY()})");
    }
    
    /// <summary>
    /// Переместить юнита на указанную позицию в мировых координатах (без построения маршрута)
    /// </summary>
    public void MoveToPosition(Vector3 position)
    {
        MoveToPositionDirect(position);
    }
    
    /// <summary>
    /// Перемещение напрямую на позицию (без построения маршрута)
    /// </summary>
    private void MoveToPositionDirect(Vector3 position)
    {
        currentPath.Clear();
        currentPathIndex = 0;
        
        // Создаем временный маршрут с одной точкой
        // Но лучше использовать старый метод
        StopMovement();
        // Здесь можно добавить прямую логику движения, если нужно
    }
    
    /// <summary>
    /// Движение по маршруту
    /// </summary>
    private void MoveAlongPath()
    {
        if (currentPath == null || currentPath.Count == 0 || currentPathIndex >= currentPath.Count)
        {
            // Маршрут завершен
            OnPathComplete();
            return;
        }
        
        // Получаем текущую целевую клетку
        CellInfo targetCell = currentPath[currentPathIndex];
        if (targetCell == null)
        {
            // Клетка уничтожена, пропускаем
            currentPathIndex++;
            return;
        }
        
        Vector3 targetPosition = targetCell.transform.position;
        float distance = Vector3.Distance(transform.position, targetPosition);
        
        if (distance > 0.01f)
        {
            // Плавное перемещение к текущей клетке маршрута
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        }
        else
        {
            // Достигли текущей клетки
            transform.position = targetPosition;
            
            // Обновляем позицию на сетке
            if (unitInfo != null)
            {
                unitInfo.SetGridPosition(targetCell.GetGridX(), targetCell.GetGridY());
            }
            
            // Переходим к следующей клетке
            currentPathIndex++;
            
            Debug.Log($"UnitController: Юнит достиг клетки ({targetCell.GetGridX()}, {targetCell.GetGridY()}), осталось {currentPath.Count - currentPathIndex} клеток");
        }
    }
    
    /// <summary>
    /// Вызывается при завершении маршрута
    /// </summary>
    private void OnPathComplete()
    {
        isMoving = false;
        currentPath.Clear();
        currentPathIndex = 0;
        Debug.Log($"UnitController: Юнит {gameObject.name} завершил маршрут");
    }
    
    /// <summary>
    /// Получает текущую клетку юнита
    /// </summary>
    private CellInfo GetCurrentCell()
    {
        if (unitInfo == null || grid == null)
            return null;
        
        if (!unitInfo.IsPositionInitialized())
        {
            // Пытаемся найти ближайшую клетку по позиции
            return FindNearestCell();
        }
        
        // Получаем клетку по координатам сетки
        return grid.GetCellInfoAt(unitInfo.GetGridX(), unitInfo.GetGridY());
    }
    
    /// <summary>
    /// Находит ближайшую клетку к текущей позиции юнита
    /// </summary>
    private CellInfo FindNearestCell()
    {
        CellInfo[] allCells = FindObjectsByType<CellInfo>(FindObjectsSortMode.None);
        if (allCells.Length == 0)
            return null;
        
        CellInfo nearestCell = null;
        float nearestDistance = float.MaxValue;
        
        foreach (CellInfo cell in allCells)
        {
            float distance = Vector3.Distance(transform.position, cell.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestCell = cell;
            }
        }
        
        return nearestCell;
    }
    
    /// <summary>
    /// Проверяет, перемещается ли юнит
    /// </summary>
    public bool IsMoving()
    {
        return isMoving;
    }
    
    /// <summary>
    /// Останавливает движение юнита
    /// </summary>
    public void StopMovement()
    {
        isMoving = false;
        currentPath.Clear();
        currentPathIndex = 0;
    }
    
    /// <summary>
    /// Получить текущий маршрут
    /// </summary>
    public List<CellInfo> GetCurrentPath()
    {
        return new List<CellInfo>(currentPath);
    }
}

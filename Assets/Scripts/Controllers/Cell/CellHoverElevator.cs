using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using CellNameSpace;

/// <summary>
/// Приподнимает клетки вокруг курсора мыши на заданную высоту
/// </summary>
public class CellHoverElevator : MonoBehaviour
{
    [Header("Настройки эффекта")]
    [SerializeField] private float hoverRadius = 2f; // Радиус круга вокруг курсора в мировых единицах
    [SerializeField] private float elevationHeight = 0.02f; // Высота приподнимания в единицах Unity (2 пикселя = 0.02 при PPU = 100)
    [SerializeField] private float elevationSpeed = 10f; // Скорость анимации приподнимания
    
    [Header("Ссылки (опционально)")]
    [SerializeField] private CellNameSpace.Grid grid; // Сетка (найдётся автоматически, если не указана)
    [SerializeField] private Camera mainCamera; // Камера (найдётся автоматически, если не указана)
    
    // Текущие приподнятые клетки (ссылки на клетки, которые сейчас приподняты)
    private HashSet<CellInfo> elevatedCells = new HashSet<CellInfo>();
    private Dictionary<CellInfo, Vector3> targetPositions = new Dictionary<CellInfo, Vector3>();
    
    // Текущая клетка под курсором
    private CellInfo currentHoveredCell = null;
    
    void Start()
    {
        // Находим Grid, если не назначен
        if (grid == null)
        {
            grid = FindFirstObjectByType<CellNameSpace.Grid>();
            if (grid == null)
            {
                Debug.LogWarning("CellHoverElevator: Grid не найден");
            }
        }
        
        // Находим камеру, если не назначена
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindFirstObjectByType<Camera>();
            }
            if (mainCamera == null)
            {
                Debug.LogWarning("CellHoverElevator: Камера не найдена");
            }
        }
    }
    
    void Update()
    {
        // Отключаем hover эффект во время стадии воспроизведения приказов
        if (TurnManager.Instance != null && TurnManager.Instance.GetCurrentState() == TurnState.Resolving)
        {
            // Сбрасываем эффект и возвращаем клетки в исходное положение
            if (currentHoveredCell != null)
            {
                currentHoveredCell = null;
                ResetElevation();
            }
            // Продолжаем возвращать клетки обратно
            AnimateReset();
            return;
        }
        
        // Проверяем, не находится ли курсор над UI элементом
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            // Если курсор над UI, сбрасываем эффект
            if (currentHoveredCell != null)
            {
                currentHoveredCell = null;
                ResetElevation();
            }
            // Продолжаем возвращать клетки обратно
            AnimateReset();
            return;
        }
        
        // Определяем клетку под курсором (только для информации)
        CellInfo hoveredCell = GetCellUnderCursor();
        currentHoveredCell = hoveredCell;
        
        // ВСЕГДА обновляем эффект на основе позиции курсора, даже если курсор между клетками
        // Это предотвращает дрожание клеток, когда курсор останавливается в пропусках
        UpdateElevation();
        
        // Плавно анимируем позиции клеток
        AnimateElevation();
        
        // ВСЕГДА обрабатываем опускание клеток, которые больше не в радиусе
        // Это нужно, чтобы клетки опускались сразу при движении курсора
        AnimateReset();
    }
    
    /// <summary>
    /// Определяет клетку под курсором мыши
    /// </summary>
    private CellInfo GetCellUnderCursor()
    {
        if (mainCamera == null || grid == null)
            return null;
        
        // Сначала пробуем 2D raycast (работает в плоскости XY, игнорируя Z)
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        // Для 2D raycast Z координата не важна, так как Physics2D работает в плоскости XY
        Vector2 mousePos2D = new Vector2(mouseWorldPos.x, mouseWorldPos.y);
        
        RaycastHit2D hit2D = Physics2D.Raycast(mousePos2D, Vector2.zero, 0f);
        if (hit2D.collider != null)
        {
            CellInfo cellInfo = hit2D.collider.GetComponent<CellInfo>();
            if (cellInfo != null)
            {
                return cellInfo;
            }
        }
        
        // Если 2D не сработал, пробуем 3D raycast (для MeshCollider)
        // 3D raycast учитывает Z координату, поэтому он должен работать с разными Z
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit3D;
        if (Physics.Raycast(ray, out hit3D, Mathf.Infinity))
        {
            CellInfo cellInfo = hit3D.collider.GetComponent<CellInfo>();
            if (cellInfo != null)
            {
                return cellInfo;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Обновляет эффект приподнимания для клеток вокруг курсора
    /// </summary>
    private void UpdateElevation()
    {
        // Сначала возвращаем все предыдущие клетки обратно
        ClearElevation();
        
        // Не проверяем currentHoveredCell - обновляем на основе позиции курсора
        if (grid == null || mainCamera == null)
            return;
        
        // Получаем позицию курсора в мировых координатах
        Vector3 cursorWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        // Игнорируем Z координату курсора для расчета расстояния (используем только X и Y)
        Vector2 cursorPos2D = new Vector2(cursorWorldPos.x, cursorWorldPos.y);
        
        // Получаем все клетки сетки
        int gridWidth = grid.GetGridWidth();
        int gridHeight = grid.GetGridHeight();
        
        // Проходим по всем клеткам и проверяем, попадают ли они в круг
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                CellInfo cell = grid.GetCellInfoAt(x, y);
                if (cell == null)
                    continue;
                
                // ВСЕГДА используем изначальную позицию из CellInfo, даже если клетка уже приподнята
                Vector3 cellOriginalPos = cell.GetOriginalPosition();
                // Используем только X и Y координаты для расчета расстояния (игнорируем Z)
                Vector2 cellPos2D = new Vector2(cellOriginalPos.x, cellOriginalPos.y);
                
                // Вычисляем расстояние от курсора до изначальной позиции клетки (только по X и Y, игнорируя Z)
                float distance = Vector2.Distance(cursorPos2D, cellPos2D);
                
                // Проверяем, попадает ли клетка в круг
                if (distance <= hoverRadius)
                {
                    // Вычисляем коэффициент от 1.0 (центр) до 0.0 (край)
                    // distance = 0 (центр) -> factor = 1.0
                    // distance = hoverRadius (край) -> factor = 0.0
                    float factor = 1f - (distance / hoverRadius);
                    
                    // Вычисляем высоту: максимальный подъем для центра, плавно уменьшается до нуля на краю
                    float cellElevationHeight = elevationHeight * factor;
                    
                    // Добавляем клетку в список приподнятых
                    elevatedCells.Add(cell);
                    
                    // Вычисляем целевую позицию (приподнятую с учетом расстояния)
                    // ВСЕГДА от изначальной позиции, а не от текущей
                    Vector3 targetPos = new Vector3(cellOriginalPos.x, cellOriginalPos.y + cellElevationHeight, cellOriginalPos.z);
                    targetPositions[cell] = targetPos;
                }
            }
        }
    }
    
    /// <summary>
    /// Плавно анимирует приподнимание клеток
    /// </summary>
    private void AnimateElevation()
    {
        List<CellInfo> cellsToRemove = new List<CellInfo>();
        
        foreach (var kvp in targetPositions)
        {
            CellInfo cell = kvp.Key;
            Vector3 targetPos = kvp.Value;
            
            if (cell == null)
            {
                cellsToRemove.Add(cell);
                continue;
            }
            
            // Плавно перемещаем к целевой позиции
            cell.transform.position = Vector3.MoveTowards(
                cell.transform.position, 
                targetPos, 
                elevationSpeed * Time.deltaTime);
        }
        
        // Удаляем уничтоженные клетки
        foreach (CellInfo cell in cellsToRemove)
        {
            elevatedCells.Remove(cell);
            targetPositions.Remove(cell);
        }
    }
    
    /// <summary>
    /// Плавно возвращает клетки в исходное положение
    /// Обрабатывает только клетки, которые больше не в targetPositions (не в радиусе)
    /// </summary>
    private void AnimateReset()
    {
        List<CellInfo> cellsToRemove = new List<CellInfo>();
        
        foreach (CellInfo cell in elevatedCells)
        {
            if (cell == null)
            {
                cellsToRemove.Add(cell);
                continue;
            }
            
            // Если клетка все еще в targetPositions, значит она должна быть приподнята - пропускаем
            if (targetPositions.ContainsKey(cell))
                continue;
            
            // Получаем изначальную позицию из самой клетки
            Vector3 originalPos = cell.GetOriginalPosition();
            
            // Плавно возвращаем в исходное положение
            cell.transform.position = Vector3.MoveTowards(
                cell.transform.position, 
                originalPos, 
                elevationSpeed * Time.deltaTime);
            
            // Если достигли исходной позиции, удаляем из списка
            if (Vector3.Distance(cell.transform.position, originalPos) < 0.001f)
            {
                cell.transform.position = originalPos; // Точная позиция
                cellsToRemove.Add(cell);
            }
        }
        
        // Удаляем клетки из словарей
        foreach (CellInfo cell in cellsToRemove)
        {
            elevatedCells.Remove(cell);
            targetPositions.Remove(cell);
        }
    }
    
    /// <summary>
    /// Очищает все приподнятые клетки (используется при смене клетки под курсором)
    /// </summary>
    private void ClearElevation()
    {
        // Удаляем все клетки из targetPositions, чтобы они начали возвращаться
        targetPositions.Clear();
        
        // Теперь все клетки в elevatedCells будут возвращаться обратно через AnimateReset
    }
    
    /// <summary>
    /// Сбрасывает эффект приподнимания, возвращая клетки в исходное положение немедленно
    /// </summary>
    private void ResetElevation()
    {
        // Возвращаем все клетки в исходное положение немедленно
        foreach (CellInfo cell in elevatedCells)
        {
            if (cell != null)
            {
                // Получаем изначальную позицию из самой клетки
                Vector3 originalPos = cell.GetOriginalPosition();
                cell.transform.position = originalPos;
            }
        }
        
        // Очищаем все словари
        elevatedCells.Clear();
        targetPositions.Clear();
    }
    
    void OnDisable()
    {
        // При отключении скрипта сбрасываем все эффекты
        ResetElevation();
        currentHoveredCell = null;
    }
}


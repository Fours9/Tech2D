using UnityEngine;
using System.Collections.Generic;
using CellNameSpace;
using FogOfWar;

/// <summary>
/// Менеджер для управления туманом войны
/// Отслеживает видимость клеток на основе позиций юнитов
/// </summary>
public class FogOfWarManager : MonoBehaviour
{
    public static FogOfWarManager Instance { get; private set; }
    
    [Header("Настройки видимости")]
    [Tooltip("Включен ли туман войны. Если отключен, все клетки всегда будут видимыми.")]
    [SerializeField] private bool fogOfWarEnabled = true;
    
    [Header("Материалы тумана")]
    [Tooltip("Материал для неразведанных клеток (темнее, почти глухой)")]
    [SerializeField] private Material fogUnseenMaterial;
    [Tooltip("Материал для разведанных, но невидимых клеток (светлее, с более заметным glow)")]
    [SerializeField] private Material fogExploredMaterial;
    
    [Header("Ссылки")]
    [SerializeField] private CellNameSpace.Grid grid; // Сетка (найдется автоматически, если не указана)
    [SerializeField] private CityManager cityManager; // Менеджер городов (найдется автоматически, если не указан)
    
    private HashSet<CellInfo> visibleCells = new HashSet<CellInfo>(); // Клетки, видимые в текущий момент
    private HashSet<CellInfo> exploredCells = new HashSet<CellInfo>(); // Клетки, которые были исследованы
    
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
    }
    
    private void Start()
    {
        if (grid == null)
        {
            grid = FindFirstObjectByType<CellNameSpace.Grid>();
        }
        
        if (cityManager == null)
        {
            cityManager = FindFirstObjectByType<CityManager>();
        }
        
        // Инициализируем _HexRadius в материалах на основе mesh
        InitializeHexRadius();
        
        // Инициализируем туман войны или делаем все клетки видимыми
        if (fogOfWarEnabled)
        {
            InitializeFogOfWar();
        }
        else
        {
            // Если туман войны отключен при старте, делаем все клетки видимыми
            MakeAllCellsVisible();
        }
    }
    
    /// <summary>
    /// Инициализирует _HexRadius в материалах тумана на основе mesh.bounds.extents.y
    /// </summary>
    private void InitializeHexRadius()
    {
        if (grid == null)
            return;
        
        // Находим первую клетку для получения mesh из fogOfWarRenderer
        int gridWidth = grid.GetGridWidth();
        int gridHeight = grid.GetGridHeight();
        
        MeshFilter meshFilter = null;
        for (int x = 0; x < gridWidth && meshFilter == null; x++)
        {
            for (int y = 0; y < gridHeight && meshFilter == null; y++)
            {
                CellInfo cell = grid.GetCellInfoAt(x, y);
                if (cell != null)
                {
                    // Ищем MeshFilter в дочерних объектах клетки (где находится fogOfWarRenderer)
                    meshFilter = cell.GetComponentInChildren<MeshFilter>();
                }
            }
        }
        
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogWarning("[FogOfWarManager] MeshFilter или sharedMesh не найдены для инициализации _HexRadius");
            return;
        }
        
        // Получаем hexRadius из bounds.extents.y
        // ВАЖНО: берем напрямую из mesh.bounds.extents.y, БЕЗ умножения на scale
        // mesh.bounds - это локальные границы mesh (до применения scale)
        // В шейдере v.vertex.xy - это локальные координаты вершины, которые уже учитывают scale transform
        Bounds meshBounds = meshFilter.sharedMesh.bounds;
        float hexRadius = meshBounds.extents.y;
        
        // Логируем для проверки
        Debug.Log($"[FogOfWarManager] mesh.bounds.extents.x = {meshBounds.extents.x}, extents.y = {meshBounds.extents.y}");
        Debug.Log($"[FogOfWarManager] hexRadius = {hexRadius}");
        Debug.Log($"[FogOfWarManager] mesh.bounds.size: {meshBounds.size}");
        Debug.Log($"[FogOfWarManager] meshFilter.transform.localScale: {meshFilter.transform.localScale}");
        
        // Устанавливаем _HexRadius в материалы тумана
        if (fogUnseenMaterial != null)
        {
            fogUnseenMaterial.SetFloat("_HexRadius", hexRadius);
            Debug.Log($"[FogOfWarManager] Установлен _HexRadius = {hexRadius} в fogUnseenMaterial");
        }
        
        if (fogExploredMaterial != null)
        {
            fogExploredMaterial.SetFloat("_HexRadius", hexRadius);
            Debug.Log($"[FogOfWarManager] Установлен _HexRadius = {hexRadius} в fogExploredMaterial");
        }
    }
    
    /// <summary>
    /// Инициализирует туман войны для всех клеток
    /// </summary>
    private void InitializeFogOfWar()
    {
        if (grid == null)
            return;
        
        int gridWidth = grid.GetGridWidth();
        int gridHeight = grid.GetGridHeight();
        
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                CellInfo cell = grid.GetCellInfoAt(x, y);
                if (cell != null)
                {
                    cell.SetFogOfWarState(FogOfWarState.Hidden);
                }
            }
        }
        
        // Обновляем видимость на основе текущих позиций юнитов
        UpdateVisibility();
    }
    
    /// <summary>
    /// Обновляет видимость всех клеток на основе позиций юнитов и городов
    /// </summary>
    public void UpdateVisibility()
    {
        if (!fogOfWarEnabled || grid == null)
            return;
        
        // Сначала собираем все видимые клетки от всех юнитов
        // Используем HashSet координат для надежной проверки
        HashSet<Vector2Int> visibleCellCoords = new HashSet<Vector2Int>();
        List<CellInfo> newVisibleCellsList = new List<CellInfo>();
        
        // Находим все юниты на карте
        UnitInfo[] allUnits = FindObjectsByType<UnitInfo>(FindObjectsSortMode.None);
        
        // Для каждого юнита определяем видимые клетки
        foreach (UnitInfo unit in allUnits)
        {
            if (unit == null || !unit.IsPositionInitialized())
                continue;
            
            // Используем только координаты сетки (X, Y), игнорируя Z (2D проект)
            int unitX = unit.GetGridX();
            int unitY = unit.GetGridY();
            
            // Получаем радиус видимости из статов юнита
            UnitStats unitStats = unit.GetUnitStats();
            if (unitStats == null || unitStats.visionRadius <= 0)
            {
                // Если у юнита нет радиуса видимости в статах, пропускаем его
                continue;
            }
            
            int unitVisionRadius = unitStats.visionRadius;
            
            // Получаем все клетки в радиусе видимости (используем только координаты сетки, игнорируя Z)
            List<CellInfo> cellsInRange = GetCellsInVisionRange(unitX, unitY, unitVisionRadius);
            
            foreach (CellInfo cell in cellsInRange)
            {
                if (cell != null)
                {
                    Vector2Int cellCoord = new Vector2Int(cell.GetGridX(), cell.GetGridY());
                    if (!visibleCellCoords.Contains(cellCoord))
                    {
                        visibleCellCoords.Add(cellCoord);
                        newVisibleCellsList.Add(cell);
                    }
                }
            }
        }
        
        // Теперь обрабатываем города - для каждой клетки города считаем видимость на visionRadius клеток
        if (cityManager != null)
        {
            Dictionary<Vector2Int, CityInfo> allCities = cityManager.GetAllCities();
            foreach (var kvp in allCities)
            {
                CityInfo city = kvp.Value;
                if (city == null || city.visionRadius <= 0)
                    continue;
                
                // Для каждой клетки, принадлежащей городу, считаем видимость
                foreach (Vector2Int ownedCellPos in city.ownedCells)
                {
                    CellInfo ownedCell = grid.GetCellInfoAt(ownedCellPos.x, ownedCellPos.y);
                    if (ownedCell == null)
                        continue;
                    
                    // Получаем все клетки в радиусе видимости от этой клетки города
                    List<CellInfo> cellsInRange = GetCellsInVisionRange(ownedCellPos.x, ownedCellPos.y, city.visionRadius);
                    
                    foreach (CellInfo cell in cellsInRange)
                    {
                        if (cell != null)
                        {
                            Vector2Int cellCoord = new Vector2Int(cell.GetGridX(), cell.GetGridY());
                            if (!visibleCellCoords.Contains(cellCoord))
                            {
                                visibleCellCoords.Add(cellCoord);
                                newVisibleCellsList.Add(cell);
                            }
                        }
                    }
                }
            }
        }
        
        // Теперь обновляем состояние всех клеток
        int gridWidth = grid.GetGridWidth();
        int gridHeight = grid.GetGridHeight();
        
        // Обновляем все клетки
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                CellInfo cell = grid.GetCellInfoAt(x, y);
                if (cell == null)
                    continue;
                
                Vector2Int cellCoord = new Vector2Int(x, y);
                
                // Если клетка видима
                if (visibleCellCoords.Contains(cellCoord))
                {
                    cell.SetFogOfWarState(FogOfWarState.Visible);
                }
                else
                {
                    // Клетка не видима
                    FogOfWarState currentState = cell.GetFogOfWarState();
                    
                    // Если клетка была видимой, но больше не видна, делаем её исследованной
                    if (currentState == FogOfWarState.Visible)
                    {
                        if (cell.HasBeenExplored())
                        {
                            cell.SetFogOfWarState(FogOfWarState.Explored);
                        }
                        else
                        {
                            cell.SetFogOfWarState(FogOfWarState.Hidden);
                        }
                    }
                    // Если клетка уже была Hidden или Explored, оставляем как есть
                }
            }
        }
        
        // Обновляем текущий список видимых клеток
        visibleCells = new HashSet<CellInfo>(newVisibleCellsList);
    }
    
    /// <summary>
    /// Получает все клетки в радиусе видимости от указанной позиции
    /// Использует точно такую же логику, как ReachableCellsHighlighter (BFS через HexagonalGridHelper)
    /// Использует только координаты сетки (X, Y), игнорируя Z координату (2D проект)
    /// </summary>
    private List<CellInfo> GetCellsInVisionRange(int centerX, int centerY, int radius)
    {
        List<CellInfo> cells = new List<CellInfo>();
        
        if (grid == null)
            return cells;
        
        // Получаем стартовую клетку
        CellInfo startCell = grid.GetCellInfoAt(centerX, centerY);
        if (startCell == null)
            return cells;
        
        int gridWidth = grid.GetGridWidth();
        int gridHeight = grid.GetGridHeight();
        
        // BFS/поиск в ширину по расстоянию в шагах (точно так же, как в ReachableCellsHighlighter)
        Queue<(CellInfo cell, int distance)> queue = new Queue<(CellInfo, int)>();
        Dictionary<CellInfo, int> bestDistance = new Dictionary<CellInfo, int>();
        
        queue.Enqueue((startCell, 0));
        bestDistance[startCell] = 0;
        
        while (queue.Count > 0)
        {
            var (currentCell, currentDistance) = queue.Dequeue();
            
            // Если достигли максимального радиуса, не проверяем дальше
            if (currentDistance >= radius)
                continue;
            
            int cx = currentCell.GetGridX();
            int cy = currentCell.GetGridY();
            
            // Получаем соседей точно так же, как в ReachableCellsHighlighter
            List<Vector2Int> neighbors = HexagonalGridHelper.GetNeighbors(cx, cy, gridWidth, gridHeight);
            foreach (var pos in neighbors)
            {
                CellInfo neighbor = grid.GetCellInfoAt(pos.x, pos.y);
                if (neighbor == null)
                    continue;
                
                // Расстояние увеличивается на 1 за каждый шаг
                int newDistance = currentDistance + 1;
                
                // Если превысили радиус, пропускаем
                if (newDistance > radius)
                    continue;
                
                // Если уже нашли более короткий путь, пропускаем
                if (bestDistance.TryGetValue(neighbor, out int oldDistance) && oldDistance <= newDistance)
                    continue;
                
                bestDistance[neighbor] = newDistance;
                queue.Enqueue((neighbor, newDistance));
            }
        }
        
        // Добавляем все найденные клетки (включая стартовую) в список для возврата
        foreach (var kvp in bestDistance)
        {
            if (kvp.Key != null)
            {
                cells.Add(kvp.Key);
            }
        }
        
        return cells;
    }
    
    /// <summary>
    /// Включает или выключает туман войны
    /// </summary>
    public void SetFogOfWarEnabled(bool enabled)
    {
        fogOfWarEnabled = enabled;
        
        if (!enabled)
        {
            // Если туман войны отключен, делаем все клетки видимыми
            MakeAllCellsVisible();
        }
        else
        {
            // Если туман войны включен, инициализируем его
            InitializeFogOfWar();
        }
    }
    
    /// <summary>
    /// Проверяет, включен ли туман войны
    /// </summary>
    public bool IsFogOfWarEnabled()
    {
        return fogOfWarEnabled;
    }
    
    /// <summary>
    /// Делает все клетки видимыми (используется при отключении тумана войны)
    /// </summary>
    private void MakeAllCellsVisible()
    {
        if (grid == null)
            return;
        
        int gridWidth = grid.GetGridWidth();
        int gridHeight = grid.GetGridHeight();
        
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                CellInfo cell = grid.GetCellInfoAt(x, y);
                if (cell != null)
                {
                    cell.SetFogOfWarState(FogOfWarState.Visible);
                }
            }
        }
    }
    
    /// <summary>
    /// Получает материал для неразведанных клеток
    /// </summary>
    public Material GetFogUnseenMaterial()
    {
        return fogUnseenMaterial;
    }
    
    /// <summary>
    /// Получает материал для разведанных, но невидимых клеток
    /// </summary>
    public Material GetFogExploredMaterial()
    {
        return fogExploredMaterial;
    }
}



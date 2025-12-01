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
    
    [Header("Настройки виньетки")]
    [Tooltip("Включена ли виньетка для тумана войны (FogOfWarNoise шейдер)")]
    [SerializeField] private bool fogOfWarVignetteEnabled = true;
    [Tooltip("Включена ли виньетка для текстур клеток (WorldSpaceTexture шейдер)")]
    [SerializeField] private bool worldSpaceTextureVignetteEnabled = true;
    [Tooltip("Множитель радиуса для виньетки (0-1, где 1 = полный радиус из mesh)")]
    [Range(0.0f, 1.0f)]
    [SerializeField] private float vignetteHexRadius = 1.0f;
    
    [Header("Настройки неровных краев")]
    [Tooltip("Включены ли неровные края для тумана войны (FogOfWarNoise шейдер)")]
    [SerializeField] private bool raggedEdgesEnabled = false;
    
    [Header("Неровные края по граням")]
    [Tooltip("Включена ли неровность для Top Left (30-90°)")]
    [SerializeField] private bool raggedEdgeTopLeft = true;
    [Tooltip("Включена ли неровность для Top Right (90-150°)")]
    [SerializeField] private bool raggedEdgeFlatLeft = true;
    [Tooltip("Включена ли неровность для Flat Right (150-210°)")]
    [SerializeField] private bool raggedEdgeBottomLeft = true;
    [Tooltip("Включена ли неровность для Bottom Right (210-270°)")]
    [SerializeField] private bool raggedEdgeBottomRight = true;
    [Tooltip("Включена ли неровность для Bottom Left (270-330°)")]
    [SerializeField] private bool raggedEdgeFlatRight = true;
    [Tooltip("Включена ли неровность для Flat Left (330-30°)")]
    [SerializeField] private bool raggedEdgeTopRight = true;
    
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
    /// Возвращает ссылку на текущую сетку
    /// </summary>
    public CellNameSpace.Grid GetGrid()
    {
        return grid;
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
        
        // Устанавливаем _HexRadius, _VignetteHexRadius, _VignetteEnabled, _RaggedEdgesEnabled и параметры граней в материалы тумана
        if (fogUnseenMaterial != null)
        {
            fogUnseenMaterial.SetFloat("_HexRadius", hexRadius);
            fogUnseenMaterial.SetFloat("_VignetteHexRadius", vignetteHexRadius);
            fogUnseenMaterial.SetFloat("_VignetteEnabled", fogOfWarVignetteEnabled ? 1.0f : 0.0f);
            fogUnseenMaterial.SetFloat("_RaggedEdgesEnabled", raggedEdgesEnabled ? 1.0f : 0.0f);
            fogUnseenMaterial.SetFloat("_RaggedEdgeTopRight", raggedEdgeTopRight ? 1.0f : 0.0f);
            fogUnseenMaterial.SetFloat("_RaggedEdgeTopLeft", raggedEdgeTopLeft ? 1.0f : 0.0f);
            fogUnseenMaterial.SetFloat("_RaggedEdgeFlatLeft", raggedEdgeFlatLeft ? 1.0f : 0.0f);
            fogUnseenMaterial.SetFloat("_RaggedEdgeBottomLeft", raggedEdgeBottomLeft ? 1.0f : 0.0f);
            fogUnseenMaterial.SetFloat("_RaggedEdgeBottomRight", raggedEdgeBottomRight ? 1.0f : 0.0f);
            fogUnseenMaterial.SetFloat("_RaggedEdgeFlatRight", raggedEdgeFlatRight ? 1.0f : 0.0f);
            Debug.Log($"[FogOfWarManager] Установлены параметры неровных краев в fogUnseenMaterial");
        }
        
        if (fogExploredMaterial != null)
        {
            fogExploredMaterial.SetFloat("_HexRadius", hexRadius);
            fogExploredMaterial.SetFloat("_VignetteHexRadius", vignetteHexRadius);
            fogExploredMaterial.SetFloat("_VignetteEnabled", fogOfWarVignetteEnabled ? 1.0f : 0.0f);
            fogExploredMaterial.SetFloat("_RaggedEdgesEnabled", raggedEdgesEnabled ? 1.0f : 0.0f);
            fogExploredMaterial.SetFloat("_RaggedEdgeTopRight", raggedEdgeTopRight ? 1.0f : 0.0f);
            fogExploredMaterial.SetFloat("_RaggedEdgeTopLeft", raggedEdgeTopLeft ? 1.0f : 0.0f);
            fogExploredMaterial.SetFloat("_RaggedEdgeFlatLeft", raggedEdgeFlatLeft ? 1.0f : 0.0f);
            fogExploredMaterial.SetFloat("_RaggedEdgeBottomLeft", raggedEdgeBottomLeft ? 1.0f : 0.0f);
            fogExploredMaterial.SetFloat("_RaggedEdgeBottomRight", raggedEdgeBottomRight ? 1.0f : 0.0f);
            fogExploredMaterial.SetFloat("_RaggedEdgeFlatRight", raggedEdgeFlatRight ? 1.0f : 0.0f);
            Debug.Log($"[FogOfWarManager] Установлены параметры неровных краев в fogExploredMaterial");
        }
        
        // Устанавливаем _HexRadius и _VignetteEnabled во все материалы, использующие WorldSpaceTexture шейдер
        Shader worldSpaceTextureShader = Shader.Find("Custom/WorldSpaceTexture");
        if (worldSpaceTextureShader != null)
        {
            // Находим все материалы с этим шейдером
            Material[] allMaterials = Resources.FindObjectsOfTypeAll<Material>();
            int materialsUpdated = 0;
            foreach (Material mat in allMaterials)
            {
                if (mat != null && mat.shader != null && mat.shader.name == "Custom/WorldSpaceTexture")
                {
                    mat.SetFloat("_HexRadius", hexRadius);
                    mat.SetFloat("_VignetteEnabled", worldSpaceTextureVignetteEnabled ? 1.0f : 0.0f);
                    materialsUpdated++;
                }
            }
            if (materialsUpdated > 0)
            {
                Debug.Log($"[FogOfWarManager] Установлен _HexRadius = {hexRadius}, _VignetteEnabled = {worldSpaceTextureVignetteEnabled} в {materialsUpdated} материалах с шейдером WorldSpaceTexture");
            }
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
        
        // После того как все состояния обновлены, пересчитываем неровные края
        // для всех клеток, чтобы оборванные края соответствовали текущим соседям.
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                CellInfo cell = grid.GetCellInfoAt(x, y);
                if (cell == null)
                    continue;
                
                cell.RefreshFogOfWarRaggedEdges();
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
    
    /// <summary>
    /// Включает или выключает виньетку для тумана войны (FogOfWarNoise шейдер)
    /// </summary>
    public void SetFogOfWarVignetteEnabled(bool enabled)
    {
        fogOfWarVignetteEnabled = enabled;
        
        // Обновляем _VignetteEnabled в материалах тумана
        float vignetteValue = enabled ? 1.0f : 0.0f;
        
        if (fogUnseenMaterial != null)
        {
            fogUnseenMaterial.SetFloat("_VignetteEnabled", vignetteValue);
        }
        
        if (fogExploredMaterial != null)
        {
            fogExploredMaterial.SetFloat("_VignetteEnabled", vignetteValue);
        }
    }
    
    /// <summary>
    /// Включает или выключает виньетку для текстур клеток (WorldSpaceTexture шейдер)
    /// </summary>
    public void SetWorldSpaceTextureVignetteEnabled(bool enabled)
    {
        worldSpaceTextureVignetteEnabled = enabled;
        
        // Обновляем _VignetteEnabled во всех материалах с WorldSpaceTexture шейдером
        float vignetteValue = enabled ? 1.0f : 0.0f;
        
        Shader worldSpaceTextureShader = Shader.Find("Custom/WorldSpaceTexture");
        if (worldSpaceTextureShader != null)
        {
            Material[] allMaterials = Resources.FindObjectsOfTypeAll<Material>();
            foreach (Material mat in allMaterials)
            {
                if (mat != null && mat.shader != null && mat.shader.name == "Custom/WorldSpaceTexture")
                {
                    mat.SetFloat("_VignetteEnabled", vignetteValue);
                }
            }
        }
    }
    
    /// <summary>
    /// Проверяет, включена ли виньетка для тумана войны
    /// </summary>
    public bool IsFogOfWarVignetteEnabled()
    {
        return fogOfWarVignetteEnabled;
    }
    
    /// <summary>
    /// Проверяет, включена ли виньетка для текстур клеток
    /// </summary>
    public bool IsWorldSpaceTextureVignetteEnabled()
    {
        return worldSpaceTextureVignetteEnabled;
    }
    
    /// <summary>
    /// Устанавливает множитель радиуса для виньетки (0-1)
    /// </summary>
    public void SetVignetteHexRadius(float radius)
    {
        vignetteHexRadius = Mathf.Clamp01(radius);
        UpdateVignetteHexRadius();
    }
    
    /// <summary>
    /// Получает текущий множитель радиуса для виньетки
    /// </summary>
    public float GetVignetteHexRadius()
    {
        return vignetteHexRadius;
    }
    
    /// <summary>
    /// Обновляет _VignetteHexRadius в материалах тумана
    /// </summary>
    private void UpdateVignetteHexRadius()
    {
        if (fogUnseenMaterial != null)
        {
            fogUnseenMaterial.SetFloat("_VignetteHexRadius", vignetteHexRadius);
        }
        
        if (fogExploredMaterial != null)
        {
            fogExploredMaterial.SetFloat("_VignetteHexRadius", vignetteHexRadius);
        }
    }
    
    /// <summary>
    /// Вызывается при изменении значений в инспекторе
    /// </summary>
    private void OnValidate()
    {
        // Обновляем параметры при изменении значений в инспекторе
        if (Application.isPlaying)
        {
            UpdateVignetteHexRadius();
        }
    }
    
    /// <summary>
    /// Включает или выключает неровные края для тумана войны (FogOfWarNoise шейдер)
    /// </summary>
    public void SetRaggedEdgesEnabled(bool enabled)
    {
        raggedEdgesEnabled = enabled;
        
        // Обновляем _RaggedEdgesEnabled в материалах тумана
        float raggedEdgesValue = enabled ? 1.0f : 0.0f;
        
        if (fogUnseenMaterial != null)
        {
            fogUnseenMaterial.SetFloat("_RaggedEdgesEnabled", raggedEdgesValue);
        }
        
        if (fogExploredMaterial != null)
        {
            fogExploredMaterial.SetFloat("_RaggedEdgesEnabled", raggedEdgesValue);
        }
    }
    
    /// <summary>
    /// Проверяет, включены ли неровные края для тумана войны
    /// </summary>
    public bool IsRaggedEdgesEnabled()
    {
        return raggedEdgesEnabled;
    }
    
    /// <summary>
    /// Обновляет параметры граней в материалах тумана
    /// </summary>
    private void UpdateRaggedEdgesFaceParameters()
    {
        if (fogUnseenMaterial != null)
        {
            fogUnseenMaterial.SetFloat("_RaggedEdgeTopRight", raggedEdgeTopRight ? 1.0f : 0.0f);
            fogUnseenMaterial.SetFloat("_RaggedEdgeTopLeft", raggedEdgeTopLeft ? 1.0f : 0.0f);
            fogUnseenMaterial.SetFloat("_RaggedEdgeFlatLeft", raggedEdgeFlatLeft ? 1.0f : 0.0f);
            fogUnseenMaterial.SetFloat("_RaggedEdgeBottomLeft", raggedEdgeBottomLeft ? 1.0f : 0.0f);
            fogUnseenMaterial.SetFloat("_RaggedEdgeBottomRight", raggedEdgeBottomRight ? 1.0f : 0.0f);
            fogUnseenMaterial.SetFloat("_RaggedEdgeFlatRight", raggedEdgeFlatRight ? 1.0f : 0.0f);
        }
        
        if (fogExploredMaterial != null)
        {
            fogExploredMaterial.SetFloat("_RaggedEdgeTopRight", raggedEdgeTopRight ? 1.0f : 0.0f);
            fogExploredMaterial.SetFloat("_RaggedEdgeTopLeft", raggedEdgeTopLeft ? 1.0f : 0.0f);
            fogExploredMaterial.SetFloat("_RaggedEdgeFlatLeft", raggedEdgeFlatLeft ? 1.0f : 0.0f);
            fogExploredMaterial.SetFloat("_RaggedEdgeBottomLeft", raggedEdgeBottomLeft ? 1.0f : 0.0f);
            fogExploredMaterial.SetFloat("_RaggedEdgeBottomRight", raggedEdgeBottomRight ? 1.0f : 0.0f);
            fogExploredMaterial.SetFloat("_RaggedEdgeFlatRight", raggedEdgeFlatRight ? 1.0f : 0.0f);
        }
    }
    
    /// <summary>
    /// Устанавливает состояние неровности для Top Left (30-90°)
    /// </summary>
    public void SetRaggedEdgeTopLeft(bool enabled)
    {
        raggedEdgeTopLeft = enabled;
        UpdateRaggedEdgesFaceParameters();
    }
    
    /// <summary>
    /// Устанавливает состояние неровности для Top Right (90-150°)
    /// </summary>
    public void SetRaggedEdgeFlatLeft(bool enabled)
    {
        raggedEdgeFlatLeft = enabled;
        UpdateRaggedEdgesFaceParameters();
    }
    
    /// <summary>
    /// Устанавливает состояние неровности для Flat Right (150-210°)
    /// </summary>
    public void SetRaggedEdgeBottomLeft(bool enabled)
    {
        raggedEdgeBottomLeft = enabled;
        UpdateRaggedEdgesFaceParameters();
    }
    
    /// <summary>
    /// Устанавливает состояние неровности для Bottom Right (210-270°)
    /// </summary>
    public void SetRaggedEdgeBottomRight(bool enabled)
    {
        raggedEdgeBottomRight = enabled;
        UpdateRaggedEdgesFaceParameters();
    }
    
    /// <summary>
    /// Устанавливает состояние неровности для Bottom Left (270-330°)
    /// </summary>
    public void SetRaggedEdgeFlatRight(bool enabled)
    {
        raggedEdgeFlatRight = enabled;
        UpdateRaggedEdgesFaceParameters();
    }
    
    /// <summary>
    /// Устанавливает состояние неровности для Flat Left (330-30°)
    /// </summary>
    public void SetRaggedEdgeTopRight(bool enabled)
    {
        raggedEdgeTopRight = enabled;
        UpdateRaggedEdgesFaceParameters();
    }
}



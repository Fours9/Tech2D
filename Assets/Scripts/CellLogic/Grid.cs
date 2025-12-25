using UnityEngine;
using System.Collections;
using System.Collections.Generic;


namespace CellNameSpace
{
    public class Grid : MonoBehaviour
    {
        [SerializeField] private GameObject cellPrefab;
        [SerializeField] private int gridWidth = 10;
        [SerializeField] private int gridHeight = 10;
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private float pixelGap = 0.1f; // Зазор между клетками в пикселях (1 пиксель = 0.01 единицы при стандартном PPU = 100)
        
        [Header("Генерация суши")]
        [SerializeField] private bool useVoronoiForLand = false; // Использовать Voronoi для генерации континентов
        [Range(5, 50)]
        [SerializeField] private int voronoiRegions = 15; // Количество регионов Voronoi (континентов)
        [SerializeField] private int voronoiSeed = 0; // Сид для генерации Voronoi (0 = случайный)
        [SerializeField] [Min(1)] private int voronoiMinRegionSize = 5; // Минимальный размер региона в клетках (маленькие регионы объединяются с соседними)
        [Range(0f, 1f)]
        [SerializeField] private float landFrequency = 0.5f; // Частота появления суши (field)
        [Range(0f, 1f)]
        [SerializeField] private float landFragmentation = 0.5f; // Раздробленность суши
        [SerializeField] private int landSeed = 0; // Сид для генерации суши (0 = случайный)
        
        [Header("Генерация водоемов")]
        [Range(0f, 1f)]
        [SerializeField] private float waterFrequency = 0.2f; // Частота появления водоемов
        [Range(0f, 1f)]
        [SerializeField] private float waterFragmentation = 0.5f; // Раздробленность водоемов (чем больше, тем более раздробленные)
        [SerializeField] private int waterSeed = 0; // Сид для генерации водоемов (0 = случайный)
        [SerializeField] private bool convertShallowOnlyToDeep = true; // Если true, клетки shallow с соседями только shallow становятся deep_water
        [SerializeField] private bool convertShallowNearDeepToDeep = true; // Если true, клетки shallow с соседом deep_water могут стать deep_water
        [Range(0f, 1f)]
        [SerializeField] private float shallowToDeepChance = 0.2f; // Шанс превращения shallow в deep_water при наличии соседа deep_water (0-1)
        [SerializeField] [Min(1)] private int waterProcessingIterations = 3; // Количество итераций обработки воды (сколько раз прогонять все правила по кругу)
        
        [Header("Генерация островов (второй этап суши)")]
        [Range(0f, 1f)]
        [SerializeField] private float islandsFrequency = 0.15f; // Частота появления островов в воде
        [Range(0f, 1f)]
        [SerializeField] private float islandsFragmentation = 0.5f; // Раздробленность островов
        [SerializeField] private int islandsSeed = 0; // Сид для генерации островов (0 = случайный)
        
        [Header("Генерация озер")]
        [Range(0f, 1f)]
        [SerializeField] private float lakeFrequency = 0.1f; // Частота появления озер
        [SerializeField] [Min(1)] private int lakeMinSize = 3; // Минимальный размер озера в клетках
        [SerializeField] private int lakeSeed = 0; // Сид для генерации озер (0 = случайный)
        [Range(0f, 1f)]
        [SerializeField] private float lakeMaxPercentage = 0.02f; // Максимальная доля площади под озера (от всей карты)
        
        [Header("Генерация внутренних морей")]
        [Range(0f, 1f)]
        [SerializeField] private float inlandSeaFrequency = 0.05f; // Частота появления внутренних морей
        [SerializeField] [Min(1)] private int inlandSeaMinSize = 10; // Минимальный размер внутреннего моря в клетках
        [SerializeField] private int inlandSeaSeed = 0; // Сид для генерации внутренних морей (0 = случайный)
        [Range(0f, 1f)]
        [SerializeField] private float inlandSeaMaxPercentage = 0.05f; // Максимальная доля площади под внутренние моря (от всей карты)
        [Range(0f, 1f)]
        [SerializeField] private float inlandSeaLandNeighborThreshold = 0.3f; // Доля соседей-суши, требуемая для признания области внутренним морем
        
        [Header("Генерация рек")]
        [Range(0f, 1f)]
        [SerializeField] private float riverChance = 0.8f; // Шанс генерации реки от озера
        [SerializeField] private int riverSeed = 0; // Сид для генерации рек (0 = случайный)
        [Range(0f, 2f)]
        [SerializeField] private float riverMeanderStrength = 0.6f; // Насколько извилистые реки (0 = прямые)
        [SerializeField] private float riverMeanderNoiseScale = 0.25f; // Масштаб шума для извилин
        
        [Header("Генерация гор")]
        [Range(0f, 1f)]
        [SerializeField] private float mountainFrequency = 0.15f; // Частота появления гор
        [Range(0f, 1f)]
        [SerializeField] private float mountainFragmentation = 0.5f; // Раздробленность гор
        [SerializeField] private int mountainSeed = 0; // Сид для генерации гор (0 = случайный)
        
        [Header("Генерация лесов")]
        [Range(0f, 1f)]
        [SerializeField] private float forestFrequency = 0.2f; // Частота появления лесов
        [Range(0f, 1f)]
        [SerializeField] private float forestFragmentation = 0.5f; // Раздробленность лесов
        [SerializeField] private int forestSeed = 0; // Сид для генерации лесов (0 = случайный)
        
        [Header("Генерация пустынь")]
        [Range(0f, 1f)]
        [SerializeField] private float desertFrequency = 0.15f; // Частота появления пустынь
        [Range(0f, 1f)]
        [SerializeField] private float desertFragmentation = 0.5f; // Раздробленность пустынь
        [SerializeField] private int desertSeed = 0; // Сид для генерации пустынь (0 = случайный)
        
        [Header("Оптимизация")]
        [SerializeField] private int cellsPerFrame = 50; // Количество клеток для обработки за кадр (для корутин)
        [SerializeField] private bool useCoroutines = true; // Использовать ли корутины для распределения работы
        [SerializeField] private bool pauseGameDuringGeneration = true; // Ставить игру на паузу во время генерации карты
        
        private List<GameObject> cells = new List<GameObject>();
        private float savedTimeScale = 1f; // Сохраненное значение timeScale
        
        // Кэшированные значения для расчета ширины карты
        private float cachedHexWidth = 0f;
        private float cachedHexHeight = 0f;
        private float cachedHexOffset = 0f;
        private float cachedActualCellSize = 0f;
        
        /// <summary>
        /// Флаг, показывающий, что генерация сетки и типов клеток завершена.
        /// Можно использовать, чтобы безопасно спавнить юнитов только после готовности карты.
        /// </summary>
        public bool IsGenerationComplete { get; private set; } = false;
        
        void Start()
        {
            GenerateGrid();
        }
        
        private void GenerateGrid()
        {
            // При любом запуске генерации считаем, что она ещё не завершена
            IsGenerationComplete = false;

            if (cellPrefab == null)
            {
                Debug.LogError("Cell Prefab не назначен!");
                return;
            }
            
            // Ставим игру на паузу во время генерации (если включено)
            if (pauseGameDuringGeneration && Application.isPlaying)
            {
                savedTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }
            
            // Очищаем существующие клетки
            ClearGrid();
            
            // Получаем реальный размер префаба для правильного расчета расстояний и масштабирования
            Renderer prefabRenderer = cellPrefab.GetComponent<Renderer>();

            float actualCellSize = cellSize;
            
            if (prefabRenderer != null)
            {

                // Используем размер bounds префаба, если cellSize не задан явно
                if (cellSize <= 0.1f)
                {
                    actualCellSize = Mathf.Max(prefabRenderer.bounds.size.x, prefabRenderer.bounds.size.y)*cellSize;
                }
            }
            
            // Для гексагональной тесселяции (шестиугольники)
            // Горизонтальное расстояние между центрами: cellSize * √3 + зазор
            float hexWidth = actualCellSize * 1.732f + pixelGap; // √3 ≈ 1.732
            // Вертикальное расстояние между центрами: cellSize * 1.5 + зазор
            float hexHeight = actualCellSize * 1.5f + pixelGap;
            // Смещение для нечетных строк: (cellSize * √3 + зазор) / 2
            float hexOffset = (actualCellSize * 1.732f + pixelGap) * 0.5f; // √3 / 2 ≈ 0.866
            
            // Кэшируем значения для расчета ширины карты
            cachedHexWidth = hexWidth;
            cachedHexHeight = hexHeight;
            cachedHexOffset = hexOffset;
            cachedActualCellSize = actualCellSize;
            
            // Сначала создаем все клетки как field
            // ВАЖНО: визуально строим поле СВЕРХУ ВНИЗ,
            // но логическая индексация (row/col и список cells) остаётся прежней.
            //
            // То есть row = 0 — это ВЕРХНЯЯ строка на карте,
            // row = gridHeight - 1 — нижняя строка.
            float startY = (gridHeight - 1) * hexHeight;
            for (int row = 0; row < gridHeight; row++)
            {
                for (int col = 0; col < gridWidth; col++)
                {
                    // Смещение для нечетных строк для правильной гексагональной укладки
                    float offsetX = (row % 2 == 0) ? 0f : hexOffset;
                    float x = col * hexWidth + offsetX;
                    // Строим сетку сверху вниз: первая строка (row = 0) — самая верхняя
                    float y = startY - row * hexHeight;
                    // Z координата: верхние ряды имеют больший Z, нижние — меньший
                    float z = gridHeight - row;
                    
                    Vector3 position = new Vector3(x, y, z);
                    // Поворот префаба на 180 градусов по оси Y
                    Quaternion rotation = Quaternion.Euler(0f, 180f, 0f);
                    GameObject cell = Instantiate(cellPrefab, position, rotation, transform);
                    // Применяем масштаб на основе cellSize
                    cell.transform.localScale = cell.transform.localScale * cellSize;
                    cell.name = $"Cell_{row}_{col}";
                    
                    // Все клетки сначала shallow
                    CellInfo cellInfo = cell.GetComponent<CellInfo>();
                    if (cellInfo != null)
                    {
                        // Пока не передаем менеджеры, они будут найдены позже при SetCellType
                        // Это оптимизирует процесс, так как менеджеры могут еще не быть инициализированы
                        cellInfo.Initialize(col, row, CellType.shallow);
                    }
                    else
                    {
                        Debug.LogWarning($"CellInfo не найден на клетке {cell.name}");
                    }
                    
                    cells.Add(cell);
                }
            }
            
            // Инициализируем кэш соседей для оптимизации
            HexagonalGridHelper.InitializeCache(gridWidth, gridHeight);
            
            // Создаем массив типов для генерации
            CellType[,] grid = new CellType[gridWidth, gridHeight];
            for (int row = 0; row < gridHeight; row++)
            {
                for (int col = 0; col < gridWidth; col++)
                {
                    grid[col, row] = CellType.shallow;
                }
            }
            
            // Генерируем сушу (field клетки)
            if (useVoronoiForLand)
            {
                // Используем Voronoi для генерации континентов
                int actualVoronoiSeed = voronoiSeed == 0 ? Random.Range(1, 1000000) : voronoiSeed;
                int[,] voronoiRegions = VoronoiGenerator.GenerateVoronoiRegions(
                    gridWidth, gridHeight, this.voronoiRegions, actualVoronoiSeed,
                    cachedHexWidth, cachedHexHeight, cachedHexOffset);
                
                // Фильтруем маленькие регионы, объединяя их с соседними
                VoronoiGenerator.FilterSmallRegions(voronoiRegions, gridWidth, gridHeight, voronoiMinRegionSize);
                
                int actualLandSeed = landSeed == 0 ? Random.Range(1, 1000000) : landSeed;
                VoronoiGenerator.ApplyVoronoiToGrid(grid, voronoiRegions, gridWidth, gridHeight,
                    landFrequency, actualLandSeed);
            }
            else
            {
                // Используем обычный метод генерации суши
                int actualLandSeed = landSeed == 0 ? Random.Range(1, 1000000) : landSeed;
                TerrainGenerator.GenerateLand(grid, gridWidth, gridHeight,
                    landFrequency, landFragmentation, actualLandSeed);
            }
            
            // Генерируем водоемы
            int actualWaterSeed = waterSeed == 0 ? Random.Range(1, 1000000) : waterSeed;
            WaterBodyGenerator.GenerateWaterBodies(grid, gridWidth, gridHeight, 
                waterFrequency, waterFragmentation, actualWaterSeed,
                convertShallowOnlyToDeep, convertShallowNearDeepToDeep, shallowToDeepChance, waterProcessingIterations);
            
            // Генерируем острова (второй этап генерации суши - создаем острова в океанах)
            int actualIslandsSeed = islandsSeed == 0 ? Random.Range(1, 1000000) : islandsSeed;
            TerrainGenerator.GenerateIslands(grid, gridWidth, gridHeight,
                islandsFrequency, islandsFragmentation, actualIslandsSeed);
            
            // Генерируем озера (на суше, не соединенные с океаном)
            int actualLakeSeed = lakeSeed == 0 ? Random.Range(1, 1000000) : lakeSeed;
            LakeGenerator.GenerateLakes(grid, gridWidth, gridHeight,
                lakeFrequency, lakeMinSize, actualLakeSeed, lakeMaxPercentage);
            
            // Генерируем внутренние моря (большие водоемы внутри континентов)
            int actualInlandSeaSeed = inlandSeaSeed == 0 ? Random.Range(1, 1000000) : inlandSeaSeed;
            InlandSeaGenerator.GenerateInlandSeas(grid, gridWidth, gridHeight,
                inlandSeaFrequency, inlandSeaMinSize, actualInlandSeaSeed, inlandSeaMaxPercentage, inlandSeaLandNeighborThreshold);
            
            // Генерируем реки (от озер к ближайшей воде)
            int actualRiverSeed = riverSeed == 0 ? Random.Range(1, 1000000) : riverSeed;
            RiverGenerator.GenerateRivers(grid, gridWidth, gridHeight,
                riverChance, actualRiverSeed, riverMeanderStrength, riverMeanderNoiseScale);
            
            // Обработка воды (ProcessWaterBodies) - после всех основных этапов
            WaterBodyGenerator.ProcessWaterBodies(grid, gridWidth, gridHeight,
                convertShallowOnlyToDeep, convertShallowNearDeepToDeep, shallowToDeepChance, waterProcessingIterations);
            
            // Генерируем пустыни (не перекрывая водоемы)
            int actualDesertSeed = desertSeed == 0 ? Random.Range(1, 1000000) : desertSeed;
            TerrainGenerator.GenerateDeserts(grid, gridWidth, gridHeight,
                desertFrequency, desertFragmentation, actualDesertSeed);
            
            // Генерируем леса (не перекрывая водоемы)
            int actualForestSeed = forestSeed == 0 ? Random.Range(1, 1000000) : forestSeed;
            TerrainGenerator.GenerateForests(grid, gridWidth, gridHeight,
                forestFrequency, forestFragmentation, actualForestSeed);
            
            // Генерируем горы (не перекрывая водоемы)
            int actualMountainSeed = mountainSeed == 0 ? Random.Range(1, 1000000) : mountainSeed;
            TerrainGenerator.GenerateMountains(grid, gridWidth, gridHeight,
                mountainFrequency, mountainFragmentation, actualMountainSeed);
            
            // Применяем логику совместимости
            TerrainCompatibility.ApplyCompatibilityRules(grid, gridWidth, gridHeight);
            
            // Находим менеджеры один раз для оптимизации
            CellMaterialManager materialManager = FindFirstObjectByType<CellMaterialManager>();
            CellOverlayManager overlayManager = FindFirstObjectByType<CellOverlayManager>();
            
            // Применяем результаты к GameObject'ам
            if (useCoroutines && Application.isPlaying)
            {
                // Используем корутину для распределения работы по кадрам
                StartCoroutine(ApplyCellTypesCoroutine(grid, materialManager, overlayManager));
            }
            else
            {
                // Синхронное применение (для Editor или если корутины отключены)
                ApplyCellTypesSync(grid, materialManager, overlayManager);
            }
        }
        
        /// <summary>
        /// Синхронно применяет типы клеток к GameObject'ам
        /// </summary>
        private void ApplyCellTypesSync(CellType[,] grid, CellMaterialManager materialManager, CellOverlayManager overlayManager)
        {
            for (int row = 0; row < gridHeight; row++)
            {
                for (int col = 0; col < gridWidth; col++)
                {
                    GameObject cell = cells[row * gridWidth + col];
                    CellInfo cellInfo = cell.GetComponent<CellInfo>();
                    if (cellInfo != null)
                    {
                        // Устанавливаем менеджеры для оптимизации (если они найдены)
                        cellInfo.SetManagers(materialManager, overlayManager);
                        // Отключаем обновление оверлеев при массовом создании для оптимизации
                        cellInfo.SetCellType(grid[col, row], updateOverlays: false);
                    }
                }
            }
            
            // Установка ресурсов и построек происходит автоматически через SetCellType() -> SetResourceStats() / SetBuildingStats()
            // Возобновляем игру после завершения генерации
                ResumeGame();

            // Типы всех клеток применены — считаем генерацию завершённой
            IsGenerationComplete = true;
        }
        
        /// <summary>
        /// Корутина для применения типов клеток с распределением по кадрам
        /// </summary>
        private IEnumerator ApplyCellTypesCoroutine(CellType[,] grid, CellMaterialManager materialManager, CellOverlayManager overlayManager)
        {
            int processed = 0;
            
            for (int row = 0; row < gridHeight; row++)
            {
                for (int col = 0; col < gridWidth; col++)
                {
                    GameObject cell = cells[row * gridWidth + col];
                    CellInfo cellInfo = cell.GetComponent<CellInfo>();
                    if (cellInfo != null)
                    {
                        // Устанавливаем менеджеры для оптимизации (если они найдены)
                        cellInfo.SetManagers(materialManager, overlayManager);
                        // Отключаем обновление оверлеев при массовом создании для оптимизации
                        cellInfo.SetCellType(grid[col, row], updateOverlays: false);
                        processed++;
                        
                        // Каждые cellsPerFrame клеток делаем паузу на один кадр
                        if (processed >= cellsPerFrame)
                        {
                            processed = 0;
                            // Используем WaitForEndOfFrame для работы при timeScale = 0
                            yield return new WaitForEndOfFrame();
                        }
                    }
                }
            }
            
            // Установка ресурсов и построек происходит автоматически через SetCellType() -> SetResourceStats() / SetBuildingStats()
            // Типы всех клеток применены — считаем генерацию завершённой
            IsGenerationComplete = true;
            
            // Возобновляем игру после завершения генерации
            ResumeGame();
        }
        
        /// <summary>
        /// Возобновляет игру после завершения генерации
        /// </summary>
        private void ResumeGame()
        {
            if (pauseGameDuringGeneration && Application.isPlaying)
            {
                Time.timeScale = savedTimeScale;
            }
        }
        
        private void ClearGrid()
        {
            foreach (GameObject cell in cells)
            {
                if (cell != null)
                {
                    DestroyImmediate(cell);
                }
            }
            cells.Clear();
            
            // Очищаем кэш соседей при очистке сетки
            HexagonalGridHelper.ClearCache();
        }
        
        // Метод для пересоздания сетки (можно вызвать из инспектора или кода)
        [ContextMenu("Regenerate Grid")]
        public void RegenerateGrid()
        {
            GenerateGrid();
        }
        
        /// <summary>
        /// Получает ширину карты в мировых координатах
        /// </summary>
        /// <returns>Ширина карты в мировых координатах</returns>
        public float GetMapWidth()
        {
            // Если значения еще не кэшированы, вычисляем их
            if (cachedHexWidth <= 0f)
            {
                Renderer prefabRenderer = cellPrefab != null ? cellPrefab.GetComponent<Renderer>() : null;
                float actualCellSize = cellSize;
                
                if (prefabRenderer != null && cellSize <= 0.1f)
                {
                    actualCellSize = Mathf.Max(prefabRenderer.bounds.size.x, prefabRenderer.bounds.size.y) * cellSize;
                }
                
                cachedHexWidth = actualCellSize * 1.732f + pixelGap;
                cachedHexOffset = (actualCellSize * 1.732f + pixelGap) * 0.5f;
                cachedActualCellSize = actualCellSize;
            }
            
            // Ширина карты = (gridWidth - 1) * hexWidth + максимальное смещение для нечетных строк
            // Максимальное смещение = hexOffset (для последней нечетной строки)
            float maxOffset = (gridHeight > 0 && (gridHeight - 1) % 2 != 0) ? cachedHexOffset : 0f;
            return (gridWidth - 1) * cachedHexWidth + maxOffset;
        }
        
        /// <summary>
        /// Получает клетку по координатам сетки
        /// </summary>
        /// <param name="gridX">Координата X в сетке</param>
        /// <param name="gridY">Координата Y в сетке</param>
        /// <returns>GameObject клетки или null, если не найдена</returns>
        public GameObject GetCellAt(int gridX, int gridY)
        {
            // Проверяем границы
            if (gridX < 0 || gridX >= gridWidth || gridY < 0 || gridY >= gridHeight)
            {
                return null;
            }
            
            // Вычисляем индекс в списке cells
            // Формула: row * gridWidth + col
            int index = gridY * gridWidth + gridX;
            
            if (index >= 0 && index < cells.Count)
            {
                return cells[index];
            }
            
            return null;
        }
        
        /// <summary>
        /// Получает CellInfo по координатам сетки
        /// </summary>
        /// <param name="gridX">Координата X в сетке</param>
        /// <param name="gridY">Координата Y в сетке</param>
        /// <returns>CellInfo или null, если не найдена</returns>
        public CellInfo GetCellInfoAt(int gridX, int gridY)
        {
            GameObject cell = GetCellAt(gridX, gridY);
            if (cell != null)
            {
                return cell.GetComponent<CellInfo>();
            }
            return null;
        }
        
        /// <summary>
        /// Получает ширину сетки
        /// </summary>
        public int GetGridWidth()
        {
            return gridWidth;
        }
        
        /// <summary>
        /// Получает высоту сетки
        /// </summary>
        public int GetGridHeight()
        {
            return gridHeight;
        }
        
        /// <summary>
        /// Включает/выключает обводку для всех клеток
        /// </summary>
        /// <param name="enabled">Включить обводку</param>
        /// <param name="outlineColor">Цвет обводки (по умолчанию черный)</param>
        /// <param name="outlineWidth">Толщина обводки в пикселях (по умолчанию 2)</param>
        public void SetAllCellsOutline(bool enabled, Color? outlineColor = null, float outlineWidth = 2f)
        {
            foreach (GameObject cell in cells)
            {
                if (cell == null)
                    continue;
                    
                CellInfo cellInfo = cell.GetComponent<CellInfo>();
                if (cellInfo != null)
                {
                    cellInfo.SetOutline(enabled, outlineColor, outlineWidth);
                }
            }
        }
    }
}

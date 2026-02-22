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
        
        // Список чанков для системы оптимизации рендеринга
        private List<CellChunk> chunks = new List<CellChunk>();
        private Dictionary<Vector2Int, CellChunk> chunkByChunkCoord = new Dictionary<Vector2Int, CellChunk>();
        
        // Система пересборки чанков
        private HashSet<CellChunk> dirtyChunks = new HashSet<CellChunk>(); // Чанки, требующие пересборки
        private Coroutine rebuildChunksCoroutine = null; // Корутина для пересборки чанков
        [SerializeField] private int chunksPerFrame = 5; // Количество чанков для пересборки за кадр
        [SerializeField] private int createChunksPerFrame = 2; // Количество чанков для создания за кадр
        
        // Система запекания текстур чанков
        private Coroutine bakeChunksCoroutine = null; // Корутина для асинхронного запекания чанков
        [SerializeField] private int bakeChunksPerFrame = 2; // Количество чанков для запекания за кадр
        [SerializeField] private Material chunkMaterialTemplate; // Шаблон материала для чанков
        private Coroutine memoryCleanupCoroutine = null; // Корутина для периодической очистки памяти
        
        // Кэшированные значения для расчета ширины карты
        private float cachedHexWidth = 0f;
        private float cachedHexHeight = 0f;
        private float cachedHexOffset = 0f;
        private float cachedActualCellSize = 0f;
        private float cachedStartY = 0f; // Кешированное значение startY для оптимизации
        
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
            
            // Очищаем существующие клетки и чанки
            ClearChunks();
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
            cachedStartY = startY; // Кешируем startY
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
                    // float z = gridHeight - row;
                    float z = 0f; // Временно все клетки на одном уровне по Z
                    
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
                        // Устанавливаем ссылку на Grid для оптимизации (избегает FindFirstObjectByType в UpdateMainRendererActualState)
                        cellInfo.SetGrid(this);
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
            
            // Применяем результаты к GameObject'ам
            if (useCoroutines && Application.isPlaying)
            {
                // Используем корутину для распределения работы по кадрам
                StartCoroutine(ApplyCellTypesCoroutine(grid, materialManager));
            }
            else
            {
                // Синхронное применение (для Editor или если корутины отключены)
                ApplyCellTypesSync(grid, materialManager);
            }
        }
        
        /// <summary>
        /// Синхронно применяет типы клеток к GameObject'ам
        /// </summary>
        private void ApplyCellTypesSync(CellType[,] grid, CellMaterialManager materialManager)
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
                        cellInfo.SetManagers(materialManager);
                        // Устанавливаем ссылку на Grid для оптимизации (избегает FindFirstObjectByType в UpdateMainRendererActualState)
                        cellInfo.SetGrid(this);
                        // Отключаем обновление оверлеев при массовом создании для оптимизации
                        cellInfo.SetCellType(grid[col, row], updateOverlays: false);
                    }
                }
            }
            
            // Установка ресурсов и построек происходит автоматически через SetCellType() -> SetFeatureId() / SetBuildingId()
            
            // Создаем чанки после применения типов клеток
            // Игра будет возобновлена и IsGenerationComplete установлен после завершения создания всех чанков
            StartCoroutine(CreateChunksCoroutine());
        }
        
        /// <summary>
        /// Корутина для применения типов клеток с распределением по кадрам
        /// </summary>
        private IEnumerator ApplyCellTypesCoroutine(CellType[,] grid, CellMaterialManager materialManager)
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
                        cellInfo.SetManagers(materialManager);
                        // Устанавливаем ссылку на Grid для оптимизации (избегает FindFirstObjectByType в UpdateMainRendererActualState)
                        cellInfo.SetGrid(this);
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
            
            // Установка ресурсов и построек происходит автоматически через SetCellType() -> SetFeatureId() / SetBuildingId()
            
            // Создаем чанки после применения типов клеток
            // Ждем завершения создания всех чанков перед возобновлением игры
            yield return StartCoroutine(CreateChunksCoroutine());
            
            // Игра будет возобновлена и IsGenerationComplete установлен в CreateChunksCoroutine()
        }
        
        /// <summary>
        /// Корутина для создания чанков с распределением по кадрам
        /// </summary>
        private IEnumerator CreateChunksCoroutine()
        {
            // Вычисляем размер чанка
            int chunkSize = CalculateChunkSize(gridWidth, gridHeight);
            
            // Вычисляем количество чанков по ширине и высоте
            int chunksX = Mathf.CeilToInt((float)gridWidth / chunkSize);
            int chunksY = Mathf.CeilToInt((float)gridHeight / chunkSize);
            
            // Очищаем предыдущие чанки (если есть)
            ClearChunks();
            
            // Вычисляем разрешение текстуры на основе размера чанка
            const int PIXELS_PER_CELL = 128; // ключевая цифра
            int textureResolution = Mathf.NextPowerOfTwo(chunkSize * PIXELS_PER_CELL);
            // ограничение по железу
            textureResolution = Mathf.Clamp(textureResolution, 256, 2048);
            
            // Список объектов чанков для создания
            List<GameObject> chunkObjects = new List<GameObject>();
            
            int processed = 0;
            
            // Группируем клетки по чанкам и создаем объединенные меши
            for (int chunkY = 0; chunkY < chunksY; chunkY++)
            {
                for (int chunkX = 0; chunkX < chunksX; chunkX++)
                {
                    List<GameObject> cellsInChunk = new List<GameObject>();
                    
                    // Собираем все клетки, принадлежащие этому чанку
                    int startRow = chunkY * chunkSize;
                    int endRow = Mathf.Min(startRow + chunkSize, gridHeight);
                    int startCol = chunkX * chunkSize;
                    int endCol = Mathf.Min(startCol + chunkSize, gridWidth);
                    
                    for (int row = startRow; row < endRow; row++)
                    {
                        for (int col = startCol; col < endCol; col++)
                        {
                            GameObject cell = GetCellAt(col, row);
                            if (cell != null)
                            {
                                cellsInChunk.Add(cell);
                            }
                        }
                    }
                    
                    if (cellsInChunk.Count == 0)
                        continue;
                    
                    // Создаем GameObject для чанка
                    GameObject chunkObject = new GameObject($"Chunk_{chunkX}_{chunkY}");
                    chunkObject.transform.SetParent(transform);
                    chunkObject.transform.localPosition = Vector3.zero; // Важно: позиция (0,0,0) относительно родителя
                    
                    // Объединяем меши клеток чанка и создаем текстуру
                    CellMeshCombiner.CombineResult result = CellMeshCombiner.CombineCellMeshesWithTexture(cellsInChunk, textureResolution);
                    
                    Debug.Log($"Chunk build: tex null? {result.chunkTexture == null}, mesh null? {result.mesh == null}, matTemplate null? {chunkMaterialTemplate == null}");
                    
                    if (result.mesh != null && result.chunkTexture != null)
                    {
                        // Добавляем MeshFilter и MeshRenderer к чанку
                        MeshFilter chunkMeshFilter = chunkObject.AddComponent<MeshFilter>();
                        chunkMeshFilter.sharedMesh = result.mesh;
                        
                        MeshRenderer chunkRenderer = chunkObject.AddComponent<MeshRenderer>();
                        
                        if (chunkMaterialTemplate != null)
                        {
                            // Используем общий материал для всех чанков (не создаем новый экземпляр)
                            chunkRenderer.sharedMaterial = chunkMaterialTemplate;
                            
                            // Настраиваем текстуру для правильного отображения
                            result.chunkTexture.wrapMode = TextureWrapMode.Clamp;
                            
                            // Используем MaterialPropertyBlock для установки текстуры без создания нового материала
                            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                            propertyBlock.SetTexture("_BaseMap", result.chunkTexture);
                            propertyBlock.SetTexture("_MainTex", result.chunkTexture); // Для совместимости
                            chunkRenderer.SetPropertyBlock(propertyBlock);
                            
                            chunkRenderer.enabled = true;
                        }
                        else
                        {
                            Debug.LogError("Grid: chunkMaterialTemplate не назначен! Назначьте шаблон материала в инспекторе.");
                        }
                        
                        // Добавляем компонент CellChunk и инициализируем его
                        CellChunk chunk = chunkObject.AddComponent<CellChunk>();
                        chunk.Initialize(cellsInChunk, chunkRenderer, chunkMeshFilter);
                        
                        // Сохраняем ссылку на чанк
                        Vector2Int chunkCoord = new Vector2Int(chunkX, chunkY);
                        chunks.Add(chunk);
                        chunkByChunkCoord[chunkCoord] = chunk;
                        
                        // Добавляем в список для отслеживания
                        chunkObjects.Add(chunkObject);
                    }
                    else
                    {
                        Debug.LogWarning($"Не удалось создать меш и текстуру для чанка ({chunkX}, {chunkY})");
                        // Удаляем GameObject чанка, если не удалось создать
                        if (Application.isPlaying) Destroy(chunkObject);
                        else DestroyImmediate(chunkObject);
                    }
                    
                    processed++;
                    
                    // Каждые createChunksPerFrame чанков делаем паузу на один кадр
                    if (processed >= createChunksPerFrame)
                    {
                        processed = 0;
                        // Используем WaitForEndOfFrame для работы при timeScale = 0
                        yield return new WaitForEndOfFrame();
                    }
                }
            }
            
            // Отключаем основной MeshRenderer на всех клетках (рендеринг через чанки)
            foreach (GameObject cell in cells)
            {
                CellInfo cellInfo = cell.GetComponent<CellInfo>();
                if (cellInfo != null)
                {
                    cellInfo.SetMainRendererState(false); // Отключаем рендеринг через чанк
                }
            }
            
            // Запускаем корутину периодической очистки памяти
            if (memoryCleanupCoroutine == null)
            {
                memoryCleanupCoroutine = StartCoroutine(PeriodicMemoryCleanup());
            }
            
            // Все чанки созданы — считаем генерацию завершённой
            IsGenerationComplete = true;
            
            // Возобновляем игру после завершения создания всех чанков
            ResumeGame();
        }
        
        /// <summary>
        /// Корутина для периодической очистки памяти
        /// </summary>
        private IEnumerator PeriodicMemoryCleanup()
        {
            while (true)
            {
                yield return new WaitForSeconds(30f); // Каждые 30 секунд
                Resources.UnloadUnusedAssets();
            }
        }
        
        /// <summary>
        /// Очищает все чанки (используется при регенерации карты)
        /// </summary>
        private void ClearChunks()
        {
            // Останавливаем корутину пересборки, если она выполняется
            if (rebuildChunksCoroutine != null)
            {
                StopCoroutine(rebuildChunksCoroutine);
                rebuildChunksCoroutine = null;
            }
            
            foreach (CellChunk chunk in chunks)
            {
                if (chunk != null && chunk.gameObject != null)
                {
                    if (Application.isPlaying) Destroy(chunk.gameObject);
                    else DestroyImmediate(chunk.gameObject);
                }
            }
            chunks.Clear();
            chunkByChunkCoord.Clear();
            dirtyChunks.Clear();
        }
        
        /// <summary>
        /// Запускает корутину для пересборки грязных чанков (если есть)
        /// </summary>
        private void StartRebuildDirtyChunks()
        {
            // Останавливаем предыдущую корутину, если она еще выполняется
            if (rebuildChunksCoroutine != null)
            {
                StopCoroutine(rebuildChunksCoroutine);
            }
            
            // Запускаем новую корутину для пересборки
            rebuildChunksCoroutine = StartCoroutine(RebuildDirtyChunksCoroutine());
        }
        
        /// <summary>
        /// Корутина для пересборки грязных чанков с распределением по кадрам
        /// </summary>
        private IEnumerator RebuildDirtyChunksCoroutine()
        {
            List<CellChunk> chunksToRebuild = new List<CellChunk>(dirtyChunks);
            dirtyChunks.Clear();
            
            int processed = 0;
            
            foreach (CellChunk chunk in chunksToRebuild)
            {
                if (chunk == null || !chunk.IsDirty())
                    continue;
                
                // Пересобираем меш чанка с текстурой
                // Вычисляем разрешение текстуры на основе размера чанка
                int chunkSize = CalculateChunkSize(gridWidth, gridHeight);
                const int PIXELS_PER_CELL = 128; // ключевая цифра
                int textureResolution = Mathf.NextPowerOfTwo(chunkSize * PIXELS_PER_CELL);
                // ограничение по железу
                textureResolution = Mathf.Clamp(textureResolution, 256, 2048);
                chunk.RebuildMesh(textureResolution);
                processed++;
                
                // Каждые chunksPerFrame чанков делаем паузу на один кадр
                if (processed >= chunksPerFrame)
                {
                    processed = 0;
                    yield return null; // Ждем один кадр
                }
            }
            
            rebuildChunksCoroutine = null;
        }
        
        /// <summary>
        /// Помечает чанк как "грязный" и запускает пересборку (вызывается из CellInfo при изменении типа)
        /// </summary>
        public void MarkChunkDirty(CellChunk chunk)
        {
            if (chunk != null && chunk.IsDirty())
            {
                dirtyChunks.Add(chunk);
                
                // Запускаем пересборку, если она еще не запущена
                if (rebuildChunksCoroutine == null)
                {
                    StartRebuildDirtyChunks();
                }
            }
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
        
        /// <summary>
        /// Получает количество чанков (для тестирования и отладки)
        /// </summary>
        public int GetChunkCount()
        {
            return chunks.Count;
        }
        
        private void ClearGrid()
        {
            foreach (GameObject cell in cells)
            {
                if (cell != null)
                {
                    if (Application.isPlaying) Destroy(cell);
                    else DestroyImmediate(cell);
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
        /// Получает горизонтальное расстояние между центрами клеток (hexWidth)
        /// </summary>
        public float GetHexWidth()
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
            }
            
            return cachedHexWidth;
        }
        
        /// <summary>
        /// Получает вертикальное расстояние между центрами клеток (hexHeight)
        /// </summary>
        public float GetHexHeight()
        {
            // Если значения еще не кэшированы, вычисляем их
            if (cachedHexHeight <= 0f)
            {
                Renderer prefabRenderer = cellPrefab != null ? cellPrefab.GetComponent<Renderer>() : null;
                float actualCellSize = cellSize;
                
                if (prefabRenderer != null && cellSize <= 0.1f)
                {
                    actualCellSize = Mathf.Max(prefabRenderer.bounds.size.x, prefabRenderer.bounds.size.y) * cellSize;
                }
                
                cachedHexHeight = actualCellSize * 1.5f + pixelGap;
            }
            
            return cachedHexHeight;
        }
        
        /// <summary>
        /// Получает смещение для нечетных строк (hexOffset)
        /// </summary>
        public float GetHexOffset()
        {
            // Если значения еще не кэшированы, вычисляем их
            if (cachedHexOffset <= 0f)
            {
                Renderer prefabRenderer = cellPrefab != null ? cellPrefab.GetComponent<Renderer>() : null;
                float actualCellSize = cellSize;
                
                if (prefabRenderer != null && cellSize <= 0.1f)
                {
                    actualCellSize = Mathf.Max(prefabRenderer.bounds.size.x, prefabRenderer.bounds.size.y) * cellSize;
                }
                
                cachedHexOffset = (actualCellSize * 1.732f + pixelGap) * 0.5f;
            }
            
            return cachedHexOffset;
        }
        
        /// <summary>
        /// Получает начальную Y координату (позиция верхней строки)
        /// </summary>
        public float GetStartY()
        {
            // Используем кешированное значение, если оно доступно
            if (cachedStartY != 0f)
            {
                return cachedStartY;
            }
            // Иначе вычисляем (для обратной совместимости, если кеш еще не инициализирован)
            return (gridHeight - 1) * GetHexHeight();
        }
        
        /// <summary>
        /// Вычисляет адаптивный размер чанка на основе размера карты (целевое количество ~500 чанков)
        /// </summary>
        /// <param name="gridWidth">Ширина сетки</param>
        /// <param name="gridHeight">Высота сетки</param>
        /// <returns>Размер чанка (количество клеток по ширине и высоте)</returns>
        private int CalculateChunkSize(int gridWidth, int gridHeight)
        {
            const int TARGET_CHUNK_COUNT = 500;
            const int MIN_CHUNK_SIZE = 4;
            
            int totalCells = gridWidth * gridHeight;
            float cellsPerChunk = (float)totalCells / TARGET_CHUNK_COUNT;
            int chunkSize = Mathf.Max(MIN_CHUNK_SIZE, Mathf.RoundToInt(Mathf.Sqrt(cellsPerChunk)));
            return chunkSize;
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

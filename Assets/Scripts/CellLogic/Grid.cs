using UnityEngine;
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
        
        private List<GameObject> cells = new List<GameObject>();
        
        void Start()
        {
            GenerateGrid();
        }
        
        private void GenerateGrid()
        {
            if (cellPrefab == null)
            {
                Debug.LogError("Cell Prefab не назначен!");
                return;
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
            
            Debug.Log($"Генерация сетки: {gridWidth}x{gridHeight}, размер клетки: {actualCellSize}, расстояние: {hexWidth}x{hexHeight}");
            
            // Сначала создаем все клетки как field
            for (int row = 0; row < gridHeight; row++)
            {
                for (int col = 0; col < gridWidth; col++)
                {
                    // Смещение для нечетных строк для правильной гексагональной укладки
                    float offsetX = (row % 2 == 0) ? 0f : hexOffset;
                    float x = col * hexWidth + offsetX;
                    float y = row * hexHeight;
                    
                    // Для 2D используем X и Y, Z оставляем 0
                    Vector3 position = new Vector3(x, y, 0f);
                    // Поворот префаба на 180 градусов по оси Y
                    Quaternion rotation = Quaternion.Euler(0f, 180f, 0f);
                    GameObject cell = Instantiate(cellPrefab, position, rotation, transform);
                    // Применяем масштаб на основе cellSize
                    cell.transform.localScale = cell.transform.localScale * cellSize;
                    cell.name = $"Cell_{row}_{col}";
                    
                    // Все клетки сначала field
                    CellInfo cellInfo = cell.GetComponent<CellInfo>();
                    if (cellInfo != null)
                    {
                        cellInfo.Initialize(col, row, CellType.field);
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
                    grid[col, row] = CellType.field;
                }
            }
            
            // Генерируем водоемы
            int actualWaterSeed = waterSeed == 0 ? Random.Range(1, 1000000) : waterSeed;
            WaterBodyGenerator.GenerateWaterBodies(grid, gridWidth, gridHeight, 
                waterFrequency, waterFragmentation, actualWaterSeed,
                convertShallowOnlyToDeep, convertShallowNearDeepToDeep, shallowToDeepChance);
            Debug.Log("Водоемы сгенерированы");
            
            // Генерируем горы (не перекрывая водоемы)
            int actualMountainSeed = mountainSeed == 0 ? Random.Range(1, 1000000) : mountainSeed;
            TerrainGenerator.GenerateMountains(grid, gridWidth, gridHeight,
                mountainFrequency, mountainFragmentation, actualMountainSeed);
            Debug.Log("Горы сгенерированы");
            
            // Генерируем леса (не перекрывая водоемы)
            int actualForestSeed = forestSeed == 0 ? Random.Range(1, 1000000) : forestSeed;
            TerrainGenerator.GenerateForests(grid, gridWidth, gridHeight,
                forestFrequency, forestFragmentation, actualForestSeed);
            Debug.Log("Леса сгенерированы");
            
            // Генерируем пустыни (не перекрывая водоемы)
            int actualDesertSeed = desertSeed == 0 ? Random.Range(1, 1000000) : desertSeed;
            TerrainGenerator.GenerateDeserts(grid, gridWidth, gridHeight,
                desertFrequency, desertFragmentation, actualDesertSeed);
            Debug.Log("Пустыни сгенерированы");
            
            // Применяем логику совместимости
            TerrainCompatibility.ApplyCompatibilityRules(grid, gridWidth, gridHeight);
            Debug.Log("Логика совместимости применена");
            
            // Применяем результаты к GameObject'ам
            for (int row = 0; row < gridHeight; row++)
            {
                for (int col = 0; col < gridWidth; col++)
                {
                    GameObject cell = cells[row * gridWidth + col];
                    CellInfo cellInfo = cell.GetComponent<CellInfo>();
                    if (cellInfo != null)
                    {
                        cellInfo.SetCellType(grid[col, row]);
                    }
                }
            }
            
            Debug.Log($"Создано клеток: {cells.Count}");
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
    }
}

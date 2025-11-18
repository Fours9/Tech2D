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
        [SerializeField] private int cellularAutomataIterations = 5; // Количество итераций Cellular Automata для корректировки типов
        
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
            
            // Сначала создаем все клетки с начальными типами
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
                    
                    // Определяем тип клетки через шум Перлина и инициализируем CellInfo
                    CellType cellType = PerlinNoise.GetCellType(col, row);
                    CellInfo cellInfo = cell.GetComponent<CellInfo>();
                    if (cellInfo != null)
                    {
                        cellInfo.Initialize(col, row, cellType);
                    }
                    else
                    {
                        Debug.LogWarning($"CellInfo не найден на клетке {cell.name}");
                    }
                    
                    cells.Add(cell);
                }
            }
            
            // Применяем Cellular Automata для корректировки типов с учетом совместимости
            ApplyCellularAutomata();
            
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
        }
        
        // Метод для пересоздания сетки (можно вызвать из инспектора или кода)
        [ContextMenu("Regenerate Grid")]
        public void RegenerateGrid()
        {
            GenerateGrid();
        }
        
        /// <summary>
        /// Применяет Cellular Automata для корректировки типов клеток с учетом правил совместимости
        /// </summary>
        private void ApplyCellularAutomata()
        {
            // Создаем массив типов из GameObject'ов
            CellType[,] grid = new CellType[gridWidth, gridHeight];
            for (int row = 0; row < gridHeight; row++)
            {
                for (int col = 0; col < gridWidth; col++)
                {
                    GameObject cell = cells[row * gridWidth + col];
                    CellInfo cellInfo = cell.GetComponent<CellInfo>();
                    if (cellInfo != null)
                    {
                        grid[col, row] = cellInfo.GetCellType();
                    }
                }
            }
            
            // Применяем Cellular Automata
            CellularAutomata.ApplyCellularAutomata(grid, gridWidth, gridHeight, cellularAutomataIterations);
            
            // Применяем результаты обратно к GameObject'ам
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
        }
    }
}

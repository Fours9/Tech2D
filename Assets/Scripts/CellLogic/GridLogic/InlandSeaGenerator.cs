using UnityEngine;
using System.Collections.Generic;

namespace CellNameSpace
{
    /// <summary>
    /// Класс для генерации внутренних морей на карте
    /// </summary>
    public static class InlandSeaGenerator
    {
        /// <summary>
        /// Генерирует внутренние моря на суше
        /// </summary>
        /// <param name="grid">Сетка типов клеток</param>
        /// <param name="gridWidth">Ширина сетки</param>
        /// <param name="gridHeight">Высота сетки</param>
        /// <param name="inlandSeaFrequency">Частота появления внутренних морей (0-1)</param>
        /// <param name="inlandSeaMinSize">Минимальный размер внутреннего моря в клетках</param>
        /// <param name="inlandSeaSeed">Сид для генерации внутренних морей</param>
        public static void GenerateInlandSeas(CellType[,] grid, int gridWidth, int gridHeight,
            float inlandSeaFrequency, int inlandSeaMinSize, int inlandSeaSeed,
            float inlandSeaMaxPercentage, float landNeighborThreshold)
        {
            // Сохраняем состояние Random
            Random.State oldState = Random.state;
            Random.InitState(inlandSeaSeed);
            
            // Генерируем offset для шума
            float offsetX = Random.Range(-10000f, 10000f);
            float offsetY = Random.Range(-10000f, 10000f);
            
            // Масштаб шума для внутренних морей (больше, чем для озер, чтобы создавать большие области)
            float scale = 0.05f;
            
            float clampedMaxPercentage = Mathf.Clamp01(inlandSeaMaxPercentage);
            float clampedNeighborThreshold = Mathf.Clamp01(landNeighborThreshold);
            int maxSeaSize = Mathf.Max(1, Mathf.RoundToInt(gridWidth * gridHeight * clampedMaxPercentage));
            
            // Массив для отслеживания обработанных клеток
            bool[,] processed = new bool[gridWidth, gridHeight];
            
            // Проходим по всем клеткам суши
            for (int row = 0; row < gridHeight; row++)
            {
                for (int col = 0; col < gridWidth; col++)
                {
                    // Пропускаем уже обработанные клетки
                    if (processed[col, row])
                        continue;
                    
                    // Работаем только с сушей (field)
                    if (grid[col, row] == CellType.field)
                    {
                        // Используем шум для определения вероятности внутреннего моря
                        float noiseValue = Mathf.PerlinNoise((col + offsetX) * scale, (row + offsetY) * scale);
                        
                        // Если значение шума меньше порога частоты, создаем внутреннее море
                        if (noiseValue < inlandSeaFrequency)
                        {
                            // Находим все связанные клетки суши (flood fill)
                            List<Vector2Int> seaCells = FloodFillLand(grid, gridWidth, gridHeight, col, row, processed);
                            
                            // Проверяем минимальный размер (внутренние моря должны быть большими)
                            if (seaCells.Count >= inlandSeaMinSize)
                            {
                                if (seaCells.Count <= maxSeaSize)
                                {
                                    // Проверяем, что область хотя бы частично окружена сушей
                                    // (не должна быть на самом краю большой области суши)
                                    if (HasLandNeighbors(grid, gridWidth, gridHeight, seaCells, clampedNeighborThreshold))
                                    {
                                        // Заполняем область shallow (внутреннее море)
                                        foreach (Vector2Int cell in seaCells)
                                        {
                                            grid[cell.x, cell.y] = CellType.shallow;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            // Восстанавливаем состояние Random
            Random.state = oldState;
        }
        
        /// <summary>
        /// Flood fill для поиска всех связанных клеток суши
        /// </summary>
        private static List<Vector2Int> FloodFillLand(CellType[,] grid, int gridWidth, int gridHeight,
            int startX, int startY, bool[,] processed)
        {
            List<Vector2Int> result = new List<Vector2Int>();
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
            
            Vector2Int start = new Vector2Int(startX, startY);
            queue.Enqueue(start);
            visited.Add(start);
            
            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                result.Add(current);
                processed[current.x, current.y] = true;
                
                // Проверяем соседей
                List<Vector2Int> neighbors = HexagonalGridHelper.GetNeighbors(current.x, current.y, gridWidth, gridHeight);
                foreach (Vector2Int neighbor in neighbors)
                {
                    if (!visited.Contains(neighbor) && grid[neighbor.x, neighbor.y] == CellType.field)
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Проверяет, что область имеет достаточную долю соседей-суши (не находится на краю огромной области)
        /// </summary>
        private static bool HasLandNeighbors(CellType[,] grid, int gridWidth, int gridHeight, List<Vector2Int> cells, float landNeighborThreshold)
        {
            HashSet<Vector2Int> cellSet = new HashSet<Vector2Int>(cells);
            int landNeighborCount = 0;
            int totalNeighborCount = 0;
            
            foreach (Vector2Int cell in cells)
            {
                List<Vector2Int> neighbors = HexagonalGridHelper.GetNeighbors(cell.x, cell.y, gridWidth, gridHeight);
                foreach (Vector2Int neighbor in neighbors)
                {
                    if (!cellSet.Contains(neighbor))
                    {
                        totalNeighborCount++;
                        CellType neighborType = grid[neighbor.x, neighbor.y];
                        if (neighborType == CellType.field)
                        {
                            landNeighborCount++;
                        }
                    }
                }
            }
            
            if (totalNeighborCount == 0) return false;
            return (float)landNeighborCount / totalNeighborCount >= landNeighborThreshold;
        }
    }
}


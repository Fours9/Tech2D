using UnityEngine;
using System.Collections.Generic;

namespace CellNameSpace
{
    /// <summary>
    /// Класс для генерации озер на карте
    /// </summary>
    public static class LakeGenerator
    {
        /// <summary>
        /// Генерирует озера на суше
        /// </summary>
        /// <param name="grid">Сетка типов клеток</param>
        /// <param name="gridWidth">Ширина сетки</param>
        /// <param name="gridHeight">Высота сетки</param>
        /// <param name="lakeFrequency">Частота появления озер (0-1)</param>
        /// <param name="lakeMinSize">Минимальный размер озера в клетках</param>
        /// <param name="lakeSeed">Сид для генерации озер</param>
        public static void GenerateLakes(CellType[,] grid, int gridWidth, int gridHeight,
            float lakeFrequency, int lakeMinSize, int lakeSeed, float lakeMaxPercentage)
        {
            // Сохраняем состояние Random
            Random.State oldState = Random.state;
            Random.InitState(lakeSeed);
            
            // Генерируем offset для шума
            float offsetX = Random.Range(-10000f, 10000f);
            float offsetY = Random.Range(-10000f, 10000f);
            
            // Масштаб шума для озер
            float scale = 0.08f;
            
            // Нормализуем максимально допустимую долю (чтобы не было 0 или >1)
            float clampedMaxPercentage = Mathf.Clamp01(lakeMaxPercentage);
            int maxLakeSize = Mathf.Max(1, Mathf.RoundToInt(gridWidth * gridHeight * clampedMaxPercentage));
            
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
                        // Используем шум для определения вероятности озера
                        float noiseValue = Mathf.PerlinNoise((col + offsetX) * scale, (row + offsetY) * scale);
                        
                        // Если значение шума меньше порога частоты, создаем озеро
                        if (noiseValue < lakeFrequency)
                        {
                            // Находим все связанные клетки суши (flood fill)
                            List<Vector2Int> lakeCells = FloodFillLand(grid, gridWidth, gridHeight, col, row, processed);
                            
                            // Проверяем, что область окружена сушей (не касается океана)
                            if (IsSurroundedByLand(grid, gridWidth, gridHeight, lakeCells))
                            {
                                // Проверяем минимальный размер
                                if (lakeCells.Count >= lakeMinSize)
                                {
                                    if (lakeCells.Count <= maxLakeSize)
                                    {
                                        // Заполняем область shallow (озеро)
                                        foreach (Vector2Int cell in lakeCells)
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
        /// Проверяет, что область окружена сушей (не касается океана)
        /// </summary>
        private static bool IsSurroundedByLand(CellType[,] grid, int gridWidth, int gridHeight, List<Vector2Int> cells)
        {
            HashSet<Vector2Int> cellSet = new HashSet<Vector2Int>(cells);
            
            foreach (Vector2Int cell in cells)
            {
                List<Vector2Int> neighbors = HexagonalGridHelper.GetNeighbors(cell.x, cell.y, gridWidth, gridHeight);
                foreach (Vector2Int neighbor in neighbors)
                {
                    // Если сосед не в области озера и это вода (shallow или deep_water), значит область касается океана
                    if (!cellSet.Contains(neighbor))
                    {
                        CellType neighborType = grid[neighbor.x, neighbor.y];
                        if (neighborType == CellType.shallow || neighborType == CellType.deep_water)
                        {
                            return false;
                        }
                    }
                }
            }
            
            return true;
        }
    }
}


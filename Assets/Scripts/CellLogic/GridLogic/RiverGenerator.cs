using UnityEngine;
using System.Collections.Generic;

namespace CellNameSpace
{
    /// <summary>
    /// Класс для генерации рек на карте
    /// </summary>
    public static class RiverGenerator
    {
        /// <summary>
        /// Генерирует реки от озер к ближайшей воде
        /// </summary>
        /// <param name="grid">Сетка типов клеток</param>
        /// <param name="gridWidth">Ширина сетки</param>
        /// <param name="gridHeight">Высота сетки</param>
        /// <param name="riverChance">Шанс генерации реки от озера (0-1)</param>
        /// <param name="riverSeed">Сид для генерации рек</param>
        public static void GenerateRivers(CellType[,] grid, int gridWidth, int gridHeight,
            float riverChance, int riverSeed, float meanderStrength, float meanderNoiseScale)
        {
            // Сохраняем состояние Random
            Random.State oldState = Random.state;
            Random.InitState(riverSeed);
            float noiseOffsetX = Random.Range(0f, 10000f);
            float noiseOffsetY = Random.Range(0f, 10000f);
            
            // Находим все озера (области shallow на суше, не соединенные с океаном)
            List<List<Vector2Int>> lakes = FindLakes(grid, gridWidth, gridHeight);
            
            // Для каждого озера генерируем реки
            foreach (List<Vector2Int> lake in lakes)
            {
                // Определяем количество рек (1-2 случайно)
                int numRivers = Random.Range(1, 3); // 1 или 2
                
                // Генерируем реки с учетом шанса
                for (int i = 0; i < numRivers; i++)
                {
                    if (Random.value < riverChance)
                    {
                        // Находим точку на берегу озера
                        Vector2Int shorePoint = FindShorePoint(grid, gridWidth, gridHeight, lake);
                        
                        if (shorePoint.x >= 0 && shorePoint.y >= 0)
                        {
                            // Находим ближайшую воду (океан или другое озеро)
                            Vector2Int targetWater = FindNearestWater(grid, gridWidth, gridHeight, shorePoint, lake);
                            
                            if (targetWater.x >= 0 && targetWater.y >= 0)
                            {
                                // Прокладываем реку от берега озера к воде
                                List<Vector2Int> riverPath = FindRiverPath(
                                    grid, gridWidth, gridHeight, shorePoint, targetWater,
                                    meanderStrength, meanderNoiseScale, noiseOffsetX, noiseOffsetY);
                                
                                if (riverPath != null && riverPath.Count > 0)
                                {
                                    // Превращаем путь в реку (field -> shallow)
                                    foreach (Vector2Int cell in riverPath)
                                    {
                                        // Превращаем только field в shallow (не трогаем горы и deep_water)
                                        if (grid[cell.x, cell.y] == CellType.field)
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
        /// Находит все озера на карте (области shallow, окруженные сушей)
        /// </summary>
        private static List<List<Vector2Int>> FindLakes(CellType[,] grid, int gridWidth, int gridHeight)
        {
            List<List<Vector2Int>> lakes = new List<List<Vector2Int>>();
            bool[,] processed = new bool[gridWidth, gridHeight];
            
            for (int row = 0; row < gridHeight; row++)
            {
                for (int col = 0; col < gridWidth; col++)
                {
                    if (processed[col, row])
                        continue;
                    
                    // Ищем shallow клетки
                    if (grid[col, row] == CellType.shallow)
                    {
                        // Flood fill для поиска связанной области shallow
                        List<Vector2Int> lakeCells = FloodFillWater(grid, gridWidth, gridHeight, col, row, processed);
                        
                        // Проверяем, что это озеро (окружено сушей, не соединено с океаном)
                        if (IsLake(grid, gridWidth, gridHeight, lakeCells))
                        {
                            lakes.Add(lakeCells);
                        }
                    }
                }
            }
            
            return lakes;
        }
        
        /// <summary>
        /// Flood fill для поиска связанных клеток воды
        /// </summary>
        private static List<Vector2Int> FloodFillWater(CellType[,] grid, int gridWidth, int gridHeight,
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
                    if (!visited.Contains(neighbor))
                    {
                        CellType neighborType = grid[neighbor.x, neighbor.y];
                        if (neighborType == CellType.shallow || neighborType == CellType.deep_water)
                        {
                            visited.Add(neighbor);
                            queue.Enqueue(neighbor);
                        }
                    }
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Проверяет, что область является озером (не соединена с океаном)
        /// </summary>
        private static bool IsLake(CellType[,] grid, int gridWidth, int gridHeight, List<Vector2Int> waterCells)
        {
            HashSet<Vector2Int> cellSet = new HashSet<Vector2Int>(waterCells);
            
            // Проверяем, есть ли у области соседи, которые являются океаном (deep_water или shallow на границе карты)
            foreach (Vector2Int cell in waterCells)
            {
                List<Vector2Int> neighbors = HexagonalGridHelper.GetNeighbors(cell.x, cell.y, gridWidth, gridHeight);
                foreach (Vector2Int neighbor in neighbors)
                {
                    if (!cellSet.Contains(neighbor))
                    {
                        CellType neighborType = grid[neighbor.x, neighbor.y];
                        // Если сосед - deep_water, это океан, не озеро
                        if (neighborType == CellType.deep_water)
                        {
                            return false;
                        }
                    }
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Находит точку на берегу озера (shallow клетка с соседом field)
        /// </summary>
        private static Vector2Int FindShorePoint(CellType[,] grid, int gridWidth, int gridHeight, List<Vector2Int> lake)
        {
            HashSet<Vector2Int> lakeSet = new HashSet<Vector2Int>(lake);
            
            // Ищем shallow клетку с соседом field
            foreach (Vector2Int cell in lake)
            {
                List<Vector2Int> neighbors = HexagonalGridHelper.GetNeighbors(cell.x, cell.y, gridWidth, gridHeight);
                foreach (Vector2Int neighbor in neighbors)
                {
                    if (!lakeSet.Contains(neighbor) && grid[neighbor.x, neighbor.y] == CellType.field)
                    {
                        return cell;
                    }
                }
            }
            
            return new Vector2Int(-1, -1);
        }
        
        /// <summary>
        /// Находит ближайшую воду (океан или другое озеро) от заданной точки
        /// </summary>
        private static Vector2Int FindNearestWater(CellType[,] grid, int gridWidth, int gridHeight,
            Vector2Int start, List<Vector2Int> excludeLake)
        {
            HashSet<Vector2Int> excludeSet = new HashSet<Vector2Int>(excludeLake);
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
            
            queue.Enqueue(start);
            visited.Add(start);
            
            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                
                // Проверяем соседей
                List<Vector2Int> neighbors = HexagonalGridHelper.GetNeighbors(current.x, current.y, gridWidth, gridHeight);
                foreach (Vector2Int neighbor in neighbors)
                {
                    if (visited.Contains(neighbor))
                        continue;
                    
                    visited.Add(neighbor);
                    
                    CellType neighborType = grid[neighbor.x, neighbor.y];
                    
                    // Если это вода и не из исходного озера - нашли цель
                    if ((neighborType == CellType.shallow || neighborType == CellType.deep_water) &&
                        !excludeSet.Contains(neighbor))
                    {
                        return neighbor;
                    }
                    
                    // Если это суша, добавляем в очередь для дальнейшего поиска
                    if (neighborType == CellType.field)
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }
            
            return new Vector2Int(-1, -1);
        }
        
        /// <summary>
        /// Находит путь для реки от стартовой точки к целевой воде (упрощенный A*)
        /// </summary>
        private static List<Vector2Int> FindRiverPath(CellType[,] grid, int gridWidth, int gridHeight,
            Vector2Int start, Vector2Int target, float meanderStrength, float meanderNoiseScale,
            float noiseOffsetX, float noiseOffsetY)
        {
            float heuristicWeight = Mathf.Clamp(1f - meanderStrength * 0.5f, 0.2f, 1f);
            float noiseScale = Mathf.Max(0f, meanderNoiseScale);
            float meander = Mathf.Max(0f, meanderStrength);
            
            // Упрощенный A* алгоритм
            Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            Dictionary<Vector2Int, float> gScore = new Dictionary<Vector2Int, float>();
            Dictionary<Vector2Int, float> fScore = new Dictionary<Vector2Int, float>();
            
            List<Vector2Int> openSet = new List<Vector2Int>();
            HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();
            
            gScore[start] = 0;
            fScore[start] = HeuristicDistance(start, target) * heuristicWeight;
            openSet.Add(start);
            
            while (openSet.Count > 0)
            {
                // Находим узел с минимальным fScore
                Vector2Int current = openSet[0];
                float minF = fScore.ContainsKey(current) ? fScore[current] : float.MaxValue;
                foreach (Vector2Int node in openSet)
                {
                    float f = fScore.ContainsKey(node) ? fScore[node] : float.MaxValue;
                    if (f < minF)
                    {
                        minF = f;
                        current = node;
                    }
                }
                
                if (current.x == target.x && current.y == target.y)
                {
                    // Восстанавливаем путь
                    List<Vector2Int> path = new List<Vector2Int>();
                    Vector2Int node = current;
                    while (cameFrom.ContainsKey(node))
                    {
                        path.Add(node);
                        node = cameFrom[node];
                    }
                    path.Add(start);
                    path.Reverse();
                    return path;
                }
                
                openSet.Remove(current);
                closedSet.Add(current);
                
                // Проверяем соседей
                List<Vector2Int> neighbors = HexagonalGridHelper.GetNeighbors(current.x, current.y, gridWidth, gridHeight);
                foreach (Vector2Int neighbor in neighbors)
                {
                    if (closedSet.Contains(neighbor))
                        continue;
                    
                    // Река может проходить только по field (суше)
                    // Не может проходить через горы или deep_water
                    CellType neighborType = grid[neighbor.x, neighbor.y];
                    if (neighborType != CellType.field && neighborType != CellType.shallow && neighborType != CellType.deep_water)
                        continue;
                    
                    // Если это целевая вода, разрешаем
                    if (neighbor.x == target.x && neighbor.y == target.y)
                    {
                        // OK
                    }
                    // Если это не суша и не целевая вода, пропускаем
                    else if (neighborType != CellType.field)
                    {
                        continue;
                    }
                    
                    float baseCost = (gScore.ContainsKey(current) ? gScore[current] : float.MaxValue);
                    float noiseCost = 0f;
                    if (noiseScale > 0f && meander > 0f)
                    {
                        float n = Mathf.PerlinNoise((neighbor.x + noiseOffsetX) * noiseScale, (neighbor.y + noiseOffsetY) * noiseScale);
                        noiseCost = (1f - n) * meander; // ниже шум -> выше стоимость, чтобы избегать прямых путей
                    }
                    
                    float tentativeGScore = baseCost + 1f + noiseCost;
                    
                    if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeGScore;
                        fScore[neighbor] = tentativeGScore + HeuristicDistance(neighbor, target) * heuristicWeight;
                        
                        if (!openSet.Contains(neighbor))
                        {
                            openSet.Add(neighbor);
                        }
                    }
                }
            }
            
            return null; // Путь не найден
        }
        
        /// <summary>
        /// Эвристическая функция для оценки расстояния (манхэттенское расстояние)
        /// </summary>
        private static float HeuristicDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }
    }
}




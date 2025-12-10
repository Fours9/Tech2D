using UnityEngine;
using System.Collections.Generic;

namespace CellNameSpace
{
    /// <summary>
    /// Класс для генерации диаграмм Вороного (Voronoi) на гексагональной сетке
    /// Используется для разделения карты на регионы/континенты
    /// </summary>
    public static class VoronoiGenerator
    {
        /// <summary>
        /// Генерирует диаграмму Вороного и возвращает массив регионов
        /// Каждая клетка получает ID региона (индекс ближайшего центра)
        /// </summary>
        /// <param name="gridWidth">Ширина сетки</param>
        /// <param name="gridHeight">Высота сетки</param>
        /// <param name="numRegions">Количество регионов (центров Voronoi)</param>
        /// <param name="seed">Сид для генерации центров</param>
        /// <param name="hexWidth">Горизонтальное расстояние между центрами гексагонов</param>
        /// <param name="hexHeight">Вертикальное расстояние между центрами гексагонов</param>
        /// <param name="hexOffset">Смещение для нечетных строк</param>
        /// <returns>Массив регионов: grid[x, y] = ID региона (0..numRegions-1)</returns>
        public static int[,] GenerateVoronoiRegions(int gridWidth, int gridHeight, int numRegions, int seed,
            float hexWidth, float hexHeight, float hexOffset)
        {
            // Сохраняем состояние Random
            Random.State oldState = Random.state;
            Random.InitState(seed);
            
            // Генерируем центры регионов (seeds)
            List<Vector2> regionCenters = new List<Vector2>();
            for (int i = 0; i < numRegions; i++)
            {
                // Генерируем случайные координаты в пределах сетки
                float x = Random.Range(0f, gridWidth * hexWidth);
                float y = Random.Range(0f, gridHeight * hexHeight);
                regionCenters.Add(new Vector2(x, y));
            }
            
            // Восстанавливаем состояние Random
            Random.state = oldState;
            
            // Создаем массив регионов
            int[,] regions = new int[gridWidth, gridHeight];
            
            // Для каждой клетки находим ближайший центр
            float startY = (gridHeight - 1) * hexHeight;
            for (int row = 0; row < gridHeight; row++)
            {
                for (int col = 0; col < gridWidth; col++)
                {
                    // Вычисляем мировые координаты клетки (как в Grid.cs)
                    float offsetX = (row % 2 == 0) ? 0f : hexOffset;
                    float x = col * hexWidth + offsetX;
                    float y = startY - row * hexHeight;
                    
                    Vector2 cellPosition = new Vector2(x, y);
                    
                    // Находим ближайший центр
                    int nearestRegionId = 0;
                    float minDistance = float.MaxValue;
                    
                    for (int i = 0; i < regionCenters.Count; i++)
                    {
                        float distance = Vector2.Distance(cellPosition, regionCenters[i]);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            nearestRegionId = i;
                        }
                    }
                    
                    regions[col, row] = nearestRegionId;
                }
            }
            
            return regions;
        }
        
        /// <summary>
        /// Применяет Voronoi-регионы к генерации карты
        /// Каждый регион может быть либо сушей, либо водой, в зависимости от шума
        /// </summary>
        /// <param name="grid">Сетка типов клеток</param>
        /// <param name="regions">Массив регионов Voronoi</param>
        /// <param name="gridWidth">Ширина сетки</param>
        /// <param name="gridHeight">Высота сетки</param>
        /// <param name="landFrequency">Частота появления суши (0-1)</param>
        /// <param name="seed">Сид для определения типа региона</param>
        public static void ApplyVoronoiToGrid(CellType[,] grid, int[,] regions, int gridWidth, int gridHeight,
            float landFrequency, int seed)
        {
            // Сохраняем состояние Random
            Random.State oldState = Random.state;
            Random.InitState(seed);
            
            // Определяем тип для каждого региона (сушу или воду)
            Dictionary<int, bool> regionIsLand = new Dictionary<int, bool>();
            
            // Собираем все уникальные ID регионов, которые реально существуют на карте
            HashSet<int> existingRegions = new HashSet<int>();
            for (int row = 0; row < gridHeight; row++)
            {
                for (int col = 0; col < gridWidth; col++)
                {
                    existingRegions.Add(regions[col, row]);
                }
            }
            
            // Для каждого существующего региона определяем, будет ли он сушей
            List<int> regionList = new List<int>(existingRegions);
            int landCount = 0;
            int waterCount = 0;
            
            foreach (int regionId in regionList)
            {
                float randomValue = Random.value; // 0.0 - 1.0
                bool isLand = randomValue < landFrequency;
                regionIsLand[regionId] = isLand;
                
                if (isLand)
                    landCount++;
                else
                    waterCount++;
            }
            
            // Гарантируем минимум 30% регионов суши (если получилось меньше)
            int minLandRegions = Mathf.Max(1, Mathf.RoundToInt(regionList.Count * 0.3f));
            if (landCount < minLandRegions)
            {
                // Превращаем случайные водные регионы в сушу
                List<int> waterRegions = new List<int>();
                foreach (int regionId in regionList)
                {
                    if (!regionIsLand[regionId])
                    {
                        waterRegions.Add(regionId);
                    }
                }
                
                // Перемешиваем список водных регионов
                for (int i = 0; i < waterRegions.Count; i++)
                {
                    int randomIndex = Random.Range(i, waterRegions.Count);
                    int temp = waterRegions[i];
                    waterRegions[i] = waterRegions[randomIndex];
                    waterRegions[randomIndex] = temp;
                }
                
                // Превращаем необходимое количество в сушу
                int needed = minLandRegions - landCount;
                for (int i = 0; i < needed && i < waterRegions.Count; i++)
                {
                    regionIsLand[waterRegions[i]] = true;
                }
            }
            
            // Восстанавливаем состояние Random
            Random.state = oldState;
            
            // Применяем типы регионов к клеткам
            for (int row = 0; row < gridHeight; row++)
            {
                for (int col = 0; col < gridWidth; col++)
                {
                    int regionId = regions[col, row];
                    if (regionIsLand.ContainsKey(regionId) && regionIsLand[regionId])
                    {
                        // Регион суши - делаем клетку field
                        grid[col, row] = CellType.field;
                    }
                    else
                    {
                        // Регион воды - оставляем shallow
                        grid[col, row] = CellType.shallow;
                    }
                }
            }
        }
        
        /// <summary>
        /// Фильтрует слишком маленькие регионы, объединяя их с соседними
        /// </summary>
        /// <param name="regions">Массив регионов Voronoi</param>
        /// <param name="gridWidth">Ширина сетки</param>
        /// <param name="gridHeight">Высота сетки</param>
        /// <param name="minRegionSize">Минимальный размер региона в клетках</param>
        public static void FilterSmallRegions(int[,] regions, int gridWidth, int gridHeight, int minRegionSize)
        {
            // Подсчитываем размер каждого региона
            Dictionary<int, int> regionSizes = new Dictionary<int, int>();
            
            for (int row = 0; row < gridHeight; row++)
            {
                for (int col = 0; col < gridWidth; col++)
                {
                    int regionId = regions[col, row];
                    if (!regionSizes.ContainsKey(regionId))
                    {
                        regionSizes[regionId] = 0;
                    }
                    regionSizes[regionId]++;
                }
            }
            
            // Находим маленькие регионы и объединяем их с соседними
            foreach (var kvp in regionSizes)
            {
                int regionId = kvp.Key;
                int size = kvp.Value;
                
                if (size < minRegionSize)
                {
                    // Находим самый большой соседний регион
                    int largestNeighborRegion = FindLargestNeighborRegion(regions, gridWidth, gridHeight, regionId, regionSizes);
                    
                    if (largestNeighborRegion >= 0)
                    {
                        // Заменяем маленький регион на соседний
                        for (int row = 0; row < gridHeight; row++)
                        {
                            for (int col = 0; col < gridWidth; col++)
                            {
                                if (regions[col, row] == regionId)
                                {
                                    regions[col, row] = largestNeighborRegion;
                                }
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Находит самый большой соседний регион для заданного региона
        /// </summary>
        private static int FindLargestNeighborRegion(int[,] regions, int gridWidth, int gridHeight,
            int targetRegionId, Dictionary<int, int> regionSizes)
        {
            Dictionary<int, int> neighborSizes = new Dictionary<int, int>();
            
            // Проходим по всем клеткам целевого региона
            for (int row = 0; row < gridHeight; row++)
            {
                for (int col = 0; col < gridWidth; col++)
                {
                    if (regions[col, row] == targetRegionId)
                    {
                        // Проверяем соседей
                        List<Vector2Int> neighbors = HexagonalGridHelper.GetNeighbors(col, row, gridWidth, gridHeight);
                        foreach (Vector2Int neighbor in neighbors)
                        {
                            int neighborRegionId = regions[neighbor.x, neighbor.y];
                            if (neighborRegionId != targetRegionId)
                            {
                                if (!neighborSizes.ContainsKey(neighborRegionId))
                                {
                                    neighborSizes[neighborRegionId] = regionSizes.ContainsKey(neighborRegionId) 
                                        ? regionSizes[neighborRegionId] : 0;
                                }
                            }
                        }
                    }
                }
            }
            
            // Находим самый большой соседний регион
            int largestRegionId = -1;
            int largestSize = 0;
            
            foreach (var kvp in neighborSizes)
            {
                if (kvp.Value > largestSize)
                {
                    largestSize = kvp.Value;
                    largestRegionId = kvp.Key;
                }
            }
            
            return largestRegionId;
        }
    }
}


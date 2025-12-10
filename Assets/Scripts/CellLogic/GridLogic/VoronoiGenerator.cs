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
            
            // Находим максимальный ID региона
            int maxRegionId = 0;
            for (int row = 0; row < gridHeight; row++)
            {
                for (int col = 0; col < gridWidth; col++)
                {
                    if (regions[col, row] > maxRegionId)
                        maxRegionId = regions[col, row];
                }
            }
            
            // Для каждого региона определяем, будет ли он сушей
            for (int regionId = 0; regionId <= maxRegionId; regionId++)
            {
                float randomValue = Random.value; // 0.0 - 1.0
                regionIsLand[regionId] = randomValue < landFrequency;
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
    }
}


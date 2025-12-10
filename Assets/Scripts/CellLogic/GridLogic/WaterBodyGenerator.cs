using UnityEngine;
using System.Collections.Generic;

namespace CellNameSpace
{
    /// <summary>
    /// Класс для генерации водоемов на карте
    /// </summary>
    public static class WaterBodyGenerator
    {
        /// <summary>
        /// Генерирует водоемы на карте
        /// </summary>
        /// <param name="grid">Сетка типов клеток</param>
        /// <param name="gridWidth">Ширина сетки</param>
        /// <param name="gridHeight">Высота сетки</param>
        /// <param name="waterFrequency">Частота появления водоемов (0-1)</param>
        /// <param name="waterFragmentation">Раздробленность водоемов (0-1, чем больше, тем более раздробленные)</param>
        /// <param name="waterSeed">Сид для генерации водоемов</param>
        /// <param name="convertShallowOnlyToDeep">Если true, клетки shallow с соседями только shallow становятся deep_water</param>
        /// <param name="convertShallowNearDeepToDeep">Если true, клетки shallow с соседом deep_water могут стать deep_water</param>
        /// <param name="shallowToDeepChance">Шанс превращения shallow в deep_water при наличии соседа deep_water (0-1)</param>
        /// <param name="processingIterations">Количество итераций обработки воды (сколько раз прогонять все правила по кругу)</param>
        public static void GenerateWaterBodies(CellType[,] grid, int gridWidth, int gridHeight, 
            float waterFrequency, float waterFragmentation, int waterSeed,
            bool convertShallowOnlyToDeep = true, bool convertShallowNearDeepToDeep = true, float shallowToDeepChance = 0.2f, int processingIterations = 3)
        {
            // Сохраняем состояние Random
            Random.State oldState = Random.state;
            Random.InitState(waterSeed);
            
            // Генерируем offset для шума
            float offsetX = Random.Range(-10000f, 10000f);
            float offsetY = Random.Range(-10000f, 10000f);
            
            // Восстанавливаем состояние Random
            Random.state = oldState;
            
            // Масштаб шума (чем больше fragmentation, тем меньше масштаб = более раздробленные водоемы)
            float scale = 0.1f * (1f + waterFragmentation);
            
            // Сначала все водоемы делаем shallow
            for (int row = 0; row < gridHeight; row++)
            {
                for (int col = 0; col < gridWidth; col++)
                {
                    if (grid[col, row] == CellType.field)
                    {
                        float noiseValue = Mathf.PerlinNoise((col + offsetX) * scale, (row + offsetY) * scale);
                        
                        // Если значение шума меньше порога частоты, создаем водоем
                        if (noiseValue < waterFrequency)
                        {
                            grid[col, row] = CellType.shallow;
                        }
                    }
                }
            }
            
            // Теперь применяем логику для определения deep_water и shallow
            ProcessWaterBodies(grid, gridWidth, gridHeight, convertShallowOnlyToDeep, convertShallowNearDeepToDeep, shallowToDeepChance, processingIterations);
        }
        
        /// <summary>
        /// <summary>
        /// Обрабатывает водоемы, определяя какие должны быть shallow, а какие deep_water
        /// Логика выполняется несколько итераций по кругу:
        /// 1. Если контактирует с землей → обязательно shallow
        /// 2. Если shallow имеет соседа, который контактирует с землей → становится shallow
        /// 3. Если у клетки нет соседей, которые контактируют с землей → становится deep_water
        /// 4. Если клетка shallow имеет соседей ТОЛЬКО shallow → становится deep_water (опционально)
        /// 5. Если клетка shallow имеет соседа deep_water → шанс стать deep_water (опционально)
        /// </summary>
        public static void ProcessWaterBodies(CellType[,] grid, int gridWidth, int gridHeight,
            bool convertShallowOnlyToDeep, bool convertShallowNearDeepToDeep, float shallowToDeepChance, int processingIterations)
        {
            // Выполняем все проходы несколько раз по кругу
            for (int iteration = 0; iteration < processingIterations; iteration++)
            {
                // Создаем временный массив для новых типов
                CellType[,] newGrid = new CellType[gridWidth, gridHeight];
                
                // Копируем текущее состояние
                for (int row = 0; row < gridHeight; row++)
                {
                    for (int col = 0; col < gridWidth; col++)
                    {
                        newGrid[col, row] = grid[col, row];
                    }
                }
                
                // Обрабатываем каждую клетку водоема
                for (int row = 0; row < gridHeight; row++)
                {
                    for (int col = 0; col < gridWidth; col++)
                    {
                        CellType currentType = grid[col, row];
                        
                        // Обрабатываем только водоемы (shallow)
                        if (currentType == CellType.shallow || currentType == CellType.deep_water)
                        {
                            List<Vector2Int> neighbors = HexagonalGridHelper.GetNeighbors(col, row, gridWidth, gridHeight);
                            
                            // 1. Проверяем, контактирует ли сама клетка с землей
                            bool contactsLand = false;
                            foreach (Vector2Int neighbor in neighbors)
                            {
                                CellType neighborType = grid[neighbor.x, neighbor.y];
                                if (IsLandType(neighborType))
                                {
                                    contactsLand = true;
                                    break;
                                }
                            }
                            
                            // Если контактирует с землей, обязательно shallow
                            if (contactsLand)
                            {
                                newGrid[col, row] = CellType.shallow;
                                continue;
                            }
                            
                            
                            // 2. Проверяем, есть ли любой сосед (не обязательно shallow), который контактирует с землей
                            bool hasNeighborContactingLand = false;
                            foreach (Vector2Int neighbor in neighbors)
                            {
                                CellType neighborType = grid[neighbor.x, neighbor.y];
                                // Проверяем соседей водоема (shallow или deep_water)
                                if (neighborType == CellType.shallow || neighborType == CellType.deep_water)
                                {
                                    List<Vector2Int> neighborNeighbors = HexagonalGridHelper.GetNeighbors(neighbor.x, neighbor.y, gridWidth, gridHeight);
                                    foreach (Vector2Int neighborNeighbor in neighborNeighbors)
                                    {
                                        if (IsLandType(grid[neighborNeighbor.x, neighborNeighbor.y]))
                                        {
                                            hasNeighborContactingLand = true;
                                            break;
                                        }
                                    }
                                    if (hasNeighborContactingLand)
                                        break;
                                }
                            }
                            
                            // Если есть сосед, который контактирует с землей, становится shallow
                            if (hasNeighborContactingLand)
                            {
                                newGrid[col, row] = CellType.shallow;
                            }
                            else
                            {
                                // 3. Если нет соседей, которые контактируют с землей, становится deep_water
                                newGrid[col, row] = CellType.deep_water;
                            }
                        }
                    }
                }
                
                // Применяем изменения
                for (int row = 0; row < gridHeight; row++)
                {
                    for (int col = 0; col < gridWidth; col++)
                    {
                        grid[col, row] = newGrid[col, row];
                    }
                }
                
                // 4. Условие: Если клетка shallow имеет соседей ТОЛЬКО shallow, то она становится deep_water
                if (convertShallowOnlyToDeep)
                {
                    for (int row = 0; row < gridHeight; row++)
                    {
                        for (int col = 0; col < gridWidth; col++)
                        {
                            if (grid[col, row] == CellType.shallow)
                            {
                                List<Vector2Int> neighbors = HexagonalGridHelper.GetNeighbors(col, row, gridWidth, gridHeight);
                                
                                // Проверяем, что все соседи - только shallow
                                bool allNeighborsAreShallow = true;
                                foreach (Vector2Int neighbor in neighbors)
                                {
                                    CellType neighborType = grid[neighbor.x, neighbor.y];
                                    if (neighborType != CellType.shallow)
                                    {
                                        allNeighborsAreShallow = false;
                                        break;
                                    }
                                }
                                
                                // Если все соседи - только shallow, становится deep_water
                                if (allNeighborsAreShallow && neighbors.Count > 0)
                                {
                                    grid[col, row] = CellType.deep_water;
                                }
                            }
                        }
                    }
                }
                
                // 5. Последнее условие: Если клетка shallow имеет соседа deep_water, то она имеет шанс стать deep_water
                // Шанс НЕ увеличивается с ростом количества соседей deep_water
                if (convertShallowNearDeepToDeep)
                {
                    for (int row = 0; row < gridHeight; row++)
                    {
                        for (int col = 0; col < gridWidth; col++)
                        {
                            if (grid[col, row] == CellType.shallow)
                            {
                                List<Vector2Int> neighbors = HexagonalGridHelper.GetNeighbors(col, row, gridWidth, gridHeight);
                                
                                // Проверяем, есть ли хотя бы один сосед deep_water
                                bool hasDeepWaterNeighbor = false;
                                foreach (Vector2Int neighbor in neighbors)
                                {
                                    if (grid[neighbor.x, neighbor.y] == CellType.deep_water)
                                    {
                                        hasDeepWaterNeighbor = true;
                                        break; // Достаточно одного соседа deep_water
                                    }
                                }
                                
                                // Если есть сосед deep_water, с заданным шансом становится deep_water
                                if (hasDeepWaterNeighbor && Random.Range(0f, 1f) < shallowToDeepChance)
                                {
                                    grid[col, row] = CellType.deep_water;
                                }
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Проверяет, является ли тип клетки типом земли
        /// </summary>
        private static bool IsLandType(CellType type)
        {
            return type == CellType.field || 
                   type == CellType.forest || 
                   type == CellType.desert || 
                   type == CellType.mountain;
        }
    }
}


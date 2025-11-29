using UnityEngine;

namespace CellNameSpace
{
    /// <summary>
    /// Класс для генерации типов местности (land, mountain, forest, desert)
    /// </summary>
    public static class TerrainGenerator
    {
        /// <summary>
        /// Генерирует сушу (field клетки) на карте
        /// </summary>
        public static void GenerateLand(CellType[,] grid, int gridWidth, int gridHeight,
            float landFrequency, float landFragmentation, int landSeed)
        {
            // Сохраняем состояние Random
            Random.State oldState = Random.state;
            Random.InitState(landSeed);
            
            // Генерируем offset для шума
            float offsetX = Random.Range(-10000f, 10000f);
            float offsetY = Random.Range(-10000f, 10000f);
            
            // Восстанавливаем состояние Random
            Random.state = oldState;
            
            // Масштаб шума для суши (меньший базовый масштаб = более кучные области)
            // При fragmentation = 0: scale = 0.05 (очень кучные области)
            // При fragmentation = 1: scale = 0.1 (более раздробленные)
            float scale = 0.05f * (1f + landFragmentation);
            
            // Применяем шум только к клеткам shallow, создавая field (сушу)
            for (int row = 0; row < gridHeight; row++)
            {
                for (int col = 0; col < gridWidth; col++)
                {
                    // Применяем только к shallow (вода), создавая field (сушу)
                    if (grid[col, row] == CellType.shallow)
                    {
                        float noiseValue = Mathf.PerlinNoise((col + offsetX) * scale, (row + offsetY) * scale);
                        
                        // Если значение шума меньше порога частоты, создаем сушу (field)
                        if (noiseValue < landFrequency)
                        {
                            grid[col, row] = CellType.field;
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Генерирует горы на карте
        /// </summary>
        public static void GenerateMountains(CellType[,] grid, int gridWidth, int gridHeight,
            float mountainFrequency, float mountainFragmentation, int mountainSeed)
        {
            GenerateTerrainType(grid, gridWidth, gridHeight, 
                CellType.mountain, mountainFrequency, mountainFragmentation, mountainSeed);
        }
        
        /// <summary>
        /// Генерирует леса на карте
        /// </summary>
        public static void GenerateForests(CellType[,] grid, int gridWidth, int gridHeight,
            float forestFrequency, float forestFragmentation, int forestSeed)
        {
            GenerateTerrainType(grid, gridWidth, gridHeight, 
                CellType.forest, forestFrequency, forestFragmentation, forestSeed);
        }
        
        /// <summary>
        /// Генерирует пустыни на карте
        /// </summary>
        public static void GenerateDeserts(CellType[,] grid, int gridWidth, int gridHeight,
            float desertFrequency, float desertFragmentation, int desertSeed)
        {
            GenerateTerrainType(grid, gridWidth, gridHeight, 
                CellType.desert, desertFrequency, desertFragmentation, desertSeed);
        }
        
        /// <summary>
        /// Общий метод для генерации типа местности
        /// </summary>
        private static void GenerateTerrainType(CellType[,] grid, int gridWidth, int gridHeight,
            CellType terrainType, float frequency, float fragmentation, int seed)
        {
            // Сохраняем состояние Random
            Random.State oldState = Random.state;
            Random.InitState(seed);
            
            // Генерируем offset для шума
            float offsetX = Random.Range(-10000f, 10000f);
            float offsetY = Random.Range(-10000f, 10000f);
            
            // Восстанавливаем состояние Random
            Random.state = oldState;
            
            // Масштаб шума (чем больше fragmentation, тем меньше масштаб = более раздробленные)
            float scale = 0.1f * (1f + fragmentation);
            
            // Применяем шум только к клеткам земли (field), не перекрывая водоемы
            for (int row = 0; row < gridHeight; row++)
            {
                for (int col = 0; col < gridWidth; col++)
                {
                    // Применяем только к field (земля), не трогаем водоемы
                    if (grid[col, row] == CellType.field)
                    {
                        float noiseValue = Mathf.PerlinNoise((col + offsetX) * scale, (row + offsetY) * scale);
                        
                        // Если значение шума меньше порога частоты, создаем тип местности
                        if (noiseValue < frequency)
                        {
                            grid[col, row] = terrainType;
                        }
                    }
                }
            }
        }
    }
}


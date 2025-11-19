using System.Collections.Generic;
using UnityEngine;

namespace CellNameSpace
{
    /// <summary>
    /// Класс для проверки совместимости типов местности
    /// </summary>
    public static class TerrainCompatibility
    {
        /// <summary>
        /// Применяет логику совместимости к сетке
        /// </summary>
        public static void ApplyCompatibilityRules(CellType[,] grid, int gridWidth, int gridHeight)
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
            
            // Первыми просчитываются desert
            for (int row = 0; row < gridHeight; row++)
            {
                for (int col = 0; col < gridWidth; col++)
                {
                    if (grid[col, row] == CellType.desert)
                    {
                        List<Vector2Int> neighbors = HexagonalGridHelper.GetNeighbors(col, row, gridWidth, gridHeight);
                        
                        // Проверяем соседей
                        bool hasForestNeighbor = false;
                        bool hasShallowNeighbor = false;

                        
                        foreach (Vector2Int neighbor in neighbors)
                        {
                            CellType neighborType = grid[neighbor.x, neighbor.y];
                            
                            if (neighborType == CellType.forest)
                                hasForestNeighbor = true;
                            else if (neighborType == CellType.shallow)
                                hasShallowNeighbor = true;
                        }
                        
                        // Если desert соседствует с forest, обязательно становится field
                        if (hasForestNeighbor)
                        {
                            newGrid[col, row] = CellType.field;
                        }
                        // Если desert соседствует с shallow, с шансом 20% становится field
                        else if (hasShallowNeighbor)
                        {
                            if (Random.Range(0f, 1f) < 0.2f)
                            {
                                newGrid[col, row] = CellType.field;
                            }
                        }
                        // Если соседствует с mountain или desert, не изменяется
                        // (уже установлено в newGrid)
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
        }
    }
}


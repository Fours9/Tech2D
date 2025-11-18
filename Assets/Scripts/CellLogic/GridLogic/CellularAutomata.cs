using UnityEngine;
using System.Collections.Generic;

namespace CellNameSpace
{
    /// <summary>
    /// Класс для применения алгоритма Cellular Automata к сетке клеток
    /// </summary>
    public static class CellularAutomata
    {
        /// <summary>
        /// Применяет Cellular Automata для корректировки типов клеток с учетом правил совместимости
        /// </summary>
        /// <param name="grid">Двумерный массив типов клеток</param>
        /// <param name="gridWidth">Ширина сетки</param>
        /// <param name="gridHeight">Высота сетки</param>
        /// <param name="maxIterations">Максимальное количество итераций</param>
        /// <returns>Количество выполненных итераций</returns>
        public static int ApplyCellularAutomata(CellType[,] grid, int gridWidth, int gridHeight, int maxIterations)
        {
            int iterations = 0;
            bool hasChanges = true;
            
            Debug.Log($"Начало применения Cellular Automata (максимум {maxIterations} итераций)");
            
            // Итеративный процесс: на каждой итерации проверяем и корректируем все клетки
            while (iterations < maxIterations && hasChanges)
            {
                hasChanges = false;
                iterations++;
                
                // Создаем временный массив для новых типов (чтобы все изменения применялись одновременно)
                CellType[,] newTypes = new CellType[gridWidth, gridHeight];
                
                // Проходим по всем клеткам и определяем новые типы
                for (int row = 0; row < gridHeight; row++)
                {
                    for (int col = 0; col < gridWidth; col++)
                    {
                        CellType currentType = grid[col, row];
                        
                        // Получаем соседей
                        List<Vector2Int> neighbors = HexagonalGridHelper.GetNeighbors(col, row, gridWidth, gridHeight);
                        List<CellType> neighborTypes = new List<CellType>();
                        
                        // Собираем типы всех соседей
                        foreach (Vector2Int neighbor in neighbors)
                        {
                            neighborTypes.Add(grid[neighbor.x, neighbor.y]);
                        }
                        
                        // Проверяем и корректируем тип
                        CellType correctedType = CellTypeCompatibility.ValidateAndCorrectCellType(currentType, neighborTypes);
                        newTypes[col, row] = correctedType;
                        
                        if (correctedType != currentType)
                        {
                            hasChanges = true;
                        }
                    }
                }
                
                // Применяем все изменения одновременно
                for (int row = 0; row < gridHeight; row++)
                {
                    for (int col = 0; col < gridWidth; col++)
                    {
                        grid[col, row] = newTypes[col, row];
                    }
                }
                
                if (hasChanges)
                {
                    Debug.Log($"Итерация {iterations}: внесены изменения");
                }
                else
                {
                    Debug.Log($"Итерация {iterations}: стабилизация достигнута, процесс завершен");
                }
            }
            
            Debug.Log($"Cellular Automata завершен за {iterations} итераций");
            return iterations;
        }
    }
}


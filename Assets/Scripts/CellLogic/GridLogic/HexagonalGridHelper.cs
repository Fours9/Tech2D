using UnityEngine;
using System.Collections.Generic;

namespace CellNameSpace
{
    /// <summary>
    /// Утилитный класс для работы с гексагональной сеткой
    /// </summary>
    public static class HexagonalGridHelper
    {
        // Кэш для хранения соседей каждой клетки
        // Ключ: строка вида "x_y_gridWidth_gridHeight", значение: список соседей
        private static Dictionary<string, List<Vector2Int>> neighborsCache = new Dictionary<string, List<Vector2Int>>();
        
        // Текущие размеры сетки для кэша
        private static int cachedGridWidth = -1;
        private static int cachedGridHeight = -1;
        
        /// <summary>
        /// Очищает кэш соседей. Вызывать при начале новой генерации сетки
        /// </summary>
        public static void ClearCache()
        {
            neighborsCache.Clear();
            cachedGridWidth = -1;
            cachedGridHeight = -1;
        }
        
        /// <summary>
        /// Инициализирует кэш для всей сетки. Опционально, можно вызвать для предварительного заполнения
        /// </summary>
        public static void InitializeCache(int gridWidth, int gridHeight)
        {
            // Если кэш уже инициализирован для этих размеров, не пересоздаем
            if (cachedGridWidth == gridWidth && cachedGridHeight == gridHeight && neighborsCache.Count > 0)
                return;
            
            ClearCache();
            cachedGridWidth = gridWidth;
            cachedGridHeight = gridHeight;
            
            // Предварительно заполняем кэш для всех клеток
            for (int row = 0; row < gridHeight; row++)
            {
                for (int col = 0; col < gridWidth; col++)
                {
                    string key = GetCacheKey(col, row, gridWidth, gridHeight);
                    if (!neighborsCache.ContainsKey(key))
                    {
                        neighborsCache[key] = CalculateNeighbors(col, row, gridWidth, gridHeight);
                    }
                }
            }
        }
        
        /// <summary>
        /// Возвращает координаты всех соседей клетки в гексагональной сетке (с кэшированием)
        /// </summary>
        /// <param name="x">Координата X клетки</param>
        /// <param name="y">Координата Y клетки</param>
        /// <param name="gridWidth">Ширина сетки</param>
        /// <param name="gridHeight">Высота сетки</param>
        /// <returns>Список координат соседей (x, y)</returns>
        public static List<Vector2Int> GetNeighbors(int x, int y, int gridWidth, int gridHeight)
        {
            // Если размеры сетки изменились, очищаем кэш
            if (cachedGridWidth != gridWidth || cachedGridHeight != gridHeight)
            {
                ClearCache();
                cachedGridWidth = gridWidth;
                cachedGridHeight = gridHeight;
            }
            
            string key = GetCacheKey(x, y, gridWidth, gridHeight);
            
            // Проверяем кэш
            if (neighborsCache.TryGetValue(key, out List<Vector2Int> cachedNeighbors))
            {
                return cachedNeighbors;
            }
            
            // Если нет в кэше, вычисляем и сохраняем
            List<Vector2Int> neighbors = CalculateNeighbors(x, y, gridWidth, gridHeight);
            neighborsCache[key] = neighbors;
            
            return neighbors;
        }
        
        /// <summary>
        /// Вычисляет соседей для клетки (без кэширования)
        /// </summary>
        private static List<Vector2Int> CalculateNeighbors(int x, int y, int gridWidth, int gridHeight)
        {
            List<Vector2Int> neighbors = new List<Vector2Int>();
            
            // В гексагональной сетке у каждой клетки 6 соседей
            // Расположение зависит от того, четная или нечетная строка
            bool isEvenRow = (y % 2 == 0);
            
            if (isEvenRow)
            {
                // Для четных строк
                neighbors.Add(new Vector2Int(x - 1, y - 1)); // Верхний левый
                neighbors.Add(new Vector2Int(x, y - 1));     // Верхний
                neighbors.Add(new Vector2Int(x - 1, y));     // Левый
                neighbors.Add(new Vector2Int(x + 1, y));     // Правый
                neighbors.Add(new Vector2Int(x - 1, y + 1)); // Нижний левый
                neighbors.Add(new Vector2Int(x, y + 1));     // Нижний
            }
            else
            {
                // Для нечетных строк
                neighbors.Add(new Vector2Int(x, y - 1));     // Верхний левый
                neighbors.Add(new Vector2Int(x + 1, y - 1)); // Верхний
                neighbors.Add(new Vector2Int(x - 1, y));     // Левый
                neighbors.Add(new Vector2Int(x + 1, y));     // Правый
                neighbors.Add(new Vector2Int(x, y + 1));     // Нижний левый
                neighbors.Add(new Vector2Int(x + 1, y + 1)); // Нижний
            }
            
            // Фильтруем соседей с учетом цикличности по горизонтали
            List<Vector2Int> validNeighbors = new List<Vector2Int>();
            foreach (Vector2Int neighbor in neighbors)
            {
                // Применяем цикличность по горизонтали (X)
                int wrappedX = ((neighbor.x % gridWidth) + gridWidth) % gridWidth;
                
                // Вертикальные границы остаются обычными (без цикличности)
                if (neighbor.y >= 0 && neighbor.y < gridHeight)
                {
                    validNeighbors.Add(new Vector2Int(wrappedX, neighbor.y));
                }
            }
            
            return validNeighbors;
        }
        
        /// <summary>
        /// Генерирует ключ для кэша
        /// </summary>
        private static string GetCacheKey(int x, int y, int gridWidth, int gridHeight)
        {
            return $"{x}_{y}_{gridWidth}_{gridHeight}";
        }
    }
}


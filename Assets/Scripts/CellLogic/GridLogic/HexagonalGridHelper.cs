using UnityEngine;
using System.Collections.Generic;

namespace CellNameSpace
{
    /// <summary>
    /// Утилитный класс для работы с гексагональной сеткой
    /// </summary>
    public static class HexagonalGridHelper
    {
        /// <summary>
        /// Возвращает координаты всех соседей клетки в гексагональной сетке
        /// </summary>
        /// <param name="x">Координата X клетки</param>
        /// <param name="y">Координата Y клетки</param>
        /// <param name="gridWidth">Ширина сетки</param>
        /// <param name="gridHeight">Высота сетки</param>
        /// <returns>Список координат соседей (x, y)</returns>
        public static List<Vector2Int> GetNeighbors(int x, int y, int gridWidth, int gridHeight)
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
            
            // Фильтруем соседей, которые находятся за пределами сетки
            List<Vector2Int> validNeighbors = new List<Vector2Int>();
            foreach (Vector2Int neighbor in neighbors)
            {
                if (neighbor.x >= 0 && neighbor.x < gridWidth && 
                    neighbor.y >= 0 && neighbor.y < gridHeight)
                {
                    validNeighbors.Add(neighbor);
                }
            }
            
            return validNeighbors;
        }
    }
}


using UnityEngine;
using System.Collections.Generic;
using CellNameSpace;

/// <summary>
/// Класс для поиска пути по гексагональной сетке с использованием алгоритма A*
/// </summary>
public static class Pathfinder
{
    /// <summary>
    /// Узел для алгоритма A*
    /// </summary>
    private class PathNode
    {
        public Vector2Int position;
        public int gCost; // Стоимость пути от старта
        public int hCost; // Эвристическая стоимость до цели
        public int fCost => gCost + hCost; // Общая стоимость
        public PathNode parent;
        
        public PathNode(Vector2Int pos)
        {
            position = pos;
            gCost = 0;
            hCost = 0;
            parent = null;
        }
    }
    
    /// <summary>
    /// Находит путь от стартовой клетки к целевой клетке
    /// </summary>
    /// <param name="startCell">Стартовая клетка</param>
    /// <param name="targetCell">Целевая клетка</param>
    /// <param name="grid">Grid для получения размеров сетки</param>
    /// <returns>Список CellInfo в порядке от старта до цели, или null если путь не найден</returns>
    public static List<CellInfo> FindPath(CellInfo startCell, CellInfo targetCell, CellNameSpace.Grid grid)
    {
        if (startCell == null || targetCell == null || grid == null)
        {
            Debug.LogWarning("Pathfinder: Один из параметров равен null");
            return null;
        }
        
        // Если стартовая и целевая клетки совпадают
        if (startCell.GetGridX() == targetCell.GetGridX() && startCell.GetGridY() == targetCell.GetGridY())
        {
            return new List<CellInfo> { targetCell };
        }
        
        Vector2Int startPos = new Vector2Int(startCell.GetGridX(), startCell.GetGridY());
        Vector2Int targetPos = new Vector2Int(targetCell.GetGridX(), targetCell.GetGridY());
        
        // Получаем размеры сетки из Grid
        int gridWidth = GetGridWidth(grid);
        int gridHeight = GetGridHeight(grid);
        
        if (gridWidth <= 0 || gridHeight <= 0)
        {
            Debug.LogWarning("Pathfinder: Не удалось получить размеры сетки");
            return null;
        }
        
        // Открытый список (кандидаты на проверку)
        List<PathNode> openList = new List<PathNode>();
        // Закрытый список (уже проверенные узлы)
        HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();
        
        // Начинаем с стартового узла
        PathNode startNode = new PathNode(startPos);
        openList.Add(startNode);
        
        int maxIterations = gridWidth * gridHeight; // Защита от бесконечного цикла
        int iterations = 0;
        
        while (openList.Count > 0 && iterations < maxIterations)
        {
            iterations++;
            
            // Находим узел с наименьшей fCost
            PathNode currentNode = openList[0];
            for (int i = 1; i < openList.Count; i++)
            {
                if (openList[i].fCost < currentNode.fCost || 
                    (openList[i].fCost == currentNode.fCost && openList[i].hCost < currentNode.hCost))
                {
                    currentNode = openList[i];
                }
            }
            
            // Убираем текущий узел из открытого списка
            openList.Remove(currentNode);
            closedSet.Add(currentNode.position);
            
            // Если достигли цели, строим путь
            if (currentNode.position.x == targetPos.x && currentNode.position.y == targetPos.y)
            {
                return ReconstructPath(currentNode, grid);
            }
            
            // Проверяем соседей
            List<Vector2Int> neighbors = HexagonalGridHelper.GetNeighbors(
                currentNode.position.x, 
                currentNode.position.y, 
                gridWidth, 
                gridHeight
            );
            
            foreach (Vector2Int neighborPos in neighbors)
            {
                // Пропускаем, если уже в закрытом списке
                if (closedSet.Contains(neighborPos))
                    continue;
                
                // Получаем клетку для проверки проходимости
                CellInfo neighborCell = grid.GetCellInfoAt(neighborPos.x, neighborPos.y);
                if (neighborCell == null || !IsWalkable(neighborCell))
                    continue;
                
                // Вычисляем стоимость движения к соседу
                // Вместо равномерной стоимости используем цену клетки (movement cost)
                int movementCost = currentNode.gCost + GetMovementCost(neighborCell);
                
                // Ищем соседа в открытом списке
                PathNode neighborNode = openList.Find(n => n.position.x == neighborPos.x && n.position.y == neighborPos.y);
                
                if (neighborNode == null)
                {
                    // Создаем новый узел
                    neighborNode = new PathNode(neighborPos);
                    neighborNode.gCost = movementCost;
                    neighborNode.hCost = GetDistance(neighborPos, targetPos);
                    neighborNode.parent = currentNode;
                    openList.Add(neighborNode);
                }
                else if (movementCost < neighborNode.gCost)
                {
                    // Нашли лучший путь к этому узлу
                    neighborNode.gCost = movementCost;
                    neighborNode.parent = currentNode;
                }
            }
        }
        
        // Путь не найден
        Debug.LogWarning($"Pathfinder: Путь от ({startPos.x}, {startPos.y}) до ({targetPos.x}, {targetPos.y}) не найден");
        return null;
    }
    
    /// <summary>
    /// Восстанавливает путь от целевого узла к стартовому
    /// </summary>
    private static List<CellInfo> ReconstructPath(PathNode endNode, CellNameSpace.Grid grid)
    {
        List<CellInfo> path = new List<CellInfo>();
        PathNode currentNode = endNode;
        
        // Проходим по цепочке родителей от цели к старту
        while (currentNode != null)
        {
            CellInfo cell = grid.GetCellInfoAt(currentNode.position.x, currentNode.position.y);
            if (cell != null)
            {
                path.Add(cell);
            }
            currentNode = currentNode.parent;
        }
        
        // Разворачиваем путь (чтобы был от старта к цели)
        path.Reverse();
        
        return path;
    }
    
    /// <summary>
    /// Вычисляет расстояние между двумя точками (манхэттенское расстояние для гексагональной сетки)
    /// </summary>
    private static int GetDistance(Vector2Int a, Vector2Int b)
    {
        // Для гексагональной сетки используется более точная эвристика
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        
        // Учитываем смещение для нечетных строк
        if ((a.y % 2 == 1 && a.x > b.x) || (b.y % 2 == 1 && b.x > a.x))
        {
            dx = Mathf.Max(0, dx - 1);
        }
        
        return Mathf.Max(dx, dy);
    }
    
    /// <summary>
    /// Проверяет, проходима ли клетка
    /// Использует CellStatsManager, если доступен, иначе fallback на старые значения
    /// </summary>
    private static bool IsWalkable(CellInfo cell)
    {
        if (cell == null)
            return false;
        
        CellType cellType = cell.GetCellType();
        
        // Пытаемся использовать CellStatsManager
        if (CellStatsManager.Instance != null)
        {
            return CellStatsManager.Instance.IsWalkable(cellType);
        }
        
        // Fallback: Непроходимые типы клеток
        List<CellType> unwalkableTypes = new List<CellType>
        {
            CellType.deep_water,
            CellType.shallow
        };
        
        return !unwalkableTypes.Contains(cellType);
    }

    /// <summary>
    /// Стоимость перемещения на указанную клетку (movement cost).
    /// Чем больше значение, тем \"тяжелее\" клетка для прохода.
    /// </summary>
    private static int GetMovementCost(CellInfo cell)
    {
        return GetMovementCostInternal(cell);
    }

    /// <summary>
    /// Публичный доступ к стоимости перемещения (для других классов).
    /// </summary>
    public static int GetMovementCostPublic(CellInfo cell)
    {
        return GetMovementCostInternal(cell);
    }

    /// <summary>
    /// Общая внутренняя реализация стоимости перемещения.
    /// Использует CellStatsManager, если доступен, иначе fallback на старые значения
    /// </summary>
    private static int GetMovementCostInternal(CellInfo cell)
    {
        if (cell == null)
            return int.MaxValue;

        CellType cellType = cell.GetCellType();
        
        // Пытаемся использовать CellStatsManager
        if (CellStatsManager.Instance != null)
        {
            return CellStatsManager.Instance.GetMovementCost(cellType);
        }

        // Fallback: старые жестко прописанные значения
        switch (cellType)
        {
            case CellType.field:
                return 1; // Обычная земля
            case CellType.forest:
                return 2; // Лес — дороже
            case CellType.desert:
                return 2; // Пустыня — дороже
            case CellType.mountain:
                return 3; // Горы — самые дорогие (если вообще проходимы)

            // Воды по идее непроходимы, но на всякий случай ставим очень большую цену
            case CellType.deep_water:
            case CellType.shallow:
                return 1000;

            default:
                return 1;
        }
    }
    
    /// <summary>
    /// Получает ширину сетки
    /// </summary>
    private static int GetGridWidth(CellNameSpace.Grid grid)
    {
        if (grid != null)
        {
            return grid.GetGridWidth();
        }
        return 0;
    }
    
    /// <summary>
    /// Получает высоту сетки
    /// </summary>
    private static int GetGridHeight(CellNameSpace.Grid grid)
    {
        if (grid != null)
        {
            return grid.GetGridHeight();
        }
        return 0;
    }
}


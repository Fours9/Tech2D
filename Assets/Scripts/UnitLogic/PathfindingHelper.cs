using UnityEngine;
using System.Collections.Generic;
using CellNameSpace;

/// <summary>
/// Вспомогательный класс для поиска пути в гексагональной сетке
/// </summary>
public static class PathfindingHelper
{
    /// <summary>
    /// Узел для поиска пути
    /// </summary>
    private class PathNode
    {
        public Vector2Int position;
        public PathNode parent;
        public float gCost; // Стоимость пути от старта
        public float hCost; // Эвристическая стоимость до цели
        
        public float FCost => gCost + hCost;
        
        public PathNode(Vector2Int pos)
        {
            position = pos;
            gCost = 0;
            hCost = 0;
            parent = null;
        }
    }
    
    /// <summary>
    /// Находит путь от стартовой позиции до целевой через проходимые клетки
    /// </summary>
    /// <param name="startX">Начальная координата X</param>
    /// <param name="startY">Начальная координата Y</param>
    /// <param name="targetX">Целевая координата X</param>
    /// <param name="targetY">Целевая координата Y</param>
    /// <param name="grid">Ссылка на Grid</param>
    /// <param name="allowedCellTypes">Список разрешенных типов клеток для прохода</param>
    /// <returns>Список позиций пути (от старта до цели) или null, если путь не найден</returns>
    public static List<Vector2Int> FindPath(int startX, int startY, int targetX, int targetY, 
        CellNameSpace.Grid grid, List<CellType> allowedCellTypes = null)
    {
        if (grid == null)
        {
            Debug.LogError("PathfindingHelper: Grid равен null");
            return null;
        }
        
        // Если стартовая и целевая позиции одинаковые
        if (startX == targetX && startY == targetY)
        {
            return new List<Vector2Int> { new Vector2Int(targetX, targetY) };
        }
        
        // Получаем размеры сетки (нужно будет добавить метод в Grid)
        int gridWidth = GetGridWidth(grid);
        int gridHeight = GetGridHeight(grid);
        
        if (gridWidth <= 0 || gridHeight <= 0)
        {
            Debug.LogError("PathfindingHelper: Не удалось определить размеры сетки");
            return null;
        }
        
        // Если не указаны разрешенные типы клеток, разрешаем все кроме воды и гор
        if (allowedCellTypes == null || allowedCellTypes.Count == 0)
        {
            allowedCellTypes = new List<CellType> 
            { 
                CellType.field, 
                CellType.forest, 
                CellType.desert 
            };
        }
        
        // Проверяем доступность стартовой и целевой клеток
        CellInfo startCell = grid.GetCellInfoAt(startX, startY);
        CellInfo targetCell = grid.GetCellInfoAt(targetX, targetY);
        
        if (startCell == null || targetCell == null)
        {
            Debug.LogWarning($"PathfindingHelper: Стартовая или целевая клетка не найдена");
            return null;
        }
        
        if (!allowedCellTypes.Contains(startCell.GetCellType()))
        {
            Debug.LogWarning($"PathfindingHelper: Стартовая клетка непроходима ({startCell.GetCellType()})");
            return null;
        }
        
        if (!allowedCellTypes.Contains(targetCell.GetCellType()))
        {
            Debug.LogWarning($"PathfindingHelper: Целевая клетка непроходима ({targetCell.GetCellType()})");
            return null;
        }
        
        // A* алгоритм поиска пути
        List<PathNode> openSet = new List<PathNode>();
        HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();
        
        Vector2Int startPos = new Vector2Int(startX, startY);
        Vector2Int targetPos = new Vector2Int(targetX, targetY);
        
        PathNode startNode = new PathNode(startPos);
        startNode.gCost = 0;
        startNode.hCost = HeuristicDistance(startPos, targetPos);
        openSet.Add(startNode);
        
        while (openSet.Count > 0)
        {
            // Находим узел с наименьшей F стоимостью
            PathNode currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].FCost < currentNode.FCost || 
                    (openSet[i].FCost == currentNode.FCost && openSet[i].hCost < currentNode.hCost))
                {
                    currentNode = openSet[i];
                }
            }
            
            openSet.Remove(currentNode);
            closedSet.Add(currentNode.position);
            
            // Если достигли цели, строим путь
            if (currentNode.position.x == targetPos.x && currentNode.position.y == targetPos.y)
            {
                return ReconstructPath(currentNode);
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
                // Пропускаем уже проверенные узлы
                if (closedSet.Contains(neighborPos))
                    continue;
                
                // Проверяем проходимость клетки
                CellInfo neighborCell = grid.GetCellInfoAt(neighborPos.x, neighborPos.y);
                if (neighborCell == null || !allowedCellTypes.Contains(neighborCell.GetCellType()))
                    continue;
                
                // Вычисляем стоимость перемещения
                float moveCost = currentNode.gCost + 1f; // Базовая стоимость = 1
                
                // Ищем существующий узел в открытом множестве
                PathNode existingNode = openSet.Find(n => n.position.x == neighborPos.x && n.position.y == neighborPos.y);
                
                if (existingNode == null)
                {
                    // Создаем новый узел
                    PathNode newNode = new PathNode(neighborPos);
                    newNode.parent = currentNode;
                    newNode.gCost = moveCost;
                    newNode.hCost = HeuristicDistance(neighborPos, targetPos);
                    openSet.Add(newNode);
                }
                else if (moveCost < existingNode.gCost)
                {
                    // Обновляем узел, если нашли лучший путь
                    existingNode.parent = currentNode;
                    existingNode.gCost = moveCost;
                }
            }
        }
        
        // Путь не найден
        Debug.LogWarning($"PathfindingHelper: Путь от ({startX}, {startY}) до ({targetX}, {targetY}) не найден");
        return null;
    }
    
    /// <summary>
    /// Эвристическая функция для оценки расстояния (манхэттенское расстояние для гексагональной сетки)
    /// </summary>
    private static float HeuristicDistance(Vector2Int a, Vector2Int b)
    {
        // Упрощенная эвристика для гексагональной сетки
        int dx = Mathf.Abs(b.x - a.x);
        int dy = Mathf.Abs(b.y - a.y);
        
        // Учитываем смещение для нечетных строк
        if (a.y % 2 != b.y % 2)
        {
            // Если строки разной четности, учитываем дополнительное смещение
            dx += (b.y % 2 == 1 && a.x > b.x) ? 1 : 0;
        }
        
        return dx + dy;
    }
    
    /// <summary>
    /// Восстанавливает путь от целевого узла до стартового
    /// </summary>
    private static List<Vector2Int> ReconstructPath(PathNode endNode)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        PathNode currentNode = endNode;
        
        while (currentNode != null)
        {
            path.Add(currentNode.position);
            currentNode = currentNode.parent;
        }
        
        // Разворачиваем путь (от старта к цели)
        path.Reverse();
        return path;
    }
    
    /// <summary>
    /// Получает ширину сетки через рефлексию или поиск
    /// </summary>
    private static int GetGridWidth(CellNameSpace.Grid grid)
    {
        // Пробуем получить через рефлексию (временно, пока не добавим публичные методы)
        var field = typeof(CellNameSpace.Grid).GetField("gridWidth", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            return (int)field.GetValue(grid);
        }
        
        // Альтернативный способ: ищем максимальные координаты
        CellInfo[] allCells = Object.FindObjectsByType<CellInfo>(FindObjectsSortMode.None);
        int maxX = -1;
        foreach (var cell in allCells)
        {
            if (cell.GetGridX() > maxX)
                maxX = cell.GetGridX();
        }
        return maxX + 1;
    }
    
    /// <summary>
    /// Получает высоту сетки через рефлексию или поиск
    /// </summary>
    private static int GetGridHeight(CellNameSpace.Grid grid)
    {
        // Пробуем получить через рефлексию
        var field = typeof(CellNameSpace.Grid).GetField("gridHeight", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            return (int)field.GetValue(grid);
        }
        
        // Альтернативный способ: ищем максимальные координаты
        CellInfo[] allCells = Object.FindObjectsByType<CellInfo>(FindObjectsSortMode.None);
        int maxY = -1;
        foreach (var cell in allCells)
        {
            if (cell.GetGridY() > maxY)
                maxY = cell.GetGridY();
        }
        return maxY + 1;
    }
}


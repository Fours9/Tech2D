using System.Collections.Generic;
using UnityEngine;
using CellNameSpace;

/// <summary>
/// Подсвечивает клетки, на которые может походить выделенный юнит
/// за один ход (с учётом movementPoints и стоимости клеток).
/// </summary>
public class ReachableCellsHighlighter : MonoBehaviour
{
    [Header("Ссылки (опционально)")]
    [SerializeField] private CellNameSpace.Grid grid; // Сетка (найдётся автоматически, если не указана)

    // Текущий список подсвеченных клеток
    private readonly List<CellInfo> highlightedCells = new List<CellInfo>();

    // Запоминаем текущего выделенного юнита и его последнюю подсвеченную позицию,
    // чтобы можно было обновлять подсветку при его перемещении.
    private UnitInfo currentUnit;
    private Vector2Int lastHighlightedPos = new Vector2Int(-1, -1);

    private void OnEnable()
    {
        UnitSelectionManager.OnUnitSelectedEvent += OnUnitSelected;
        UnitSelectionManager.OnUnitDeselectedEvent += OnUnitDeselected;
        TurnManager.OnPlanningPhaseStarted += OnPlanningPhaseStarted;
    }

    private void OnDisable()
    {
        UnitSelectionManager.OnUnitSelectedEvent -= OnUnitSelected;
        UnitSelectionManager.OnUnitDeselectedEvent -= OnUnitDeselected;
        TurnManager.OnPlanningPhaseStarted -= OnPlanningPhaseStarted;

        ClearHighlight();
    }

    private void OnUnitSelected(UnitInfo unit)
    {
        currentUnit = unit;
        HighlightReachableCells(unit);
    }

    private void OnUnitDeselected()
    {
        currentUnit = null;
        lastHighlightedPos = new Vector2Int(-1, -1);
        ClearHighlight();
    }

    /// <summary>
    /// Обработчик события начала фазы планирования.
    /// Обновляет подсветку, если юнит уже выделен (очки движения были восстановлены).
    /// </summary>
    private void OnPlanningPhaseStarted()
    {
        // Если юнит уже выделен, обновляем подсветку с учётом восстановленных очков движения
        if (currentUnit != null)
        {
            HighlightReachableCells(currentUnit);
        }
    }

    private void Update()
    {
        // Пока юнит выделен, проверяем, не сменил ли он клетку.
        if (currentUnit == null)
            return;

        if (!currentUnit.IsPositionInitialized())
            return;

        Vector2Int currentPos = new Vector2Int(currentUnit.GetGridX(), currentUnit.GetGridY());
        if (currentPos != lastHighlightedPos)
        {
            // Позиция юнита изменилась — пересчитываем reachable-клетки
            HighlightReachableCells(currentUnit);
        }
    }

    /// <summary>
    /// Подсвечивает клетки, достижимые для данного юнита за один ход.
    /// </summary>
    private void HighlightReachableCells(UnitInfo unit)
    {
        ClearHighlight();

        Debug.Log($"ReachableCellsHighlighter: HighlightReachableCells для юнита {unit.gameObject.name}");

        if (unit == null)
            return;

        if (grid == null)
        {
            grid = FindFirstObjectByType<CellNameSpace.Grid>();
            if (grid == null)
            {
                Debug.LogWarning("ReachableCellsHighlighter: Grid не найден");
                return;
            }
        }

        UnitStats stats = unit.GetUnitStats();
        if (stats == null)
        {
            Debug.LogWarning("ReachableCellsHighlighter: У юнита нет UnitStats");
            return;
        }

        // Получаем список запрещенных типов клеток для этого юнита
        List<CellType> forbiddenCellTypes = null;
        if (stats.forbiddenCellTypes != null && stats.forbiddenCellTypes.Count > 0)
        {
            forbiddenCellTypes = stats.forbiddenCellTypes;
        }

        int remainingMovement = unit.GetRemainingMovementPoints();
        if (remainingMovement <= 0)
        {
            Debug.Log("ReachableCellsHighlighter: У юнита нет оставшихся очков движения");
            return;
        }

        // Получаем стартовую клетку
        CellInfo startCell = grid.GetCellInfoAt(unit.GetGridX(), unit.GetGridY());
        if (startCell == null)
        {
            Debug.LogWarning("ReachableCellsHighlighter: Не удалось получить стартовую клетку юнита");
            return;
        }

        lastHighlightedPos = new Vector2Int(unit.GetGridX(), unit.GetGridY());

        int gridWidth = grid.GetGridWidth();
        int gridHeight = grid.GetGridHeight();

        // BFS/поиск в ширину по стоимости движения
        Queue<(CellInfo cell, int cost)> queue = new Queue<(CellInfo, int)>();
        Dictionary<CellInfo, int> bestCost = new Dictionary<CellInfo, int>();

        queue.Enqueue((startCell, 0));
        bestCost[startCell] = 0;

        while (queue.Count > 0)
        {
            var (currentCell, currentCost) = queue.Dequeue();

            int cx = currentCell.GetGridX();
            int cy = currentCell.GetGridY();

            List<Vector2Int> neighbors = HexagonalGridHelper.GetNeighbors(cx, cy, gridWidth, gridHeight);
            foreach (var pos in neighbors)
            {
                CellInfo neighbor = grid.GetCellInfoAt(pos.x, pos.y);
                if (neighbor == null)
                    continue;

                if (!IsWalkableForHighlight(neighbor, forbiddenCellTypes))
                    continue;

                int stepCost = Pathfinder.GetMovementCostPublic(neighbor);
                int newCost = currentCost + stepCost;

                if (newCost > remainingMovement)
                    continue;

                if (bestCost.TryGetValue(neighbor, out int oldCost) && oldCost <= newCost)
                    continue;

                bestCost[neighbor] = newCost;
                queue.Enqueue((neighbor, newCost));
            }
        }

        Debug.Log($"ReachableCellsHighlighter: найдено достижимых клеток (включая стартовую) = {bestCost.Count}");

        // Подсвечиваем все клетки, кроме стартовой
        foreach (var kvp in bestCost)
        {
            CellInfo cell = kvp.Key;
            if (cell == startCell)
                continue;

            cell.SetMovementHighlight(true);
            highlightedCells.Add(cell);
        }
    }

    /// <summary>
    /// Сбрасывает подсветку со всех клеток.
    /// </summary>
    private void ClearHighlight()
    {
        foreach (var cell in highlightedCells)
        {
            if (cell != null)
            {
                cell.SetMovementHighlight(false);
            }
        }
        highlightedCells.Clear();
    }

    /// <summary>
    /// Проверка проходимости для подсветки (упрощённая версия IsWalkable).
    /// Учитывает запрещенные типы клеток для конкретного юнита.
    /// </summary>
    private bool IsWalkableForHighlight(CellInfo cell, List<CellType> forbiddenCellTypes = null)
    {
        if (cell == null)
            return false;

        CellType type = cell.GetCellType();
        
        // Проверяем, не запрещен ли этот тип клетки для юнита
        if (forbiddenCellTypes != null && forbiddenCellTypes.Contains(type))
        {
            return false;
        }
        
        // Используем CellStatsManager, если доступен
        if (CellStatsManager.Instance != null)
        {
            return CellStatsManager.Instance.IsWalkable(type);
        }
        
        // Fallback: проверяем непроходимые типы
        return type != CellType.deep_water && type != CellType.shallow;
    }
}




using UnityEngine;

/// <summary>
/// Приказ на перемещение юнита на целевую клетку.
/// Создаётся в фазе планирования, выполняется в фазе исполнения.
/// </summary>
public class MoveUnitOrder : TurnOrder
{
    private readonly UnitController unitController;
    private readonly CellNameSpace.CellInfo targetCell;

    public MoveUnitOrder(UnitController unitController, CellNameSpace.CellInfo targetCell)
    {
        this.unitController = unitController;
        this.targetCell = targetCell;
    }

    public override int OwnerId => unitController?.GetComponent<UnitInfo>()?.GetOwnerId() ?? 0;

    public override string GetDescription()
    {
        if (unitController == null || targetCell == null)
            return "MoveUnitOrder (invalid)";

        return $"Перемещение юнита {unitController.gameObject.name} " +
               $"к ({targetCell.GetGridX()}, {targetCell.GetGridY()})";
    }

    public override void Execute(TurnManager turnManager)
    {
        if (unitController == null || targetCell == null)
        {
            Debug.LogWarning("MoveUnitOrder: unitController или targetCell равны null, приказ пропущен");
            return;
        }

        // Пытаемся получить оставшиеся очки движения юнита
        UnitInfo info = unitController.GetComponent<UnitInfo>();
        int maxMovement = int.MaxValue;

        if (info != null)
        {
            int remaining = info.GetRemainingMovementPoints();
            if (remaining > 0)
            {
                maxMovement = remaining;
            }
        }

        // Если известен лимит очков движения — двигаемся с ограничением
        if (maxMovement < int.MaxValue)
        {
            unitController.MoveToCellWithLimit(targetCell, maxMovement);
        }
        else
        {
            // Fallback: нет данных о статах — двигаем без ограничения
            unitController.MoveToCell(targetCell);
        }
    }

    /// <summary>
    /// Приказ движения считается завершённым, когда юнит перестал двигаться.
    /// </summary>
    public override bool IsComplete
    {
        get
        {
            return unitController == null || !unitController.IsMoving();
        }
    }
}



using UnityEngine;

/// <summary>
/// Приказ на строительство постройки на указанной клетке.
/// Создаётся в фазе планирования, выполняется в фазе исполнения.
/// </summary>
public class BuildBuildingOrder : TurnOrder
{
    private readonly Vector2Int cellPosition;
    private readonly BuildingInfo buildingInfo;
    private readonly int ownerId;

    public BuildBuildingOrder(Vector2Int cellPosition, BuildingInfo buildingInfo, int ownerId)
    {
        this.cellPosition = cellPosition;
        this.buildingInfo = buildingInfo;
        this.ownerId = ownerId;
    }

    public override int OwnerId => ownerId;

    public override string GetDescription()
    {
        if (buildingInfo == null)
            return $"BuildBuildingOrder (invalid) at ({cellPosition.x}, {cellPosition.y})";

        return $"Строительство '{buildingInfo.name}' на клетке ({cellPosition.x}, {cellPosition.y})";
    }

    public override void Execute(TurnManager turnManager)
    {
        if (buildingInfo == null)
        {
            Debug.LogWarning("BuildBuildingOrder: buildingInfo == null, приказ пропущен");
            return;
        }

        BuildingManager buildingManager = null;

        if (turnManager != null)
        {
            buildingManager = turnManager.GetBuildingManager();
        }

        if (buildingManager == null)
        {
            buildingManager = Object.FindFirstObjectByType<BuildingManager>();
        }

        if (buildingManager == null)
        {
            Debug.LogError("BuildBuildingOrder: BuildingManager не найден, приказ пропущен");
            return;
        }

        bool success = buildingManager.PlaceBuilding(cellPosition, buildingInfo);
        if (success)
        {
            Debug.Log($"BuildBuildingOrder: Постройка '{buildingInfo.name}' установлена на клетку ({cellPosition.x}, {cellPosition.y})");
        }
    }
}



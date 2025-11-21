using UnityEngine;
using CellNameSpace;

/// <summary>
/// Контроллер для управления перемещением юнита
/// </summary>
public class UnitController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f; // Скорость перемещения
    
    private UnitInfo unitInfo;
    private bool isMoving = false;
    private Vector3 targetPosition;
    
    void Start()
    {
        unitInfo = GetComponent<UnitInfo>();
        if (unitInfo == null)
        {
            Debug.LogError($"UnitController: UnitInfo не найден на {gameObject.name}");
        }
    }

    void Update()
    {
        if (isMoving)
        {
            MoveToTarget();
        }
    }
    
    /// <summary>
    /// Переместить юнита на указанную клетку
    /// </summary>
    public void MoveToCell(CellInfo targetCell)
    {
        if (targetCell == null)
        {
            Debug.LogWarning("UnitController: Попытка переместиться на null клетку");
            return;
        }
        
        if (unitInfo == null)
        {
            Debug.LogError("UnitController: UnitInfo не найден");
            return;
        }
        
        // Обновляем позицию на сетке
        unitInfo.SetGridPosition(targetCell.GetGridX(), targetCell.GetGridY());
        
        // Устанавливаем целевую позицию (центр клетки)
        targetPosition = targetCell.transform.position;
        
        // Начинаем движение
        isMoving = true;
        
        Debug.Log($"Юнит {gameObject.name} перемещается на клетку ({targetCell.GetGridX()}, {targetCell.GetGridY()})");
    }
    
    /// <summary>
    /// Переместить юнита на указанную позицию в мировых координатах
    /// </summary>
    public void MoveToPosition(Vector3 position)
    {
        targetPosition = position;
        isMoving = true;
    }
    
    /// <summary>
    /// Обновление движения к цели
    /// </summary>
    private void MoveToTarget()
    {
        float distance = Vector3.Distance(transform.position, targetPosition);
        
        if (distance > 0.01f)
        {
            // Плавное перемещение
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        }
        else
        {
            // Достигли цели
            transform.position = targetPosition;
            isMoving = false;
            Debug.Log($"Юнит {gameObject.name} достиг цели");
        }
    }
    
    /// <summary>
    /// Проверяет, перемещается ли юнит
    /// </summary>
    public bool IsMoving()
    {
        return isMoving;
    }
    
    /// <summary>
    /// Останавливает движение юнита
    /// </summary>
    public void StopMovement()
    {
        isMoving = false;
    }
}

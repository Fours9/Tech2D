using UnityEngine;

/// <summary>
/// Информация о конкретном экземпляре юнита на карте:
/// позиция на сетке и ссылка на его тип/статы (UnitStats).
/// </summary>
public class UnitInfo : MonoBehaviour
{
    [Header("Позиция на сетке")]
    [SerializeField] private int gridX = -1;
    [SerializeField] private int gridY = -1;

    [Header("Тип юнита и статы")]
    [SerializeField] private UnitStats unitStats; // ScriptableObject с данными типа юнита

    [Header("Состояние движения")]
    [SerializeField] private int currentMovementPoints = 0; // Оставшиеся очки движения на текущий ход
    
    private void Awake()
    {
        // Регистрируем юнита в FogOfWarManager для оптимизации
        if (FogOfWarManager.Instance != null)
        {
            FogOfWarManager.Instance.RegisterUnit(this);
        }
    }
    
    private void OnDestroy()
    {
        // Отменяем регистрацию юнита при уничтожении
        if (FogOfWarManager.Instance != null)
        {
            FogOfWarManager.Instance.UnregisterUnit(this);
        }
    }
    
    /// <summary>
    /// Получить позицию X в сетке
    /// </summary>
    public int GetGridX()
    {
        return gridX;
    }
    
    /// <summary>
    /// Получить позицию Y в сетке
    /// </summary>
    public int GetGridY()
    {
        return gridY;
    }
    
    /// <summary>
    /// Установить позицию юнита на сетке
    /// </summary>
    public void SetGridPosition(int x, int y)
    {
        gridX = x;
        gridY = y;
    }
    
    /// <summary>
    /// Проверяет, инициализирована ли позиция на сетке
    /// </summary>
    public bool IsPositionInitialized()
    {
        return gridX >= 0 && gridY >= 0;
    }

    /// <summary>
    /// Получить статы / тип юнита (ScriptableObject).
    /// </summary>
    public UnitStats GetUnitStats()
    {
        return unitStats;
    }

    /// <summary>
    /// Установить статы / тип юнита (например, при спавне).
    /// </summary>
    public void SetUnitStats(UnitStats stats)
    {
        unitStats = stats;
    }

    /// <summary>
    /// Сбросить очки движения в начало хода (берётся из UnitStats.movementPoints).
    /// </summary>
    public void ResetMovementPoints()
    {
        if (unitStats != null && unitStats.movementPoints > 0)
        {
            currentMovementPoints = unitStats.movementPoints;
        }
        else
        {
            currentMovementPoints = 0;
        }
    }

    /// <summary>
    /// Получить оставшиеся очки движения на текущий ход.
    /// </summary>
    public int GetRemainingMovementPoints()
    {
        return currentMovementPoints;
    }

    /// <summary>
    /// Попробовать потратить очки движения. Возвращает true, если удалось.
    /// </summary>
    public bool TrySpendMovementPoints(int amount)
    {
        if (amount <= 0)
            return true;

        if (currentMovementPoints < amount)
            return false;

        currentMovementPoints -= amount;
        return true;
    }
}

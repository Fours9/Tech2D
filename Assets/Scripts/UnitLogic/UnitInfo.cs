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
}

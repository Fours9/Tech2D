using UnityEngine;

public class UnitInfo : MonoBehaviour
{
    [SerializeField] private int gridX = -1;
    [SerializeField] private int gridY = -1;
    
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
}

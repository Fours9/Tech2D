using UnityEngine;
using System;

/// <summary>
/// Менеджер для управления выбранным юнитом
/// </summary>
public class UnitSelectionManager : MonoBehaviour
{
    private static UnitSelectionManager instance;
    
    private UnitInfo selectedUnit = null;
    
    // События для уведомления о выборе/снятии выбора юнита
    public static event Action<UnitInfo> OnUnitSelectedEvent;
    public static event Action OnUnitDeselectedEvent;
    
    public static UnitSelectionManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<UnitSelectionManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("UnitSelectionManager");
                    instance = go.AddComponent<UnitSelectionManager>();
                }
            }
            return instance;
        }
    }
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            
            // Перемещаем GameObject в корень, если он дочерний (DontDestroyOnLoad работает только для корневых объектов)
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }
            
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Выбрать юнита
    /// </summary>
    public void SelectUnit(UnitInfo unit)
    {
        // Снимаем выделение с предыдущего юнита
        if (selectedUnit != null && selectedUnit != unit)
        {
            OnUnitDeselected(selectedUnit);
        }
        
        selectedUnit = unit;
        
        if (selectedUnit != null)
        {
            OnUnitSelected(selectedUnit);
            OnUnitSelectedEvent?.Invoke(selectedUnit);
            Debug.Log($"Юнит выбран: {selectedUnit.gameObject.name}");
        }
    }
    
    /// <summary>
    /// Снять выделение с юнита
    /// </summary>
    public void DeselectUnit()
    {
        if (selectedUnit != null)
        {
            OnUnitDeselected(selectedUnit);
            selectedUnit = null;
            OnUnitDeselectedEvent?.Invoke();
            Debug.Log("Выделение снято с юнита");
        }
    }
    
    /// <summary>
    /// Получить выбранного юнита
    /// </summary>
    public UnitInfo GetSelectedUnit()
    {
        return selectedUnit;
    }
    
    /// <summary>
    /// Проверяет, выбран ли юнит
    /// </summary>
    public bool HasSelectedUnit()
    {
        return selectedUnit != null;
    }
    
    /// <summary>
    /// Вызывается при выборе юнита (можно переопределить для визуальной индикации)
    /// </summary>
    private void OnUnitSelected(UnitInfo unit)
    {
        // Здесь можно добавить визуальную индикацию выбранного юнита
        // Например, изменить цвет, добавить подсветку и т.д.
    }
    
    /// <summary>
    /// Вызывается при снятии выделения с юнита
    /// </summary>
    private void OnUnitDeselected(UnitInfo unit)
    {
        // Здесь можно убрать визуальную индикацию
    }
}



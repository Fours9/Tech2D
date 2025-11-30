using UnityEngine;
using System;

/// <summary>
/// Менеджер для управления выбранным городом
/// </summary>
public class CitySelectionManager : MonoBehaviour
{
    private static CitySelectionManager instance;
    
    private CityInfo selectedCity = null;
    
    // События для уведомления о выборе/снятии выбора города
    public static event Action<CityInfo> OnCitySelectedEvent;
    public static event Action OnCityDeselectedEvent;
    
    public static CitySelectionManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<CitySelectionManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("CitySelectionManager");
                    instance = go.AddComponent<CitySelectionManager>();
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
    /// Выбрать город
    /// </summary>
    public void SelectCity(CityInfo city)
    {
        // Снимаем выделение с предыдущего города
        if (selectedCity != null && selectedCity != city)
        {
            OnCityDeselected(selectedCity);
        }
        
        selectedCity = city;
        
        if (selectedCity != null)
        {
            OnCitySelected(selectedCity);
            OnCitySelectedEvent?.Invoke(selectedCity);
            Debug.Log($"Город выбран: {selectedCity.name}");
        }
    }
    
    /// <summary>
    /// Снять выделение с города
    /// </summary>
    public void DeselectCity()
    {
        if (selectedCity != null)
        {
            OnCityDeselected(selectedCity);
            selectedCity = null;
            OnCityDeselectedEvent?.Invoke();
            Debug.Log("Выделение снято с города");
        }
    }
    
    /// <summary>
    /// Получить выбранный город
    /// </summary>
    public CityInfo GetSelectedCity()
    {
        return selectedCity;
    }
    
    /// <summary>
    /// Проверяет, выбран ли город
    /// </summary>
    public bool HasSelectedCity()
    {
        return selectedCity != null;
    }
    
    /// <summary>
    /// Вызывается при выборе города (можно переопределить для визуальной индикации)
    /// </summary>
    private void OnCitySelected(CityInfo city)
    {
        // Здесь можно добавить визуальную индикацию выбранного города
        // Например, изменить цвет, добавить подсветку и т.д.
    }
    
    /// <summary>
    /// Вызывается при снятии выделения с города
    /// </summary>
    private void OnCityDeselected(CityInfo city)
    {
        // Здесь можно убрать визуальную индикацию
    }
}



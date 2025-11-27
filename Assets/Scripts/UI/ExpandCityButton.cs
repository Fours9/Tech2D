using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Скрипт для кнопки-переключателя режима расширения города
/// Когда включена - можно добавлять клетки к городу кликом
/// Когда выключена - нельзя добавлять клетки
/// Работает только когда город выделен
/// </summary>
public class ExpandCityButton : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private Toggle toggle; // Переключатель (найдет автоматически, если не указан)
    [SerializeField] private CityManager cityManager; // Менеджер городов (найдет автоматически, если не указан)
    
    private static bool expansionModeEnabled = false; // Статическая переменная для проверки режима расширения
    
    /// <summary>
    /// Проверяет, включен ли режим расширения города
    /// </summary>
    public static bool IsExpansionModeEnabled()
    {
        return expansionModeEnabled;
    }
    
    void Start()
    {
        // Находим переключатель, если не указан
        if (toggle == null)
        {
            toggle = GetComponent<Toggle>();
            if (toggle == null)
            {
                Debug.LogError("ExpandCityButton: Toggle не найден! Используйте Toggle вместо Button.");
                return;
            }
        }
        
        // Находим CityManager, если не указан
        if (cityManager == null)
        {
            cityManager = FindFirstObjectByType<CityManager>();
            if (cityManager == null)
            {
                Debug.LogError("ExpandCityButton: CityManager не найден!");
                if (toggle != null)
                    toggle.interactable = false;
                return;
            }
        }
        
        // Инициализируем состояние
        expansionModeEnabled = false;
        toggle.isOn = false;
        
        // Подписываемся на событие изменения состояния переключателя
        toggle.onValueChanged.AddListener(OnToggleValueChanged);
        
        // Подписываемся на события выбора города
        CitySelectionManager.OnCitySelectedEvent += OnCitySelected;
        CitySelectionManager.OnCityDeselectedEvent += OnCityDeselected;
        
        // Обновляем начальное состояние
        UpdateButtonState();
    }
    
    void OnDestroy()
    {
        // Отписываемся от события при уничтожении
        if (toggle != null)
        {
            toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
        }
        
        CitySelectionManager.OnCitySelectedEvent -= OnCitySelected;
        CitySelectionManager.OnCityDeselectedEvent -= OnCityDeselected;
    }
    
    /// <summary>
    /// Обработчик события выбора города
    /// </summary>
    private void OnCitySelected(CityInfo city)
    {
        UpdateButtonState();
    }
    
    /// <summary>
    /// Обработчик события снятия выбора города
    /// </summary>
    private void OnCityDeselected()
    {
        // Выключаем режим расширения и toggle при снятии выделения
        if (toggle != null && toggle.isOn)
        {
            toggle.isOn = false;
        }
        UpdateButtonState();
    }
    
    /// <summary>
    /// Обработчик изменения состояния переключателя
    /// </summary>
    private void OnToggleValueChanged(bool isOn)
    {
        // Режим расширения работает только если город выделен
        bool hasSelectedCity = CitySelectionManager.Instance != null && 
                               CitySelectionManager.Instance.HasSelectedCity();
        
        if (isOn && !hasSelectedCity)
        {
            // Если пытаются включить без выбранного города, выключаем
            toggle.isOn = false;
            expansionModeEnabled = false;
            Debug.LogWarning("ExpandCityButton: Нельзя включить режим расширения без выбранного города");
            return;
        }
        
        expansionModeEnabled = isOn && hasSelectedCity;
        
        if (expansionModeEnabled)
        {
            Debug.Log("ExpandCityButton: Режим расширения города включен. Кликайте по соседним клеткам для добавления к городу.");
        }
        else
        {
            Debug.Log("ExpandCityButton: Режим расширения города выключен.");
        }
    }
    
    /// <summary>
    /// Обновляет состояние переключателя (активен/неактивен) в зависимости от наличия выбранного города
    /// </summary>
    public void UpdateButtonState()
    {
        if (toggle == null || cityManager == null)
            return;
        
        bool hasSelectedCity = CitySelectionManager.Instance != null && 
                               CitySelectionManager.Instance.HasSelectedCity();
        
        // Переключатель активен только если есть выбранный город
        toggle.interactable = hasSelectedCity;
        
        // Если нет выбранного города, выключаем режим расширения
        if (!hasSelectedCity && toggle.isOn)
        {
            toggle.isOn = false;
            expansionModeEnabled = false;
        }
    }
}




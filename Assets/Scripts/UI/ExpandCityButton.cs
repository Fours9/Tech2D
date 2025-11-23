using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Скрипт для кнопки-переключателя режима расширения города
/// Когда включена - можно добавлять клетки к городу кликом
/// Когда выключена - нельзя добавлять клетки
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
        expansionModeEnabled = toggle.isOn;
        
        // Подписываемся на событие изменения состояния переключателя
        toggle.onValueChanged.AddListener(OnToggleValueChanged);
    }
    
    void OnDestroy()
    {
        // Отписываемся от события при уничтожении
        if (toggle != null)
        {
            toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
        }
    }
    
    /// <summary>
    /// Обработчик изменения состояния переключателя
    /// </summary>
    private void OnToggleValueChanged(bool isOn)
    {
        expansionModeEnabled = isOn;
        
        if (isOn)
        {
            Debug.Log("ExpandCityButton: Режим расширения города включен. Кликайте по соседним клеткам для добавления к городу.");
        }
        else
        {
            Debug.Log("ExpandCityButton: Режим расширения города выключен.");
        }
    }
    
    /// <summary>
    /// Обновляет состояние переключателя (активен/неактивен) в зависимости от наличия городов
    /// </summary>
    public void UpdateButtonState()
    {
        if (toggle == null || cityManager == null)
            return;
        
        var allCities = cityManager.GetAllCities();
        toggle.interactable = allCities.Count > 0;
        
        // Если нет городов, выключаем режим расширения
        if (allCities.Count == 0 && toggle.isOn)
        {
            toggle.isOn = false;
        }
    }
}




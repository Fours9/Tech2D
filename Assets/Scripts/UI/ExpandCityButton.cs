using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Скрипт для кнопки расширения города
/// </summary>
public class ExpandCityButton : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private Button button; // Кнопка (найдет автоматически, если не указана)
    [SerializeField] private CityManager cityManager; // Менеджер городов (найдет автоматически, если не указан)
    
    void Start()
    {
        // Находим кнопку, если не указана
        if (button == null)
        {
            button = GetComponent<Button>();
            if (button == null)
            {
                Debug.LogError("ExpandCityButton: Кнопка не найдена!");
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
                button.interactable = false;
                return;
            }
        }
        
        // Подписываемся на событие клика кнопки
        button.onClick.AddListener(OnButtonClick);
    }
    
    void OnDestroy()
    {
        // Отписываемся от события при уничтожении
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClick);
        }
    }
    
    /// <summary>
    /// Обработчик клика по кнопке
    /// </summary>
    private void OnButtonClick()
    {
        // Проверяем, есть ли выбранный город (через клик по клетке с городом)
        // Для упрощения, расширяем первый найденный город или город под курсором
        // В будущем можно добавить систему выбора городов
        
        // Получаем все города
        var allCities = cityManager.GetAllCities();
        if (allCities.Count == 0)
        {
            Debug.LogWarning("ExpandCityButton: Нет городов для расширения");
            return;
        }
        
        // Для начала расширяем первый город (в будущем можно добавить выбор города)
        // Или можно расширять город, на клетку которого кликнули
        Vector2Int? selectedCityPosition = GetSelectedCityPosition();
        
        if (!selectedCityPosition.HasValue)
        {
            // Если нет выбранного города, берем первый
            foreach (var kvp in allCities)
            {
                selectedCityPosition = kvp.Key;
                break;
            }
        }
        
        if (selectedCityPosition.HasValue)
        {
            // Расширяем город автоматически (на один радиус)
            bool success = cityManager.ExpandCity(selectedCityPosition.Value);
            
            if (success)
            {
                Debug.Log($"ExpandCityButton: Город на позиции ({selectedCityPosition.Value.x}, {selectedCityPosition.Value.y}) успешно расширен");
            }
            else
            {
                Debug.LogWarning("ExpandCityButton: Не удалось расширить город (возможно, нет доступных клеток)");
            }
        }
    }
    
    /// <summary>
    /// Получает позицию выбранного города (можно расширить для работы с системой выбора)
    /// </summary>
    private Vector2Int? GetSelectedCityPosition()
    {
        // В будущем здесь можно добавить логику получения выбранного города
        // Например, через систему выбора клеток или UI панель городов
        return null;
    }
    
    /// <summary>
    /// Обновляет состояние кнопки (активна/неактивна) в зависимости от наличия городов
    /// </summary>
    public void UpdateButtonState()
    {
        if (button == null || cityManager == null)
            return;
        
        var allCities = cityManager.GetAllCities();
        button.interactable = allCities.Count > 0;
    }
}




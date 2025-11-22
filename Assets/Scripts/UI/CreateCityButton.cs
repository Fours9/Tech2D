using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Скрипт для кнопки создания города
/// </summary>
public class CreateCityButton : MonoBehaviour
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
                Debug.LogError("CreateCityButton: Кнопка не найдена!");
                return;
            }
        }
        
        // Находим CityManager, если не указан
        if (cityManager == null)
        {
            cityManager = FindFirstObjectByType<CityManager>();
            if (cityManager == null)
            {
                Debug.LogError("CreateCityButton: CityManager не найден!");
                button.interactable = false;
                return;
            }
        }
        
        // Подписываемся на событие клика кнопки
        button.onClick.AddListener(OnButtonClick);
        
        // Подписываемся на события выбора юнита для обновления состояния кнопки
        UnitSelectionManager.OnUnitSelectedEvent += OnUnitSelected;
        UnitSelectionManager.OnUnitDeselectedEvent += OnUnitDeselected;
        
        // Обновляем начальное состояние кнопки
        UpdateButtonState();
    }
    
    void OnDestroy()
    {
        // Отписываемся от событий при уничтожении
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClick);
        }
        
        UnitSelectionManager.OnUnitSelectedEvent -= OnUnitSelected;
        UnitSelectionManager.OnUnitDeselectedEvent -= OnUnitDeselected;
    }
    
    /// <summary>
    /// Обработчик события выбора юнита
    /// </summary>
    private void OnUnitSelected(UnitInfo unit)
    {
        UpdateButtonState();
    }
    
    /// <summary>
    /// Обработчик события снятия выбора юнита
    /// </summary>
    private void OnUnitDeselected()
    {
        UpdateButtonState();
    }
    
    /// <summary>
    /// Обработчик клика по кнопке
    /// </summary>
    private void OnButtonClick()
    {
        // Проверяем, есть ли выбранный юнит
        if (UnitSelectionManager.Instance == null || !UnitSelectionManager.Instance.HasSelectedUnit())
        {
            Debug.LogWarning("CreateCityButton: Нет выбранного юнита для создания города");
            return;
        }
        
        UnitInfo selectedUnit = UnitSelectionManager.Instance.GetSelectedUnit();
        if (selectedUnit == null)
        {
            Debug.LogWarning("CreateCityButton: Выбранный юнит равен null");
            return;
        }
        
        // Проверяем, что юнит находится на валидной позиции
        if (!selectedUnit.IsPositionInitialized())
        {
            Debug.LogWarning("CreateCityButton: Позиция юнита не инициализирована");
            return;
        }
        
        // Проверяем, нет ли уже города на этой позиции
        Vector2Int unitPosition = new Vector2Int(selectedUnit.GetGridX(), selectedUnit.GetGridY());
        if (cityManager.HasCityAt(unitPosition))
        {
            Debug.LogWarning($"CreateCityButton: На позиции ({unitPosition.x}, {unitPosition.y}) уже есть город");
            return;
        }
        
        // Создаем город
        bool success = cityManager.CreateCityFromUnit(selectedUnit);
        
        if (success)
        {
            Debug.Log($"CreateCityButton: Город успешно создан на позиции ({unitPosition.x}, {unitPosition.y})");
        }
        else
        {
            Debug.LogWarning("CreateCityButton: Не удалось создать город");
        }
    }
    
    /// <summary>
    /// Обновляет состояние кнопки (активна/неактивна) в зависимости от наличия выбранного юнита
    /// Вызывать извне при изменении выбора юнита
    /// </summary>
    public void UpdateButtonState()
    {
        if (button == null)
            return;
        
        bool hasSelectedUnit = UnitSelectionManager.Instance != null && 
                               UnitSelectionManager.Instance.HasSelectedUnit();
        
        button.interactable = hasSelectedUnit;
    }
}


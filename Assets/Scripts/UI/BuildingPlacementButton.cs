using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Скрипт для кнопки выбора постройки
/// </summary>
public class BuildingPlacementButton : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private int buildingIndex = 0; // Индекс постройки в списке BuildingManager (0 = первая постройка)
    [SerializeField] private Button button; // Кнопка (найдет автоматически, если не указана)
    [SerializeField] private BuildingManager buildingManager; // Менеджер построек (найдет автоматически, если не указан)
    
    private BuildingInfo buildingInfo; // Информация о постройке (берется из BuildingManager)
    
    void Start()
    {
        // Находим кнопку, если не указана
        if (button == null)
        {
            button = GetComponent<Button>();
            if (button == null)
            {
                Debug.LogError("BuildingPlacementButton: Кнопка не найдена!");
                return;
            }
        }
        
        // Находим BuildingManager, если не указан
        if (buildingManager == null)
        {
            buildingManager = FindFirstObjectByType<BuildingManager>();
            if (buildingManager == null)
            {
                Debug.LogError("BuildingPlacementButton: BuildingManager не найден!");
                button.interactable = false;
                return;
            }
        }
        
        // Создаем или находим BuildingInfo
        InitializeBuildingInfo();
        
        // Подписываемся на событие клика кнопки
        button.onClick.AddListener(OnButtonClick);
        
        // Обновляем текст кнопки
        UpdateButtonText();
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
        if (buildingInfo == null)
        {
            Debug.LogWarning("BuildingPlacementButton: BuildingInfo не назначен!");
            return;
        }
        
        // Выбираем постройку для установки
        buildingManager.SelectBuilding(buildingInfo);
        Debug.Log($"BuildingPlacementButton: Выбрана постройка '{buildingInfo.name}'");
    }
    
    /// <summary>
    /// Обновляет текст кнопки
    /// </summary>
    private void UpdateButtonText()
    {
        // Ищем компонент Text на дочернем объекте
        Text buttonText = GetComponentInChildren<Text>();
        if (buttonText != null && buildingInfo != null)
        {
            buttonText.text = buildingInfo.name;
        }
    }
    
    /// <summary>
    /// Инициализирует информацию о постройке из BuildingManager
    /// </summary>
    private void InitializeBuildingInfo()
    {
        if (buildingManager == null)
            return;
        
        var availableBuildings = buildingManager.GetAvailableBuildings();
        
        // Проверяем, что индекс валидный
        if (buildingIndex >= 0 && buildingIndex < availableBuildings.Count)
        {
            buildingInfo = availableBuildings[buildingIndex];
        }
        else
        {
            Debug.LogWarning($"BuildingPlacementButton: Индекс {buildingIndex} вне диапазона. Доступно построек: {availableBuildings.Count}");
        }
    }
    
    /// <summary>
    /// Устанавливает индекс постройки (можно вызвать извне)
    /// </summary>
    public void SetBuildingIndex(int index)
    {
        buildingIndex = index;
        InitializeBuildingInfo();
        UpdateButtonText();
    }
}



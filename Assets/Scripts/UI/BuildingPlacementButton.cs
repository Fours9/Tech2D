using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Скрипт для кнопки выбора постройки
/// Теперь использует BuildingStats через BuildingStatsManager
/// </summary>
public class BuildingPlacementButton : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private BuildingType buildingType = BuildingType.Farm; // Тип постройки из BuildingStatsManager
    [SerializeField] private Button button; // Кнопка (найдет автоматически, если не указана)
    [SerializeField] private BuildingManager buildingManager; // Менеджер построек (найдет автоматически, если не указан)
    
    private BuildingStats buildingStats; // Статы постройки (из BuildingStatsManager)
    
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
        
        // Инициализируем BuildingStats из BuildingStatsManager
        InitializeBuildingStats();
        
        // Подписываемся на событие клика кнопки
        button.onClick.AddListener(OnButtonClick);
        
        // Обновляем текст и иконку кнопки
        UpdateButtonUI();
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
        if (buildingStats == null)
        {
            Debug.LogWarning($"BuildingPlacementButton: BuildingStats не найден для типа {buildingType}!");
            return;
        }
        
        // Создаем BuildingInfo из BuildingStats и выбираем постройку для установки
        BuildingInfo buildingInfo = CreateBuildingInfoFromStats(buildingStats);
        buildingManager.SelectBuilding(buildingInfo);
        Debug.Log($"BuildingPlacementButton: Выбрана постройка '{buildingStats.displayName}' (тип: {buildingType})");
    }
    
    /// <summary>
    /// Создает BuildingInfo из BuildingStats
    /// </summary>
    private BuildingInfo CreateBuildingInfoFromStats(BuildingStats stats)
    {
        BuildingInfo info = new BuildingInfo
        {
            buildingStats = stats,
            // Поля для обратной совместимости заполняются автоматически через методы GetName(), GetSprite() и т.д.
            name = stats.displayName,
            sprite = stats.sprite,
            buildingType = stats.buildingType,
            description = stats.description
        };
        return info;
    }
    
    /// <summary>
    /// Обновляет текст и иконку кнопки
    /// </summary>
    private void UpdateButtonUI()
    {
        if (buildingStats == null)
            return;
        
        // Обновляем текст кнопки
        Text buttonText = GetComponentInChildren<Text>();
        if (buttonText != null)
        {
            buttonText.text = buildingStats.displayName;
        }
        
        // Обновляем иконку кнопки, если есть Image компонент
        Image buttonImage = button.image;
        if (buttonImage != null && buildingStats.icon != null)
        {
            buttonImage.sprite = buildingStats.icon;
        }
    }
    
    /// <summary>
    /// Инициализирует BuildingStats из BuildingStatsManager
    /// </summary>
    private void InitializeBuildingStats()
    {
        if (BuildingStatsManager.Instance == null)
        {
            Debug.LogError($"BuildingPlacementButton: BuildingStatsManager.Instance равен null! Кнопка для типа {buildingType} не будет работать.");
            button.interactable = false;
            return;
        }
        
        buildingStats = BuildingStatsManager.Instance.GetBuildingStats(buildingType);
        if (buildingStats == null)
        {
            Debug.LogWarning($"BuildingPlacementButton: BuildingStats не найден для типа {buildingType}. Проверьте настройки в BuildingStatsManager.");
            button.interactable = false;
        }
        else
        {
            button.interactable = true;
        }
    }
    
    /// <summary>
    /// Устанавливает тип постройки (можно вызвать извне)
    /// </summary>
    public void SetBuildingType(BuildingType type)
    {
        buildingType = type;
        InitializeBuildingStats();
        UpdateButtonUI();
    }
}



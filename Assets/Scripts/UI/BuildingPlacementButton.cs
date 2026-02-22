using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Скрипт для кнопки выбора постройки
/// Использует BuildingStats через BuildingStatsManager по id или прямой ссылке
/// </summary>
public class BuildingPlacementButton : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private string buildingId = "house"; // Id постройки из BuildingStatsManager
    [SerializeField] private BuildingStats buildingStatsDirect; // Прямая ссылка на BuildingStats (приоритет над buildingId)
    [SerializeField] private Button button; // Кнопка (найдет автоматически, если не указана)
    [SerializeField] private BuildingManager buildingManager; // Менеджер построек (найдет автоматически, если не указан)
    
    private BuildingStats buildingStats; // Статы постройки (из BuildingStatsManager или прямой ссылки)
    
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
            Debug.LogWarning("BuildingPlacementButton: BuildingStats не найден!");
            return;
        }
        
        // Создаем BuildingInfo из BuildingStats и выбираем постройку для установки
        BuildingInfo buildingInfo = CreateBuildingInfoFromStats(buildingStats);
        buildingManager.SelectBuilding(buildingInfo);
        Debug.Log($"BuildingPlacementButton: Выбрана постройка '{buildingStats.displayName}' (id: {buildingStats.id})");
    }
    
    /// <summary>
    /// Создает BuildingInfo из BuildingStats
    /// </summary>
    private BuildingInfo CreateBuildingInfoFromStats(BuildingStats stats)
    {
        BuildingInfo info = new BuildingInfo
        {
            buildingStats = stats,
            name = stats.displayName,
            sprite = stats.sprite,
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
        buildingStats = buildingStatsDirect;
        if (buildingStats == null && BuildingStatsManager.Instance != null && !string.IsNullOrEmpty(buildingId))
            buildingStats = BuildingStatsManager.Instance.GetBuildingStatsById(buildingId);

        if (buildingStats == null)
        {
            Debug.LogWarning("BuildingPlacementButton: BuildingStats не найден. Укажите buildingStatsDirect или buildingId.");
            if (button != null) button.interactable = false;
        }
        else if (button != null)
        {
            button.interactable = true;
        }
    }
    
    /// <summary>
    /// Устанавливает постройку по id (можно вызвать извне)
    /// </summary>
    public void SetBuildingId(string id)
    {
        buildingId = id ?? "";
        buildingStatsDirect = null;
        InitializeBuildingStats();
        UpdateButtonUI();
    }
}



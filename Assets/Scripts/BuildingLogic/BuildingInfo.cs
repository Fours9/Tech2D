using UnityEngine;

/// <summary>
/// Информация о типе постройки
/// Может использовать BuildingStats (ScriptableObject) или хранить данные напрямую (для обратной совместимости)
/// </summary>
[System.Serializable]
public class BuildingInfo
{
    [Header("Ссылка на BuildingStats (предпочтительно)")]
    public BuildingStats buildingStats; // ScriptableObject с данными типа постройки
    
    [Header("Прямые значения (используются, если buildingStats не задан)")]
    public string name; // Название постройки
    public Sprite sprite; // Спрайт постройки
    public BuildingType buildingType; // Тип постройки
    public string description; // Описание постройки
    
    /// <summary>
    /// Получает название постройки (из BuildingStats или из прямого поля)
    /// </summary>
    public string GetName()
    {
        if (buildingStats != null && !string.IsNullOrEmpty(buildingStats.displayName))
            return buildingStats.displayName;
        return name;
    }
    
    /// <summary>
    /// Получает спрайт постройки (из BuildingStats или из прямого поля)
    /// </summary>
    public Sprite GetSprite()
    {
        if (buildingStats != null && buildingStats.sprite != null)
            return buildingStats.sprite;
        return sprite;
    }
    
    /// <summary>
    /// Получает тип постройки (из BuildingStats или из прямого поля)
    /// </summary>
    public BuildingType GetBuildingType()
    {
        if (buildingStats != null)
            return buildingStats.buildingType;
        return buildingType;
    }
    
    /// <summary>
    /// Получает описание постройки (из BuildingStats или из прямого поля)
    /// </summary>
    public string GetDescription()
    {
        if (buildingStats != null && !string.IsNullOrEmpty(buildingStats.description))
            return buildingStats.description;
        return description;
    }
}

/// <summary>
/// Типы построек
/// </summary>
public enum BuildingType
{
    Farm,        // Ферма
    Mine,        // Шахта
    LumberMill,  // Лесопилка
    Quarry,      // Каменоломня
    Windmill,    // Ветряная мельница
    Custom       // Пользовательская постройка
}



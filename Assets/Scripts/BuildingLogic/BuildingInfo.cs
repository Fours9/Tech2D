using UnityEngine;

/// <summary>
/// Информация о типе постройки
/// </summary>
[System.Serializable]
public class BuildingInfo
{
    public string name; // Название постройки
    public Sprite sprite; // Спрайт постройки
    public BuildingType buildingType; // Тип постройки
    public string description; // Описание постройки
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



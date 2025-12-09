using UnityEngine;

/// <summary>
/// Данные типа ресурса: визуал и свойства.
/// Хранится как ScriptableObject-ассет.
/// </summary>
[CreateAssetMenu(fileName = "ResourceStats", menuName = "Tech2D/Resource Stats", order = 3)]
public class ResourceStats : ScriptableObject
{
    [Header("Идентификатор")]
    public string id;              // Внутренний ID (уникальный ключ, латиницей)
    public string displayName;     // Имя для UI
    [TextArea]
    public string description;     // Описание

    [Header("Тип ресурса")]
    // TODO: Создать enum для типов ресурсов на клетках (отдельно от ResourceType для ресурсов игрока)
    // public CellResourceType resourceType; // Enum тип ресурса

    [Header("Визуал")]
    public Sprite icon;            // Иконка ресурса для UI
    public Sprite sprite;          // Спрайт ресурса для отображения на клетке
}


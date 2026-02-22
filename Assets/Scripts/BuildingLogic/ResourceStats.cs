using UnityEngine;

/// <summary>
/// Определение ресурса (Food, Wood, Board): id, type, визуал.
/// Используется в ResourceStatEntry и ResourceStatModifier как ссылка.
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
    [Tooltip("Категория для применения модификаторов (Plant, RawMaterial)")]
    public ResourceStatType type = ResourceStatType.RawMaterial;

    [Header("Визуал")]
    public Sprite icon;            // Иконка для UI
    public Sprite sprite;          // Спрайт для отображения на клетке
}

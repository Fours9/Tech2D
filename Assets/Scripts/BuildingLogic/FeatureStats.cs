using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Данные фичи на клетке (например Forest): визуал и вклад в ресурсы.
/// Хранится как ScriptableObject-ассет.
/// </summary>
[CreateAssetMenu(fileName = "FeatureStats", menuName = "Tech2D/Feature Stats", order = 3)]
public class FeatureStats : ScriptableObject
{
    [Header("Идентификатор")]
    public string id;              // Внутренний ID (уникальный ключ, латиницей)
    public string displayName;     // Имя для UI
    [TextArea]
    public string description;     // Описание

    [Header("Визуал")]
    public Sprite icon;            // Иконка для UI
    public Sprite sprite;          // Спрайт для отображения на клетке

    [Header("Ресурсы")]
    [Tooltip("Что даёт фича: напр. wood +10, food -5")]
    public List<ResourceStatEntry> resourceEntries = new List<ResourceStatEntry>();
}

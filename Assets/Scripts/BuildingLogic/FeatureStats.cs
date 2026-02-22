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
    [Tooltip("Что даёт фича: ссылка на ResourceStats + значение, напр. Wood +10, Food -5")]
    public List<ResourceStatEntry> resourceEntries = new List<ResourceStatEntry>();

    [Header("Модификаторы ресурсов")]
    [Tooltip("В каждой записи: ИЛИ ссылка на ассет (resourceRef), ИЛИ тип (targetType). Модификатор в [-1, ∞).")]
    public List<ResourceStatModifier> resourceModifiers = new List<ResourceStatModifier>();

    [Header("Бонусы (Cell/City/Player)")]
    [Tooltip("В каждой записи: ИЛИ ссылка на ассет (targetResource), ИЛИ тип (targetType). Передаются при опросе городами.")]
    public List<ResourceBonus> resourceBonuses = new List<ResourceBonus>();

    public List<ResourceBonus> GetResourceBonuses() => resourceBonuses ?? new List<ResourceBonus>();
}

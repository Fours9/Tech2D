using UnityEngine;
using System.Collections.Generic;
using CellNameSpace;

/// <summary>
/// Данные типа клетки: статы движения, проходимость, визуал, модификаторы ресурсов.
/// Хранится как ScriptableObject-ассет.
/// </summary>
[CreateAssetMenu(fileName = "CellTypeStats", menuName = "Tech2D/Cell Type Stats", order = 1)]
public class CellTypeStats : ScriptableObject
{
    [Header("Идентификатор")]
    public string id;              // Внутренний ID (уникальный ключ, латиницей)
    public string displayName;     // Имя для UI
    [TextArea]
    public string description;     // Описание

    [Header("Тип клетки")]
    public CellType cellType;      // Enum тип для обратной совместимости

    [Header("Движение")]
    public int movementCost = 1;   // Стоимость перемещения по клетке
    public bool isWalkable = true; // Проходима ли клетка для юнитов

    [Header("Визуал")]
    public Material material;      // Материал для этого типа клетки (опционально)
    public Color baseColor = Color.white; // Базовый цвет (используется, если material не задан)

    [Header("Модификаторы ресурсов")]
    [Tooltip("В каждой записи указать ИЛИ ссылку на ассет ресурса (resourceRef), ИЛИ тип (targetType). Модификатор в [-1, ∞).")]
    public List<ResourceStatModifier> resourceModifiers = new List<ResourceStatModifier>();

    [Header("Ресурсы по умолчанию")]
    [Tooltip("Базовые значения ресурсов для пустой клетки, напр. food +10")]
    public List<ResourceStatEntry> defaultResources = new List<ResourceStatEntry>();

    [Header("Бонусы (Cell/City/Player)")]
    [Tooltip("В каждой записи: ИЛИ ссылка на ассет (targetResource), ИЛИ тип (targetType). Передаются при опросе городами.")]
    public List<ResourceBonus> resourceBonuses = new List<ResourceBonus>();

    public List<ResourceBonus> GetResourceBonuses() => resourceBonuses ?? new List<ResourceBonus>();
}

/// <summary>
/// Obsolete: оставлен для совместимости с существующими ассетами. Используйте CellTypeStats.
/// </summary>
[System.Obsolete("Use CellTypeStats")]
public class CellStats : CellTypeStats { }




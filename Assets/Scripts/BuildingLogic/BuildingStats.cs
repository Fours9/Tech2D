using UnityEngine;
using System.Collections.Generic;
using CellNameSpace;

/// <summary>
/// Данные типа постройки: стоимость, доходы, требования.
/// Хранится как ScriptableObject-ассет.
/// </summary>
[CreateAssetMenu(fileName = "BuildingStats", menuName = "Tech2D/Building Stats", order = 2)]
public class BuildingStats : ScriptableObject
{
    [Header("Идентификатор")]
    public string id;              // Внутренний ID (уникальный ключ, латиницей)
    public string displayName;     // Имя для UI
    [TextArea]
    public string description;     // Описание

    [Header("Тип постройки")]
    public BuildingType buildingType; // Enum тип для обратной совместимости

    [Header("Визуал")]
    public Sprite icon;            // Иконка постройки для UI
    public Sprite sprite;          // Спрайт постройки для отображения на клетке

    [Header("Стоимость")]
    public int costGold = 10;
    public int costFood = 0;
    public int costMaterials = 0;

    [Header("Доходы (за ход)")]
    public int incomeGold = 0;
    public int incomeFood = 0;
    public int incomeMaterials = 0;

    [Header("Требования")]
    [Tooltip("Типы клеток, на которых можно строить эту постройку. Пустой список = можно строить везде.")]
    public List<CellType> allowedCellTypes = new List<CellType>();

    [Header("Движение")]
    [Tooltip("Изменение стоимости перемещения по клетке с постройкой")]
    public int movementCostDelta = 0;

    [Header("Ресурсы (потребление/производство)")]
    [Tooltip("Ссылка на ResourceStats + значение: напр. Wood -5, Board +5")]
    public List<ResourceStatEntry> resourceEntries = new List<ResourceStatEntry>();

    [Header("Модификаторы ресурсов")]
    [Tooltip("В каждой записи: ИЛИ ссылка на ассет (resourceRef), ИЛИ тип (targetType). Модификатор в [-1, ∞).")]
    public List<ResourceStatModifier> resourceModifiers = new List<ResourceStatModifier>();

    [Header("Бонусы (Cell/City/Player)")]
    [Tooltip("В каждой записи: ИЛИ ссылка на ассет (targetResource), ИЛИ тип (targetType). Передаются при опросе городами.")]
    public List<ResourceBonus> resourceBonuses = new List<ResourceBonus>();

    public List<ResourceBonus> GetResourceBonuses() => resourceBonuses ?? new List<ResourceBonus>();
}




using UnityEngine;

/// <summary>
/// Данные типа юнита: статы, визуал, стоимость.
/// Хранится как ScriptableObject-ассет.
/// </summary>
[CreateAssetMenu(fileName = "UnitStats", menuName = "Tech2D/Unit Stats", order = 0)]
public class UnitStats : ScriptableObject
{
    [Header("Идентификатор")]
    public string id;              // Внутренний ID (уникальный ключ, латиницей)
    public string displayName;     // Имя для UI
    [TextArea]
    public string description;     // Описание

    [Header("Визуал")]
    public Sprite icon;            // Иконка юнита для UI
    public GameObject prefab;      // Префаб этого типа юнита (опционально)

    [Header("Базовые статы")]
    public int maxHealth = 10;
    public int attack = 2;
    public int defense = 1;
    public int movementPoints = 4;

    [Header("Экономика")]
    public int costGold = 10;
    public int upkeepGold = 1;
}



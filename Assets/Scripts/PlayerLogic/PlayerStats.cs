using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Данные расы игрока: приоритет, слоты действий, бонусы, особые юниты.
/// Хранится как ScriptableObject-ассет.
/// </summary>
[CreateAssetMenu(fileName = "PlayerStats", menuName = "Tech2D/Player Stats", order = 4)]
public class PlayerStats : ScriptableObject
{
    [Header("Идентификатор")]
    public string id;
    public string displayName;
    [TextArea]
    public string description;

    [Header("Очередь исполнения")]
    [Tooltip("Очки приоритета: выше — раньше в раунде исполнения")]
    public int priorityPoints = 0;
    [Tooltip("Число слотов действий за ход")]
    public int actionSlots = 5;

    [Header("Расовые бонусы")]
    public List<ResourceBonus> resourceBonuses = new List<ResourceBonus>();

    [Header("Особые юниты расы")]
    public List<UnitStats> specialUnits = new List<UnitStats>();
}

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Информация об игроке
/// </summary>
[System.Serializable]
public class PlayerInfo
{
    [Header("Идентификатор")]
    public int playerId = 0; // Уникальный ID игрока
    public string playerName = "Player"; // Имя игрока
    
    [Header("Визуал")]
    public Color playerColor = Color.white; // Цвет игрока для визуализации территории

    [Header("Бонусы (Cell/City/Player)")]
    public List<ResourceBonus> bonuses = new List<ResourceBonus>();

    /// <summary> Список бонусов игрока (Cell/City/Player). </summary>
    public List<ResourceBonus> Bonuses => bonuses;

    /// <summary>
    /// Конструктор для создания игрока
    /// </summary>
    public PlayerInfo(int id, string name, Color color)
    {
        playerId = id;
        playerName = name;
        playerColor = color;
    }
    
    /// <summary>
    /// Конструктор по умолчанию
    /// </summary>
    public PlayerInfo()
    {
        playerId = 0;
        playerName = "Player";
        playerColor = Color.white;
    }
}


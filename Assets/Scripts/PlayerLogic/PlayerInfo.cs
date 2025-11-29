using UnityEngine;

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


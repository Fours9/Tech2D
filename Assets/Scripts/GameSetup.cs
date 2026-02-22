using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Настройка игрока для GameSetup (id, имя, цвет).
/// </summary>
[System.Serializable]
public class PlayerSetupConfig
{
    public int id = 0;
    public string displayName = "Player";
    public Color color = Color.white;
    [Tooltip("Раса игрока (обязательно).")]
    public PlayerStats playerStats;
}

/// <summary>
/// Единая точка bootstrap: создание игроков, инициализация их кешей, регистрация в PlayerManager.
/// Script Execution Order: -100 (раньше менеджеров).
/// </summary>
public class GameSetup : MonoBehaviour
{
    [Header("Игроки")]
    [Tooltip("Список игроков для мультиплеера. Минимум 1. id должен быть уникальным.")]
    [SerializeField] private List<PlayerSetupConfig> playersConfig = new List<PlayerSetupConfig>
    {
        new PlayerSetupConfig { id = 0, displayName = "Player 1", color = new Color(0.2f, 0.4f, 0.9f) }
    };

    private void Start()
    {
        // Создаём игроков из конфига
        var players = new List<PlayerInfo>();
        foreach (var c in playersConfig ?? new List<PlayerSetupConfig>())
        {
            players.Add(new PlayerInfo(c.id, c.displayName ?? "Player", c.color, c.playerStats));
        }
        if (players.Count == 0)
        {
            players.Add(new PlayerInfo(0, "Player 1", new Color(0.2f, 0.4f, 0.9f), null));
        }

        // Инициализируем кеши статов из FirstStatsManager
        if (FirstStatsManager.Instance != null)
        {
            foreach (var p in players)
            {
                p.InitializeBuildingCache(FirstStatsManager.Instance.GetAllBuildingStats());
                p.InitializeFeatureCache(FirstStatsManager.Instance.GetAllFeatureStats());
                p.InitializeCellTypeCache(FirstStatsManager.Instance.GetAllCellTypeStats());
            }
        }
        else
        {
            Debug.LogWarning("GameSetup: FirstStatsManager.Instance == null. Кеши статов не инициализированы.");
        }

        // Регистрируем в PlayerManager
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.Initialize(players);
        }
        else
        {
            Debug.LogWarning("GameSetup: PlayerManager.Instance == null. Игроки не зарегистрированы.");
        }
    }
}

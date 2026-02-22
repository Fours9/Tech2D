using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Единая точка bootstrap: создание игроков, инициализация их кешей, регистрация в PlayerManager.
/// Script Execution Order: -100 (раньше менеджеров).
/// </summary>
public class GameSetup : MonoBehaviour
{
    private void Start()
    {
        // Создаём игроков
        var players = new List<PlayerInfo>
        {
            new PlayerInfo(0, "Player 1", new Color(0.2f, 0.4f, 0.9f))
        };

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

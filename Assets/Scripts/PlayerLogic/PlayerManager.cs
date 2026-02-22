using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Реестр игроков. Игроки создаются в GameSetup с начала игры.
/// </summary>
public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    private List<PlayerInfo> players = new List<PlayerInfo>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (transform.parent != null)
        {
            transform.SetParent(null);
        }

        DontDestroyOnLoad(gameObject);
    }

    /// <summary> Регистрирует игроков (вызывается из GameSetup). </summary>
    public void Initialize(List<PlayerInfo> playersList)
    {
        players = playersList != null ? new List<PlayerInfo>(playersList) : new List<PlayerInfo>();
    }

    /// <summary> Получает игрока по ownerId. </summary>
    public PlayerInfo GetPlayerByOwnerId(int ownerId)
    {
        return players.Find(p => p != null && p.playerId == ownerId);
    }

    /// <summary> Все игроки. </summary>
    public List<PlayerInfo> GetAllPlayers()
    {
        return new List<PlayerInfo>(players);
    }
}

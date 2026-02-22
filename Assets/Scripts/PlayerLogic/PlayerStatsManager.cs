using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Централизованный менеджер для PlayerStats (ScriptableObject ассетами рас).
/// Обеспечивает доступ к статам рас по id.
/// </summary>
public class PlayerStatsManager : MonoBehaviour
{
    public static PlayerStatsManager Instance { get; private set; }

    [Header("Статы рас")]
    [SerializeField] private List<PlayerStats> playerStatsList = new List<PlayerStats>();

    private Dictionary<string, PlayerStats> playerStatsDictionary;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (transform.parent != null)
            transform.SetParent(null);

        DontDestroyOnLoad(gameObject);
        InitializeDictionary();
    }

    private void InitializeDictionary()
    {
        playerStatsDictionary = new Dictionary<string, PlayerStats>();

        if (playerStatsList == null) return;

        foreach (var s in playerStatsList)
        {
            if (s != null && !string.IsNullOrEmpty(s.id))
                playerStatsDictionary[s.id] = s;
        }
    }

    /// <summary> Получает PlayerStats по id. </summary>
    public PlayerStats GetPlayerStatsById(string id)
    {
        if (playerStatsDictionary == null)
            InitializeDictionary();

        if (string.IsNullOrEmpty(id)) return null;

        return playerStatsDictionary.TryGetValue(id, out var stats) ? stats : null;
    }

    /// <summary> Возвращает все PlayerStats для UI выбора расы. </summary>
    public List<PlayerStats> GetAllPlayerStats()
    {
        return playerStatsList != null ? new List<PlayerStats>(playerStatsList) : new List<PlayerStats>();
    }

    public void RefreshPlayerStats()
    {
        InitializeDictionary();
    }
}

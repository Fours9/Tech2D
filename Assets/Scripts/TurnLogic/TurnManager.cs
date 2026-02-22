using System.Collections.Generic;
using System;
using UnityEngine;
using CellNameSpace;

/// <summary>
/// Состояния хода
/// </summary>
public enum TurnState
{
    Planning,   // Фаза планирования (игрок ставит приказы)
    Resolving,  // Фаза исполнения (приказы применяются к миру)
    Waiting     // Режим ожидания (на будущее, для мультиплеера/переходов)
}

/// <summary>
/// Централизованный менеджер ходов (WEGO)
/// Пока отвечает только за номер хода и смену фаз.
/// Позже сюда добавятся приказы, экономика и мультиплеерная логика.
/// </summary>
public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }
    
    /// <summary>
    /// Событие, вызываемое при начале фазы планирования (когда сбрасываются очки движения)
    /// </summary>
    public static event Action OnPlanningPhaseStarted;

    [Header("Состояние хода")]
    [SerializeField] private int currentTurn = 1;
    [SerializeField] private TurnState currentState = TurnState.Planning;

    [Header("Ссылки на менеджеры (опционально)")]
    [SerializeField] private UnitManager unitManager;
    [SerializeField] private CityManager cityManager;
    [SerializeField] private BuildingManager buildingManager;
    [SerializeField] private ResourceManager resourceManager;

    /// <summary>
    /// Очередь приказов текущего хода.
    /// Пока один список; позже можно разделить по типам/приоритетам.
    /// </summary>
    private readonly List<TurnOrder> currentOrders = new List<TurnOrder>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        
        // Перемещаем GameObject в корень, если он дочерний (DontDestroyOnLoad работает только для корневых объектов)
        if (transform.parent != null)
        {
            transform.SetParent(null);
        }
        
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Если ссылки не заданы в инспекторе, пытаемся найти их в сцене
        if (unitManager == null)
            unitManager = FindFirstObjectByType<UnitManager>();

        if (cityManager == null)
            cityManager = FindFirstObjectByType<CityManager>();

        if (buildingManager == null)
            buildingManager = FindFirstObjectByType<BuildingManager>();

        if (resourceManager == null)
            resourceManager = FindFirstObjectByType<ResourceManager>();

        // Стартуем игру с фазы планирования первого хода
        StartPlanningPhase();
    }

    /// <summary>
    /// Текущий номер хода
    /// </summary>
    public int GetCurrentTurn()
    {
        return currentTurn;
    }

    /// <summary>
    /// Текущее состояние (фаза) хода
    /// </summary>
    public TurnState GetCurrentState()
    {
        return currentState;
    }

    /// <summary>
    /// Публичный доступ к менеджерам (для приказов и других систем).
    /// </summary>
    public UnitManager GetUnitManager() => unitManager;
    public CityManager GetCityManager() => cityManager;
    public BuildingManager GetBuildingManager() => buildingManager;
    public ResourceManager GetResourceManager() => resourceManager;

    /// <summary>
    /// Добавить приказ в очередь текущего хода (можно вызывать только в фазе планирования).
    /// </summary>
    public void EnqueueOrder(TurnOrder order)
    {
        if (order == null)
            return;

        if (currentState != TurnState.Planning)
        {
            Debug.LogWarning("TurnManager: Попытка добавить приказ вне фазы планирования");
            return;
        }

        // Если это приказ на перемещение юнита, удаляем предыдущие приказы
        // перемещения для того же юнита из очереди текущего хода.
        if (order is MoveUnitOrder moveOrder)
        {
            // Мы не хотим, чтобы один и тот же юнит имел несколько приказов движения:
            // должна выполняться только последняя команда игрока.
            int removed = currentOrders.RemoveAll(existing =>
                existing is MoveUnitOrder existingMove &&
                ReferenceEquals(
                    GetUnitControllerFromMove(existingMove),
                    GetUnitControllerFromMove(moveOrder)
                )
            );

        }

        currentOrders.Add(order);
    }

    /// <summary>
    /// Вспомогательный метод для извлечения UnitController из MoveUnitOrder.
    /// Нужен, чтобы можно было сравнивать приказы по юниту.
    /// </summary>
    private UnitController GetUnitControllerFromMove(MoveUnitOrder moveOrder)
    {
        // Используем reflection, чтобы не раскрывать внутренности MoveUnitOrder наружу.
        if (moveOrder == null) return null;

        var field = typeof(MoveUnitOrder).GetField("unitController", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field?.GetValue(moveOrder) as UnitController;
    }

    /// <summary>
    /// Включает фазу планирования.
    /// Игрок может ставить приказы, но мир ещё не обновляется.
    /// </summary>
    public void StartPlanningPhase()
    {
        currentState = TurnState.Planning;

        // В начале фазы планирования сбрасываем очки движения у всех юнитов
        if (unitManager == null)
        {
            unitManager = FindFirstObjectByType<UnitManager>();
        }

        if (unitManager != null)
        {
            var units = unitManager.GetSpawnedUnits();
            foreach (var go in units)
            {
                if (go == null) continue;
                UnitInfo info = go.GetComponent<UnitInfo>();
                if (info != null)
                {
                    info.ResetMovementPoints();
                }
            }
        }
        
        // Уведомляем все системы о начале фазы планирования (для обновления подсветки и т.д.)
        OnPlanningPhaseStarted?.Invoke();
    }

    /// <summary>
    /// Завершает фазу планирования и запускает фазу исполнения.
    /// Приказы будут выполняться по очереди в Update, пока все не завершатся.
    /// </summary>
    public void EndPlanningAndResolve()
    {
        if (currentState != TurnState.Planning)
            return;

        currentState = TurnState.Resolving;

        // Подготавливаем очередь приказов для последовательного исполнения
        ResolveOrders();
    }

    /// <summary>
    /// Подготавливает очередь приказов текущего хода к исполнению.
    /// Сами приказы исполняются по одному в Update.
    /// </summary>
    private void ResolveOrders()
    {
        // Очищаем состояние прошлой фазы исполнения
        _pendingOrders.Clear();
        _activeOrder = null;
        _isResolvingOrders = false;

        if (currentOrders.Count == 0)
        {
            // Даже если приказов нет, окончание хода обработаем в Update,
            // когда увидим, что очередь пуста.
            _isResolvingOrders = true;
            return;
        }

        // Сортируем приказы по приоритету (чем меньше, тем раньше)
        currentOrders.Sort((a, b) => a.Priority.CompareTo(b.Priority));

        // Переносим приказы в очередь для поочерёдного исполнения
        _pendingOrders.AddRange(currentOrders);
        currentOrders.Clear();

        _activeOrder = null;
        _isResolvingOrders = true;

    }

    // ---- Новая логика поочерёдного исполнения приказов ----

    private readonly List<TurnOrder> _pendingOrders = new List<TurnOrder>();
    private TurnOrder _activeOrder = null;
    private bool _isResolvingOrders = false;

    private void Update()
    {
        if (currentState != TurnState.Resolving || !_isResolvingOrders)
            return;

        // Если сейчас нет активного приказа — берём следующий
        if (_activeOrder == null)
        {
            if (_pendingOrders.Count == 0)
            {
                // Все приказы выполнены: применяем экономику и завершаем ход
                _isResolvingOrders = false;
                ApplyEndTurnEconomy();
                FinishTurn();
                return;
            }

            _activeOrder = _pendingOrders[0];
            _pendingOrders.RemoveAt(0);

            try
            {
                _activeOrder.Execute(this);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"TurnManager: Ошибка при запуске приказа {_activeOrder.GetDescription()}: {ex}");
                _activeOrder = null;
            }
        }
        else
        {
            // Ждём завершения активного приказа (для движения юнита — пока он не дойдёт)
            if (_activeOrder.IsComplete)
            {
                _activeOrder = null;
            }
        }
    }

    /// <summary>
    /// Применяет экономику конца хода: городоцентричная модель — города опрашивают клетки, доход начисляется владельцам с учётом бонусов.
    /// </summary>
    private void ApplyEndTurnEconomy()
    {
        if (resourceManager == null)
            resourceManager = FindFirstObjectByType<ResourceManager>();
        if (resourceManager == null)
        {
            Debug.LogWarning("TurnManager: ResourceManager не найден, экономика конца хода пропущена");
            return;
        }

        if (cityManager == null)
            cityManager = FindFirstObjectByType<CityManager>();
        if (cityManager == null) return;

        CellNameSpace.Grid grid = FindFirstObjectByType<CellNameSpace.Grid>();
        if (grid == null)
        {
            Debug.LogWarning("TurnManager: Grid не найден, экономика конца хода пропущена");
            return;
        }

        var allCities = cityManager.GetAllCities();
        var resourcesByOwner = new Dictionary<int, List<ResourceIncomeEntry>>();
        var playerBonusesByOwner = new Dictionary<int, List<ResourceBonus>>();

        foreach (var kvp in allCities)
        {
            CityInfo city = kvp.Value;
            if (city == null) continue;

            int ownerId = city.ownerId;
            CityResourceIncomeResult cityResult = city.GetResourceIncome(grid);

            if (!resourcesByOwner.ContainsKey(ownerId))
            {
                resourcesByOwner[ownerId] = new List<ResourceIncomeEntry>();
                playerBonusesByOwner[ownerId] = new List<ResourceBonus>();
            }

            foreach (var e in cityResult.resources)
                resourcesByOwner[ownerId].Add(e);
            foreach (var b in cityResult.playerBonusesToPass)
                playerBonusesByOwner[ownerId].Add(b);
        }

        foreach (int ownerId in resourcesByOwner.Keys)
        {
            List<ResourceIncomeEntry> list = resourcesByOwner[ownerId];
            var playerBonuses = new List<ResourceBonus>(playerBonusesByOwner[ownerId]);
            PlayerInfo ownerPlayer = GetPlayerByOwnerId(ownerId);
            if (ownerPlayer?.Bonuses != null)
                foreach (var b in ownerPlayer.Bonuses)
                    if (b.applicationLevel == BonusApplicationLevel.Player)
                        playerBonuses.Add(b);

            var sumByResourceId = new Dictionary<string, (float amount, ResourceStatType type)>();
            foreach (var e in list)
            {
                if (!sumByResourceId.ContainsKey(e.resourceId))
                    sumByResourceId[e.resourceId] = (0f, e.resourceStatType);
                var t = sumByResourceId[e.resourceId];
                sumByResourceId[e.resourceId] = (t.amount + e.amount, t.type);
            }

            var aggregated = new Dictionary<string, float>();
            foreach (var kv in sumByResourceId)
            {
                float sumFlat = 0f;
                float sumPercent = 0f;
                foreach (var b in playerBonuses)
                {
                    bool match = b.targetResource != null ? (b.targetResource.id == kv.Key) : b.targetType == kv.Value.type;
                    if (!match) continue;
                    sumFlat += b.flatValue;
                    sumPercent += b.EffectivePercent;
                }
                aggregated[kv.Key] = (kv.Value.amount + sumFlat) * (1f + sumPercent);
            }

            foreach (var kv in aggregated)
            {
                var rs = FirstStatsManager.Instance?.GetResourceStatsById(kv.Key);
                if (rs == null || !rs.contributesToIncome) continue;

                float rounded = Mathf.Round(kv.Value);
                if (Mathf.Abs(rounded) > 0.0001f)
                    resourceManager.Add(ownerId, kv.Key, rounded);
            }
        }
    }

    private PlayerInfo GetPlayerByOwnerId(int ownerId)
    {
        if (PlayerManager.Instance != null)
            return PlayerManager.Instance.GetPlayerByOwnerId(ownerId);
        // Fallback: ищем по городам (если PlayerManager ещё не инициализирован)
        if (cityManager == null) return null;
        foreach (var kvp in cityManager.GetAllCities())
        {
            if (kvp.Value?.player != null && kvp.Value.player.playerId == ownerId)
                return kvp.Value.player;
        }
        return null;
    }

    /// <summary>
    /// Завершает текущий ход и запускает следующий.
    /// </summary>
    private void FinishTurn()
    {
        currentTurn++;
        StartNextTurn();
    }

    /// <summary>
    /// Подготовка и запуск следующего хода.
    /// Сейчас просто возвращаемся в фазу планирования.
    /// </summary>
    private void StartNextTurn()
    {
        StartPlanningPhase();
    }
}



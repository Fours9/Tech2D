using System.Collections.Generic;
using UnityEngine;

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

        currentOrders.Add(order);
        Debug.Log($"TurnManager: Приказ добавлен: {order.GetDescription()}");
    }

    /// <summary>
    /// Включает фазу планирования.
    /// Игрок может ставить приказы, но мир ещё не обновляется.
    /// </summary>
    public void StartPlanningPhase()
    {
        currentState = TurnState.Planning;
        Debug.Log($"TurnManager: Начало фазы планирования. Ход {currentTurn}");

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
        Debug.Log($"TurnManager: Завершение планирования, начало исполнения. Ход {currentTurn}");

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
            Debug.Log("TurnManager: Нет приказов для исполнения в этом ходу");
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

        Debug.Log($"TurnManager: Подготовлено {_pendingOrders.Count} приказ(ов) к исполнению");
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
                Debug.Log($"TurnManager: Запущен приказ: {_activeOrder.GetDescription()}");
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
                Debug.Log($"TurnManager: Приказ завершён: {_activeOrder.GetDescription()}");
                _activeOrder = null;
            }
        }
    }

    /// <summary>
    /// Применяет экономику конца хода: доход от городов и построек.
    /// </summary>
    private void ApplyEndTurnEconomy()
    {
        if (resourceManager == null)
        {
            resourceManager = FindFirstObjectByType<ResourceManager>();
        }

        if (resourceManager == null)
        {
            Debug.LogWarning("TurnManager: ResourceManager не найден, экономика конца хода пропущена");
            return;
        }

        int goldIncome = 0;
        int foodIncome = 0;
        int materialsIncome = 0;

        // Доход от городов: по 1 золоту за город (для начала)
        if (cityManager != null)
        {
            var allCities = cityManager.GetAllCities();
            goldIncome += allCities.Count;
        }

        // Доход от построек
        if (buildingManager != null)
        {
            var placed = buildingManager.GetAllPlacedBuildings();
            foreach (var kvp in placed)
            {
                BuildingInfo building = kvp.Value;
                if (building == null)
                    continue;

                switch (building.buildingType)
                {
                    case BuildingType.Farm:
                        foodIncome += 1;
                        break;
                    case BuildingType.Mine:
                    case BuildingType.LumberMill:
                    case BuildingType.Quarry:
                        materialsIncome += 1;
                        break;
                    case BuildingType.Windmill:
                        foodIncome += 1;
                        break;
                }
            }
        }

        if (goldIncome != 0)
            resourceManager.Add(ResourceType.Gold, goldIncome);
        if (foodIncome != 0)
            resourceManager.Add(ResourceType.Food, foodIncome);
        if (materialsIncome != 0)
            resourceManager.Add(ResourceType.Materials, materialsIncome);

        Debug.Log($"TurnManager: Экономика конца хода - золото +{goldIncome}, еда +{foodIncome}, материалы +{materialsIncome}");
    }

    /// <summary>
    /// Завершает текущий ход и запускает следующий.
    /// </summary>
    private void FinishTurn()
    {
        Debug.Log($"TurnManager: Ход {currentTurn} завершён");
        currentTurn++;
        StartNextTurn();
    }

    /// <summary>
    /// Подготовка и запуск следующего хода.
    /// Сейчас просто возвращаемся в фазу планирования.
    /// </summary>
    private void StartNextTurn()
    {
        Debug.Log($"TurnManager: Начало хода {currentTurn}");
        StartPlanningPhase();
    }
}



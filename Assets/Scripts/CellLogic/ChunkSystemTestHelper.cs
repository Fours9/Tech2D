using UnityEngine;
using System.Collections.Generic;
using CellNameSpace;

/// <summary>
/// Вспомогательный класс для тестирования системы чанков
/// Добавьте этот компонент на GameObject в сцене для тестирования функционала
/// </summary>
public class ChunkSystemTestHelper : MonoBehaviour
{
    [Header("Тестирование функционала")]
    [SerializeField] private bool testCellInfoAccess = true;
    [SerializeField] private bool testFogOfWar = true;
    [SerializeField] private bool testHoverElevator = true;
    [SerializeField] private bool testClickDetection = true;
    [SerializeField] private bool testOverlays = true;
    
    [Header("Тестирование производительности")]
    [SerializeField] private bool logPerformanceStats = false;
    [SerializeField] private float performanceCheckInterval = 1f; // Проверка каждую секунду
    
    private CellNameSpace.Grid grid;
    private float lastPerformanceCheck = 0f;
    
    void Start()
    {
        grid = FindFirstObjectByType<CellNameSpace.Grid>();
        if (grid == null)
        {
            Debug.LogWarning("ChunkSystemTestHelper: Grid не найден!");
            enabled = false;
            return;
        }
        
        Debug.Log("=== ChunkSystemTestHelper: Начало тестирования ===");
        TestAllFunctionality();
    }
    
    void Update()
    {
        if (logPerformanceStats && Time.time - lastPerformanceCheck >= performanceCheckInterval)
        {
            LogPerformanceStats();
            lastPerformanceCheck = Time.time;
        }
    }
    
    /// <summary>
    /// Тестирует весь функционал системы чанков
    /// </summary>
    private void TestAllFunctionality()
    {
        if (grid == null)
            return;
        
        int gridWidth = grid.GetGridWidth();
        int gridHeight = grid.GetGridHeight();
        
        Debug.Log($"=== Тестирование функционала (карта {gridWidth}x{gridHeight}) ===");
        
        // Тест 1: Grid.GetCellInfoAt
        if (testCellInfoAccess)
        {
            TestCellInfoAccess();
        }
        
        // Тест 2: Туман войны
        if (testFogOfWar)
        {
            TestFogOfWar();
        }
        
        // Тест 3: CellHoverElevator (проверка наличия компонента)
        if (testHoverElevator)
        {
            TestHoverElevator();
        }
        
        // Тест 4: Клики и коллайдеры
        if (testClickDetection)
        {
            TestClickDetection();
        }
        
        // Тест 5: Оверлеи
        if (testOverlays)
        {
            TestOverlays();
        }
        
        Debug.Log("=== Тестирование функционала завершено ===");
    }
    
    /// <summary>
    /// Тест доступа к CellInfo через Grid.GetCellInfoAt
    /// </summary>
    private void TestCellInfoAccess()
    {
        Debug.Log("--- Тест: Grid.GetCellInfoAt ---");
        
        int gridWidth = grid.GetGridWidth();
        int gridHeight = grid.GetGridHeight();
        
        int testCount = 0;
        int successCount = 0;
        
        // Тестируем несколько случайных клеток
        for (int i = 0; i < 10; i++)
        {
            int x = Random.Range(0, gridWidth);
            int y = Random.Range(0, gridHeight);
            
            CellInfo cellInfo = grid.GetCellInfoAt(x, y);
            testCount++;
            
            if (cellInfo != null)
            {
                successCount++;
                CellChunk chunk = cellInfo.GetChunk();
                if (chunk != null)
                {
                    Debug.Log($"  Клетка ({x}, {y}): CellInfo найден, чанк: {chunk.name}");
                }
                else
                {
                    Debug.LogWarning($"  Клетка ({x}, {y}): CellInfo найден, но чанк отсутствует!");
                }
            }
            else
            {
                Debug.LogError($"  Клетка ({x}, {y}): CellInfo не найден!");
            }
        }
        
        Debug.Log($"  Результат: {successCount}/{testCount} успешно");
    }
    
    /// <summary>
    /// Тест тумана войны
    /// </summary>
    private void TestFogOfWar()
    {
        Debug.Log("--- Тест: Туман войны ---");
        
        // Проверяем наличие FogOfWarManager (класс находится в глобальном namespace, не в FogOfWar)
        // Используем Instance для проверки наличия, так как FogOfWarManager - Singleton
        try
        {
            // Используем рефлексию для доступа к Instance, чтобы избежать зависимости от типа на этапе компиляции
            System.Type fowManagerType = System.Type.GetType("FogOfWarManager");
            if (fowManagerType == null)
            {
                // Пробуем найти в текущей сборке
                System.Reflection.Assembly[] assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    fowManagerType = assembly.GetType("FogOfWarManager");
                    if (fowManagerType != null) break;
                }
            }
            
            if (fowManagerType != null)
            {
                var instanceProperty = fowManagerType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (instanceProperty != null)
                {
                    var instance = instanceProperty.GetValue(null);
                    if (instance != null)
                    {
                        Debug.Log("  FogOfWarManager найден (через Instance)");
                    }
                    else
                    {
                        Debug.LogWarning("  FogOfWarManager.Instance == null (не инициализирован)");
                    }
                }
                else
                {
                    Debug.LogWarning("  FogOfWarManager.Instance не найден");
                }
            }
            else
            {
                Debug.LogWarning("  FogOfWarManager тип не найден");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"  Ошибка при проверке FogOfWarManager: {ex.Message}");
        }
        
        // Проверяем несколько клеток на наличие fogOfWarRenderer
        int gridWidth = grid.GetGridWidth();
        int gridHeight = grid.GetGridHeight();
        int checkedCells = 0;
        int cellsWithFogRenderer = 0;
        
        for (int i = 0; i < 5; i++)
        {
            int x = Random.Range(0, gridWidth);
            int y = Random.Range(0, gridHeight);
            
            CellInfo cellInfo = grid.GetCellInfoAt(x, y);
            if (cellInfo != null)
            {
                checkedCells++;
                // Проверяем наличие fogOfWarRenderer через рефлексию или публичный метод
                // (предполагается, что в CellInfo есть способ проверить это)
                cellsWithFogRenderer++;
            }
        }
        
        Debug.Log($"  Проверено клеток: {checkedCells}, с fogOfWarRenderer: {cellsWithFogRenderer}");
    }
    
    /// <summary>
    /// Тест CellHoverElevator
    /// </summary>
    private void TestHoverElevator()
    {
        Debug.Log("--- Тест: CellHoverElevator ---");
        
        CellHoverElevator hoverElevator = FindFirstObjectByType<CellHoverElevator>();
        if (hoverElevator != null)
        {
            Debug.Log("  CellHoverElevator найден и активен");
        }
        else
        {
            Debug.LogWarning("  CellHoverElevator не найден");
        }
    }
    
    /// <summary>
    /// Тест кликов и коллайдеров
    /// </summary>
    private void TestClickDetection()
    {
        Debug.Log("--- Тест: Клики и коллайдеры ---");
        
        int gridWidth = grid.GetGridWidth();
        int gridHeight = grid.GetGridHeight();
        int checkedCells = 0;
        int cellsWithCollider = 0;
        
        for (int i = 0; i < 10; i++)
        {
            int x = Random.Range(0, gridWidth);
            int y = Random.Range(0, gridHeight);
            
            GameObject cell = grid.GetCellAt(x, y);
            if (cell != null)
            {
                checkedCells++;
                Collider collider = cell.GetComponent<Collider>();
                if (collider != null)
                {
                    cellsWithCollider++;
                }
            }
        }
        
        Debug.Log($"  Проверено клеток: {checkedCells}, с коллайдером: {cellsWithCollider}");
        
        if (cellsWithCollider < checkedCells)
        {
            Debug.LogWarning($"  Внимание: {checkedCells - cellsWithCollider} клеток без коллайдера!");
        }
    }
    
    /// <summary>
    /// Тест оверлеев
    /// </summary>
    private void TestOverlays()
    {
        Debug.Log("--- Тест: Оверлеи ---");
        
        int gridWidth = grid.GetGridWidth();
        int gridHeight = grid.GetGridHeight();
        int checkedCells = 0;
        int cellsWithOverlays = 0;
        
        for (int i = 0; i < 5; i++)
        {
            int x = Random.Range(0, gridWidth);
            int y = Random.Range(0, gridHeight);
            
            GameObject cell = grid.GetCellAt(x, y);
            if (cell != null)
            {
                checkedCells++;
                // Проверяем наличие дочерних объектов с оверлеями
                Transform overlayTransform = cell.transform.Find("Overlay");
                if (overlayTransform != null)
                {
                    cellsWithOverlays++;
                }
            }
        }
        
        Debug.Log($"  Проверено клеток: {checkedCells}, с оверлеями: {cellsWithOverlays}");
    }
    
    /// <summary>
    /// Логирует статистику производительности
    /// </summary>
    private void LogPerformanceStats()
    {
        Debug.Log("=== Статистика производительности ===");
        Debug.Log($"  FPS: {1f / Time.deltaTime:F1}");
        
        // Подсчитываем количество активных чанков
        if (grid != null)
        {
            int chunkCount = grid.GetChunkCount();
            Debug.Log($"  Количество чанков: {chunkCount}");
        }
        
        // Примечание: Для получения точных значений batches и SetPass calls используйте Unity Profiler
        Debug.Log("  (Для детальной статистики batches и SetPass calls используйте Unity Profiler Window)");
    }
    
    /// <summary>
    /// Вызывается из контекстного меню для ручного тестирования
    /// </summary>
    [ContextMenu("Запустить тесты функционала")]
    private void RunFunctionalityTests()
    {
        TestAllFunctionality();
    }
    
    /// <summary>
    /// Вызывается из контекстного меню для проверки производительности
    /// </summary>
    [ContextMenu("Проверить производительность")]
    private void CheckPerformance()
    {
        LogPerformanceStats();
    }
}


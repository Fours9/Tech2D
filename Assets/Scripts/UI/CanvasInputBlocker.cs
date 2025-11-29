using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Блокирует все интерактивные элементы на Canvas во время стадии воспроизведения приказов.
/// Использует CanvasGroup для централизованного управления блокировкой.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class CanvasInputBlocker : MonoBehaviour
{
    [Header("Настройки")]
    [Tooltip("Если true, скрипт автоматически добавит CanvasGroup, если его нет")]
    [SerializeField] private bool autoAddCanvasGroup = true;
    
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private bool wasBlocked = false; // Флаг для отслеживания предыдущего состояния
    
    void Start()
    {
        canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("CanvasInputBlocker: Canvas не найден на объекте!");
            enabled = false;
            return;
        }
        
        // Находим или создаем CanvasGroup
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null && autoAddCanvasGroup)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            Debug.Log("CanvasInputBlocker: CanvasGroup автоматически добавлен к Canvas");
        }
        
        if (canvasGroup == null)
        {
            Debug.LogWarning("CanvasInputBlocker: CanvasGroup не найден и autoAddCanvasGroup = false. Блокировка не будет работать!");
            enabled = false;
            return;
        }
        
        // Инициализируем состояние
        UpdateBlockingState();
    }
    
    void Update()
    {
        UpdateBlockingState();
    }
    
    /// <summary>
    /// Обновляет состояние блокировки в зависимости от стадии TurnManager
    /// </summary>
    private void UpdateBlockingState()
    {
        if (canvasGroup == null)
            return;
        
        bool shouldBlock = false;
        
        // Блокируем во время стадии воспроизведения приказов
        if (TurnManager.Instance != null)
        {
            shouldBlock = TurnManager.Instance.GetCurrentState() == TurnState.Resolving;
        }
        
        // Обновляем только если состояние изменилось
        if (shouldBlock != wasBlocked)
        {
            canvasGroup.interactable = !shouldBlock; // false = блокирует взаимодействие
            canvasGroup.blocksRaycasts = !shouldBlock; // false = блокирует raycast события
            
            wasBlocked = shouldBlock;
            
            if (shouldBlock)
            {
                Debug.Log("CanvasInputBlocker: UI заблокирован (стадия воспроизведения приказов)");
            }
            else
            {
                Debug.Log("CanvasInputBlocker: UI разблокирован");
            }
        }
    }
    
    /// <summary>
    /// Принудительно обновляет состояние блокировки
    /// </summary>
    public void ForceUpdate()
    {
        wasBlocked = !wasBlocked; // Инвертируем, чтобы принудительно обновить
        UpdateBlockingState();
    }
}


using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Скрипт для кнопки "Завершить ход".
/// При нажатии завершает фазу планирования и запускает фазу исполнения в TurnManager.
/// </summary>
public class EndTurnButton : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private Button button; // Кнопка (найдет автоматически, если не указана)

    void Start()
    {
        // Находим кнопку, если не указана
        if (button == null)
        {
            button = GetComponent<Button>();
            if (button == null)
            {
                Debug.LogError("EndTurnButton: Кнопка не найдена!");
                return;
            }
        }

        // Подписываемся на событие клика
        button.onClick.AddListener(OnButtonClick);

        // В начале игры кнопка активна только в фазе планирования
        UpdateButtonState();
    }

    void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClick);
        }
    }

    /// <summary>
    /// Каждый кадр обновляем активность кнопки в зависимости от состояния хода.
    /// </summary>
    private void Update()
    {
        UpdateButtonState();
    }

    /// <summary>
    /// Обновляет свойство interactable у кнопки в зависимости от TurnManager.
    /// Кнопка активна только во время фазы планирования.
    /// </summary>
    private void UpdateButtonState()
    {
        if (button == null)
            return;

        if (TurnManager.Instance == null)
        {
            button.interactable = false;
            return;
        }

        button.interactable = TurnManager.Instance.GetCurrentState() == TurnState.Planning;
    }

    /// <summary>
    /// Обработчик клика по кнопке "Завершить ход".
    /// </summary>
    private void OnButtonClick()
    {
        if (TurnManager.Instance == null)
        {
            Debug.LogWarning("EndTurnButton: TurnManager.Instance == null, завершить ход нельзя");
            return;
        }

        // Дополнительная защита: не даём завершить ход, если не фаза планирования
        if (TurnManager.Instance.GetCurrentState() != TurnState.Planning)
        {
            Debug.LogWarning("EndTurnButton: Завершить ход можно только во время фазы планирования");
            return;
        }

        // Завершаем фазу планирования и запускаем исполнение приказов
        TurnManager.Instance.EndPlanningAndResolve();
    }
}



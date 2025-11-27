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
    }

    void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClick);
        }
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

        // Завершаем фазу планирования и запускаем исполнение приказов
        TurnManager.Instance.EndPlanningAndResolve();
    }
}



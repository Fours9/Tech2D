using UnityEngine;
using UnityEngine.EventSystems;
using CellNameSpace;

/// <summary>
/// Обработчик клика по юниту для его выбора
/// </summary>
public class UnitSelection : MonoBehaviour
{
    private UnitInfo unitInfo;
    private static CameraController cameraController;
    private bool mouseDownOnThisUnit = false;
    private Vector3 mouseDownPosition;
    private float mouseDownTime;
    
    private const float maxClickTime = 0.3f; // Максимальное время для считания кликом (секунды)
    private const float maxClickDistance = 5f; // Максимальное расстояние перемещения для считания кликом (в пикселях)
    
    void Start()
    {
        unitInfo = GetComponent<UnitInfo>();
        if (unitInfo == null)
        {
            Debug.LogError($"UnitSelection: UnitInfo не найден на {gameObject.name}");
        }
        
        // Находим CameraController при старте
        if (cameraController == null)
        {
            cameraController = FindFirstObjectByType<CameraController>();
            if (cameraController == null)
            {
                Debug.LogWarning("UnitSelection: CameraController не найден. Клики могут работать некорректно.");
            }
        }
    }

    private void OnMouseDown()
    {
        // Блокируем действия игрока во время стадии воспроизведения приказов
        if (TurnManager.Instance != null && TurnManager.Instance.GetCurrentState() == TurnState.Resolving)
        {
            return; // Игнорируем клик во время стадии воспроизведения
        }
        
        // Проверяем, не находится ли курсор над UI элементом
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return; // Игнорируем клик, если курсор над UI
        }
        
        Debug.Log($"UnitSelection: OnMouseDown вызван на {gameObject.name}");
        mouseDownOnThisUnit = true;
        mouseDownPosition = Input.mousePosition;
        mouseDownTime = Time.time;
    }
    
    private void OnMouseUp()
    {
        // Блокируем действия игрока во время стадии воспроизведения приказов
        if (TurnManager.Instance != null && TurnManager.Instance.GetCurrentState() == TurnState.Resolving)
        {
            mouseDownOnThisUnit = false; // Сбрасываем флаг
            return; // Игнорируем клик во время стадии воспроизведения
        }
        
        // Проверяем, не находится ли курсор над UI элементом
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            mouseDownOnThisUnit = false; // Сбрасываем флаг
            return; // Игнорируем клик, если курсор над UI
        }
        
        Debug.Log($"UnitSelection: OnMouseUp вызван на {gameObject.name}, mouseDownOnThisUnit = {mouseDownOnThisUnit}");
        
        // Проверяем, был ли это клик по этому юниту
        if (mouseDownOnThisUnit)
        {
            float clickDuration = Time.time - mouseDownTime;
            float mouseDistance = Vector3.Distance(Input.mousePosition, mouseDownPosition);
            
            // Проверяем через CameraController, была ли перемещена камера
            bool cameraWasMoved = cameraController != null && cameraController.WasCameraMoved();
            
            Debug.Log($"UnitSelection: clickDuration={clickDuration}, mouseDistance={mouseDistance}, cameraWasMoved={cameraWasMoved}");
            
            // Клик считается, если:
            // 1. Время нажатия короткое
            // 2. Расстояние перемещения мыши маленькое
            // 3. Камера не была перемещена
            if (clickDuration <= maxClickTime && 
                mouseDistance <= maxClickDistance && 
                !cameraWasMoved)
            {
                Debug.Log($"UnitSelection: Условия клика выполнены, вызываем OnUnitClick");
                OnUnitClick();
            }
            else
            {
                Debug.Log($"UnitSelection: Условия клика НЕ выполнены");
            }
            
            mouseDownOnThisUnit = false;
        }
    }
    
    private void OnUnitClick()
    {
        Debug.Log($"UnitSelection: OnUnitClick вызван на {gameObject.name}");
        if (unitInfo != null)
        {
            Debug.Log($"UnitSelection: Выбираем юнита {unitInfo.gameObject.name}");
            UnitSelectionManager.Instance.SelectUnit(unitInfo);
        }
        else
        {
            Debug.LogError($"UnitSelection: unitInfo равен null на {gameObject.name}");
        }
    }
}


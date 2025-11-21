using UnityEngine;

namespace CellNameSpace
{
    public class CellClick : MonoBehaviour
    {
        private static CameraController cameraController;
        private bool mouseDownOnThisCell = false;
        private Vector3 mouseDownPosition;
        private float mouseDownTime;
        
        private const float maxClickTime = 0.3f; // Максимальное время для считания кликом (секунды)
        private const float maxClickDistance = 5f; // Максимальное расстояние перемещения для считания кликом (в пикселях)
        
        void Start()
        {
            // Находим CameraController при старте
            if (cameraController == null)
            {
                cameraController = FindFirstObjectByType<CameraController>();
                if (cameraController == null)
                {
                    Debug.LogWarning("CellClick: CameraController не найден. Клики могут работать некорректно.");
                }
            }
        }

        private void OnMouseDown()
        {
            mouseDownOnThisCell = true;
            mouseDownPosition = Input.mousePosition;
            mouseDownTime = Time.time;
        }
        
        private void OnMouseUp()
        {
            // Проверяем, был ли это клик по этой клетке
            if (mouseDownOnThisCell)
            {
                float clickDuration = Time.time - mouseDownTime;
                float mouseDistance = Vector3.Distance(Input.mousePosition, mouseDownPosition);
                
                // Проверяем через CameraController, была ли перемещена камера
                bool cameraWasMoved = cameraController != null && cameraController.WasCameraMoved();
                
                // Клик считается, если:
                // 1. Время нажатия короткое
                // 2. Расстояние перемещения мыши маленькое
                // 3. Камера не была перемещена
                if (clickDuration <= maxClickTime && 
                    mouseDistance <= maxClickDistance && 
                    !cameraWasMoved)
                {
                    OnCellClick();
                }
                
                mouseDownOnThisCell = false;
            }
        }
        
        private void OnCellClick()
        {
            Debug.Log($"Клик по гексу: {gameObject.name} (позиция: {transform.position})");
            
            // Проверяем, есть ли выбранный юнит
            if (UnitSelectionManager.Instance.HasSelectedUnit())
            {
                UnitInfo selectedUnit = UnitSelectionManager.Instance.GetSelectedUnit();
                UnitController unitController = selectedUnit.GetComponent<UnitController>();
                
                if (unitController != null)
                {
                    // Получаем информацию о клетке
                    CellInfo cellInfo = GetComponent<CellInfo>();
                    if (cellInfo != null)
                    {
                        // Перемещаем юнит на эту клетку
                        unitController.MoveToCell(cellInfo);
                    }
                    else
                    {
                        Debug.LogWarning($"CellClick: CellInfo не найден на клетке {gameObject.name}");
                    }
                }
                else
                {
                    Debug.LogWarning($"CellClick: UnitController не найден на выбранном юните");
                }
            }
        }
    }
}

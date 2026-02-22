using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

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
            
                mouseDownOnThisCell = true;
                mouseDownPosition = Input.mousePosition;
                mouseDownTime = Time.time;
        }
        
        private void OnMouseUp()
        {
            // Блокируем действия игрока во время стадии воспроизведения приказов
            if (TurnManager.Instance != null && TurnManager.Instance.GetCurrentState() == TurnState.Resolving)
            {
                mouseDownOnThisCell = false; // Сбрасываем флаг
                return; // Игнорируем клик во время стадии воспроизведения
            }
            
            // Проверяем, не находится ли курсор над UI элементом
            // Также проверяем, не началось ли нажатие над UI элементом
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                mouseDownOnThisCell = false; // Сбрасываем флаг
                return; // Игнорируем клик, если курсор над UI
            }
            
            // Дополнительная проверка: если нажатие началось над UI, но отпускание над клеткой,
            // все равно игнорируем клик (проверяем позицию нажатия)
            if (mouseDownOnThisCell && EventSystem.current != null)
            {
                // Проверяем, не было ли нажатие над UI элементом в момент OnMouseDown
                // Для этого используем RaycastAll в позиции нажатия
                UnityEngine.EventSystems.PointerEventData pointerData = new UnityEngine.EventSystems.PointerEventData(EventSystem.current);
                pointerData.position = mouseDownPosition;
                var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
                EventSystem.current.RaycastAll(pointerData, results);
                
                // Если в момент нажатия курсор был над UI, игнорируем клик
                if (results.Count > 0)
                {
                    mouseDownOnThisCell = false;
                    return;
                }
                }
                
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
            
            // Получаем информацию о клетке
            CellInfo cellInfo = GetComponent<CellInfo>();
            if (cellInfo == null)
            {
                Debug.LogWarning($"CellClick: CellInfo не найден на клетке {gameObject.name}");
                return;
            }
            
            Vector2Int cellPosition = new Vector2Int(cellInfo.GetGridX(), cellInfo.GetGridY());
            
            // Проверяем, есть ли выбранный юнит
            if (UnitSelectionManager.Instance.HasSelectedUnit())
            {
                UnitInfo selectedUnit = UnitSelectionManager.Instance.GetSelectedUnit();
                UnitController unitController = selectedUnit.GetComponent<UnitController>();
                
                if (unitController != null)
                {
                    // Если есть TurnManager и мы в фазе планирования — создаём приказ на перемещение
                    if (TurnManager.Instance != null && TurnManager.Instance.GetCurrentState() == TurnState.Planning)
                        {
                        MoveUnitOrder moveOrder = new MoveUnitOrder(unitController, cellInfo);
                        TurnManager.Instance.EnqueueOrder(moveOrder);
                        Debug.Log("CellClick: Приказ на перемещение юнита добавлен в очередь");
                    }
                    else
                    {
                        // Fallback: если TurnManager недоступен или другая фаза — перемещаем сразу
                        unitController.MoveToCell(cellInfo);
                    }
                }
                else
                {
                    Debug.LogWarning($"CellClick: UnitController не найден на выбранном юните");
                }
            }
            // Проверяем, можно ли расширить город на эту клетку (только если режим расширения включен и город выделен)
            else if (ExpandCityButton.IsExpansionModeEnabled() && 
                     CitySelectionManager.Instance != null && 
                     CitySelectionManager.Instance.HasSelectedCity())
            {
                TryExpandCityToCell(cellPosition);
            }
            // Проверяем, есть ли выбранный город - обрабатываем клик для выделения/снятия выделения
            else if (CitySelectionManager.Instance != null && CitySelectionManager.Instance.HasSelectedCity())
            {
                CityManager cityManager = FindFirstObjectByType<CityManager>();
                if (cityManager == null)
                return;
                
                // Проверяем, принадлежит ли клетка выбранному городу
                CityInfo cityOwningCell = cityManager.GetCityOwningCell(cellPosition);
                CityInfo selectedCity = CitySelectionManager.Instance.GetSelectedCity();
                
                if (cityOwningCell != null && cityOwningCell == selectedCity)
                {
                    // Клетка принадлежит выбранному городу - не перевыделяем
                    // Проверяем, выбрана ли постройка для установки
                    TryPlaceBuildingOnCell(cellPosition);
                }
                else
                {
                    // Клетка не принадлежит выбранному городу - снимаем выделение
                    CitySelectionManager.Instance.DeselectCity();
                }
            }
            // Если нет выбранного города, проверяем, можно ли выделить город по клику на его клетку
            else
            {
                CityManager cityManager = FindFirstObjectByType<CityManager>();
                if (cityManager != null)
            {
                    // Проверяем, принадлежит ли клетка какому-либо городу
                    CityInfo cityOwningCell = cityManager.GetCityOwningCell(cellPosition);
                    if (cityOwningCell != null)
                    {
                        // Выделяем город, которому принадлежит клетка
                        if (CitySelectionManager.Instance != null)
                        {
                            CitySelectionManager.Instance.SelectCity(cityOwningCell);
                        }
            }
            else
            {
                        // Клетка не принадлежит городу - постройки можно ставить только на клетки города
                        // Ничего не делаем
                    }
                }
            }
        }
        
        /// <summary>
        /// Пытается расширить город на указанную клетку
        /// </summary>
        private void TryExpandCityToCell(Vector2Int cellPosition)
        {
            CityManager cityManager = FindFirstObjectByType<CityManager>();
            if (cityManager == null)
                return;
            
            // Проверяем, есть ли выбранный город
            if (CitySelectionManager.Instance == null || !CitySelectionManager.Instance.HasSelectedCity())
            {
                Debug.LogWarning("CellClick: Нет выбранного города для расширения");
                return;
            }
            
            CityInfo selectedCity = CitySelectionManager.Instance.GetSelectedCity();
            if (selectedCity == null)
                return;
            
            // Проверяем, принадлежит ли клетка какому-либо городу
            if (cityManager.IsCellOwnedByCity(cellPosition))
            {
                // Клетка уже принадлежит городу, ничего не делаем
                return;
            }
            
            // Расширяем только выбранный город
            Vector2Int cityPosition = selectedCity.position;
            bool success = cityManager.ExpandCity(cityPosition, cellPosition);
            if (success)
            {
                Debug.Log($"CellClick: Город {selectedCity.name} расширен на клетку ({cellPosition.x}, {cellPosition.y})");
            }
            else
                    {
                // Если расширение не удалось (клетка не является соседом города), снимаем выделение
                Debug.Log($"CellClick: Не удалось расширить город на клетку ({cellPosition.x}, {cellPosition.y}), снимаем выделение");
                if (CitySelectionManager.Instance != null)
                {
                    CitySelectionManager.Instance.DeselectCity();
                }
            }
        }
        
        /// <summary>
        /// Пытается установить постройку на указанную клетку
        /// </summary>
        private void TryPlaceBuildingOnCell(Vector2Int cellPosition)
        {
            BuildingManager buildingManager = FindFirstObjectByType<BuildingManager>();
            if (buildingManager == null)
                return;

            CityManager cityManager = FindFirstObjectByType<CityManager>();
            CityInfo city = cityManager?.GetCityOwningCell(cellPosition);
            if (city == null)
                return; // Клетка не в территории города — строить нельзя

            int ownerId = city.ownerId;

            // Проверяем, выбрана ли постройка
            BuildingInfo selectedBuilding = buildingManager.GetSelectedBuilding();
            if (selectedBuilding == null)
                return;

            // Если есть TurnManager и мы в фазе планирования — создаём приказ на строительство
            if (TurnManager.Instance != null && TurnManager.Instance.GetCurrentState() == TurnState.Planning)
            {
                BuildBuildingOrder buildOrder = new BuildBuildingOrder(cellPosition, selectedBuilding, ownerId);
                TurnManager.Instance.EnqueueOrder(buildOrder);
                Debug.Log($"CellClick: Приказ на строительство '{selectedBuilding.name}' добавлен в очередь для клетки ({cellPosition.x}, {cellPosition.y})");
            }
            else
            {
                // Fallback: если TurnManager недоступен или другая фаза — строим сразу
            bool success = buildingManager.PlaceBuilding(cellPosition, selectedBuilding);
            if (success)
            {
                Debug.Log($"CellClick: Постройка '{selectedBuilding.name}' установлена на клетку ({cellPosition.x}, {cellPosition.y})");
                }
            }
        }
    }
}

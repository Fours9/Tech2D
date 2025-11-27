using UnityEngine;
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
                    // Перемещаем юнит на эту клетку
                    unitController.MoveToCell(cellInfo);
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
                    // Клетка принадлежит выбранному городу - подтверждаем выделение
                    CitySelectionManager.Instance.SelectCity(selectedCity);
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
                        // Клетка не принадлежит городу - пытаемся установить постройку
                        TryPlaceBuildingOnCell(cellPosition);
                    }
                }
                else
                {
                    // Если нет CityManager, пытаемся установить постройку
                    TryPlaceBuildingOnCell(cellPosition);
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
        }
        
        /// <summary>
        /// Пытается установить постройку на указанную клетку
        /// </summary>
        private void TryPlaceBuildingOnCell(Vector2Int cellPosition)
        {
            BuildingManager buildingManager = FindFirstObjectByType<BuildingManager>();
            if (buildingManager == null)
                return;
            
            // Проверяем, выбрана ли постройка
            BuildingInfo selectedBuilding = buildingManager.GetSelectedBuilding();
            if (selectedBuilding == null)
                return;
            
            // Пытаемся установить постройку
            bool success = buildingManager.PlaceBuilding(cellPosition, selectedBuilding);
            if (success)
            {
                Debug.Log($"CellClick: Постройка '{selectedBuilding.name}' установлена на клетку ({cellPosition.x}, {cellPosition.y})");
            }
        }
    }
}

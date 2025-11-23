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
            // Проверяем, можно ли расширить город на эту клетку (только если режим расширения включен)
            else if (ExpandCityButton.IsExpansionModeEnabled())
            {
                TryExpandCityToCell(cellPosition);
            }
            // Проверяем, можно ли установить постройку (если выбрана постройка)
            else
            {
                TryPlaceBuildingOnCell(cellPosition);
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
            
            // Проверяем, принадлежит ли клетка какому-либо городу
            if (cityManager.IsCellOwnedByCity(cellPosition))
            {
                // Клетка уже принадлежит городу, ничего не делаем
                return;
            }
            
            // Ищем ближайший город, который может расшириться на эту клетку
            var allCities = cityManager.GetAllCities();
            foreach (var kvp in allCities)
            {
                CityInfo city = kvp.Value;
                Vector2Int cityPosition = kvp.Key;
                
                // Проверяем, является ли эта клетка соседом города
                CellNameSpace.Grid grid = FindFirstObjectByType<CellNameSpace.Grid>();
                if (grid == null)
                    return;
                
                int gridWidth = grid.GetGridWidth();
                int gridHeight = grid.GetGridHeight();
                
                // Проверяем, является ли клетка соседом какой-либо клетки города
                List<Vector2Int> neighbors = CellNameSpace.HexagonalGridHelper.GetNeighbors(
                    cellPosition.x, cellPosition.y, gridWidth, gridHeight);
                
                foreach (Vector2Int neighborPos in neighbors)
                {
                    if (city.ownedCells.Contains(neighborPos))
                    {
                        // Нашли соседнюю клетку города, расширяем город
                        bool success = cityManager.ExpandCity(cityPosition, cellPosition);
                        if (success)
                        {
                            Debug.Log($"CellClick: Город {city.name} расширен на клетку ({cellPosition.x}, {cellPosition.y})");
                        }
                        return;
                    }
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

using UnityEngine;
using FogOfWar;

namespace CellNameSpace
{
    /// <summary>
    /// Менеджер для управления визуализацией клеток с учетом тумана войны.
    /// Разделяет данные клетки от визуализации - применяет визуализацию только для видимых клеток.
    /// </summary>
    public static class CellVisualizationManager
    {
        /// <summary>
        /// Применяет всю визуализацию клетки (используется при переходе в Visible состояние)
        /// </summary>
        /// <param name="cell">Клетка для визуализации</param>
        public static void ApplyAllVisualization(CellInfo cell)
        {
            if (cell == null)
                return;
            
            ApplyBuildingVisualization(cell);
            ApplyOwnershipVisualization(cell);
            ApplyResourceVisualization(cell);
        }
        
        /// <summary>
        /// Применяет визуализацию постройки на клетке.
        /// Проверяет состояние тумана войны - применяет только для видимых клеток.
        /// </summary>
        /// <param name="cell">Клетка для визуализации</param>
        public static void ApplyBuildingVisualization(CellInfo cell)
        {
            if (cell == null)
                return;
            
            // Проверяем состояние тумана войны - применяем визуализацию только для видимых клеток
            if (cell.GetFogOfWarState() != FogOfWarState.Visible)
                return;
            
            BuildingStats buildingStats = cell.GetBuildingStats();
            SpriteRenderer buildingsOverlay = cell.GetBuildingsOverlay();
            
            if (buildingsOverlay == null)
                return;
            
            if (buildingStats != null && buildingStats.sprite != null)
            {
                // Применяем спрайт постройки
                buildingsOverlay.sprite = buildingStats.sprite;
                buildingsOverlay.enabled = true;
                
                // Масштабируем спрайт под размер клетки
                Vector2 cellSize = cell.GetCellSize();
                cell.ScaleSpriteToCellSize(buildingsOverlay, buildingStats.sprite, cellSize);
                
                // Обновляем позицию и масштаб ресурсов после установки постройки
                cell.UpdateResourcePositionAndScale();
            }
            else
            {
                // Убираем спрайт постройки
                buildingsOverlay.sprite = null;
                buildingsOverlay.enabled = false;
                
                // Обновляем позицию и масштаб ресурсов после удаления постройки
                cell.UpdateResourcePositionAndScale();
            }
        }
        
        /// <summary>
        /// Применяет визуализацию принадлежности клетки игроку/городу.
        /// Проверяет состояние тумана войны - применяет только для видимых клеток.
        /// </summary>
        /// <param name="cell">Клетка для визуализации</param>
        public static void ApplyOwnershipVisualization(CellInfo cell)
        {
            if (cell == null)
                return;
            
            // Проверяем состояние тумана войны - применяем визуализацию только для видимых клеток
            if (cell.GetFogOfWarState() != FogOfWarState.Visible)
                return;
            
            CityInfo owningCity = cell.GetOwningCity();
            
            if (owningCity != null)
            {
                // Получаем цвет игрока из города (город наследует цвет от игрока-владельца)
                Color playerColor = CityManager.GetCityColor(owningCity);
                
                // Применяем overlay-тинтинг (OutlineOverlay теперь только для движения юнита)
                cell.ApplyOwnershipTinting(playerColor);
            }
            else
            {
                // Отключаем overlay-тинтинг
                cell.DisableOwnershipTinting();
            }
        }
        
        /// <summary>
        /// Применяет визуализацию ресурса на клетке.
        /// Проверяет состояние тумана войны - применяет только для видимых клеток.
        /// </summary>
        /// <param name="cell">Клетка для визуализации</param>
        public static void ApplyResourceVisualization(CellInfo cell)
        {
            if (cell == null)
                return;
            
            // Проверяем состояние тумана войны - применяем визуализацию только для видимых клеток
            if (cell.GetFogOfWarState() != FogOfWarState.Visible)
                return;
            
            ResourceStats resourceStats = cell.GetResourceStats();
            SpriteRenderer resourcesOverlay = cell.GetResourcesOverlay();
            
            if (resourcesOverlay == null)
                return;
            
            if (resourceStats != null && resourceStats.sprite != null)
            {
                // Применяем спрайт ресурса
                resourcesOverlay.sprite = resourceStats.sprite;
                resourcesOverlay.enabled = true;
                
                // Масштабируем спрайт под размер клетки
                Vector2 cellSize = cell.GetCellSize();
                cell.ScaleSpriteToCellSize(resourcesOverlay, resourceStats.sprite, cellSize);
                
                // Обновляем позицию и масштаб ресурсов
                cell.UpdateResourcePositionAndScale();
            }
            else
            {
                // Убираем спрайт ресурса
                resourcesOverlay.sprite = null;
                resourcesOverlay.enabled = false;
            }
        }
    }
}



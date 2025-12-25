using UnityEngine;
using System.Collections.Generic;

namespace CellNameSpace
{
    /// <summary>
    /// Менеджер для управления цветами клеток в зависимости от их типа
    /// </summary>
    public static class CellColorManager
    {
        // Для предотвращения избыточного логирования
        private static HashSet<CellType> _loggedErrors = new HashSet<CellType>();
        /// <summary>
        /// Получает цвет для указанного типа клетки (из CellStats или fallback значения)
        /// </summary>
        /// <param name="cellType">Тип клетки</param>
        /// <returns>Цвет для данного типа</returns>
        public static Color GetColorForType(CellType cellType)
        {
            // Пытаемся получить цвет из CellStats
            if (CellStatsManager.Instance != null)
            {
                CellStats stats = CellStatsManager.Instance.GetCellStats(cellType);
                if (stats != null)
                {
                    return stats.baseColor;
                }
            }
            
            // Fallback: старые жестко прописанные значения
            switch (cellType)
            {
                case CellType.deep_water:
                    return Color.blue;
                case CellType.shallow:
                    return Color.cyan;
                case CellType.field:
                    return Color.green;
                case CellType.forest:
                    return new Color(0f, 0.5f, 0f); // Темно-зеленый
                case CellType.desert:
                    return Color.yellow;
                case CellType.mountain:
                    return Color.gray;
                default:
                    return Color.white;
            }
        }
        
        /// <summary>
        /// Применяет цвет к рендереру клетки через MaterialPropertyBlock (без создания material instance)
        /// </summary>
        /// <param name="renderer">Рендерер клетки</param>
        /// <param name="cellType">Тип клетки</param>
        /// <param name="propertyBlock">MaterialPropertyBlock для использования (если null, будет создан новый)</param>
        public static void ApplyColorToCell(Renderer renderer, CellType cellType, MaterialPropertyBlock propertyBlock = null)
        {
            if (renderer == null)
                return;
            
            Color newColor = GetColorForType(cellType);
            
            // Для SpriteRenderer
            SpriteRenderer spriteRenderer = renderer as SpriteRenderer;
            if (spriteRenderer != null)
            {
                // Сохраняем альфу из исходного цвета
                Color originalColor = spriteRenderer.color;
                newColor.a = originalColor.a;
                spriteRenderer.color = newColor;
                return;
            }
            
            // Для MeshRenderer используем MaterialPropertyBlock для избежания создания material instance
            MeshRenderer meshRenderer = renderer as MeshRenderer;
            if (meshRenderer != null && meshRenderer.sharedMaterial != null)
            {
                // Создаем MaterialPropertyBlock, если он не передан
                if (propertyBlock == null)
                {
                    propertyBlock = new MaterialPropertyBlock();
                }
                
                // Получаем текущие свойства (чтобы не потерять другие параметры)
                meshRenderer.GetPropertyBlock(propertyBlock);
                
                // Сохраняем альфу из исходного цвета материала
                Color originalColor = meshRenderer.sharedMaterial.color;
                newColor.a = originalColor.a;
                
                // Устанавливаем цвет через MaterialPropertyBlock (не создает material instance)
                propertyBlock.SetColor("_Color", newColor);
                
                // Применяем MaterialPropertyBlock к рендереру
                meshRenderer.SetPropertyBlock(propertyBlock);
            }
        }
        
        /// <summary>
        /// Применяет материал к рендереру клетки
        /// </summary>
        /// <param name="renderer">Рендерер клетки</param>
        /// <param name="cellType">Тип клетки</param>
        /// <param name="materialManager">Менеджер материалов (может быть null)</param>
        /// <param name="propertyBlock">MaterialPropertyBlock для использования (если null, будет создан новый)</param>
        public static void ApplyMaterialToCell(Renderer renderer, CellType cellType, CellMaterialManager materialManager = null, MaterialPropertyBlock propertyBlock = null)
        {
            if (renderer == null)
                return;
            
            // Защита: если materialManager не назначен или null, сразу используем цвета
            if (materialManager == null)
            {
                ApplyColorToCell(renderer, cellType, propertyBlock);
                return;
            }
            
            MeshRenderer meshRenderer = renderer as MeshRenderer;
            
            // Проверяем, что это MeshRenderer (для SpriteRenderer материалы не применяются)
            if (meshRenderer == null)
            {
                ApplyColorToCell(renderer, cellType, propertyBlock);
                return;
            }
            
            // Пытаемся получить материал из менеджера
            Material material = null;
            try
            {
                material = materialManager.GetMaterialForType(cellType);
            }
            catch (System.Exception ex)
            {
                // Логируем только при первой ошибке для каждого типа
                if (!_loggedErrors.Contains(cellType))
                {
                    Debug.LogWarning($"CellColorManager: Ошибка при получении материала для типа {cellType}: {ex.Message}");
                    _loggedErrors.Add(cellType);
                }
                ApplyColorToCell(renderer, cellType, propertyBlock);
                return;
            }
            
            // Если материал найден для типа, применяем его
            if (material != null)
            {
                try
                {
                    // Используем sharedMaterial для оптимизации - не создаем instance для каждой клетки
                    // Это значительно ускоряет создание больших сеток
                    meshRenderer.sharedMaterial = material;
                    
                    // Если передан PropertyBlock, устанавливаем цвет из материала (чтобы сбросить цвет подсветки)
                    // НЕ перекрашиваем цветом типа клетки - используем цвет материала
                    if (propertyBlock != null)
                    {
                        // Получаем текущие свойства (чтобы не потерять другие параметры)
                        meshRenderer.GetPropertyBlock(propertyBlock);
                        
                        // Устанавливаем цвет из материала в PropertyBlock (сбрасывает подсветку, но не перекрашивает)
                        Color materialColor = material.color;
                        propertyBlock.SetColor("_Color", materialColor);
                        
                        // Применяем обновленный PropertyBlock
                        meshRenderer.SetPropertyBlock(propertyBlock);
                    }
                    return;
                }
                catch (System.Exception ex)
                {
                    // Логируем только при первой ошибке для каждого типа
                    if (!_loggedErrors.Contains(cellType))
                    {
                        Debug.LogWarning($"CellColorManager: Ошибка при применении материала '{material.name}' к клетке типа {cellType}: {ex.Message}");
                        _loggedErrors.Add(cellType);
                    }
                }
            }
            
            // Если материал не найден для типа - красим цветом через MaterialPropertyBlock, НЕ меняя материал префаба
            ApplyColorToCell(renderer, cellType, propertyBlock);
        }
    }
}




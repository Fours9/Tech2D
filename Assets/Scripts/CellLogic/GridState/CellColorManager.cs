using UnityEngine;

namespace CellNameSpace
{
    /// <summary>
    /// Менеджер для управления цветами клеток в зависимости от их типа
    /// </summary>
    public static class CellColorManager
    {
        /// <summary>
        /// Получает цвет для указанного типа клетки
        /// </summary>
        /// <param name="cellType">Тип клетки</param>
        /// <returns>Цвет для данного типа</returns>
        public static Color GetColorForType(CellType cellType)
        {
            switch (cellType)
            {
                case CellType.deep_water:
                    return Color.blue; // Временно, замените на нужные цвета позже
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
        /// Применяет цвет к рендереру клетки, сохраняя альфу исходного цвета
        /// </summary>
        /// <param name="renderer">Рендерер клетки</param>
        /// <param name="cellType">Тип клетки</param>
        public static void ApplyColorToCell(Renderer renderer, CellType cellType)
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
            
            // Для MeshRenderer (через Material)
            MeshRenderer meshRenderer = renderer as MeshRenderer;
            if (meshRenderer != null)
            {
                // Если есть instance материала (от предыдущего типа), нужно его сбросить
                // чтобы не оставалась текстура от старого материала
                if (meshRenderer.material != null && meshRenderer.material.name.Contains("(Instance)"))
                {
                    // Возвращаемся к sharedMaterial (если есть) или создаем новый стандартный материал
                    // Но проще просто применить цвет к текущему материалу - текстура останется, но цвет изменится
                    // Для правильной работы нужно сбросить материал полностью
                    // Используем sharedMaterial если он есть, иначе создаем новый материал
                    if (meshRenderer.sharedMaterial != null)
                    {
                        // Возвращаемся к sharedMaterial и меняем его цвет
                        meshRenderer.material = meshRenderer.sharedMaterial;
                    }
                }
                
                if (meshRenderer.material != null)
                {
                    // Сохраняем альфу из исходного цвета материала
                    Color originalColor = meshRenderer.material.color;
                    newColor.a = originalColor.a;
                    meshRenderer.material.color = newColor;
                }
            }
        }
        
        /// <summary>
        /// Применяет материал к рендереру клетки
        /// </summary>
        /// <param name="renderer">Рендерер клетки</param>
        /// <param name="cellType">Тип клетки</param>
        /// <param name="materialManager">Менеджер материалов (может быть null)</param>
        public static void ApplyMaterialToCell(Renderer renderer, CellType cellType, CellMaterialManager materialManager = null)
        {
            if (renderer == null)
                return;
            
            // Защита: если materialManager не назначен или null, сразу используем цвета
            if (materialManager == null)
            {
                ApplyColorToCell(renderer, cellType);
                return;
            }
            
            MeshRenderer meshRenderer = renderer as MeshRenderer;
            
            // Пытаемся получить материал из менеджера
            Material material = null;
            try
            {
                material = materialManager.GetMaterialForType(cellType);
            }
            catch (System.Exception)
            {
                // Если произошла ошибка при получении материала, просто красим цветом
                ApplyColorToCell(renderer, cellType);
                return;
            }
            
            // Если материал найден для типа, применяем его
            if (material != null)
            {
                if (meshRenderer != null)
                {
                    try
                    {
                        // Используем material (создает instance), чтобы каждая клетка имела свой материал
                        meshRenderer.material = material;
                        
                        // Отладочная информация
                        if (cellType == CellType.field || cellType == CellType.desert)
                        {
                            Debug.Log($"[CellColorManager] Applied material '{material.name}' to cellType={cellType}");
                        }
                        return;
                    }
                    catch (System.Exception)
                    {
                        // Если не удалось применить материал, красим цветом
                    }
                }
            }
            
            // Если материал не найден для типа - красим цветом, НЕ меняя материал префаба
            // Отладочная информация
            if (cellType == CellType.field || cellType == CellType.desert)
            {
                Debug.Log($"[CellColorManager] Material NOT found for cellType={cellType}, coloring without changing material");
            }
            
            ApplyColorToCell(renderer, cellType);
        }
    }
}




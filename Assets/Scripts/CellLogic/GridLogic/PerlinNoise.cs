using UnityEngine;

namespace CellNameSpace
{
    public static class PerlinNoise 
    {
        // Параметры для генерации шума
        private static float scale = 0.1f; // Масштаб шума (чем меньше, тем более плавные переходы)
        private static float offsetX = 0f; // Смещение по X для вариативности
        private static float offsetY = 0f; // Смещение по Y для вариативности
        
        /// <summary>
        /// Генерирует значение шума Перлина для заданной позиции
        /// </summary>
        /// <param name="x">Координата X в сетке</param>
        /// <param name="y">Координата Y в сетке</param>
        /// <returns>Значение от 0 до 1</returns>
        public static float GetNoiseValue(int x, int y)
        {
            float sampleX = (x + offsetX) * scale;
            float sampleY = (y + offsetY) * scale;
            
            // Используем встроенный шум Перлина Unity
            float noiseValue = Mathf.PerlinNoise(sampleX, sampleY);
            
            return noiseValue;
        }
        
        /// <summary>
        /// Определяет тип клетки на основе значения шума Перлина
        /// </summary>
        /// <param name="x">Координата X в сетке</param>
        /// <param name="y">Координата Y в сетке</param>
        /// <returns>Тип клетки</returns>
        public static CellType GetCellType(int x, int y)
        {
            float noiseValue = GetNoiseValue(x, y);
            
            // Распределяем типы клеток по диапазонам шума
            // deep_water: 0.0 - 0.15 (самые низкие значения)
            // shallow: 0.15 - 0.3
            // field: 0.3 - 0.5
            // forest: 0.5 - 0.7
            // desert: 0.7 - 0.85
            // mountain: 0.85 - 1.0 (самые высокие значения)
            
            if (noiseValue < 0.15f)
                return CellType.deep_water;
            else if (noiseValue < 0.3f)
                return CellType.shallow;
            else if (noiseValue < 0.5f)
                return CellType.field;
            else if (noiseValue < 0.7f)
                return CellType.forest;
            else if (noiseValue < 0.85f)
                return CellType.desert;
            else
                return CellType.mountain;
        }
        
        /// <summary>
        /// Устанавливает параметры генерации шума
        /// </summary>
        /// <param name="newScale">Новый масштаб шума</param>
        /// <param name="newOffsetX">Новое смещение по X</param>
        /// <param name="newOffsetY">Новое смещение по Y</param>
        public static void SetParameters(float newScale, float newOffsetX = 0f, float newOffsetY = 0f)
        {
            scale = newScale;
            offsetX = newOffsetX;
            offsetY = newOffsetY;
        }
        
        /// <summary>
        /// Получить текущие параметры генерации
        /// </summary>
        public static void GetParameters(out float currentScale, out float currentOffsetX, out float currentOffsetY)
        {
            currentScale = scale;
            currentOffsetX = offsetX;
            currentOffsetY = offsetY;
        }
    }
}

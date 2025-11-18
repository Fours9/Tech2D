using UnityEngine;

namespace CellNameSpace
{
    /// <summary>
    /// Класс для настройки частоты появления различных типов клеток на карте
    /// </summary>
    [System.Serializable]
    public class CellTypeDistribution
    {
        [Range(0f, 1f)]
        [SerializeField] private float deepWaterFrequency = 0.15f;
        
        [Range(0f, 1f)]
        [SerializeField] private float shallowFrequency = 0.15f;
        
        [Range(0f, 1f)]
        [SerializeField] private float fieldFrequency = 0.2f;
        
        [Range(0f, 1f)]
        [SerializeField] private float forestFrequency = 0.2f;
        
        [Range(0f, 1f)]
        [SerializeField] private float desertFrequency = 0.15f;
        
        [Range(0f, 1f)]
        [SerializeField] private float mountainFrequency = 0.15f;
        
        // Кэшированные диапазоны для быстрого доступа
        private float[] thresholds = null;
        private bool isDirty = true;
        
        /// <summary>
        /// Получить частоту deep_water
        /// </summary>
        public float GetDeepWaterFrequency() => deepWaterFrequency;
        
        /// <summary>
        /// Получить частоту shallow
        /// </summary>
        public float GetShallowFrequency() => shallowFrequency;
        
        /// <summary>
        /// Получить частоту field
        /// </summary>
        public float GetFieldFrequency() => fieldFrequency;
        
        /// <summary>
        /// Получить частоту forest
        /// </summary>
        public float GetForestFrequency() => forestFrequency;
        
        /// <summary>
        /// Получить частоту desert
        /// </summary>
        public float GetDesertFrequency() => desertFrequency;
        
        /// <summary>
        /// Получить частоту mountain
        /// </summary>
        public float GetMountainFrequency() => mountainFrequency;
        
        /// <summary>
        /// Установить частоту deep_water
        /// </summary>
        public void SetDeepWaterFrequency(float value)
        {
            deepWaterFrequency = Mathf.Clamp01(value);
            isDirty = true;
        }
        
        /// <summary>
        /// Установить частоту shallow
        /// </summary>
        public void SetShallowFrequency(float value)
        {
            shallowFrequency = Mathf.Clamp01(value);
            isDirty = true;
        }
        
        /// <summary>
        /// Установить частоту field
        /// </summary>
        public void SetFieldFrequency(float value)
        {
            fieldFrequency = Mathf.Clamp01(value);
            isDirty = true;
        }
        
        /// <summary>
        /// Установить частоту forest
        /// </summary>
        public void SetForestFrequency(float value)
        {
            forestFrequency = Mathf.Clamp01(value);
            isDirty = true;
        }
        
        /// <summary>
        /// Установить частоту desert
        /// </summary>
        public void SetDesertFrequency(float value)
        {
            desertFrequency = Mathf.Clamp01(value);
            isDirty = true;
        }
        
        /// <summary>
        /// Установить частоту mountain
        /// </summary>
        public void SetMountainFrequency(float value)
        {
            mountainFrequency = Mathf.Clamp01(value);
            isDirty = true;
        }
        
        /// <summary>
        /// Получить пороговые значения для определения типов клеток
        /// Пороги рассчитываются на основе частот и нормализуются
        /// </summary>
        public float[] GetThresholds()
        {
            if (thresholds == null || isDirty)
            {
                RecalculateThresholds();
            }
            return thresholds;
        }
        
        /// <summary>
        /// Пересчитывает пороговые значения на основе текущих частот
        /// </summary>
        private void RecalculateThresholds()
        {
            // Собираем все частоты
            float[] frequencies = new float[]
            {
                deepWaterFrequency,
                shallowFrequency,
                fieldFrequency,
                forestFrequency,
                desertFrequency,
                mountainFrequency
            };
            
            // Вычисляем сумму всех частот
            float totalFrequency = 0f;
            foreach (float freq in frequencies)
            {
                totalFrequency += freq;
            }
            
            // Если сумма равна нулю, используем равномерное распределение
            if (totalFrequency <= 0f)
            {
                float equalFrequency = 1f / 6f;
                for (int i = 0; i < frequencies.Length; i++)
                {
                    frequencies[i] = equalFrequency;
                }
                totalFrequency = 1f;
            }
            
            // Нормализуем частоты и создаем пороги
            thresholds = new float[6];
            float cumulative = 0f;
            
            for (int i = 0; i < frequencies.Length; i++)
            {
                cumulative += frequencies[i] / totalFrequency;
                thresholds[i] = cumulative;
            }
            
            // Убеждаемся, что последний порог равен 1.0
            thresholds[5] = 1.0f;
            
            isDirty = false;
        }
        
        /// <summary>
        /// Определяет тип клетки на основе значения шума (0-1) и текущих частот
        /// </summary>
        public CellType GetCellTypeFromNoise(float noiseValue)
        {
            float[] thresholds = GetThresholds();
            
            if (noiseValue < thresholds[0])
                return CellType.deep_water;
            else if (noiseValue < thresholds[1])
                return CellType.shallow;
            else if (noiseValue < thresholds[2])
                return CellType.field;
            else if (noiseValue < thresholds[3])
                return CellType.forest;
            else if (noiseValue < thresholds[4])
                return CellType.desert;
            else
                return CellType.mountain;
        }
    }
}


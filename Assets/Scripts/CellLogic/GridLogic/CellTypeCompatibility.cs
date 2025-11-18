using System.Collections.Generic;

namespace CellNameSpace
{
    /// <summary>
    /// Класс для проверки совместимости типов клеток и их корректировки
    /// </summary>
    public static class CellTypeCompatibility
    {
        /// <summary>
        /// Проверяет совместимость типа клетки с соседями и возвращает скорректированный тип при необходимости
        /// </summary>
        /// <param name="currentType">Текущий тип клетки</param>
        /// <param name="neighborTypes">Список типов соседних клеток</param>
        /// <returns>Скорректированный тип клетки</returns>
        public static CellType ValidateAndCorrectCellType(CellType currentType, List<CellType> neighborTypes)
        {
            // Если нет соседей, возвращаем исходный тип
            if (neighborTypes.Count == 0)
                return currentType;
            
            // Проверяем совместимость в зависимости от типа клетки
            switch (currentType)
            {
                case CellType.forest:
                    // Рядом с forest могут быть только forest, shallow, field, mountain
                    foreach (CellType neighborType in neighborTypes)
                    {
                        if (neighborType != CellType.forest && 
                            neighborType != CellType.shallow && 
                            neighborType != CellType.field && 
                            neighborType != CellType.mountain)
                        {
                            // Находим наиболее подходящий тип из разрешенных
                            return FindCompatibleType(neighborTypes, 
                                new[] { CellType.forest, CellType.shallow, CellType.field, CellType.mountain });
                        }
                    }
                    break;
                    
                case CellType.desert:
                    // Рядом с desert могут быть только desert, mountain, shallow, field
                    foreach (CellType neighborType in neighborTypes)
                    {
                        if (neighborType != CellType.desert && 
                            neighborType != CellType.mountain && 
                            neighborType != CellType.shallow && 
                            neighborType != CellType.field)
                        {
                            return FindCompatibleType(neighborTypes, 
                                new[] { CellType.desert, CellType.mountain, CellType.shallow, CellType.field });
                        }
                    }
                    break;
                    
                case CellType.deep_water:
                    // Рядом с deep_water могут быть только deep_water, shallow
                    foreach (CellType neighborType in neighborTypes)
                    {
                        if (neighborType != CellType.deep_water && neighborType != CellType.shallow)
                        {
                            return FindCompatibleType(neighborTypes, 
                                new[] { CellType.deep_water, CellType.shallow });
                        }
                    }
                    break;
                    
                case CellType.shallow:
                    // Рядом с shallow могут быть forest, mountain, field, desert, deep_water
                    // Если все соседи deep_water, должен быть хотя бы один сосед shallow
                    // (это обеспечивается тем, что сама клетка остается shallow)
                    foreach (CellType neighborType in neighborTypes)
                    {
                        // Проверяем, что сосед разрешен
                        if (neighborType != CellType.forest && 
                            neighborType != CellType.mountain && 
                            neighborType != CellType.field && 
                            neighborType != CellType.desert && 
                            neighborType != CellType.deep_water && 
                            neighborType != CellType.shallow)
                        {
                            return FindCompatibleType(neighborTypes, 
                                new[] { CellType.forest, CellType.mountain, CellType.field, CellType.desert, CellType.deep_water, CellType.shallow });
                        }
                    }
                    // Если все соседи deep_water, клетка остается shallow, 
                    // что обеспечивает наличие shallow соседа для других deep_water клеток
                    break;
                    
                case CellType.field:
                    // Рядом с field могут быть любые клетки, кроме deep_water
                    foreach (CellType neighborType in neighborTypes)
                    {
                        if (neighborType == CellType.deep_water)
                        {
                            return FindCompatibleType(neighborTypes, 
                                new[] { CellType.forest, CellType.desert, CellType.shallow, CellType.mountain, CellType.field });
                        }
                    }
                    break;
                    
                case CellType.mountain:
                    // Рядом с mountain могут быть любые клетки, кроме deep_water
                    foreach (CellType neighborType in neighborTypes)
                    {
                        if (neighborType == CellType.deep_water)
                        {
                            return FindCompatibleType(neighborTypes, 
                                new[] { CellType.forest, CellType.desert, CellType.shallow, CellType.field, CellType.mountain });
                        }
                    }
                    break;
            }
            
            return currentType;
        }
        
        /// <summary>
        /// Находит наиболее подходящий тип из разрешенных на основе типов соседей
        /// </summary>
        /// <param name="neighborTypes">Типы соседей</param>
        /// <param name="allowedTypes">Разрешенные типы</param>
        /// <returns>Наиболее подходящий тип</returns>
        private static CellType FindCompatibleType(List<CellType> neighborTypes, CellType[] allowedTypes)
        {
            // Подсчитываем частоту каждого разрешенного типа среди соседей
            Dictionary<CellType, int> typeCounts = new Dictionary<CellType, int>();
            foreach (CellType allowedType in allowedTypes)
            {
                typeCounts[allowedType] = 0;
            }
            
            foreach (CellType neighborType in neighborTypes)
            {
                if (typeCounts.ContainsKey(neighborType))
                {
                    typeCounts[neighborType]++;
                }
            }
            
            // Находим тип с максимальной частотой
            CellType bestType = allowedTypes[0];
            int maxCount = typeCounts[bestType];
            
            foreach (var kvp in typeCounts)
            {
                if (kvp.Value > maxCount)
                {
                    maxCount = kvp.Value;
                    bestType = kvp.Key;
                }
            }
            
            return bestType;
        }
    }
}


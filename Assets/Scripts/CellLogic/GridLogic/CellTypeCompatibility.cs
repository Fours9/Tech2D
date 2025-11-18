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
                    // Проверяем наличие запрещенных соседей (desert, deep_water)
                    bool hasDesertNeighbor = false;
                    bool hasForbiddenNeighbor = false;
                    int mountainCount = 0;
                    int otherAllowedCount = 0;
                    
                    foreach (CellType neighborType in neighborTypes)
                    {
                        if (neighborType == CellType.desert)
                        {
                            hasDesertNeighbor = true;
                            hasForbiddenNeighbor = true;
                            break;
                        }
                        if (neighborType == CellType.mountain)
                            mountainCount++;
                        else if (neighborType == CellType.forest || 
                                 neighborType == CellType.shallow || 
                                 neighborType == CellType.field)
                            otherAllowedCount++;
                        
                        if (neighborType != CellType.forest && 
                            neighborType != CellType.shallow && 
                            neighborType != CellType.field && 
                            neighborType != CellType.mountain)
                        {
                            hasForbiddenNeighbor = true;
                        }
                    }
                    
                    if (hasDesertNeighbor)
                    {
                        // Если есть сосед desert, ВСЕГДА меняем на безопасный тип-буфер
                        // Предпочитаем field, если он есть среди соседей, иначе shallow
                        foreach (CellType neighborType in neighborTypes)
                        {
                            if (neighborType == CellType.field)
                                return CellType.field;
                            if (neighborType == CellType.shallow)
                                return CellType.shallow;
                        }
                        // Если нет field или shallow среди соседей, выбираем field как безопасный буфер
                        return CellType.field;
                    }
                    
                    // Если forest окружен только mountain (нет других разрешенных типов),
                    // меняемся на field для разнообразия и предотвращения больших массивов forest вокруг mountain
                    if (mountainCount > 0 && otherAllowedCount == 0 && neighborTypes.Count == mountainCount)
                    {
                        return CellType.field;
                    }
                    
                    // Предотвращаем образование прямых линий из forest
                    // Если все или почти все соседи - forest, иногда меняем на field для разнообразия
                    int forestNeighborCount = 0;
                    foreach (CellType neighborType in neighborTypes)
                    {
                        if (neighborType == CellType.forest)
                            forestNeighborCount++;
                    }
                    
                    // Если все соседи - forest (однородное окружение), меняем на field для разнообразия
                    // Это предотвращает образование длинных прямых линий
                    if (forestNeighborCount == neighborTypes.Count && neighborTypes.Count >= 4)
                    {
                        return CellType.field;
                    }
                    
                    // Если большинство соседей (80%+) - forest, иногда меняем на field
                    // Используем детерминированный подход: если четное количество соседей forest, меняем
                    if (forestNeighborCount >= neighborTypes.Count * 0.8f && neighborTypes.Count >= 3)
                    {
                        // Если количество forest соседей четное, меняем на field для разнообразия
                        if (forestNeighborCount % 2 == 0)
                        {
                            return CellType.field;
                        }
                    }
                    
                    if (hasForbiddenNeighbor)
                    {
                        // Если есть другой запрещенный сосед, выбираем безопасный тип-буфер
                        // field - приоритетный тип при конфликтах
                        return FindCompatibleTypeWithPriority(neighborTypes, 
                            new[] { CellType.forest, CellType.shallow, CellType.field, CellType.mountain },
                            new[] { CellType.field }); // Приоритет: field
                    }
                    break;
                    
                case CellType.desert:
                    // Рядом с desert могут быть только mountain, field, desert, shallow
                    // Проверяем наличие запрещенных соседей (forest, deep_water)
                    bool hasForestNeighbor = false;
                    bool hasForbiddenNeighborDesert = false;
                    int forestCountDesert = 0;
                    
                    foreach (CellType neighborType in neighborTypes)
                    {
                        if (neighborType == CellType.forest)
                        {
                            hasForestNeighbor = true;
                            hasForbiddenNeighborDesert = true;
                            forestCountDesert++;
                        }
                        if (neighborType != CellType.mountain && 
                            neighborType != CellType.field && 
                            neighborType != CellType.desert && 
                            neighborType != CellType.shallow)
                        {
                            hasForbiddenNeighborDesert = true;
                        }
                    }
                    
                    if (hasForestNeighbor)
                    {
                        // Если есть сосед forest, ВСЕГДА меняем на безопасный тип-буфер
                        // Предпочитаем field, если он есть среди соседей, иначе shallow
                        foreach (CellType neighborType in neighborTypes)
                        {
                            if (neighborType == CellType.field)
                                return CellType.field;
                            if (neighborType == CellType.shallow)
                                return CellType.shallow;
                        }
                        // Если нет field или shallow среди соседей, выбираем field как безопасный буфер
                        return CellType.field;
                    }
                    
                    // Если вокруг слишком много forest (даже если они не соседи напрямую, но могут появиться)
                    // или есть другой запрещенный сосед, выбираем безопасный тип-буфер
                    if (hasForbiddenNeighborDesert)
                    {
                        // field - приоритетный тип при конфликтах
                        return FindCompatibleTypeWithPriority(neighborTypes, 
                            new[] { CellType.mountain, CellType.field, CellType.desert, CellType.shallow },
                            new[] { CellType.field }); // Приоритет: field
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
                    // Но mountain не должен быть полностью окружен forest
                    int forestCount = 0;
                    int otherTypeCount = 0;
                    bool hasDeepWater = false;
                    
                    foreach (CellType neighborType in neighborTypes)
                    {
                        if (neighborType == CellType.forest)
                            forestCount++;
                        else if (neighborType != CellType.deep_water)
                            otherTypeCount++;
                        
                        if (neighborType == CellType.deep_water)
                            hasDeepWater = true;
                    }
                    
                    // Если большинство соседей (50%+) - forest, и нет других типов кроме forest,
                    // меняемся на более подходящий тип, чтобы избежать полного окружения forest
                    if (forestCount >= neighborTypes.Count * 0.5f && otherTypeCount == 0)
                    {
                        // Если mountain полностью окружен forest, меняемся на field
                        return CellType.field;
                    }
                    
                    // Если слишком много forest (60%+), предпочитаем измениться
                    if (forestCount >= neighborTypes.Count * 0.6f)
                    {
                        // field - приоритетный тип при конфликтах
                        return FindCompatibleTypeWithPriority(neighborTypes, 
                            new[] { CellType.forest, CellType.desert, CellType.shallow, CellType.field, CellType.mountain },
                            new[] { CellType.field, CellType.mountain, CellType.desert, CellType.shallow }); // Приоритет: field, затем mountain, desert, shallow (не forest)
                    }
                    
                    if (hasDeepWater)
                    {
                        // При конфликте с deep_water, field - приоритетный тип
                        return FindCompatibleTypeWithPriority(neighborTypes, 
                            new[] { CellType.forest, CellType.desert, CellType.shallow, CellType.field, CellType.mountain },
                            new[] { CellType.field }); // Приоритет: field
                    }
                    break;
            }
            
            return currentType;
        }
        
        /// <summary>
        /// Находит наиболее подходящий тип из разрешенных на основе типов соседей
        /// field имеет приоритет при конфликтах
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
            
            // Если field есть среди разрешенных типов и среди соседей, предпочитаем его
            if (typeCounts.ContainsKey(CellType.field) && typeCounts[CellType.field] > 0)
            {
                return CellType.field;
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
        
        /// <summary>
        /// Находит наиболее подходящий тип из разрешенных с учетом приоритетных типов-буферов
        /// </summary>
        /// <param name="neighborTypes">Типы соседей</param>
        /// <param name="allowedTypes">Разрешенные типы</param>
        /// <param name="priorityTypes">Приоритетные типы-буферы (используются при конфликтах)</param>
        /// <returns>Наиболее подходящий тип</returns>
        private static CellType FindCompatibleTypeWithPriority(List<CellType> neighborTypes, CellType[] allowedTypes, CellType[] priorityTypes)
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
            
            // Сначала проверяем приоритетные типы-буферы
            foreach (CellType priorityType in priorityTypes)
            {
                if (typeCounts.ContainsKey(priorityType) && typeCounts[priorityType] > 0)
                {
                    return priorityType; // Возвращаем первый найденный приоритетный тип
                }
            }
            
            // Если приоритетных типов нет среди соседей, выбираем наиболее частый
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
            
            // Если все счетчики равны нулю, возвращаем первый приоритетный тип
            if (maxCount == 0 && priorityTypes.Length > 0)
            {
                return priorityTypes[0];
            }
            
            return bestType;
        }
    }
}


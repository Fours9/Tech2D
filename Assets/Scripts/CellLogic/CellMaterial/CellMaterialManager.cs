using UnityEngine;
using System.Collections.Generic;
using CellNameSpace;

namespace CellNameSpace
{
    /// <summary>
    /// Менеджер для управления материалами клеток в зависимости от их типа.
    /// Теперь использует CellTypeStats через CellTypeStatsManager для получения материалов.
    /// Старый маппинг удален - материалы берутся напрямую из CellTypeStats.
    /// </summary>
    public class CellMaterialManager : MonoBehaviour
    {
        /// <summary>
        /// Получает материал из CellTypeStats (per-player) или fallback на CellTypeStatsManager по cellType.
        /// </summary>
        public Material GetMaterialFromStats(CellTypeStats cellTypeStats, CellType fallbackCellType)
        {
            if (cellTypeStats != null && cellTypeStats.material != null) return cellTypeStats.material;
            return GetMaterialForType(fallbackCellType);
        }

        /// <summary>
        /// Получает материал для указанного типа клетки из CellTypeStats
        /// </summary>
        /// <param name="cellType">Тип клетки</param>
        /// <returns>Материал для данного типа, или null если не найден</returns>
        public Material GetMaterialForType(CellType cellType)
        {
            // Используем CellTypeStatsManager для получения материала из CellTypeStats
            if (CellTypeStatsManager.Instance == null)
            {
                // Логируем только один раз при первом обращении
                if (!_hasLoggedMissingManager)
                {
                    Debug.LogWarning($"CellMaterialManager: CellTypeStatsManager.Instance равен null! Материалы не будут применяться. Убедитесь, что CellTypeStatsManager добавлен в сцену.");
                    _hasLoggedMissingManager = true;
                }
                return null;
            }
            
            CellTypeStats stats = CellTypeStatsManager.Instance.GetCellTypeStats(cellType);
            if (stats == null)
            {
                // Логируем только один раз для каждого типа
                if (!_loggedMissingStats.Contains(cellType))
                {
                    Debug.LogWarning($"CellMaterialManager: CellTypeStats не найден для типа {cellType}. Проверьте, настроен ли маппинг в CellTypeStatsManager.");
                    _loggedMissingStats.Add(cellType);
                }
                return null;
            }
            
            if (stats.material == null)
            {
                // Логируем только один раз для каждого типа
                if (!_loggedMissingMaterials.Contains(cellType))
                {
                    Debug.LogWarning($"CellMaterialManager: В CellTypeStats для типа {cellType} (ID: {stats.id ?? "null"}) не задан материал. Будет использоваться цвет.");
                    _loggedMissingMaterials.Add(cellType);
                }
                return null;
            }
            
            return stats.material;
        }
        
        // Флаги для предотвращения избыточного логирования
        private static bool _hasLoggedMissingManager = false;
        private static HashSet<CellType> _loggedMissingStats = new HashSet<CellType>();
        private static HashSet<CellType> _loggedMissingMaterials = new HashSet<CellType>();
        
        /// <summary>
        /// Проверяет, есть ли материал для указанного типа клетки
        /// </summary>
        /// <param name="cellType">Тип клетки</param>
        /// <returns>true если материал найден, false иначе</returns>
        public bool HasMaterialForType(CellType cellType)
        {
            return GetMaterialForType(cellType) != null;
        }
    }
}



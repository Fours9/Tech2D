using UnityEngine;
using System.Collections.Generic;

namespace CellNameSpace
{
    /// <summary>
    /// Менеджер для управления оверлеями ресурсов и построек для клеток в зависимости от их типа.
    /// Хранит маппинг CellType -> ResourceStats/BuildingStats.
    /// </summary>
    public class CellOverlayManager : MonoBehaviour
    {
        [System.Serializable]
        public class FeatureOverlayMapping
        {
            public CellType cellType;
            [UnityEngine.Serialization.FormerlySerializedAs("resourceStats")]
            public FeatureStats featureStats;
        }
        
        [System.Serializable]
        public class BuildingOverlayMapping
        {
            public CellType cellType;
            public BuildingStats buildingStats;
        }
        
        [Header("Фичи для типов клеток")]
        [UnityEngine.Serialization.FormerlySerializedAs("resourceMappings")]
        [SerializeField] private List<FeatureOverlayMapping> featureMappings = new List<FeatureOverlayMapping>();
        
        [Header("Постройки для типов клеток")]
        [SerializeField] private List<BuildingOverlayMapping> buildingMappings = new List<BuildingOverlayMapping>();
        
        private Dictionary<CellType, FeatureStats> featureDictionary;
        private Dictionary<CellType, BuildingStats> buildingDictionary;
        
        private void Awake()
        {
            InitializeDictionaries();
        }
        
        /// <summary>
        /// Инициализирует словари оверлеев из списков маппингов
        /// </summary>
        private void InitializeDictionaries()
        {
            featureDictionary = new Dictionary<CellType, FeatureStats>();
            buildingDictionary = new Dictionary<CellType, BuildingStats>();
            
            foreach (var mapping in featureMappings)
            {
                if (mapping.featureStats != null)
                {
                    featureDictionary[mapping.cellType] = mapping.featureStats;
                }
            }
            
            foreach (var mapping in buildingMappings)
            {
                if (mapping.buildingStats != null)
                {
                    buildingDictionary[mapping.cellType] = mapping.buildingStats;
                }
            }
        }
        
        /// <summary>
        /// Получает FeatureStats для указанного типа клетки
        /// </summary>
        /// <param name="cellType">Тип клетки</param>
        /// <returns>FeatureStats для данного типа, или null если не найден</returns>
        public FeatureStats GetFeatureStats(CellType cellType)
        {
            if (featureDictionary == null)
            {
                InitializeDictionaries();
            }
            
            if (featureDictionary.TryGetValue(cellType, out FeatureStats stats))
            {
                return stats;
            }
            
            return null;
        }
        
        /// <summary>
        /// Получает BuildingStats для указанного типа клетки
        /// </summary>
        /// <param name="cellType">Тип клетки</param>
        /// <returns>BuildingStats для данного типа, или null если не найден</returns>
        public BuildingStats GetBuildingStats(CellType cellType)
        {
            if (buildingDictionary == null)
            {
                InitializeDictionaries();
            }
            
            if (buildingDictionary.TryGetValue(cellType, out BuildingStats stats))
            {
                return stats;
            }
            
            return null;
        }
        
        /// <summary>
        /// Обновляет словари оверлеев (вызывать после изменения маппингов в Inspector)
        /// </summary>
        public void RefreshOverlays()
        {
            InitializeDictionaries();
        }
        
        /// <summary>
        /// Публичный метод для обновления оверлеев (можно вызвать из Inspector через Context Menu)
        /// </summary>
        [ContextMenu("Refresh Overlays")]
        public void RefreshOverlaysPublic()
        {
            InitializeDictionaries();
            Debug.Log($"[CellOverlayManager] Overlays refreshed. Features: {featureDictionary.Count}, Buildings: {buildingDictionary.Count}");
            
            // Выводим информацию о всех фичах
            foreach (var kvp in featureDictionary)
            {
                Debug.Log($"[CellOverlayManager] Feature: {kvp.Key} -> {(kvp.Value != null ? kvp.Value.displayName : "NULL")}");
            }
            
            // Выводим информацию о всех постройках
            foreach (var kvp in buildingDictionary)
            {
                Debug.Log($"[CellOverlayManager] Building: {kvp.Key} -> {(kvp.Value != null ? kvp.Value.displayName : "NULL")}");
            }
        }
        
        /// <summary>
        /// Вызывается при изменении значений в Inspector (только в Editor)
        /// </summary>
        private void OnValidate()
        {
            // В Editor режиме обновляем словари при изменении в Inspector
            if (!Application.isPlaying && (featureMappings != null || buildingMappings != null))
            {
                // Не вызываем InitializeDictionaries здесь, чтобы не создавать словари в Editor
            }
        }
    }
}

using UnityEngine;
using System.Collections.Generic;

namespace CellNameSpace
{
    /// <summary>
    /// Менеджер для управления материалами клеток в зависимости от их типа
    /// </summary>
    public class CellMaterialManager : MonoBehaviour
    {
        [System.Serializable]
        public class CellMaterialMapping
        {
            public CellType cellType;
            public Material material;
        }
        
        [Header("Материалы для типов клеток")]
        [SerializeField] private List<CellMaterialMapping> materialMappings = new List<CellMaterialMapping>();
        
        private Dictionary<CellType, Material> materialDictionary;
        
        private void Awake()
        {
            InitializeDictionary();
        }
        
        /// <summary>
        /// Инициализирует словарь материалов из списка маппингов
        /// </summary>
        private void InitializeDictionary()
        {
            materialDictionary = new Dictionary<CellType, Material>();
            
            foreach (var mapping in materialMappings)
            {
                if (mapping.material != null)
                {
                    materialDictionary[mapping.cellType] = mapping.material;
                }
            }
        }
        
        /// <summary>
        /// Получает материал для указанного типа клетки
        /// </summary>
        /// <param name="cellType">Тип клетки</param>
        /// <returns>Материал для данного типа, или null если не найден</returns>
        public Material GetMaterialForType(CellType cellType)
        {
            if (materialDictionary == null)
            {
                InitializeDictionary();
            }
            
            if (materialDictionary.TryGetValue(cellType, out Material material))
            {
                return material;
            }
            
            return null;
        }
        
        /// <summary>
        /// Проверяет, есть ли материал для указанного типа клетки
        /// </summary>
        /// <param name="cellType">Тип клетки</param>
        /// <returns>true если материал найден, false иначе</returns>
        public bool HasMaterialForType(CellType cellType)
        {
            if (materialDictionary == null)
            {
                InitializeDictionary();
            }
            
            return materialDictionary.ContainsKey(cellType) && materialDictionary[cellType] != null;
        }
        
        /// <summary>
        /// Обновляет словарь материалов (вызывать после изменения materialMappings в Inspector)
        /// </summary>
        public void RefreshMaterials()
        {
            InitializeDictionary();
        }
        
        /// <summary>
        /// Публичный метод для обновления материалов (можно вызвать из Inspector через Context Menu)
        /// </summary>
        [ContextMenu("Refresh Materials")]
        public void RefreshMaterialsPublic()
        {
            InitializeDictionary();
            Debug.Log($"[CellMaterialManager] Materials refreshed. Dictionary count: {materialDictionary.Count}");
            
            // Выводим информацию о всех материалах
            foreach (var kvp in materialDictionary)
            {
                Debug.Log($"[CellMaterialManager] {kvp.Key} -> {(kvp.Value != null ? kvp.Value.name : "NULL")}");
            }
        }
        
        /// <summary>
        /// Вызывается при изменении значений в Inspector (только в Editor)
        /// </summary>
        private void OnValidate()
        {
            // В Editor режиме обновляем словарь при изменении в Inspector
            if (!Application.isPlaying && materialMappings != null)
            {
                // Не вызываем InitializeDictionary здесь, чтобы не создавать словарь в Editor
                // Но можно добавить проверку, если нужно
            }
        }
    }
}



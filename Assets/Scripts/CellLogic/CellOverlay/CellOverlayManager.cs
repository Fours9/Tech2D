using UnityEngine;
using System.Collections.Generic;

namespace CellNameSpace
{
    /// <summary>
    /// Типы слоев оверлеев для клеток
    /// </summary>
    public enum OverlayLayer
    {
        Resources,  // Слой ресурсов (деревья, камни и т.д.)
        Buildings   // Слой построек
    }
    
    /// <summary>
    /// Менеджер для управления оверлеями (спрайтами) клеток в зависимости от их типа и слоя
    /// </summary>
    public class CellOverlayManager : MonoBehaviour
    {
        [System.Serializable]
        public class CellOverlayMapping
        {
            public CellType cellType;
            public OverlayLayer layer;
            public Sprite sprite;
        }
        
        [Header("Оверлеи для типов клеток")]
        [SerializeField] private List<CellOverlayMapping> overlayMappings = new List<CellOverlayMapping>();
        
        private Dictionary<(CellType, OverlayLayer), Sprite> overlayDictionary;
        
        private void Awake()
        {
            InitializeDictionary();
        }
        
        /// <summary>
        /// Инициализирует словарь оверлеев из списка маппингов
        /// </summary>
        private void InitializeDictionary()
        {
            overlayDictionary = new Dictionary<(CellType, OverlayLayer), Sprite>();
            
            foreach (var mapping in overlayMappings)
            {
                if (mapping.sprite != null)
                {
                    overlayDictionary[(mapping.cellType, mapping.layer)] = mapping.sprite;
                }
            }
        }
        
        /// <summary>
        /// Получает спрайт оверлея для указанного типа клетки и слоя
        /// </summary>
        /// <param name="cellType">Тип клетки</param>
        /// <param name="layer">Слой оверлея</param>
        /// <returns>Спрайт для данного типа и слоя, или null если не найден</returns>
        public Sprite GetOverlaySprite(CellType cellType, OverlayLayer layer)
        {
            if (overlayDictionary == null)
            {
                InitializeDictionary();
            }
            
            if (overlayDictionary.TryGetValue((cellType, layer), out Sprite sprite))
            {
                return sprite;
            }
            
            return null;
        }
        
        /// <summary>
        /// Проверяет, есть ли оверлей для указанного типа клетки и слоя
        /// </summary>
        /// <param name="cellType">Тип клетки</param>
        /// <param name="layer">Слой оверлея</param>
        /// <returns>true если оверлей найден, false иначе</returns>
        public bool HasOverlayForType(CellType cellType, OverlayLayer layer)
        {
            if (overlayDictionary == null)
            {
                InitializeDictionary();
            }
            
            return overlayDictionary.ContainsKey((cellType, layer)) && overlayDictionary[(cellType, layer)] != null;
        }
        
        /// <summary>
        /// Обновляет словарь оверлеев (вызывать после изменения overlayMappings в Inspector)
        /// </summary>
        public void RefreshOverlays()
        {
            InitializeDictionary();
        }
        
        /// <summary>
        /// Публичный метод для обновления оверлеев (можно вызвать из Inspector через Context Menu)
        /// </summary>
        [ContextMenu("Refresh Overlays")]
        public void RefreshOverlaysPublic()
        {
            InitializeDictionary();
            Debug.Log($"[CellOverlayManager] Overlays refreshed. Dictionary count: {overlayDictionary.Count}");
            
            // Выводим информацию о всех оверлеях
            foreach (var kvp in overlayDictionary)
            {
                Debug.Log($"[CellOverlayManager] {kvp.Key.Item1} + {kvp.Key.Item2} -> {(kvp.Value != null ? kvp.Value.name : "NULL")}");
            }
        }
        
        /// <summary>
        /// Вызывается при изменении значений в Inspector (только в Editor)
        /// </summary>
        private void OnValidate()
        {
            // В Editor режиме обновляем словарь при изменении в Inspector
            if (!Application.isPlaying && overlayMappings != null)
            {
                // Не вызываем InitializeDictionary здесь, чтобы не создавать словарь в Editor
            }
        }
    }
}



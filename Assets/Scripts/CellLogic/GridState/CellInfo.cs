using UnityEngine;

namespace CellNameSpace
{
    public class CellInfo : MonoBehaviour
    {
        [SerializeField] private CellType cellType = CellType.field;
        [SerializeField] private int gridX = 0;
        [SerializeField] private int gridY = 0;
        
        [SerializeField] private SpriteRenderer resourcesOverlay;
        [SerializeField] private SpriteRenderer buildingsOverlay;

        private Renderer cellRenderer;
        private CellMaterialManager cachedMaterialManager = null;
        private CellOverlayManager cachedOverlayManager = null;
        private Vector2? cachedCellSize = null; // Кэшированный размер клетки
        
        void Awake()
        {
            // Кэшируем рендерер при создании объекта
            cellRenderer = GetComponent<Renderer>();
        }
        
        /// <summary>
        /// Инициализирует информацию о клетке
        /// </summary>
        /// <param name="x">Позиция X в сетке</param>
        /// <param name="y">Позиция Y в сетке</param>
        /// <param name="type">Тип клетки</param>
        /// <param name="materialManager">Менеджер материалов (опционально, для оптимизации)</param>
        /// <param name="overlayManager">Менеджер оверлеев (опционально, для оптимизации)</param>
        public void Initialize(int x, int y, CellType type, CellMaterialManager materialManager = null, CellOverlayManager overlayManager = null)
        {
            gridX = x;
            gridY = y;
            cellType = type;
            
            // Кэшируем менеджеры для оптимизации
            if (materialManager != null)
                cachedMaterialManager = materialManager;
            if (overlayManager != null)
                cachedOverlayManager = overlayManager;
            
            // При первом создании клеток не меняем материал - оставляем материал префаба
            // Материал/цвет будет применен позже при SetCellType
        }
        
        /// <summary>
        /// Получить тип клетки
        /// </summary>
        public CellType GetCellType()
        {
            return cellType;
        }
        
        /// <summary>
        /// Получить позицию X в сетке
        /// </summary>
        public int GetGridX()
        {
            return gridX;
        }
        
        /// <summary>
        /// Получить позицию Y в сетке
        /// </summary>
        public int GetGridY()
        {
            return gridY;
        }
        
        /// <summary>
        /// Устанавливает менеджеры для оптимизации (избегает поиска через FindFirstObjectByType)
        /// </summary>
        /// <param name="materialManager">Менеджер материалов</param>
        /// <param name="overlayManager">Менеджер оверлеев</param>
        public void SetManagers(CellMaterialManager materialManager, CellOverlayManager overlayManager)
        {
            if (materialManager != null)
                cachedMaterialManager = materialManager;
            if (overlayManager != null)
                cachedOverlayManager = overlayManager;
        }
        
        /// <summary>
        /// Установить тип клетки
        /// </summary>
        /// <param name="type">Новый тип клетки</param>
        /// <param name="updateOverlays">Обновлять ли оверлеи (по умолчанию true, можно отключить для массового обновления)</param>
        public void SetCellType(CellType type, bool updateOverlays = true)
        {
            cellType = type;
            // Обновляем цвет при изменении типа
            UpdateCellColor(updateOverlays);
        }
        
        /// <summary>
        /// Обновляет цвет клетки в зависимости от её типа
        /// Вызывается автоматически при Initialize и SetCellType,
        /// но также может быть вызван вручную при необходимости
        /// </summary>
        /// <param name="updateOverlays">Обновлять ли оверлеи (по умолчанию true)</param>
        public void UpdateCellColor(bool updateOverlays = true)
        {
            if (cellRenderer == null)
                cellRenderer = GetComponent<Renderer>();
            
            if (cellRenderer == null)
                return;
            
            // Кэшируем materialManager, но проверяем его каждый раз (на случай если он появился позже)
            if (cachedMaterialManager == null || !cachedMaterialManager.gameObject.activeInHierarchy)
            {
                cachedMaterialManager = FindMaterialManager();
            }
            
            // Применяем материал или цвет (с защитой - если materialManager null, используется цвет)
            CellColorManager.ApplyMaterialToCell(cellRenderer, cellType, cachedMaterialManager);
            
            // Обновляем оверлеи при изменении типа клетки (если требуется)
            if (updateOverlays)
            {
                UpdateOverlays();
            }
        }
        
        /// <summary>
        /// Обновляет оверлеи (спрайты) клетки в зависимости от её типа
        /// </summary>
        public void UpdateOverlays()
        {
            // Кэшируем overlayManager, но проверяем его каждый раз (на случай если он появился позже)
            if (cachedOverlayManager == null || !cachedOverlayManager.gameObject.activeInHierarchy)
            {
                cachedOverlayManager = FindOverlayManager();
            }
            
            // Получаем размер клетки для масштабирования спрайтов (с кэшированием)
            Vector2 cellSize = GetCellSize();
            
            // Обновляем слой ресурсов
            if (resourcesOverlay != null)
            {
                Sprite resourceSprite = null;
                
                if (cachedOverlayManager != null)
                {
                    resourceSprite = cachedOverlayManager.GetOverlaySprite(cellType, OverlayLayer.Resources);
                }
                
                if (resourceSprite != null)
                {
                    resourcesOverlay.sprite = resourceSprite;
                    resourcesOverlay.enabled = true;
                    // Масштабируем спрайт под размер клетки
                    ScaleSpriteToCellSize(resourcesOverlay, resourceSprite, cellSize);
                }
                else
                {
                    resourcesOverlay.sprite = null;
                    resourcesOverlay.enabled = false;
                }
            }
            
            // Обновляем слой построек (пока оставляем пустым, будет использоваться позже)
            if (buildingsOverlay != null)
            {
                Sprite buildingSprite = null;
                
                if (cachedOverlayManager != null)
                {
                    buildingSprite = cachedOverlayManager.GetOverlaySprite(cellType, OverlayLayer.Buildings);
                }
                
                if (buildingSprite != null)
                {
                    buildingsOverlay.sprite = buildingSprite;
                    buildingsOverlay.enabled = true;
                    // Масштабируем спрайт под размер клетки
                    ScaleSpriteToCellSize(buildingsOverlay, buildingSprite, cellSize);
                }
                else
                {
                    buildingsOverlay.sprite = null;
                    buildingsOverlay.enabled = false;
                }
            }
        }
        
        /// <summary>
        /// Получает размер клетки в локальных координатах (без учета масштаба родителя)
        /// Использует кэширование для оптимизации
        /// </summary>
        private Vector2 GetCellSize()
        {
            // Возвращаем кэшированное значение, если оно есть
            if (cachedCellSize.HasValue)
                return cachedCellSize.Value;
            
            if (cellRenderer == null)
                cellRenderer = GetComponent<Renderer>();
            
            if (cellRenderer != null)
            {
                // Получаем размер из bounds в мировых координатах
                Bounds worldBounds = cellRenderer.bounds;
                Vector2 worldSize = new Vector2(worldBounds.size.x, worldBounds.size.y);
                
                // Преобразуем в локальные координаты, учитывая масштаб transform
                // lossyScale учитывает масштаб всех родителей
                Vector3 lossyScale = transform.lossyScale;
                if (lossyScale.x > 0f && lossyScale.y > 0f)
                {
                    Vector2 size = new Vector2(worldSize.x / lossyScale.x, worldSize.y / lossyScale.y);
                    cachedCellSize = size;
                    return size;
                }
                
                // Если масштаб нулевой, возвращаем размер по умолчанию
                cachedCellSize = worldSize;
                return worldSize;
            }
            
            // Если рендерер не найден, возвращаем размер по умолчанию
            cachedCellSize = Vector2.one;
            return Vector2.one;
        }
        
        /// <summary>
        /// Сбрасывает кэш размера клетки (вызывать при изменении масштаба)
        /// </summary>
        public void InvalidateCellSizeCache()
        {
            cachedCellSize = null;
        }
        
        /// <summary>
        /// Масштабирует спрайт так, чтобы он точно совпадал с размером клетки
        /// </summary>
        private void ScaleSpriteToCellSize(SpriteRenderer spriteRenderer, Sprite sprite, Vector2 targetSize)
        {
            if (spriteRenderer == null || sprite == null)
                return;
            
            // Получаем размер спрайта в локальных единицах
            // sprite.bounds.size возвращает размер в локальных единицах спрайта
            Vector2 spriteSize = sprite.bounds.size;
            
            if (spriteSize.x <= 0f || spriteSize.y <= 0f)
                return;
            
            // Вычисляем масштаб для совпадения с размером клетки
            // targetSize - размер клетки в локальных координатах
            // spriteSize - размер спрайта в локальных единицах
            // Масштаб = targetSize / spriteSize
            float scaleX = targetSize.x / spriteSize.x;
            float scaleY = targetSize.y / spriteSize.y;
            
            // Используем равномерное масштабирование для сохранения пропорций
            // Или можно использовать разные значения для точной подгонки под шестиугольник
            float uniformScale = Mathf.Min(scaleX, scaleY);
            
            spriteRenderer.transform.localScale = new Vector3(uniformScale, uniformScale, 1f);
        }
        
        /// <summary>
        /// Находит CellMaterialManager в сцене
        /// </summary>
        private CellMaterialManager FindMaterialManager()
        {
            // Используем FindFirstObjectByType для поиска CellMaterialManager в сцене
            // Но только в Play Mode, чтобы не зависать в Editor
            if (Application.isPlaying)
            {
                return FindFirstObjectByType<CellMaterialManager>();
            }
            
            // В Editor режиме возвращаем null, чтобы использовать цвета
            return null;
        }
        
        /// <summary>
        /// Находит CellOverlayManager в сцене
        /// </summary>
        private CellOverlayManager FindOverlayManager()
        {
            // Используем FindFirstObjectByType для поиска CellOverlayManager в сцене
            // Но только в Play Mode, чтобы не зависать в Editor
            if (Application.isPlaying)
            {
                return FindFirstObjectByType<CellOverlayManager>();
            }
            
            // В Editor режиме возвращаем null
            return null;
        }
    }
}

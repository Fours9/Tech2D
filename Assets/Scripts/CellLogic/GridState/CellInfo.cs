using UnityEngine;
using System.Collections;
using FogOfWar;

namespace CellNameSpace
{
    public class CellInfo : MonoBehaviour
    {
        [SerializeField] private CellType cellType = CellType.field;
        [SerializeField] private int gridX = 0;
        [SerializeField] private int gridY = 0;
        
        [SerializeField] private SpriteRenderer resourcesOverlay;
        [SerializeField] private SpriteRenderer buildingsOverlay;
        [SerializeField] private SpriteRenderer cityBorderOverlay; // Оверлей для границы города (опционально)
        [SerializeField] private SpriteRenderer outlineOverlay; // Оверлей для обводки клетки (опционально)
        [SerializeField] private SpriteRenderer ownershipOverlay; // Оверлей для тинтинга принадлежности (TintingLayer)
        [SerializeField] private Sprite hexagonSprite; // Спрайт-шестиугольник для тинтинга (созданный из модели)
        
        [Header("Туман войны")]
        [SerializeField] private FogOfWarState fogState = FogOfWarState.Hidden; // Состояние видимости клетки
        private bool hasBeenExplored = false; // Была ли клетка когда-либо исследована
        [SerializeField] private Renderer fogOfWarRenderer; // Renderer дочернего объекта тумана (MeshRenderer)
        
        [Header("Настройки обводки")]
        [SerializeField] private bool outlineEnabled = false; // Включена ли обводка
        [SerializeField] private Color outlineColor = Color.black; // Цвет обводки
        [SerializeField] [Range(1f, 10f)] private float outlineWidth = 2f; // Толщина обводки в пикселях

        private Renderer cellRenderer;
        private CellMaterialManager cachedMaterialManager = null;
        private CellOverlayManager cachedOverlayManager = null;
        private Vector2? cachedCellSize = null; // Кэшированный размер клетки
        private CityInfo owningCity = null; // Город, которому принадлежит клетка
        
        [Header("Данные клетки (не визуализация)")]
        private BuildingStats buildingStats = null; // Данные о постройке на клетке (может быть null)
        private ResourceStats resourceStats = null; // Данные о ресурсе на клетке (может быть null)
        private Vector3 originalPosition; // Изначальная позиция клетки в мире
        private bool originalPositionSet = false; // Флаг, что изначальная позиция уже установлена
        private MaterialPropertyBlock materialPropertyBlock = null; // MaterialPropertyBlock для передачи параметров в шейдер
        private MaterialPropertyBlock fogOfWarPropertyBlock = null; // MaterialPropertyBlock для тумана войны
        // Локальный радиус гекса для этой клетки (из sharedMesh.bounds.extents.y),
        // кэшируется один раз и может использоваться для любых эффектов вокруг клетки.
        private float hexRadiusForCell = 0f;
        // Анимация переходов тумана войны
        private Coroutine transitionCoroutine = null; // Ссылка на текущую корутину анимации
        private FogOfWarState previousState = FogOfWarState.Hidden; // Предыдущее состояние для определения типа перехода
        private FogOfWarState transitionTargetState = FogOfWarState.Hidden; // Целевое состояние для анимации перехода
        
        // Кэш для состояния рваных краев - обновляется только при изменении состояния FOW
        private bool[] cachedRaggedEdges = null;
        private bool cachedForceAllEdges = false;
        
        // Флаг для отслеживания изменений состояния FOW (используется для batch-обновления)
        private bool fogStateChanged = false;
        
        void Awake()
        {
            // Кэшируем рендерер при создании объекта
            cellRenderer = GetComponent<Renderer>();
            // Сохраняем изначальную позицию только если она еще не была установлена
            if (!originalPositionSet)
            {
                originalPosition = transform.position;
                originalPositionSet = true;
            }

            // Инициализируем локальный радиус гекса для этой клетки
            InitializeHexRadiusForCell();
        }
        
        void Start()
        {
            // НЕ применяем настройки обводки из инспектора при запуске игры
            // Обводка должна включаться только явно через SetOutline()
            // ApplyOutlineFromInspector(); // Отключено, чтобы обводка не была видна при создании карты
            
            // Убеждаемся, что originalPosition передана в шейдер
            // Это также применит параметры тумана войны через ApplyOriginalPositionToShader
            ApplyOriginalPositionToShader();
            
            // Убеждаемся, что видимость оверлеев соответствует текущему состоянию тумана
            UpdateFogOfWarVisual();
            
            // Отключаем обводку при создании, если она была включена в префабе
            if (outlineOverlay != null)
            {
                outlineOverlay.enabled = false;
            }
            outlineEnabled = false;
        }

        /// <summary>
        /// Инициализирует локальный радиус гекса для этой клетки на основе sharedMesh.bounds.extents.y.
        /// Используется для локальных эффектов вокруг клетки.
        /// </summary>
        private void InitializeHexRadiusForCell()
        {
            // Если уже инициализировано, повторно не считаем
            if (hexRadiusForCell > 0f)
                return;

            // Пытаемся найти MeshFilter: сначала на самой клетке, затем в дочерних (как у FogOfWarRenderer)
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = GetComponentInChildren<MeshFilter>();
            }

            if (meshFilter == null || meshFilter.sharedMesh == null)
                return;

            // Берём локальные границы меша (до применения масштаба),
            // так же, как это делает FogOfWarManager.
            Bounds meshBounds = meshFilter.sharedMesh.bounds;
            hexRadiusForCell = meshBounds.extents.y;
        }

        /// <summary>
        /// Возвращает локальный радиус гекса для этой клетки (из sharedMesh.bounds.extents.y).
        /// Если радиус ещё не инициализирован, попытка инициализации будет выполнена лениво.
        /// </summary>
        public float GetHexRadiusForCell()
        {
            if (hexRadiusForCell <= 0f)
            {
                InitializeHexRadiusForCell();
            }
            return hexRadiusForCell;
        }
        
        /// <summary>
        /// Вызывается при изменении значений в инспекторе (только в Editor)
        /// Автоматически применяет настройки обводки
        /// </summary>
        void OnValidate()
        {
            // Применяем настройки обводки только если игра запущена
            // В Editor режиме это позволит видеть изменения в реальном времени
            if (Application.isPlaying)
            {
                ApplyOutlineFromInspector();
            }
        }
        
        /// <summary>
        /// Применяет настройки обводки из полей инспектора
        /// </summary>
        private void ApplyOutlineFromInspector()
        {
            SetOutline(outlineEnabled, outlineColor, outlineWidth);
        }
        
        /// <summary>
        /// Получает текущее состояние обводки (включена/выключена)
        /// </summary>
        public bool IsOutlineEnabled()
        {
            return outlineEnabled;
        }
        
        /// <summary>
        /// Получает текущий цвет обводки
        /// </summary>
        public Color GetOutlineColor()
        {
            return outlineColor;
        }
        
        /// <summary>
        /// Получает текущую толщину обводки
        /// </summary>
        public float GetOutlineWidth()
        {
            return outlineWidth;
        }
        
        /// <summary>
        /// Применяет настройки обводки из инспектора (для использования в Editor через контекстное меню)
        /// </summary>
        [ContextMenu("Применить настройки обводки")]
        private void ApplyOutlineContextMenu()
        {
            ApplyOutlineFromInspector();
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
            
            // Сохраняем изначальную позицию при инициализации только если она еще не была установлена
            // Z координата не меняется при подъеме (только Y), поэтому перезапись не нужна
            if (!originalPositionSet)
            {
                originalPosition = transform.position;
                originalPositionSet = true;
            }
            
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
        /// Получить изначальную позицию клетки в мире
        /// </summary>
        public Vector3 GetOriginalPosition()
        {
            return originalPosition;
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
            
            // Передаем originalPosition в шейдер через MaterialPropertyBlock
            ApplyOriginalPositionToShader();
            
            // Если клетка принадлежит городу, применяем визуализацию принадлежности
            // Визуализация применяется через CellVisualizationManager с учетом тумана войны
            if (owningCity != null)
            {
                CellVisualizationManager.ApplyOwnershipVisualization(this);
            }
            
            // Устанавливаем ResourceStats и BuildingStats из CellOverlayManager
            // Вызываем всегда, независимо от updateOverlays (это установка данных, не визуализация)
            // Визуализация применяется через CellVisualizationManager с учетом тумана войны
            if (cachedOverlayManager == null || !cachedOverlayManager.gameObject.activeInHierarchy)
            {
                cachedOverlayManager = FindOverlayManager();
            }
            
            if (cachedOverlayManager != null)
            {
                ResourceStats resourceStats = cachedOverlayManager.GetResourceStats(cellType);
                BuildingStats buildingStats = cachedOverlayManager.GetBuildingStats(cellType);
                
                SetResourceStats(resourceStats);
                SetBuildingStats(buildingStats);
            }
        }
        
        /// <summary>
        /// Применяет originalPosition в шейдер через MaterialPropertyBlock
        /// </summary>
        private void ApplyOriginalPositionToShader()
        {
            if (cellRenderer == null)
                return;
            
            MeshRenderer meshRenderer = cellRenderer as MeshRenderer;
            if (meshRenderer == null)
                return;
            
            // Создаем MaterialPropertyBlock, если его еще нет
            if (materialPropertyBlock == null)
            {
                materialPropertyBlock = new MaterialPropertyBlock();
            }
            
            // Получаем текущие свойства (если они уже были установлены)
            meshRenderer.GetPropertyBlock(materialPropertyBlock);
            
            // Устанавливаем originalPosition в шейдер
            // Используем Vector4 для совместимости с шейдером
            Vector4 originalPos = new Vector4(originalPosition.x, originalPosition.y, originalPosition.z, 0f);
            materialPropertyBlock.SetVector("_OriginalPosition", originalPos);
            
            // Устанавливаем цвет Edge в черный по умолчанию, если клетка не принадлежит городу
            // Если клетка принадлежит городу, цвет Edge будет установлен в ApplyOwnershipVisualization
            if (owningCity == null)
            {
                materialPropertyBlock.SetColor("_EdgeColor", Color.black);
            }
            
            // Применяем MaterialPropertyBlock к рендереру
            meshRenderer.SetPropertyBlock(materialPropertyBlock);
        }
        
        /// <summary>
        /// Получает размер клетки в локальных координатах (без учета масштаба родителя)
        /// Использует кэширование для оптимизации
        /// </summary>
        public Vector2 GetCellSize()
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
        public void ScaleSpriteToCellSize(SpriteRenderer spriteRenderer, Sprite sprite, Vector2 targetSize)
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
        /// Обновляет позицию и масштаб ресурсов в зависимости от наличия постройки
        /// Если есть постройка, ресурсы уменьшаются в 4 раза и размещаются в центре по X и внизу по Y
        /// </summary>
        public void UpdateResourcePositionAndScale()
        {
            if (resourcesOverlay == null || resourcesOverlay.sprite == null || !resourcesOverlay.enabled)
                return;
            
            // Проверяем, есть ли постройка на клетке
            bool hasBuilding = buildingsOverlay != null && buildingsOverlay.sprite != null && buildingsOverlay.enabled;
            
            Vector2 cellSize = GetCellSize();
            
            if (hasBuilding)
            {
                // Вычисляем нормальный масштаб для ресурса
                Vector2 spriteSize = resourcesOverlay.sprite.bounds.size;
                float scaleX = cellSize.x / spriteSize.x;
                float scaleY = cellSize.y / spriteSize.y;
                float normalScale = Mathf.Min(scaleX, scaleY);
                
                // Уменьшаем масштаб в 4 раза
                float reducedScale = normalScale * 0.25f;
                resourcesOverlay.transform.localScale = new Vector3(reducedScale, reducedScale, 1f);
                
                // Перемещаем в центр по X и вниз по Y
                // Центр по X = 0
                // Вниз по Y - нижняя граница клетки плюс половина высоты уменьшенного спрайта
                float scaledSpriteHeight = spriteSize.y * reducedScale;
                float bottomY = -cellSize.y * 0.5f + scaledSpriteHeight * 0.5f;
                
                resourcesOverlay.transform.localPosition = new Vector3(0f, bottomY, resourcesOverlay.transform.localPosition.z);
            }
            else
            {
                // Если нет постройки, возвращаем ресурсы в центр с нормальным масштабом
                Vector2 spriteSize = resourcesOverlay.sprite.bounds.size;
                float scaleX = cellSize.x / spriteSize.x;
                float scaleY = cellSize.y / spriteSize.y;
                float uniformScale = Mathf.Min(scaleX, scaleY);
                resourcesOverlay.transform.localScale = new Vector3(uniformScale, uniformScale, 1f);
                resourcesOverlay.transform.localPosition = new Vector3(0f, 0f, resourcesOverlay.transform.localPosition.z);
            }
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
        
        /// <summary>
        /// Устанавливает данные о постройке на клетке (данные, не визуализация)
        /// Вызывает визуализацию через CellVisualizationManager
        /// </summary>
        /// <param name="stats">BuildingStats постройки (может быть null для удаления)</param>
        public void SetBuildingStats(BuildingStats stats)
        {
            buildingStats = stats;
            
            // Визуализация применяется через CellVisualizationManager с учетом тумана войны
            CellVisualizationManager.ApplyBuildingVisualization(this);
        }
        
        /// <summary>
        /// Получает данные о постройке на клетке
        /// </summary>
        public BuildingStats GetBuildingStats()
        {
            return buildingStats;
        }
        
        /// <summary>
        /// Устанавливает данные о ресурсе на клетке (данные, не визуализация)
        /// Вызывает визуализацию через CellVisualizationManager
        /// </summary>
        /// <param name="stats">ResourceStats ресурса (может быть null для удаления)</param>
        public void SetResourceStats(ResourceStats stats)
        {
            resourceStats = stats;
                
            // Визуализация применяется через CellVisualizationManager с учетом тумана войны
            CellVisualizationManager.ApplyResourceVisualization(this);
        }
        
        /// <summary>
        /// Получает данные о ресурсе на клетке
        /// </summary>
        public ResourceStats GetResourceStats()
        {
            return resourceStats;
        }
        
        /// <summary>
        /// Получает оверлей построек (для использования в CellVisualizationManager)
        /// </summary>
        public SpriteRenderer GetBuildingsOverlay()
        {
            return buildingsOverlay;
        }
        
        /// <summary>
        /// Получает оверлей ресурсов (для использования в CellVisualizationManager)
        /// </summary>
        public SpriteRenderer GetResourcesOverlay()
        {
            return resourcesOverlay;
        }
        
        /// <summary>
        /// Отключает тинтинг принадлежности
        /// </summary>
        public void DisableOwnershipTinting()
        {
            if (ownershipOverlay != null)
            {
                ownershipOverlay.enabled = false;
            }
        }
        
        /// <summary>
        /// Устанавливает принадлежность клетки к городу (данные)
        /// Вызывает визуализацию через CellVisualizationManager
        /// </summary>
        /// <param name="city">Город, которому принадлежит клетка (может быть null для удаления)</param>
        public void SetCityOwnership(CityInfo city)
            {
            owningCity = city;
            
            // Визуализация применяется через CellVisualizationManager с учетом тумана войны
            CellVisualizationManager.ApplyOwnershipVisualization(this);
        }
        
        /// <summary>
        /// Применяет легкий тинтинг принадлежности через overlay-слой
        /// </summary>
        /// <param name="playerColor">Цвет игрока</param>
        public void ApplyOwnershipTinting(Color playerColor)
        {
            if (ownershipOverlay == null)
            {
                Debug.LogWarning($"CellInfo: ownershipOverlay не назначен на клетке {gameObject.name}. Убедитесь, что TintingLayer назначен в инспекторе.");
                return;
            }
            
            if (hexagonSprite == null)
            {
                Debug.LogWarning($"CellInfo: hexagonSprite не назначен на клетке {gameObject.name}. Используйте Tools/Convert Model to Sprite для создания спрайта.");
                return;
            }
            
            // Устанавливаем спрайт
            ownershipOverlay.sprite = hexagonSprite;
            ownershipOverlay.enabled = true;
            
            // Применяем цвет игрока с прозрачностью 20-30% для легкого тинтинга
            Color tintColor = new Color(playerColor.r, playerColor.g, playerColor.b, 0.25f);
            ownershipOverlay.color = tintColor;
            
            // Масштабируем спрайт под размер клетки (как для ресурсов и зданий)
            Vector2 cellSize = GetCellSize();
            ScaleSpriteToCellSize(ownershipOverlay, hexagonSprite, cellSize);
        }
        
        /// <summary>
        /// Применяет цвет Edge в шейдере через MaterialPropertyBlock
        /// </summary>
        /// <param name="edgeColor">Цвет края клетки</param>
        public void ApplyEdgeColor(Color edgeColor)
        {
            if (cellRenderer == null)
                return;
            
            MeshRenderer meshRenderer = cellRenderer as MeshRenderer;
            if (meshRenderer == null)
                return;
            
            // Создаем MaterialPropertyBlock, если его еще нет
            if (materialPropertyBlock == null)
            {
                materialPropertyBlock = new MaterialPropertyBlock();
            }
            
            // Получаем текущие свойства (если они уже были установлены)
            meshRenderer.GetPropertyBlock(materialPropertyBlock);
            
            // Устанавливаем цвет Edge в шейдер
            materialPropertyBlock.SetColor("_EdgeColor", edgeColor);
            
            // Применяем MaterialPropertyBlock к рендереру
            meshRenderer.SetPropertyBlock(materialPropertyBlock);
        }
        
        /// <summary>
        /// Убирает принадлежность клетки к городу
        /// Использует SetCityOwnership(null) для единообразия
        /// </summary>
        public void ClearCityOwnership()
        {
            SetCityOwnership(null);
        }
        
        /// <summary>
        /// Получает город, которому принадлежит клетка
        /// </summary>
        public CityInfo GetOwningCity()
        {
            return owningCity;
        }
        
        /// <summary>
        /// Проверяет, принадлежит ли клетка какому-либо городу
        /// </summary>
        public bool IsOwnedByCity()
        {
            return owningCity != null;
        }

        /// <summary>
        /// Подсветить клетку как достижимую для текущего хода юнита.
        /// В приоритете используем cityBorderOverlay как маркер контура.
        /// Если он не назначен, явно красим клетку в заметный цвет.
        /// </summary>
        public void SetMovementHighlight(bool enabled)
        {
            if (cityBorderOverlay != null)
            {
                if (enabled)
                {
                    cityBorderOverlay.enabled = true;
                    // Яркий циановый контур
                    cityBorderOverlay.color = new Color(0f, 1f, 1f, 0.8f);
                }
                else
                {
                    cityBorderOverlay.enabled = false;
                }
            }
            else
            {
                // Fallback: подсвечиваем саму клетку
                if (cellRenderer == null)
                    cellRenderer = GetComponent<Renderer>();

                if (cellRenderer == null)
                    return;

                if (enabled)
                {
                    // Для SpriteRenderer
                    SpriteRenderer spriteRenderer = cellRenderer as SpriteRenderer;
                    if (spriteRenderer != null)
                    {
                        // Яркий цвет, хорошо видимый поверх террейна
                        spriteRenderer.color = Color.cyan;
                    }
                    // Для MeshRenderer
                    else if (cellRenderer is MeshRenderer meshRenderer && meshRenderer.material != null)
                    {
                        meshRenderer.material.color = Color.cyan;
                    }
                }
                else
                {
                    // Сбрасываем цвет к базовому типу клетки
                    UpdateCellColor(false);
                }
            }
        }
        
        /// <summary>
        /// Включает/выключает обводку для клетки
        /// </summary>
        /// <param name="enabled">Включить обводку</param>
        /// <param name="outlineColor">Цвет обводки (по умолчанию черный, если null - используется значение из инспектора)</param>
        /// <param name="outlineWidth">Толщина обводки в пикселях (по умолчанию 2, если 0 - используется значение из инспектора)</param>
        public void SetOutline(bool enabled, Color? outlineColor = null, float outlineWidth = 0f)
        {
            // Сохраняем значения в поля инспектора
            outlineEnabled = enabled;
            if (outlineColor.HasValue)
            {
                this.outlineColor = outlineColor.Value;
            }
            if (outlineWidth > 0f)
            {
                this.outlineWidth = outlineWidth;
            }
            
            // Если outlineOverlay не назначен, пытаемся создать его автоматически
            if (outlineOverlay == null)
            {
                outlineOverlay = CreateOutlineOverlay();
            }
            
            if (outlineOverlay == null)
            {
                Debug.LogWarning($"CellInfo: Не удалось создать outlineOverlay для клетки {gameObject.name}. Убедитесь, что на префабе клетки есть SpriteRenderer для обводки.");
                return;
            }
            
            if (enabled)
            {
                // Используем цвет из поля инспектора
                outlineOverlay.color = this.outlineColor;
                
                // Получаем размер клетки для правильного масштабирования обводки
                Vector2 cellSize = GetCellSize();
                
                // Используем толщину из поля инспектора
                float width = this.outlineWidth;
                
                // Создаем или получаем спрайт-контур
                Sprite outlineSprite = GetOrCreateOutlineSprite(cellSize, width);
                if (outlineSprite != null)
                {
                    outlineOverlay.sprite = outlineSprite;
                    outlineOverlay.enabled = true;
                    
                    // Масштабируем спрайт под размер клетки
                    ScaleSpriteToCellSize(outlineOverlay, outlineSprite, cellSize);
                }
                else
                {
                    outlineOverlay.enabled = false;
                }
            }
            else
            {
                outlineOverlay.enabled = false;
            }
        }
        
        /// <summary>
        /// Создает SpriteRenderer для обводки, если он не был назначен в инспекторе
        /// </summary>
        private SpriteRenderer CreateOutlineOverlay()
        {
            // Создаем дочерний GameObject для обводки
            GameObject outlineObject = new GameObject("OutlineOverlay");
            outlineObject.transform.SetParent(transform);
            outlineObject.transform.localPosition = Vector3.zero;
            outlineObject.transform.localRotation = Quaternion.identity;
            outlineObject.transform.localScale = Vector3.one;
            
            // Добавляем SpriteRenderer
            SpriteRenderer renderer = outlineObject.AddComponent<SpriteRenderer>();
            
            // Настраиваем порядок отрисовки
            renderer.sortingOrder = 0;
            
            return renderer;
        }
        
        // Кэш для спрайта обводки (чтобы не создавать его каждый раз)
        private static Sprite cachedOutlineSprite = null;
        private static Vector2 cachedOutlineSize = Vector2.zero;
        private static float cachedOutlineWidth = 0f;
        
        /// <summary>
        /// Получает или создает спрайт-контур для обводки
        /// </summary>
        private Sprite GetOrCreateOutlineSprite(Vector2 cellSize, float outlineWidth)
        {
            // Проверяем, можно ли использовать кэшированный спрайт
            if (cachedOutlineSprite != null && 
                Mathf.Approximately(cachedOutlineSize.x, cellSize.x) && 
                Mathf.Approximately(cachedOutlineSize.y, cellSize.y) &&
                Mathf.Approximately(cachedOutlineWidth, outlineWidth))
            {
                return cachedOutlineSprite;
            }
            
            // Создаем новый спрайт-контур
            // Вычисляем размер текстуры на основе размера клетки для правильного масштабирования
            float maxCellSize = Mathf.Max(cellSize.x, cellSize.y);
            int textureSize = Mathf.Max(256, Mathf.RoundToInt(maxCellSize * 100f)); // Минимум 256, иначе масштабируем под размер клетки
            textureSize = Mathf.Clamp(textureSize, 256, 1024); // Ограничиваем максимальный размер для производительности
            
            Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
            
            // Заполняем прозрачным
            Color[] pixels = new Color[textureSize * textureSize];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.clear;
            }
            
            // Рисуем контур шестиугольника
            // Радиус должен соответствовать размеру клетки относительно текстуры
            Vector2 center = new Vector2(textureSize / 2f, textureSize / 2f);
            // Используем размер клетки для расчета радиуса (приводим к размеру текстуры)
            float radius = (maxCellSize / 2f) * (textureSize / maxCellSize) * 0.9f; // 90% от размера для правильного масштабирования
            float outlineThickness = Mathf.Clamp(outlineWidth * (textureSize / 100f), 2f, 30f); // Толщина обводки масштабируется под размер текстуры
            
            // Рисуем шестиугольный контур
            DrawHexagonOutline(pixels, textureSize, center, radius, outlineThickness, Color.white);
            
            texture.SetPixels(pixels);
            texture.Apply();
            
            // Вычисляем pixels per unit на основе размера клетки
            // Это обеспечит правильное масштабирование спрайта
            float pixelsPerUnit = textureSize / maxCellSize;
            
            // Создаем спрайт из текстуры
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, textureSize, textureSize),
                new Vector2(0.5f, 0.5f), // Pivot в центре
                pixelsPerUnit // Pixels per unit рассчитывается на основе размера клетки
            );
            
            // Кэшируем спрайт
            // Удаляем старый кэшированный спрайт, если он есть
            if (cachedOutlineSprite != null)
            {
                DestroyImmediate(cachedOutlineSprite.texture);
                DestroyImmediate(cachedOutlineSprite);
            }
            
            cachedOutlineSprite = sprite;
            cachedOutlineSize = cellSize;
            cachedOutlineWidth = outlineWidth;
            
            return sprite;
        }
        
        /// <summary>
        /// Рисует контур шестиугольника на текстуре
        /// </summary>
        private void DrawHexagonOutline(Color[] pixels, int textureSize, Vector2 center, float radius, float thickness, Color color)
        {
            int thicknessInt = Mathf.RoundToInt(thickness);
            
            // Генерируем точки шестиугольника
            Vector2[] hexPoints = new Vector2[6];
            for (int i = 0; i < 6; i++)
            {
                float angle = (i * 60f - 30f) * Mathf.Deg2Rad; // Поворачиваем на 30 градусов для правильной ориентации
                hexPoints[i] = center + new Vector2(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius
                );
            }
            
            // Рисуем линии между точками
            for (int i = 0; i < 6; i++)
            {
                Vector2 start = hexPoints[i];
                Vector2 end = hexPoints[(i + 1) % 6];
                DrawLine(pixels, textureSize, start, end, thicknessInt, color);
            }
        }
        
        /// <summary>
        /// Рисует линию на текстуре
        /// </summary>
        private void DrawLine(Color[] pixels, int textureSize, Vector2 start, Vector2 end, int thickness, Color color)
        {
            int x0 = Mathf.RoundToInt(start.x);
            int y0 = Mathf.RoundToInt(start.y);
            int x1 = Mathf.RoundToInt(end.x);
            int y1 = Mathf.RoundToInt(end.y);
            
            // Алгоритм Брезенхема для рисования линии
            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;
            
            int halfThickness = thickness / 2;
            
            while (true)
            {
                // Рисуем пиксель с учетом толщины
                for (int ty = -halfThickness; ty <= halfThickness; ty++)
                {
                    for (int tx = -halfThickness; tx <= halfThickness; tx++)
                    {
                        int px = x0 + tx;
                        int py = y0 + ty;
                        
                        if (px >= 0 && px < textureSize && py >= 0 && py < textureSize)
                        {
                            float dist = Mathf.Sqrt(tx * tx + ty * ty);
                            if (dist <= halfThickness)
                            {
                                int index = py * textureSize + px;
                                if (index >= 0 && index < pixels.Length)
                                {
                                    pixels[index] = color;
                                }
                            }
                        }
                    }
                }
                
                if (x0 == x1 && y0 == y1)
                    break;
                
                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }
        
        // ========== ТУМАН ВОЙНЫ ==========
        
        /// <summary>
        /// Устанавливает состояние тумана войны для клетки
        /// Если туман войны отключен, всегда устанавливает состояние Visible и не позволяет его изменить
        /// </summary>
        public void SetFogOfWarState(FogOfWarState state)
        {
            // Отладочный вывод для проверки (закомментирован для оптимизации)
            // if (state == FogOfWarState.Visible)
            // {
            //     Debug.Log($"[FogOfWar] SetFogOfWarState: Клетка ({gridX}, {gridY}) устанавливается в Visible. " +
            //         $"Текущее состояние: {fogState}, Анимация: {transitionCoroutine != null}");
            // }
            
            // Если туман войны отключен, всегда оставляем клетку видимой и игнорируем изменения
            if (FogOfWarManager.Instance != null && !FogOfWarManager.Instance.IsFogOfWarEnabled())
            {
                if (fogState != FogOfWarState.Visible)
                {
                    fogState = FogOfWarState.Visible;
                    hasBeenExplored = true;
                    
                    // Инвалидируем кэш рваных краев при изменении состояния
                    cachedRaggedEdges = null;
                    
                    UpdateFogOfWarVisual();
                }
                return;
            }
            
            // Сохраняем предыдущее состояние перед изменением
            FogOfWarState oldState = fogState;
            previousState = oldState;
            
            // Если состояние меняется, инвалидируем кэш рваных краев и помечаем изменение
            if (oldState != state)
            {
                cachedRaggedEdges = null;
                fogStateChanged = true;
            }
            
            // Если состояние не изменилось и анимация не идет, ничего не делаем
            if (oldState == state && transitionCoroutine == null)
            {
                return;
            }
            
            // Если анимация уже идет, проверяем, нужно ли её прервать
            if (transitionCoroutine != null)
            {
                // Если анимация уже идет к нужному состоянию, ничего не делаем
                if (transitionTargetState == state)
                {
                    // Анимация уже идет к нужному состоянию, не нужно ничего делать
                    return;
                }
                
                // Если новое состояние отличается от целевого состояния анимации,
                // прерываем текущую анимацию и запускаем новую
                
                // Останавливаем текущую корутину
                StopCoroutine(transitionCoroutine);
                transitionCoroutine = null;
                
                // Сбрасываем параметры анимации в шейдере
                if (fogOfWarRenderer != null)
                {
                    if (fogOfWarPropertyBlock == null)
                    {
                        fogOfWarPropertyBlock = new MaterialPropertyBlock();
                    }
                    
                    fogOfWarRenderer.GetPropertyBlock(fogOfWarPropertyBlock);
                    fogOfWarPropertyBlock.SetFloat("_TransitionType", 0f);
                    fogOfWarPropertyBlock.SetFloat("_TransitionProgress", 0f);
                    fogOfWarPropertyBlock.SetFloat("_AnimatedHexRadius", 0f);
                    fogOfWarRenderer.SetPropertyBlock(fogOfWarPropertyBlock);
                }
                
                // Обновляем previousState для корректного определения типа следующей анимации
                previousState = fogState;
                
                // Продолжаем выполнение метода для запуска новой анимации или установки состояния
            }
            
            // Проверяем, нужна ли анимация перехода
            bool willStartAnimation = false;
            if (FogOfWarManager.Instance != null && oldState != state)
            {
                // Получаем материал для проверки настроек анимации
                Material currentMaterial = null;
                if (fogOfWarRenderer != null && fogOfWarRenderer.sharedMaterial != null)
                {
                    currentMaterial = fogOfWarRenderer.sharedMaterial;
                }
                else
                {
                    // Определяем материал на основе состояния
                    if (oldState == FogOfWarState.Hidden)
                    {
                        currentMaterial = FogOfWarManager.Instance.GetFogUnseenMaterial();
                    }
                    else if (oldState == FogOfWarState.Explored || state == FogOfWarState.Explored)
                    {
                        currentMaterial = FogOfWarManager.Instance.GetFogExploredMaterial();
                    }
                }
                
                if (FogOfWarManager.Instance.AreTransitionsEnabled(currentMaterial))
                {
                    float duration = FogOfWarManager.Instance.GetTransitionDuration(oldState, state, currentMaterial);
                    if (duration > 0f)
                    {
                        // Запускаем анимацию перехода
                        // НЕ устанавливаем fogState здесь - это сделает корутина в конце анимации
                        StartTransitionAnimation(oldState, state, duration);
                        willStartAnimation = true;
                    }
                }
            }
            
            // Если анимация НЕ запускается, устанавливаем состояние сразу
            if (!willStartAnimation)
            {
                fogState = state;
                
                // Если клетка стала видимой, отмечаем её как исследованную
                if (state == FogOfWarState.Visible)
                {
                    hasBeenExplored = true;
                }
                
                // Обновляем визуализацию сразу
                UpdateFogOfWarVisual();
            }
            // Если анимация запускается, fogState будет установлен в конце корутины
        }
        
        /// <summary>
        /// Получает текущее состояние тумана войны
        /// </summary>
        public FogOfWarState GetFogOfWarState()
        {
            return fogState;
        }
        
        /// <summary>
        /// Проверяет, была ли клетка когда-либо исследована
        /// </summary>
        public bool HasBeenExplored()
        {
            return hasBeenExplored;
        }
        
        /// <summary>
        /// Устанавливает параметры тумана войны на дочернем объекте
        /// Альфа всегда берется из _Color (Color (Alpha Override)) материала
        /// </summary>
        private void UpdateFogOfWarAlpha()
        {
            if (fogOfWarRenderer == null || fogOfWarRenderer.sharedMaterial == null)
                return;
            
            // Используем MaterialPropertyBlock для изменения alpha без создания instance материала
            // Кэшируем MaterialPropertyBlock для оптимизации
            if (fogOfWarPropertyBlock == null)
            {
                fogOfWarPropertyBlock = new MaterialPropertyBlock();
            }
            
            // Получаем текущие свойства из рендерера
            fogOfWarRenderer.GetPropertyBlock(fogOfWarPropertyBlock);
            
            // Передаем _OriginalPosition для правильного натягивания текстуры
            Vector4 originalPos = new Vector4(originalPosition.x, originalPosition.y, originalPosition.z, 0f);
            fogOfWarPropertyBlock.SetVector("_OriginalPosition", originalPos);
            
            // НЕ трогаем альфу - она всегда берется из _Color (Color (Alpha Override))
            // Пользователь может управлять альфой напрямую через Inspector материала
            // Альфа из материала будет использована напрямую в шейдере
            
            // Устанавливаем параметры анимации перехода
            // Если корутина активна, она сама управляет этими параметрами, поэтому не трогаем их здесь
            // Если корутина не активна, сбрасываем параметры в 0
            if (transitionCoroutine == null)
            {
                fogOfWarPropertyBlock.SetFloat("_TransitionType", 0f);
                fogOfWarPropertyBlock.SetFloat("_TransitionProgress", 0f);
                
                // Обновляем параметры неровных краев для этой конкретной клетки
                // (только если корутина не активна, чтобы не перезаписать параметры анимации)
                UpdateRaggedEdgesPerCell(fogOfWarPropertyBlock);
            }
            // Если корутина активна, параметры уже установлены корутиной, не перезаписываем их
            
            // Применяем MaterialPropertyBlock к рендереру
            fogOfWarRenderer.SetPropertyBlock(fogOfWarPropertyBlock);
        }

        /// <summary>
        /// Публичный метод для обновления неровных краев тумана для данной клетки
        /// (вызывается менеджером тумана после пересчета видимости).
        /// </summary>
        public void RefreshFogOfWarRaggedEdges()
        {
            // Имеет смысл только для состояний, где рендерится туман (Hidden/Explored)
            if (fogState == FogOfWarState.Hidden || fogState == FogOfWarState.Explored)
            {
                // Принудительно пересчитываем края, игнорируя кэш
                if (fogOfWarPropertyBlock == null)
                {
                    fogOfWarPropertyBlock = new MaterialPropertyBlock();
                }
                
                fogOfWarRenderer.GetPropertyBlock(fogOfWarPropertyBlock);
                
                Vector4 originalPos = new Vector4(originalPosition.x, originalPosition.y, originalPosition.z, 0f);
                fogOfWarPropertyBlock.SetVector("_OriginalPosition", originalPos);
                
                // Если анимация НЕ идет, сбрасываем параметры анимации
                if (transitionCoroutine == null)
                {
                    fogOfWarPropertyBlock.SetFloat("_TransitionType", 0f);
                    fogOfWarPropertyBlock.SetFloat("_TransitionProgress", 0f);
                }
                // Если анимация идет, НЕ трогаем параметры анимации (_TransitionType, _TransitionProgress)
                // но ВСЕ РАВНО обновляем рваные края
                
                // Принудительно пересчитываем края с forceRecalculate = true
                UpdateRaggedEdgesPerCell(fogOfWarPropertyBlock, false, true);
                
                fogOfWarRenderer.SetPropertyBlock(fogOfWarPropertyBlock);
                fogStateChanged = false; // Сбрасываем флаг после обновления
            }
        }
        
        /// <summary>
        /// Проверяет, изменилось ли состояние FOW с последнего обновления краев
        /// </summary>
        public bool HasFogStateChanged()
        {
            return fogStateChanged;
        }

        /// <summary>
        /// Определяет, для каких граней текущей клетки включать неровные края,
        /// исходя из состояний тумана у соседних клеток.
        /// Логика:
        /// - для Hidden-клеток рвём край, если сосед Visible ИЛИ Explored;
        /// - для Explored-клеток рвём край только если сосед Visible (Explored не рвёт).
        /// Если сосед Hidden или отсутствует, неровный край для соответствующей грани отключается.
        /// </summary>
        /// <param name="propertyBlock">MaterialPropertyBlock для установки параметров</param>
        /// <param name="forceAllEdges">Если true, включает все рваные края независимо от соседей</param>
        /// <param name="forceRecalculate">Если true, принудительно пересчитывает края даже если кэш валиден</param>
        private void UpdateRaggedEdgesPerCell(MaterialPropertyBlock propertyBlock, bool forceAllEdges = false, bool forceRecalculate = false)
        {
            // Если нет менеджера тумана — выходим
            if (FogOfWarManager.Instance == null)
            {
                return;
            }
            
            // Если эффект неровных краев глобально отключен И мы не принудительно включаем все края — выходим
            // Для анимации сгорания (forceAllEdges = true) всегда устанавливаем параметры граней
            if (!forceAllEdges && !FogOfWarManager.Instance.IsRaggedEdgesEnabled())
            {
                return;
            }

            // Используем кэш, если он валиден и параметры не изменились
            if (!forceRecalculate && cachedRaggedEdges != null && cachedForceAllEdges == forceAllEdges)
            {
                // Применяем кэшированные значения
                ApplyRaggedEdgesToPropertyBlock(propertyBlock, cachedRaggedEdges);
                return;
            }

            CellNameSpace.Grid grid = FogOfWarManager.Instance.GetGrid();
            if (grid == null)
            {
                return;
            }

            int gridWidth = grid.GetGridWidth();
            int gridHeight = grid.GetGridHeight();

            // Маска активных граней: 0 = Flat Left, 1 = Top Right, 2 = Bottom Right,
            // 3 = Flat Right, 4 = Bottom Left, 5 = Top Left
            bool[] faceActive = new bool[6];
            
            // Если принудительно включаем все края (для анимации сгорания), устанавливаем все грани как активные
            if (forceAllEdges)
            {
                for (int i = 0; i < faceActive.Length; i++)
                {
                    faceActive[i] = true;
                }
            }
            else
            {
                // Получаем соседей по координатам сетки
                var neighbors = HexagonalGridHelper.GetNeighbors(gridX, gridY, gridWidth, gridHeight);
                
                foreach (var pos in neighbors)
                {
                    CellInfo neighbor = grid.GetCellInfoAt(pos.x, pos.y);
                    if (neighbor == null)
                        continue;

                    FogOfWarState neighborState = neighbor.GetFogOfWarState();
                    
                    // Определяем, должен ли этот сосед рвать край в зависимости от нашего состояния
                    bool neighborTriggersEdge = false;
                    if (fogState == FogOfWarState.Hidden)
                    {
                        // Для Hidden нас рвёт всё, что не Hidden: Visible или Explored
                        neighborTriggersEdge = (neighborState == FogOfWarState.Visible ||
                                                neighborState == FogOfWarState.Explored);
                    }
                    else if (fogState == FogOfWarState.Explored)
                    {
                        // Для Explored нас рвут только реально видимые соседи
                        neighborTriggersEdge = (neighborState == FogOfWarState.Visible);
                    }

                    if (!neighborTriggersEdge)
                        continue;

                    // Берём направление до соседа в ЛОКАЛЬНОЙ системе координат меша тумана,
                    // чтобы совпасть с тем, как шейдер видит вершины (v.vertex.xy).
                    Vector3 neighborWorldPos3 = neighbor.GetOriginalPosition();
                    Vector3 neighborLocalPos3 = fogOfWarRenderer.transform.InverseTransformPoint(neighborWorldPos3);
                    Vector2 dir = new Vector2(neighborLocalPos3.x, neighborLocalPos3.y);

                    if (dir.sqrMagnitude < 0.0001f)
                        continue;

                    int faceIndex = GetClosestFaceIndexForDirection(dir);
                    if (faceIndex >= 0 && faceIndex < faceActive.Length)
                    {
                        faceActive[faceIndex] = true;
                    }
                }
            }

            // Сохраняем в кэш
            if (cachedRaggedEdges == null)
            {
                cachedRaggedEdges = new bool[6];
            }
            System.Array.Copy(faceActive, cachedRaggedEdges, 6);
            cachedForceAllEdges = forceAllEdges;
            
            // Применяем маску к шейдерным параметрам граней
            ApplyRaggedEdgesToPropertyBlock(propertyBlock, faceActive);
        }
        
        /// <summary>
        /// Применяет маску рваных краев к MaterialPropertyBlock
        /// </summary>
        private void ApplyRaggedEdgesToPropertyBlock(MaterialPropertyBlock propertyBlock, bool[] faceActive)
        {
            // Соответствие индексов faceActive к параметрам шейдера:
            // 0 -> _RaggedEdgeFlatLeft   (Flat Left, 90-150°)
            // 1 -> _RaggedEdgeTopRight   (Top Right, 330-30°)
            // 2 -> _RaggedEdgeBottomRight (Bottom Right, 210-270°)
            // 3 -> _RaggedEdgeFlatRight  (Flat Right, 270-330°)
            // 4 -> _RaggedEdgeBottomLeft (Bottom Left, 150-210°)
            // 5 -> _RaggedEdgeTopLeft    (Top Left, 30-90°)
            propertyBlock.SetFloat("_RaggedEdgeFlatLeft", faceActive[0] ? 1f : 0f);
            propertyBlock.SetFloat("_RaggedEdgeTopRight", faceActive[1] ? 1f : 0f);
            propertyBlock.SetFloat("_RaggedEdgeBottomRight", faceActive[2] ? 1f : 0f);
            propertyBlock.SetFloat("_RaggedEdgeFlatRight", faceActive[3] ? 1f : 0f);
            propertyBlock.SetFloat("_RaggedEdgeBottomLeft", faceActive[4] ? 1f : 0f);
            propertyBlock.SetFloat("_RaggedEdgeTopLeft", faceActive[5] ? 1f : 0f);
        }

        /// <summary>
        /// Возвращает индекс ближайшей грани для направления dir
        /// (аналог функции getClosestFace из шейдера, работает только по направлению).
        /// </summary>
        private int GetClosestFaceIndexForDirection(Vector2 dir)
        {
            float angle = Mathf.Atan2(dir.y, dir.x); // Угол в радианах от -PI до PI

            // Нормализуем угол к диапазону [0, 2*PI]
            if (angle < 0f)
                angle += 2f * Mathf.PI;

            const float pi_6 = Mathf.PI / 6f;       // 30°
            const float pi_2 = Mathf.PI / 2f;       // 90°
            const float pi_5_6 = 5f * Mathf.PI / 6f; // 150°
            const float pi_7_6 = 7f * Mathf.PI / 6f; // 210°
            const float pi_3_2 = 3f * Mathf.PI / 2f; // 270°
            const float pi_11_6 = 11f * Mathf.PI / 6f; // 330°

            // Логика полностью повторяет getClosestFace() из шейдера:
            // if (angle >= 330° || angle < 30°)  -> 1 (Top-Right)
            // else if (angle < 90°)              -> 5 (Top-Left)
            // else if (angle < 150°)             -> 0 (Flat Left)
            // else if (angle < 210°)             -> 4 (Bottom-Left)
            // else if (angle < 270°)             -> 2 (Bottom-Right)
            // else                                -> 3 (Flat Right)
            if (angle >= pi_11_6 || angle < pi_6)
                return 1; // Top-Right
            if (angle < pi_2)
                return 5; // Top-Left
            if (angle < pi_5_6)
                return 0; // Flat Left
            if (angle < pi_7_6)
                return 4; // Bottom-Left
            if (angle < pi_3_2)
                return 2; // Bottom-Right
            return 3; // Flat Right
        }
        
        /// <summary>
        /// Обновляет визуальное отображение тумана войны
        /// </summary>
        private void UpdateFogOfWarVisual()
        {
            if (fogOfWarRenderer == null)
                return;
            
            // Если идет анимация, не меняем материал и видимость рендерера, чтобы не прервать анимацию
            // Материал и видимость уже установлены корутиной
            if (transitionCoroutine != null)
            {
                // Только обновляем видимость оверлеев, если нужно
                switch (fogState)
                {
                    case FogOfWarState.Hidden:
                        SetOverlaysVisibility(false);
                        break;
                    case FogOfWarState.Explored:
                    case FogOfWarState.Visible:
                        SetOverlaysVisibility(true);
                        break;
                }
                return;
            }
            
            // Управляем видимостью рендерера и выбором материала
            switch (fogState)
            {
                case FogOfWarState.Hidden:
                    // Неразведено: показываем туман с материалом Fog_Unseen
                    fogOfWarRenderer.enabled = true;
                    Material unseenMaterial = FogOfWarManager.Instance != null ? 
                        FogOfWarManager.Instance.GetFogUnseenMaterial() : null;
                    if (unseenMaterial != null && fogOfWarRenderer.sharedMaterial != unseenMaterial)
                    {
                        // Сохраняем текущий MaterialPropertyBlock перед сменой материала
                        if (fogOfWarPropertyBlock == null)
                        {
                            fogOfWarPropertyBlock = new MaterialPropertyBlock();
                        }
                        fogOfWarRenderer.GetPropertyBlock(fogOfWarPropertyBlock);
                        
                        fogOfWarRenderer.sharedMaterial = unseenMaterial;
                        
                        // Восстанавливаем MaterialPropertyBlock после смены материала
                        fogOfWarRenderer.SetPropertyBlock(fogOfWarPropertyBlock);
                    }
                    // Устанавливаем alpha тумана
                    UpdateFogOfWarAlpha();
                    // Скрываем все оверлеи для скрытых клеток
                    SetOverlaysVisibility(false);
                    break;
                    
                case FogOfWarState.Explored:
                    // Разведено, но не видно: показываем туман с материалом Fog_Explored
                    fogOfWarRenderer.enabled = true;
                    Material exploredMaterial = FogOfWarManager.Instance != null ? 
                        FogOfWarManager.Instance.GetFogExploredMaterial() : null;
                    if (exploredMaterial != null && fogOfWarRenderer.sharedMaterial != exploredMaterial)
                    {
                        // Сохраняем текущий MaterialPropertyBlock перед сменой материала
                        if (fogOfWarPropertyBlock == null)
                        {
                            fogOfWarPropertyBlock = new MaterialPropertyBlock();
                        }
                        fogOfWarRenderer.GetPropertyBlock(fogOfWarPropertyBlock);
                        
                        fogOfWarRenderer.sharedMaterial = exploredMaterial;
                        
                        // Восстанавливаем MaterialPropertyBlock после смены материала
                        fogOfWarRenderer.SetPropertyBlock(fogOfWarPropertyBlock);
                    }
                    // Устанавливаем alpha тумана
                    UpdateFogOfWarAlpha();
                    // Показываем оверлеи для исследованных клеток
                    SetOverlaysVisibility(true);
                    break;
                    
                case FogOfWarState.Visible:
                    // Видимо: отключаем рендерер тумана
                    fogOfWarRenderer.enabled = false;
                    // Показываем оверлеи для видимых клеток
                    SetOverlaysVisibility(true);
                    // Применяем всю визуализацию (постройки, ресурсы, принадлежность)
                    CellVisualizationManager.ApplyAllVisualization(this);
                    break;
            }
        }
        
        /// <summary>
        /// Применяет параметры тумана войны в шейдер через MaterialPropertyBlock
        /// </summary>
        private void ApplyFogOfWarToShader()
        {
            if (cellRenderer == null)
                cellRenderer = GetComponent<Renderer>();
            
            MeshRenderer meshRenderer = cellRenderer as MeshRenderer;
            if (meshRenderer == null)
                return;
            
            // Создаем MaterialPropertyBlock, если его еще нет
            if (materialPropertyBlock == null)
            {
                materialPropertyBlock = new MaterialPropertyBlock();
            }
            
            // Получаем текущие свойства (чтобы не потерять _OriginalPosition и другие)
            meshRenderer.GetPropertyBlock(materialPropertyBlock);
            
            // Применяем параметры тумана
            ApplyFogOfWarToShaderInternal();
            
            // Применяем MaterialPropertyBlock к рендереру
            meshRenderer.SetPropertyBlock(materialPropertyBlock);
        }
        
        /// <summary>
        /// Внутренний метод для установки параметров тумана в MaterialPropertyBlock
        /// (без получения и применения блока - вызывается из других методов)
        /// </summary>
        private void ApplyFogOfWarToShaderInternal()
        {
            // Устанавливаем состояние тумана в шейдер
            // 0 = Hidden, 1 = Explored, 2 = Visible
            float fogStateValue = 0f;
            switch (fogState)
            {
                case FogOfWarState.Hidden:
                    fogStateValue = 0f;
                    break;
                case FogOfWarState.Explored:
                    fogStateValue = 1f;
                    break;
                case FogOfWarState.Visible:
                    fogStateValue = 2f;
                    break;
            }
            
            materialPropertyBlock.SetFloat("_FogState", fogStateValue);
            
            // Устанавливаем цвета тумана (если они не заданы в материале, используем значения по умолчанию)
            materialPropertyBlock.SetColor("_FogColor", new Color(0f, 0f, 0f, 1f)); // Черный туман
        }
        
        /// <summary>
        /// Запускает анимацию перехода между состояниями тумана войны
        /// </summary>
        /// <param name="fromState">Исходное состояние</param>
        /// <param name="toState">Целевое состояние</param>
        /// <param name="duration">Длительность анимации в секундах</param>
        private void StartTransitionAnimation(FogOfWarState fromState, FogOfWarState toState, float duration)
        {
            // Если анимация уже идет, не прерываем её
            if (transitionCoroutine != null)
            {
                return;
            }
            
            // Сохраняем целевое состояние анимации ПЕРЕД запуском корутины
            transitionTargetState = toState;
            
            // Запускаем новую корутину
            transitionCoroutine = StartCoroutine(TransitionCoroutine(fromState, toState, duration));
        }
        
        /// <summary>
        /// Корутина для анимации перехода между состояниями тумана войны
        /// </summary>
        /// <param name="fromState">Исходное состояние</param>
        /// <param name="toState">Целевое состояние</param>
        /// <param name="duration">Длительность анимации в секундах</param>
        private IEnumerator TransitionCoroutine(FogOfWarState fromState, FogOfWarState toState, float duration)
        {
            // Определяем тип перехода
            float transitionType = 0f;
            bool isBurnAnimation = false;
            
            if (fromState == FogOfWarState.Hidden && 
                (toState == FogOfWarState.Visible || toState == FogOfWarState.Explored))
            {
                transitionType = 1f; // Сгорание
                isBurnAnimation = true;
            }
            else if (fromState == FogOfWarState.Explored && toState == FogOfWarState.Visible)
            {
                transitionType = 1f; // Сгорание (изменено с fade out на сгорание)
                isBurnAnimation = true;
            }
            else if (fromState == FogOfWarState.Visible && toState == FogOfWarState.Explored)
            {
                transitionType = 3f; // Fade in
                isBurnAnimation = false;
            }
            
            // Если тип перехода не определен, устанавливаем состояние сразу и завершаем корутину
            if (transitionType < 0.5f)
            {
                // Устанавливаем финальное состояние
                fogState = toState;
                
                // Если клетка стала видимой, отмечаем её как исследованную
                if (toState == FogOfWarState.Visible)
                {
                    hasBeenExplored = true;
                }
                
                // Сбрасываем ссылку на корутину и целевое состояние
                transitionCoroutine = null;
                transitionTargetState = FogOfWarState.Hidden;
                
                // Обновляем визуализацию
                UpdateFogOfWarVisual();
                
                yield break;
            }
            
            // Устанавливаем целевое состояние сразу после того, как запомнили его
            // Это позволяет другим системам видеть правильное состояние во время анимации
            fogState = toState;
            
            // Если клетка стала видимой, отмечаем её как исследованную
            if (toState == FogOfWarState.Visible)
            {
                hasBeenExplored = true;
            }

            UpdateFogOfWarVisual();
            
            // Определяем, является ли это анимацией сгорания
            bool isBurnAnimationActive = isBurnAnimation;
            
            // Для анимации нужно правильно настроить материал и видимость рендерера
            // В зависимости от типа перехода используем разные материалы
            Material animationMaterial = null;
            bool shouldShowRenderer = true;
            
            if (isBurnAnimationActive) // Тип 1 = сгорание (Hidden→Visible/Explored или Explored→Visible)
            {
                // Для сгорания используем материал в зависимости от исходного состояния
                if (fromState == FogOfWarState.Hidden)
                {
                    // Hidden → Visible/Explored: используем материал Hidden
                    animationMaterial = FogOfWarManager.Instance != null ? 
                        FogOfWarManager.Instance.GetFogUnseenMaterial() : null;
                }
                else if (fromState == FogOfWarState.Explored)
                {
                    // Explored → Visible: используем материал Explored
                    animationMaterial = FogOfWarManager.Instance != null ? 
                        FogOfWarManager.Instance.GetFogExploredMaterial() : null;
                }
                shouldShowRenderer = true;
            }
            else if (transitionType > 2.5f && transitionType < 3.5f) // Тип 3 = fade in (Visible→Explored)
            {
                // Для fade in используем материал Explored
                animationMaterial = FogOfWarManager.Instance != null ? 
                    FogOfWarManager.Instance.GetFogExploredMaterial() : null;
                shouldShowRenderer = true;
            }
            
            // Устанавливаем материал и видимость рендерера для анимации
            if (fogOfWarRenderer != null)
            {
                // Сначала получаем текущий MaterialPropertyBlock, чтобы сохранить параметры
                if (fogOfWarPropertyBlock == null)
                {
                    fogOfWarPropertyBlock = new MaterialPropertyBlock();
                }
                fogOfWarRenderer.GetPropertyBlock(fogOfWarPropertyBlock);
                
                // Сохраняем _OriginalPosition из текущего блока
                Vector4 savedOriginalPos = fogOfWarPropertyBlock.GetVector("_OriginalPosition");
                
                // Меняем материал
                if (animationMaterial != null)
                {
                    fogOfWarRenderer.sharedMaterial = animationMaterial;
                }
                fogOfWarRenderer.enabled = shouldShowRenderer;
                
                // Восстанавливаем MaterialPropertyBlock после смены материала
                // Это важно, так как смена материала может сбросить блок
                fogOfWarRenderer.GetPropertyBlock(fogOfWarPropertyBlock);
                
                // Восстанавливаем _OriginalPosition
                if (savedOriginalPos != Vector4.zero)
                {
                    fogOfWarPropertyBlock.SetVector("_OriginalPosition", savedOriginalPos);
                }
                else
                {
                    // Если не было сохранено, устанавливаем заново
                    Vector4 originalPos = new Vector4(originalPosition.x, originalPosition.y, originalPosition.z, 0f);
                    fogOfWarPropertyBlock.SetVector("_OriginalPosition", originalPos);
                }
            }
            
            // Обновляем параметры неровных краев (важно для эффекта сгорания)
            // Для анимации сгорания принудительно включаем все рваные края
            UpdateRaggedEdgesPerCell(fogOfWarPropertyBlock, isBurnAnimationActive);
            
            // Получаем исходный _HexRadius из материала для анимации сгорания
            float originalHexRadius = 0f;
            if (isBurnAnimationActive && animationMaterial != null && animationMaterial.HasProperty("_HexRadius"))
            {
                originalHexRadius = animationMaterial.GetFloat("_HexRadius");
            }
            
            // Устанавливаем параметры анимации
            fogOfWarPropertyBlock.SetFloat("_TransitionType", transitionType);
            fogOfWarPropertyBlock.SetFloat("_TransitionProgress", 0f);
            
            // Для анимации сгорания устанавливаем начальный анимированный радиус
            if (isBurnAnimationActive)
            {
                fogOfWarPropertyBlock.SetFloat("_AnimatedHexRadius", originalHexRadius);
            }
            
            // Применяем MaterialPropertyBlock
            fogOfWarRenderer.SetPropertyBlock(fogOfWarPropertyBlock);
            
            float elapsedTime = 0f;
            
            // Кэшируем _OriginalPosition один раз перед циклом
            Vector4 cachedOriginalPos = fogOfWarPropertyBlock.GetVector("_OriginalPosition");
            if (cachedOriginalPos == Vector4.zero)
            {
                cachedOriginalPos = new Vector4(originalPosition.x, originalPosition.y, originalPosition.z, 0f);
            }
            
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / duration);
                
                // Применяем easing для более плавного движения
                progress = Mathf.SmoothStep(0f, 1f, progress);
                
                // Обновляем только изменяющиеся параметры без лишних GetPropertyBlock
                fogOfWarPropertyBlock.SetVector("_OriginalPosition", cachedOriginalPos);
                fogOfWarPropertyBlock.SetFloat("_TransitionType", transitionType);
                fogOfWarPropertyBlock.SetFloat("_TransitionProgress", progress);
                
                // Для анимации сгорания плавно уменьшаем радиус от исходного до 0 с ускорением
                if (isBurnAnimationActive)
                {
                    // Используем квадратичную функцию для ускорения: progress^2
                    // Это создаст эффект ускорения - в начале медленнее, в конце быстрее
                    float acceleratedProgress = progress * progress;
                    // Плавно уменьшаем радиус: при progress = 0 радиус = originalHexRadius, при progress = 1 радиус = 0
                    float animatedRadius = originalHexRadius * (1f - acceleratedProgress);
                    fogOfWarPropertyBlock.SetFloat("_AnimatedHexRadius", animatedRadius);
                }
                
                // Применяем кэшированные параметры неровных краев (они уже установлены один раз перед циклом)
                // Не нужно вызывать UpdateRaggedEdgesPerCell каждый кадр
                
                fogOfWarRenderer.SetPropertyBlock(fogOfWarPropertyBlock);
                
                yield return null;
            }
            
            // Завершаем анимацию: сбрасываем ссылку на корутину ПЕРЕД вызовом UpdateFogOfWarVisual
            // Это нужно, чтобы UpdateFogOfWarVisual мог правильно установить материал
            transitionCoroutine = null;
            
            // Для анимации сгорания: перед сбросом параметров устанавливаем альфу в 0,
            // чтобы предотвратить появление полной клетки Hidden на миг
            if (isBurnAnimationActive)
            {
                if (fogOfWarPropertyBlock == null)
                {
                    fogOfWarPropertyBlock = new MaterialPropertyBlock();
                }
                
                fogOfWarRenderer.GetPropertyBlock(fogOfWarPropertyBlock);
                
                // Получаем текущий цвет и устанавливаем альфу в 0
                Color currentColor = fogOfWarPropertyBlock.GetColor("_Color");
                if (currentColor == Color.clear)
                {
                    // Если цвет не установлен, получаем из материала
                    if (fogOfWarRenderer.sharedMaterial != null && fogOfWarRenderer.sharedMaterial.HasProperty("_Color"))
                    {
                        currentColor = fogOfWarRenderer.sharedMaterial.GetColor("_Color");
                    }
                    else
                    {
                        currentColor = Color.white;
                    }
                }
                //currentColor.a = 0f; // Устанавливаем альфу в 0
                //fogOfWarPropertyBlock.SetColor("_Color", currentColor);
                fogOfWarRenderer.SetPropertyBlock(fogOfWarPropertyBlock);
            }
            
            // Сбрасываем параметры анимации
            if (fogOfWarPropertyBlock == null)
            {
                fogOfWarPropertyBlock = new MaterialPropertyBlock();
            }
            
            fogOfWarRenderer.GetPropertyBlock(fogOfWarPropertyBlock);
            
            // ========== СБРОС ПАРАМЕТРОВ АНИМАЦИИ ==========
            // Сбрасываем тип анимации перехода (0 = нет анимации)
            fogOfWarPropertyBlock.SetFloat("_TransitionType", 0f);
            
            // Сбрасываем прогресс анимации (0 = начало, 1 = конец)
            fogOfWarPropertyBlock.SetFloat("_TransitionProgress", 0f);
            
            // Сбрасываем анимированный радиус (устанавливаем в 0, чтобы не использовался)
            // Это важно, так как шейдер проверяет _AnimatedHexRadius > 0 для определения активной анимации сгорания
            fogOfWarPropertyBlock.SetFloat("_AnimatedHexRadius", 0f);
            // ================================================
            
            fogOfWarRenderer.SetPropertyBlock(fogOfWarPropertyBlock);
            
            // Устанавливаем финальное состояние после завершения анимации
            // Используем toState (который уже сохранен в transitionTargetState)
            fogState = toState;
            
            // Если клетка стала видимой, отмечаем её как исследованную
            if (toState == FogOfWarState.Visible)
            {
                hasBeenExplored = true;
            }
            
            // Сбрасываем целевое состояние анимации
            transitionTargetState = FogOfWarState.Hidden;
            
            // Применяем финальное визуальное состояние
            // Теперь UpdateFogOfWarVisual сможет правильно установить материал, так как корутина уже сброшена
            UpdateFogOfWarVisual();
            
            // Дополнительно обновляем рендер после перехода в целевое состояние
            // Это гарантирует, что все параметры (рваные края, альфа и т.д.) обновлены правильно
            if (fogState == FogOfWarState.Hidden || fogState == FogOfWarState.Explored)
            {
                UpdateFogOfWarAlpha();
            }
            
            // После завершения анимации обновляем рваные края для всех соседей
            // Это важно, так как изменение состояния этой клетки влияет на отображение краев у соседей
            RefreshNeighborsFogOfWarRaggedEdges();
        }
        
        /// <summary>
        /// Обновляет рваные края тумана войны для всех соседних клеток
        /// </summary>
        private void RefreshNeighborsFogOfWarRaggedEdges()
        {
            if (FogOfWarManager.Instance == null)
                return;
            
            CellNameSpace.Grid grid = FogOfWarManager.Instance.GetGrid();
            if (grid == null)
                return;
            
            int gridWidth = grid.GetGridWidth();
            int gridHeight = grid.GetGridHeight();
            
            // Обновляем рваные края для всех соседей
            var neighbors = HexagonalGridHelper.GetNeighbors(gridX, gridY, gridWidth, gridHeight);
            foreach (var pos in neighbors)
            {
                CellInfo neighbor = grid.GetCellInfoAt(pos.x, pos.y);
                if (neighbor != null)
                {
                    neighbor.RefreshFogOfWarRaggedEdges();
                }
            }
        }
        
        /// <summary>
        /// Устанавливает видимость оверлеев (ресурсы, постройки и т.д.)
        /// </summary>
        private void SetOverlaysVisibility(bool visible)
        {
            if (resourcesOverlay != null)
            {
                resourcesOverlay.enabled = visible;
            }
            if (buildingsOverlay != null)
            {
                buildingsOverlay.enabled = visible;
            }
            if (cityBorderOverlay != null && !outlineEnabled)
            {
                cityBorderOverlay.enabled = visible;
            }
            if (ownershipOverlay != null)
            {
                ownershipOverlay.enabled = visible;
            }
        }
        
    }
}

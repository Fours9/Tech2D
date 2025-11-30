using UnityEngine;
using System.Reflection;
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
        [SerializeField] [Range(0f, 1f)] private float hiddenAlpha = 1.0f; // Alpha для не разведанных клеток
        [SerializeField] [Range(0f, 1f)] private float exploredAlpha = 0.6f; // Alpha для разведанных клеток
        
        [Header("Настройки обводки")]
        [SerializeField] private bool outlineEnabled = false; // Включена ли обводка
        [SerializeField] private Color outlineColor = Color.black; // Цвет обводки
        [SerializeField] [Range(1f, 10f)] private float outlineWidth = 2f; // Толщина обводки в пикселях

        private Renderer cellRenderer;
        private CellMaterialManager cachedMaterialManager = null;
        private CellOverlayManager cachedOverlayManager = null;
        private Vector2? cachedCellSize = null; // Кэшированный размер клетки
        private CityInfo owningCity = null; // Город, которому принадлежит клетка
        private Vector3 originalPosition; // Изначальная позиция клетки в мире
        private bool originalPositionSet = false; // Флаг, что изначальная позиция уже установлена
        private MaterialPropertyBlock materialPropertyBlock = null; // MaterialPropertyBlock для передачи параметров в шейдер
        
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
            if (owningCity != null)
            {
                ApplyOwnershipVisualization();
            }
            
            // Обновляем оверлеи при изменении типа клетки (если требуется)
            if (updateOverlays)
            {
                UpdateOverlays();
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
            
            // Применяем MaterialPropertyBlock к рендереру
            meshRenderer.SetPropertyBlock(materialPropertyBlock);
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
        
        /// <summary>
        /// Устанавливает спрайт на слой построек (для городов и других строений)
        /// </summary>
        /// <param name="sprite">Спрайт для установки</param>
        public void SetBuildingSprite(Sprite sprite)
        {
            if (buildingsOverlay == null)
            {
                Debug.LogWarning($"CellInfo: buildingsOverlay не назначен на клетке {gameObject.name}");
                return;
            }
            
            if (sprite != null)
            {
                buildingsOverlay.sprite = sprite;
                buildingsOverlay.enabled = true;
                
                // Масштабируем спрайт под размер клетки
                Vector2 cellSize = GetCellSize();
                ScaleSpriteToCellSize(buildingsOverlay, sprite, cellSize);
                
                Debug.Log($"CellInfo: Спрайт строения установлен на клетку ({gridX}, {gridY})");
            }
            else
            {
                buildingsOverlay.sprite = null;
                buildingsOverlay.enabled = false;
            }
        }
        
        /// <summary>
        /// Удаляет спрайт со слоя построек
        /// </summary>
        public void ClearBuildingSprite()
        {
            if (buildingsOverlay != null)
            {
                buildingsOverlay.sprite = null;
                buildingsOverlay.enabled = false;
            }
        }
        
        /// <summary>
        /// Устанавливает принадлежность клетки к городу (визуальная индикация)
        /// Подсвечивает клетку изменением цвета для визуального выделения
        /// </summary>
        /// <param name="city">Город, которому принадлежит клетка</param>
        public void SetCityOwnership(CityInfo city)
        {
            owningCity = city;
            
            // Визуальная индикация: применяем границы и overlay-тинтинг
            ApplyOwnershipVisualization();
            
            Debug.Log($"CellInfo: Клетка ({gridX}, {gridY}) теперь принадлежит городу {city.name}");
        }
        
        /// <summary>
        /// Применяет визуализацию принадлежности клетки игроку/городу (границы + overlay-тинтинг)
        /// </summary>
        private void ApplyOwnershipVisualization()
        {
            if (owningCity == null)
                return;
            
            // Получаем цвет игрока из города
            Color playerColor = GetPlayerColorFromCity(owningCity);
            
            // Применяем границы через обводку
            SetOutline(true, playerColor, 2.5f);
            
            // Применяем overlay-тинтинг
            ApplyOwnershipTinting(playerColor);
        }
        
        /// <summary>
        /// Получает цвет игрока из города (вспомогательный метод для обхода проблем компиляции)
        /// </summary>
        private Color GetPlayerColorFromCity(CityInfo city)
        {
            if (city == null)
                return Color.white;
            
            // Используем рефлексию для безопасного доступа к полю player
            FieldInfo playerField = typeof(CityInfo).GetField("player");
            if (playerField != null)
            {
                object playerObj = playerField.GetValue(city);
                if (playerObj != null)
                {
                    // Получаем поле playerColor через рефлексию
                    FieldInfo colorField = playerObj.GetType().GetField("playerColor");
                    if (colorField != null)
                    {
                        object colorObj = colorField.GetValue(playerObj);
                        if (colorObj is Color)
                        {
                            return (Color)colorObj;
                        }
                    }
                }
            }
            
            return Color.white; // Цвет по умолчанию
        }
        
        /// <summary>
        /// Применяет легкий тинтинг принадлежности через overlay-слой
        /// </summary>
        /// <param name="playerColor">Цвет игрока</param>
        private void ApplyOwnershipTinting(Color playerColor)
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
        /// Убирает принадлежность клетки к городу
        /// Отключает границы и overlay-тинтинг
        /// </summary>
        public void ClearCityOwnership()
        {
            if (owningCity != null)
            {
                CityInfo cityToRemove = owningCity;
                owningCity = null; // Очищаем перед обновлением, чтобы визуализация не применилась снова
                
                // Отключаем границы
                SetOutline(false);
                
                // Отключаем overlay-тинтинг
                if (ownershipOverlay != null)
                {
                    ownershipOverlay.enabled = false;
                }
                
                // Восстанавливаем оригинальный цвет через UpdateCellColor
                UpdateCellColor(false);
                
                Debug.Log($"CellInfo: Клетка ({gridX}, {gridY}) больше не принадлежит городу {cityToRemove.name}");
            }
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
        /// </summary>
        public void SetFogOfWarState(FogOfWarState state)
        {
            fogState = state;
            
            // Если клетка стала видимой, отмечаем её как исследованную
            if (state == FogOfWarState.Visible)
            {
                hasBeenExplored = true;
            }
            
            // Обновляем визуальное отображение тумана
            UpdateFogOfWarVisual();
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
        /// Устанавливает alpha тумана войны на дочернем объекте
        /// </summary>
        private void UpdateFogOfWarAlpha()
        {
            if (fogOfWarRenderer == null)
                return;
            
            // Определяем целевой alpha на основе состояния тумана
            float targetAlpha = 0f;
            switch (fogState)
            {
                case FogOfWarState.Hidden:
                    targetAlpha = hiddenAlpha;
                    break;
                case FogOfWarState.Explored:
                    targetAlpha = exploredAlpha;
                    break;
                case FogOfWarState.Visible:
                    targetAlpha = 0f; // Полностью прозрачный
                    break;
            }
            
            // Используем MaterialPropertyBlock для изменения alpha без создания instance материала
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            fogOfWarRenderer.GetPropertyBlock(mpb);
            
            // Получаем базовый цвет материала или используем черный по умолчанию
            Color baseColor = Color.black;
            if (mpb.HasColor("_Color"))
            {
                baseColor = mpb.GetColor("_Color");
            }
            else if (fogOfWarRenderer.sharedMaterial != null)
            {
                baseColor = fogOfWarRenderer.sharedMaterial.color;
            }
            
            // Устанавливаем alpha
            baseColor.a = targetAlpha;
            mpb.SetColor("_Color", baseColor);
            
            // Применяем MaterialPropertyBlock к рендереру
            fogOfWarRenderer.SetPropertyBlock(mpb);
        }
        
        /// <summary>
        /// Обновляет визуальное отображение тумана войны
        /// </summary>
        private void UpdateFogOfWarVisual()
        {
            // Устанавливаем alpha тумана на дочернем объекте
            UpdateFogOfWarAlpha();
            
            // Управляем видимостью оверлеев
            switch (fogState)
            {
                case FogOfWarState.Hidden:
                    // Скрываем все оверлеи для скрытых клеток
                    SetOverlaysVisibility(false);
                    break;
                    
                case FogOfWarState.Explored:
                case FogOfWarState.Visible:
                    // Показываем оверлеи для исследованных и видимых клеток
                    SetOverlaysVisibility(true);
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

using UnityEngine;

namespace CellNameSpace
{
    public class CameraController : MonoBehaviour
    {
        [Header("Настройки перемещения")]
        // Примечание: движение камеры теперь соответствует движению курсора 1:1 без сглаживания
        
        [Header("Настройки зума")]
        [SerializeField] [Range(1f, 200f)] [Tooltip("Скорость приближения/отдаления камеры. Чем меньше значение, тем медленнее зум.")]
        private float zoomSpeed = 40f; // Скорость приближения/отдаления
        [SerializeField] private float minOrthoSize = 5f; // Минимальный размер (максимальное приближение)
        [SerializeField] private float maxOrthoSize = 100f; // Максимальный размер (максимальное отдаление)
        
        [Header("Настройки определения клика")]
        [SerializeField] private float maxClickTime = 0.3f; // Максимальное время для считания кликом (секунды)
        [SerializeField] private float maxClickDistance = 5f; // Максимальное расстояние перемещения для считания кликом (в пикселях)
        
        private Camera mainCamera;
        private Vector3 targetCameraPosition;
        private float targetOrthoSize;
        
        // Обработчики
        private CameraDragHandler dragHandler;
        private CameraZoomHandler zoomHandler;
        private ClickDetector clickDetector;
        
        private void Awake()
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = GetComponent<Camera>();
            }
            
            if (mainCamera == null)
            {
                Debug.LogError("CameraController: Main Camera не найдена!");
                return;
            }
            
            // Инициализируем целевые значения текущими
            targetCameraPosition = mainCamera.transform.position;
            targetOrthoSize = mainCamera.orthographicSize;
            
            // Инициализируем обработчики
            dragHandler = new CameraDragHandler(mainCamera);
            zoomHandler = new CameraZoomHandler(mainCamera, zoomSpeed, minOrthoSize, maxOrthoSize);
            clickDetector = new ClickDetector(maxClickTime, maxClickDistance);
        }
        
        private void Update()
        {
            if (mainCamera == null) return;
            
            // Проверяем, используется ли тач-экран
            if (Input.touchCount > 0)
            {
                HandleTouchInput();
            }
            else
            {
                // Используем мышь/клавиатуру
                HandleMouseInput();
            }
            
            // Применяем перемещение и зум
            ApplyMovement();
            ApplyZoom();
        }
        
        private void HandleMouseInput()
        {
            // Обработка перетаскивания
            targetCameraPosition = dragHandler.HandleMouseDrag(targetCameraPosition);
            
            // Обработка зума
            (targetCameraPosition, targetOrthoSize) = zoomHandler.HandleMouseZoom(
                targetCameraPosition, 
                targetOrthoSize, 
                dragHandler.IsDragging
            );
            
            // Обновляем детектор кликов
            if (Input.GetMouseButtonDown(0))
            {
                clickDetector.OnMouseDown();
            }
            clickDetector.SetDragging(dragHandler.IsDragging);
        }
        
        private void HandleTouchInput()
        {
            // Если два касания - pinch-зум
            if (Input.touchCount == 2)
            {
                (targetCameraPosition, targetOrthoSize) = zoomHandler.HandlePinchZoom(
                    targetCameraPosition, 
                    targetOrthoSize
                );
            }
            // Если одно касание - перетаскивание
            else if (Input.touchCount == 1)
            {
                targetCameraPosition = dragHandler.HandleTouchDrag(targetCameraPosition);
                
                // Обновляем детектор кликов
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    clickDetector.OnTouchDown();
                }
            }
        }
        
        private void ApplyMovement()
        {
            if (mainCamera == null) return;
            
            // Мгновенно применяем позицию камеры без сглаживания
            mainCamera.transform.position = targetCameraPosition;
        }
        
        private void ApplyZoom()
        {
            if (mainCamera == null) return;
            
            zoomHandler.ApplyZoom(targetOrthoSize);
        }
        
        /// <summary>
        /// Проверяет, был ли это быстрый клик (без перетаскивания камеры)
        /// </summary>
        /// <returns>true если это был клик, false если было перетаскивание</returns>
        public bool WasClick()
        {
            return clickDetector.WasClick(dragHandler.WasCameraMoved);
        }
        
        /// <summary>
        /// Проверяет, была ли камера перемещена во время последнего зажатия мыши/тача
        /// </summary>
        public bool WasCameraMoved()
        {
            return dragHandler.WasCameraMoved;
        }
        
        /// <summary>
        /// Получить время нажатия мыши (для проверки клика)
        /// </summary>
        public float GetClickDuration()
        {
            return clickDetector.GetClickDuration();
        }
        
        /// <summary>
        /// Проверяет, зажата ли сейчас левая кнопка мыши
        /// </summary>
        public bool IsMouseButtonDown()
        {
            return clickDetector.IsMouseButtonDown();
        }
        
        /// <summary>
        /// Перемещает камеру на указанную позицию (сохраняет Z координату)
        /// </summary>
        /// <param name="position">Целевая позиция камеры</param>
        /// <param name="instant">Если true, перемещает мгновенно, иначе через targetCameraPosition</param>
        public void MoveToPosition(Vector3 position, bool instant = false)
        {
            if (mainCamera == null)
            {
                Debug.LogWarning("CameraController: Камера не найдена, невозможно переместить");
                return;
            }
            
            // Сохраняем текущую Z координату камеры
            Vector3 newPosition = new Vector3(position.x, position.y, mainCamera.transform.position.z);
            
            if (instant)
            {
                // Мгновенное перемещение
                mainCamera.transform.position = newPosition;
                targetCameraPosition = newPosition;
            }
            else
            {
                // Плавное перемещение через targetCameraPosition
                targetCameraPosition = newPosition;
            }
        }
        
        /// <summary>
        /// Перемещает камеру на позицию объекта
        /// </summary>
        /// <param name="target">Целевой объект</param>
        /// <param name="instant">Если true, перемещает мгновенно</param>
        public void MoveToTarget(Transform target, bool instant = false)
        {
            if (target == null)
            {
                Debug.LogWarning("CameraController: Целевой объект равен null");
                return;
            }
            
            MoveToPosition(target.position, instant);
        }
        
        /// <summary>
        /// Перемещает камеру на позицию GameObject
        /// </summary>
        /// <param name="target">Целевой GameObject</param>
        /// <param name="instant">Если true, перемещает мгновенно</param>
        public void MoveToTarget(GameObject target, bool instant = false)
        {
            if (target == null)
            {
                Debug.LogWarning("CameraController: Целевой GameObject равен null");
                return;
            }
            
            MoveToTarget(target.transform, instant);
        }
    }
}


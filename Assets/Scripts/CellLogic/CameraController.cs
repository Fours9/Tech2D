using UnityEngine;

namespace CellNameSpace
{
    public class CameraController : MonoBehaviour
    {
        [Header("Настройки перемещения")]
        // Примечание: движение камеры теперь соответствует движению курсора 1:1 без сглаживания
        
        [Header("Настройки зума")]
        [SerializeField] private float zoomSpeed = 80f; // Скорость приближения/отдаления
        [SerializeField] private float minOrthoSize = 5f; // Минимальный размер (максимальное приближение)
        [SerializeField] private float maxOrthoSize = 100f; // Максимальный размер (максимальное отдаление)
        
        [Header("Настройки определения клика")]
        [SerializeField] private float maxClickTime = 0.3f; // Максимальное время для считания кликом (секунды)
        [SerializeField] private float maxClickDistance = 5f; // Максимальное расстояние перемещения для считания кликом (в пикселях)
        
        private Camera mainCamera;
        private bool isDragging = false;
        private Vector3 lastMousePosition;
        private float mouseDownTime;
        private Vector3 mouseDownPosition;
        private bool wasCameraMoved = false;
        private Vector3 initialCameraPosition;
        
        // Переменные для тач-управления
        private bool isTouchDragging = false;
        private Vector2 lastTouchPosition;
        private float touchDownTime;
        private Vector2 touchDownPosition;
        private Vector3 initialCameraPositionTouch;
        private bool wasCameraMovedTouch = false;
        
        // Переменные для перемещения
        private Vector3 targetCameraPosition;
        private float targetOrthoSize;
        private Vector2 lastZoomScreenPosition; // Экранная позиция курсора при начале зума
        private Vector3 fixedZoomWorldPosition; // Фиксированная мировая позиция точки под курсором во время зума
        private bool isZooming = false; // Флаг активности зума
        private float lastZoomInputTime = 0f; // Время последней прокрутки колесика
        private float lastPinchInputTime = 0f; // Время последнего pinch-жеста
        private Vector2 lastPinchCenter = Vector2.zero; // Последний центр pinch
        
        
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
            // Синхронизируем целевую позицию с текущей для мгновенного перемещения
            lastZoomScreenPosition = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f); // Центр экрана по умолчанию
            fixedZoomWorldPosition = Vector3.zero;
            
            // Инициализируем время последнего ввода для избежания ложных срабатываний
            lastZoomInputTime = Time.time;
            lastPinchInputTime = Time.time;
        }
        
        private void Update()
        {
            // Проверяем, используется ли тач-экран
            if (Input.touchCount > 0)
            {
                HandleTouchInput();
            }
            else
            {
                // Используем мышь/клавиатуру
                HandleCameraDrag();
                HandleCameraZoom();
            }
            
            // Применяем плавное перемещение и зум
            ApplySmoothMovement();
            ApplySmoothZoom();
        }
        
        private void HandleCameraDrag()
        {
            // Начало зажатия левой кнопки мыши
            if (Input.GetMouseButtonDown(0))
            {
                isDragging = true;
                lastMousePosition = Input.mousePosition;
                mouseDownTime = Time.time;
                mouseDownPosition = Input.mousePosition;
                initialCameraPosition = mainCamera.transform.position;
                wasCameraMoved = false;
                
                // Не сбрасываем фиксацию зума при начале перетаскивания, чтобы зум работал относительно курсора
            }
            
            // Перемещение камеры при зажатой кнопке
            if (isDragging && Input.GetMouseButton(0))
            {
                Vector3 mouseDelta = Input.mousePosition - lastMousePosition;
                
                // Проверяем, есть ли движение мыши
                if (mouseDelta.sqrMagnitude > 0.001f)
                {
                    // Преобразуем движение мыши в движение камеры
                    // Для 2D ортографической камеры конвертируем пиксели в единицы мира
                    float orthoSize = mainCamera.orthographicSize;
                    float screenHeight = Screen.height;
                    float pixelsToWorld = (orthoSize * 2f) / screenHeight;
                    
                    // Инвертируем движение мыши для естественного перетаскивания
                    // Движение камеры равно движению курсора (1:1)
                    Vector3 move = new Vector3(-mouseDelta.x * pixelsToWorld, 
                                              -mouseDelta.y * pixelsToWorld, 
                                              0f);
                    
                    targetCameraPosition += move;
                    
                    // Проверяем, была ли камера перемещена (минимальный порог для избежания дрожания)
                    if (Vector3.Distance(mainCamera.transform.position, initialCameraPosition) > 0.01f)
                    {
                        wasCameraMoved = true;
                    }
                }
                
                lastMousePosition = Input.mousePosition;
            }
            
            // Отпускание кнопки
            if (Input.GetMouseButtonUp(0))
            {
                isDragging = false;
            }
        }
        
        private void HandleCameraZoom()
        {
            if (mainCamera == null) return;
            
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            
            if (Mathf.Abs(scroll) > 0.01f)
            {
                float currentTime = Time.time;
                float timeSinceLastZoom = currentTime - lastZoomInputTime;
                lastZoomInputTime = currentTime;
                
                // Проверяем, завершен ли предыдущий зум
                float sizeDifference = Mathf.Abs(mainCamera.orthographicSize - targetOrthoSize);
                
                // Сбрасываем фиксацию и начинаем новый зум если:
                // 1. Предыдущий зум завершен (размер достиг цели)
                // 2. ИЛИ прошло достаточно времени с последней прокрутки (новый цикл зума)
                bool zoomCompleted = Mathf.Approximately(sizeDifference, 0f);
                bool newZoomCycle = timeSinceLastZoom > 0.02f; // Если прошло больше 0.02 секунды - новый цикл
                
                // Всегда обновляем позицию курсора при зуме (даже во время перетаскивания)
                // Это позволяет зумить к позиции курсора во время перетаскивания карты
                if (!isZooming || zoomCompleted || newZoomCycle || isDragging)
                {
                    // Фиксируем новую позицию при начале нового цикла зума
                    lastZoomScreenPosition = Input.mousePosition;
                    fixedZoomWorldPosition = mainCamera.ScreenToWorldPoint(lastZoomScreenPosition);
                    fixedZoomWorldPosition.z = mainCamera.transform.position.z;
                    isZooming = true;
                }
                
                ZoomCameraAtPosition(-scroll * zoomSpeed);
            }
        }
        
        private void HandleTouchInput()
        {
            if (mainCamera == null) return;
            
            // Если два касания - pinch-зум
            if (Input.touchCount == 2)
            {
                HandlePinchZoom();
                isTouchDragging = false;
            }
            // Если одно касание - перетаскивание
            else if (Input.touchCount == 1)
            {
                HandleTouchDrag();
            }
            else
            {
                // Нет касаний или больше двух - сбрасываем состояние
                isTouchDragging = false;
            }
        }
        
        private void HandleTouchDrag()
        {
            Touch touch = Input.GetTouch(0);
            
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    isTouchDragging = true;
                    lastTouchPosition = touch.position;
                    touchDownTime = Time.time;
                    touchDownPosition = touch.position;
                    initialCameraPositionTouch = mainCamera.transform.position;
                    wasCameraMovedTouch = false;
                    
                    // Не сбрасываем фиксацию зума при начале перетаскивания, чтобы зум работал относительно касания
                    break;
                    
                case TouchPhase.Moved:
                    if (isTouchDragging)
                    {
                        Vector2 touchDelta = touch.position - lastTouchPosition;
                        
                        // Проверяем, есть ли движение
                        if (touchDelta.sqrMagnitude > 0.001f)
                        {
                            // Преобразуем движение тача в движение камеры
                            float orthoSize = mainCamera.orthographicSize;
                            float screenHeight = Screen.height;
                            float pixelsToWorld = (orthoSize * 2f) / screenHeight;
                            
                            // Инвертируем движение для естественного перетаскивания
                            // Движение камеры равно движению касания (1:1)
                            Vector3 move = new Vector3(-touchDelta.x * pixelsToWorld,
                                                      -touchDelta.y * pixelsToWorld,
                                                      0f);
                            
                            targetCameraPosition += move;
                            
                            // Проверяем, была ли камера перемещена
                            if (Vector3.Distance(mainCamera.transform.position, initialCameraPositionTouch) > 0.01f)
                            {
                                wasCameraMovedTouch = true;
                            }
                        }
                        
                        lastTouchPosition = touch.position;
                    }
                    break;
                    
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    isTouchDragging = false;
                    break;
            }
        }
        
        private void HandlePinchZoom()
        {
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);
            
            // Вычисляем расстояние между двумя касаниями
            Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;
            
            float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;
            
            float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;
            
            if (Mathf.Abs(deltaMagnitudeDiff) > 0.01f)
            {
                // Вычисляем точку между двумя касаниями для зума к этой точке
                Vector2 pinchCenter = (touchZero.position + touchOne.position) * 0.5f;
                
                float currentTime = Time.time;
                float timeSinceLastPinch = currentTime - lastPinchInputTime;
                float pinchCenterDistance = Vector2.Distance(pinchCenter, lastPinchCenter);
                
                // Проверяем, завершен ли предыдущий зум
                float sizeDifference = Mathf.Abs(mainCamera.orthographicSize - targetOrthoSize);
                
                // Сбрасываем фиксацию и начинаем новый зум если:
                // 1. Предыдущий зум завершен (размер достиг цели)
                // 2. ИЛИ прошло достаточно времени с последнего pinch (новый цикл зума)
                // 3. ИЛИ центр pinch значительно сместился (новый pinch-жест)
                bool zoomCompleted = Mathf.Approximately(sizeDifference, 0f);
                bool newPinchCycle = timeSinceLastPinch > 0.02f || pinchCenterDistance > 50f; // Новый цикл если прошло время или центр сместился
                
                if (!isZooming || zoomCompleted || newPinchCycle)
                {
                    // Фиксируем новую позицию при начале нового цикла зума
                    lastZoomScreenPosition = pinchCenter;
                    fixedZoomWorldPosition = mainCamera.ScreenToWorldPoint(lastZoomScreenPosition);
                    fixedZoomWorldPosition.z = mainCamera.transform.position.z;
                    isZooming = true;
                    lastPinchInputTime = currentTime;
                    lastPinchCenter = pinchCenter;
                }
                
                // Преобразуем разницу расстояния в изменение зума
                float orthoSize = mainCamera.orthographicSize;
                float pixelsToWorld = (orthoSize * 2f) / Screen.height;
                float zoomDelta = deltaMagnitudeDiff * pixelsToWorld * zoomSpeed * 0.01f;
                
                ZoomCameraAtPosition(zoomDelta);
            }
        }
        
        private void ZoomCameraAtPosition(float zoomDelta)
        {
            if (mainCamera == null) return;
            
            // Изменяем целевой orthographicSize
            float oldSize = targetOrthoSize;
            float newSize = oldSize + zoomDelta;
            newSize = Mathf.Clamp(newSize, minOrthoSize, maxOrthoSize);
            
            // Если размер не изменился (достигнут лимит), не делаем ничего
            if (Mathf.Approximately(oldSize, newSize))
                return;
            
            // Применяем компенсацию позиции сразу при изменении размера
            if (isZooming)
            {
                // Получаем мировую позицию точки под курсором с текущим размером
                float currentSize = mainCamera.orthographicSize;
                
                // Вычисляем мировую позицию с текущим размером
                Vector3 worldPosBeforeZoom = GetWorldPositionAtScreenPosition(lastZoomScreenPosition, currentSize);
                
                // Вычисляем мировую позицию с новым размером
                Vector3 worldPosAfterZoom = GetWorldPositionAtScreenPosition(lastZoomScreenPosition, newSize);
                
                // Вычисляем смещение для сохранения точки под курсором
                Vector3 offset = worldPosBeforeZoom - worldPosAfterZoom;
                
                // Применяем смещение к целевой позиции камеры
                targetCameraPosition += offset;
            }
            
            targetOrthoSize = newSize;
            // Применяем размер мгновенно
            mainCamera.orthographicSize = newSize;
        }
        
        private Vector3 GetWorldPositionAtScreenPosition(Vector2 screenPosition, float orthoSize)
        {
            if (mainCamera == null) return Vector3.zero;
            
            // Сохраняем текущий размер
            float currentSize = mainCamera.orthographicSize;
            
            // Временно устанавливаем нужный размер
            mainCamera.orthographicSize = orthoSize;
            
            // Вычисляем мировую позицию
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(screenPosition);
            worldPos.z = mainCamera.transform.position.z;
            
            // Возвращаем размер обратно
            mainCamera.orthographicSize = currentSize;
            
            return worldPos;
        }
        
        private void ApplySmoothMovement()
        {
            if (mainCamera == null) return;
            
            // Мгновенно применяем позицию камеры без сглаживания
            mainCamera.transform.position = targetCameraPosition;
        }
        
        private void ApplySmoothZoom()
        {
            if (mainCamera == null) return;
            
            // Мгновенно применяем orthographicSize без сглаживания
            mainCamera.orthographicSize = targetOrthoSize;
            
            // Сбрасываем флаг зума если размер достиг цели
            if (Mathf.Approximately(mainCamera.orthographicSize, targetOrthoSize))
            {
                isZooming = false;
            }
        }
        
        /// <summary>
        /// Проверяет, был ли это быстрый клик (без перетаскивания камеры)
        /// </summary>
        /// <returns>true если это был клик, false если было перетаскивание</returns>
        public bool WasClick()
        {
            if (!isDragging && Input.GetMouseButtonUp(0))
            {
                float clickDuration = Time.time - mouseDownTime;
                float mouseDistance = Vector3.Distance(Input.mousePosition, mouseDownPosition);
                
                // Клик считается, если:
                // 1. Время нажатия короткое
                // 2. Расстояние перемещения мыши маленькое
                // 3. Камера не была перемещена
                return clickDuration <= maxClickTime && 
                       mouseDistance <= maxClickDistance && 
                       !wasCameraMoved;
            }
            
            return false;
        }
        
        /// <summary>
        /// Проверяет, была ли камера перемещена во время последнего зажатия мыши/тача
        /// </summary>
        public bool WasCameraMoved()
        {
            // Проверяем для мыши или тача в зависимости от активного ввода
            if (Input.touchCount > 0)
            {
                return wasCameraMovedTouch;
            }
            else
            {
                return wasCameraMoved;
            }
        }
        
        /// <summary>
        /// Получить время нажатия мыши (для проверки клика)
        /// </summary>
        public float GetClickDuration()
        {
            if (isDragging || Input.GetMouseButton(0))
            {
                return Time.time - mouseDownTime;
            }
            return 0f;
        }
        
        /// <summary>
        /// Проверяет, зажата ли сейчас левая кнопка мыши
        /// </summary>
        public bool IsMouseButtonDown()
        {
            return Input.GetMouseButton(0);
        }
    }
}


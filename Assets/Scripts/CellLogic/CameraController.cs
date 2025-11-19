using UnityEngine;

namespace CellNameSpace
{
    public class CameraController : MonoBehaviour
    {
        [Header("Настройки перемещения")]
        [SerializeField] private float dragSpeed = 100f;
        [SerializeField] private float movementSmoothness = 10f; // Плавность перемещения камеры
        
        [Header("Настройки зума")]
        [SerializeField] private float zoomSpeed = 80f; // Скорость приближения/отдаления
        [SerializeField] private float zoomSmoothness = 10f; // Плавность зума
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
        
        // Переменные для плавного перемещения
        private Vector3 targetCameraPosition;
        private Vector3 cameraVelocity = Vector3.zero;
        private float targetOrthoSize;
        private float orthoSizeVelocity = 0f;
        
        
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
                initialCameraPosition = targetCameraPosition;
                wasCameraMoved = false;
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
                    Vector3 move = new Vector3(-mouseDelta.x * pixelsToWorld * dragSpeed * 0.01f, 
                                              -mouseDelta.y * pixelsToWorld * dragSpeed * 0.01f, 
                                              0f);
                    
                    targetCameraPosition += move;
                    
                    // Проверяем, была ли камера перемещена (минимальный порог для избежания дрожания)
                    if (Vector3.Distance(targetCameraPosition, initialCameraPosition) > 0.01f)
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
                ZoomCameraAtPosition(Input.mousePosition, -scroll * zoomSpeed);
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
                    initialCameraPositionTouch = targetCameraPosition;
                    wasCameraMovedTouch = false;
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
                            Vector3 move = new Vector3(-touchDelta.x * pixelsToWorld * dragSpeed * 0.01f,
                                                      -touchDelta.y * pixelsToWorld * dragSpeed * 0.01f,
                                                      0f);
                            
                            targetCameraPosition += move;
                            
                            // Проверяем, была ли камера перемещена
                            if (Vector3.Distance(targetCameraPosition, initialCameraPositionTouch) > 0.01f)
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
                
                // Преобразуем разницу расстояния в изменение зума
                float orthoSize = mainCamera.orthographicSize;
                float pixelsToWorld = (orthoSize * 2f) / Screen.height;
                float zoomDelta = deltaMagnitudeDiff * pixelsToWorld * zoomSpeed * 0.01f;
                
                ZoomCameraAtPosition(pinchCenter, zoomDelta);
            }
        }
        
        private void ZoomCameraAtPosition(Vector2 screenPosition, float zoomDelta)
        {
            if (mainCamera == null) return;
            
            // Получаем позицию в мировых координатах до зума
            Vector3 worldPosBeforeZoom = mainCamera.ScreenToWorldPoint(screenPosition);
            worldPosBeforeZoom.z = targetCameraPosition.z;
            
            // Изменяем целевой orthographicSize
            float oldSize = targetOrthoSize;
            float newSize = oldSize + zoomDelta;
            newSize = Mathf.Clamp(newSize, minOrthoSize, maxOrthoSize);
            
            // Если размер не изменился (достигнут лимит), не делаем ничего
            if (Mathf.Approximately(oldSize, newSize))
                return;
            
            targetOrthoSize = newSize;
            
            // Вычисляем смещение для сохранения точки под курсором/касанием
            // Используем текущий размер для вычисления смещения
            float pixelsToWorld = (targetOrthoSize * 2f) / Screen.height;
            float sizeDiff = newSize - oldSize;
            
            // Вычисляем смещение на основе изменения размера
            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Vector2 screenPosDelta = screenPosition - screenCenter;
            Vector3 offset = new Vector3(-screenPosDelta.x * sizeDiff / Screen.height,
                                        -screenPosDelta.y * sizeDiff / Screen.height,
                                        0f);
            
            targetCameraPosition += offset;
        }
        
        private void ApplySmoothMovement()
        {
            if (mainCamera == null) return;
            
            // Плавно перемещаем камеру к целевой позиции
            mainCamera.transform.position = Vector3.SmoothDamp(
                mainCamera.transform.position,
                targetCameraPosition,
                ref cameraVelocity,
                1f / movementSmoothness
            );
        }
        
        private void ApplySmoothZoom()
        {
            if (mainCamera == null) return;
            
            float previousSize = mainCamera.orthographicSize;
            
            // Плавно изменяем orthographicSize к целевому значению
            mainCamera.orthographicSize = Mathf.SmoothDamp(
                mainCamera.orthographicSize,
                targetOrthoSize,
                ref orthoSizeVelocity,
                1f / zoomSmoothness
            );
            
            // Корректируем позицию камеры для сохранения точки под курсором при плавном зуме
            if (!Mathf.Approximately(previousSize, mainCamera.orthographicSize))
            {
                float sizeDiff = mainCamera.orthographicSize - previousSize;
                
                // Вычисляем смещение для компенсации изменения размера
                // Используем центр экрана как точку отсчета (можно использовать позицию мыши, но это сложнее при плавном зуме)
                Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                Vector3 worldPosBeforeSizeChange = mainCamera.ScreenToWorldPoint(screenCenter);
                worldPosBeforeSizeChange.z = mainCamera.transform.position.z;
                
                // Временно применяем новое значение для вычисления смещения
                float tempSize = mainCamera.orthographicSize;
                
                // Вычисляем смещение на основе изменения размера
                float pixelsToWorld = (tempSize * 2f) / Screen.height;
                float sizeRatio = sizeDiff / tempSize;
                
                // Компенсируем изменение размера перемещением камеры
                Vector3 currentWorldPos = mainCamera.ScreenToWorldPoint(screenCenter);
                currentWorldPos.z = mainCamera.transform.position.z;
                
                Vector3 offset = worldPosBeforeSizeChange - currentWorldPos;
                targetCameraPosition += offset;
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


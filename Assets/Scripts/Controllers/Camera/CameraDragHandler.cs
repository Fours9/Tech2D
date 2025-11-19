using UnityEngine;

namespace CellNameSpace
{
    /// <summary>
    /// Обработчик перетаскивания камеры (мышь и тач)
    /// </summary>
    public class CameraDragHandler
    {
        private Camera mainCamera;
        
        // Переменные для мыши
        private bool isDragging = false;
        private Vector3 lastMousePosition;
        private Vector3 initialCameraPosition;
        private bool wasCameraMoved = false;
        
        // Переменные для тач-управления
        private bool isTouchDragging = false;
        private Vector2 lastTouchPosition;
        private Vector3 initialCameraPositionTouch;
        private bool wasCameraMovedTouch = false;
        
        public bool IsDragging => isDragging || isTouchDragging;
        public bool WasCameraMoved => Input.touchCount > 0 ? wasCameraMovedTouch : wasCameraMoved;
        
        public CameraDragHandler(Camera camera)
        {
            mainCamera = camera;
        }
        
        public Vector3 HandleMouseDrag(Vector3 currentTargetPosition)
        {
            Vector3 targetPosition = currentTargetPosition;
            
            // Начало зажатия левой кнопки мыши
            if (Input.GetMouseButtonDown(0))
            {
                isDragging = true;
                lastMousePosition = Input.mousePosition;
                initialCameraPosition = mainCamera.transform.position;
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
                    float orthoSize = mainCamera.orthographicSize;
                    float screenHeight = Screen.height;
                    float pixelsToWorld = (orthoSize * 2f) / screenHeight;
                    
                    // Инвертируем движение мыши для естественного перетаскивания
                    Vector3 move = new Vector3(-mouseDelta.x * pixelsToWorld, 
                                              -mouseDelta.y * pixelsToWorld, 
                                              0f);
                    
                    targetPosition += move;
                    
                    // Проверяем, была ли камера перемещена
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
            
            return targetPosition;
        }
        
        public Vector3 HandleTouchDrag(Vector3 currentTargetPosition)
        {
            Vector3 targetPosition = currentTargetPosition;
            
            if (Input.touchCount != 1) return targetPosition;
            
            Touch touch = Input.GetTouch(0);
            
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    isTouchDragging = true;
                    lastTouchPosition = touch.position;
                    initialCameraPositionTouch = mainCamera.transform.position;
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
                            Vector3 move = new Vector3(-touchDelta.x * pixelsToWorld,
                                                      -touchDelta.y * pixelsToWorld,
                                                      0f);
                            
                            targetPosition += move;
                            
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
            
            return targetPosition;
        }
    }
}


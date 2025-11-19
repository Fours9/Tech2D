using UnityEngine;

namespace CellNameSpace
{
    /// <summary>
    /// Обработчик зума камеры (колесико мыши и pinch-жест)
    /// </summary>
    public class CameraZoomHandler
    {
        private Camera mainCamera;
        private float zoomSpeed;
        private float minOrthoSize;
        private float maxOrthoSize;
        
        private Vector2 lastZoomScreenPosition;
        private Vector3 fixedZoomWorldPosition;
        private bool isZooming = false;
        private float lastZoomInputTime = 0f;
        private float lastPinchInputTime = 0f;
        private Vector2 lastPinchCenter = Vector2.zero;
        
        public bool IsZooming => isZooming;
        
        public CameraZoomHandler(Camera camera, float speed, float minSize, float maxSize)
        {
            mainCamera = camera;
            zoomSpeed = speed;
            minOrthoSize = minSize;
            maxOrthoSize = maxSize;
            
            lastZoomScreenPosition = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            fixedZoomWorldPosition = Vector3.zero;
            lastZoomInputTime = Time.time;
            lastPinchInputTime = Time.time;
        }
        
        public void UpdateSettings(float speed, float minSize, float maxSize)
        {
            zoomSpeed = speed;
            minOrthoSize = minSize;
            maxOrthoSize = maxSize;
        }
        
        public (Vector3 newPosition, float newSize) HandleMouseZoom(Vector3 currentTargetPosition, float currentTargetSize, bool isDragging)
        {
            if (mainCamera == null) return (currentTargetPosition, currentTargetSize);
            
            Vector3 targetPosition = currentTargetPosition;
            float targetSize = currentTargetSize;
            
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            
            if (Mathf.Abs(scroll) > 0.01f)
            {
                float currentTime = Time.time;
                float timeSinceLastZoom = currentTime - lastZoomInputTime;
                lastZoomInputTime = currentTime;
                
                // Проверяем, завершен ли предыдущий зум
                float sizeDifference = Mathf.Abs(mainCamera.orthographicSize - targetSize);
                
                bool zoomCompleted = Mathf.Approximately(sizeDifference, 0f);
                bool newZoomCycle = timeSinceLastZoom > 0.02f;
                
                // Всегда обновляем позицию курсора при зуме (даже во время перетаскивания)
                if (!isZooming || zoomCompleted || newZoomCycle || isDragging)
                {
                    // Фиксируем новую позицию при начале нового цикла зума
                    lastZoomScreenPosition = Input.mousePosition;
                    fixedZoomWorldPosition = mainCamera.ScreenToWorldPoint(lastZoomScreenPosition);
                    fixedZoomWorldPosition.z = mainCamera.transform.position.z;
                    isZooming = true;
                }
                
                (targetPosition, targetSize) = ZoomCameraAtPosition(-scroll * zoomSpeed, targetPosition, targetSize);
            }
            
            return (targetPosition, targetSize);
        }
        
        public (Vector3 newPosition, float newSize) HandlePinchZoom(Vector3 currentTargetPosition, float currentTargetSize)
        {
            if (Input.touchCount != 2) return (currentTargetPosition, currentTargetSize);
            
            Vector3 targetPosition = currentTargetPosition;
            float targetSize = currentTargetSize;
            
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
                float sizeDifference = Mathf.Abs(mainCamera.orthographicSize - targetSize);
                
                bool zoomCompleted = Mathf.Approximately(sizeDifference, 0f);
                bool newPinchCycle = timeSinceLastPinch > 0.02f || pinchCenterDistance > 50f;
                
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
                
                (targetPosition, targetSize) = ZoomCameraAtPosition(zoomDelta, targetPosition, targetSize);
            }
            
            return (targetPosition, targetSize);
        }
        
        private (Vector3 newPosition, float newSize) ZoomCameraAtPosition(float zoomDelta, Vector3 currentTargetPosition, float currentTargetSize)
        {
            if (mainCamera == null) return (currentTargetPosition, currentTargetSize);
            
            Vector3 targetPosition = currentTargetPosition;
            float targetSize = currentTargetSize;
            
            // Изменяем целевой orthographicSize
            float oldSize = targetSize;
            float newSize = oldSize + zoomDelta;
            newSize = Mathf.Clamp(newSize, minOrthoSize, maxOrthoSize);
            
            // Если размер не изменился (достигнут лимит), не делаем ничего
            if (Mathf.Approximately(oldSize, newSize))
                return (targetPosition, targetSize);
            
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
                targetPosition += offset;
            }
            
            targetSize = newSize;
            // Применяем размер мгновенно
            mainCamera.orthographicSize = newSize;
            
            return (targetPosition, targetSize);
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
        
        public void ApplyZoom(float targetSize)
        {
            if (mainCamera == null) return;
            
            // Мгновенно применяем orthographicSize без сглаживания
            mainCamera.orthographicSize = targetSize;
            
            // Сбрасываем флаг зума если размер достиг цели
            if (Mathf.Approximately(mainCamera.orthographicSize, targetSize))
            {
                isZooming = false;
            }
        }
    }
}


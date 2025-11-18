using UnityEngine;

namespace CellNameSpace
{
    public class CameraController : MonoBehaviour
    {
        [Header("Настройки перемещения")]
        [SerializeField] private float dragSpeed = 100f;
        
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
            }
        }
        
        private void Update()
        {
            HandleCameraDrag();
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
                    
                    mainCamera.transform.position += move;
                    
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
        /// Проверяет, была ли камера перемещена во время последнего зажатия мыши
        /// </summary>
        public bool WasCameraMoved()
        {
            return wasCameraMoved;
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


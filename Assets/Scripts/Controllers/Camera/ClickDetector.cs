using UnityEngine;

namespace CellNameSpace
{
    /// <summary>
    /// Детектор кликов для определения быстрых кликов без перетаскивания камеры
    /// </summary>
    public class ClickDetector
    {
        private float maxClickTime;
        private float maxClickDistance;
        
        // Переменные для мыши
        private float mouseDownTime;
        private Vector3 mouseDownPosition;
        private bool isDragging;
        
        // Переменные для тача
        private float touchDownTime;
        private Vector2 touchDownPosition;
        
        public ClickDetector(float clickTime, float clickDistance)
        {
            maxClickTime = clickTime;
            maxClickDistance = clickDistance;
        }
        
        public void UpdateSettings(float clickTime, float clickDistance)
        {
            maxClickTime = clickTime;
            maxClickDistance = clickDistance;
        }
        
        public void OnMouseDown()
        {
            mouseDownTime = Time.time;
            mouseDownPosition = Input.mousePosition;
            isDragging = false;
        }
        
        public void OnTouchDown()
        {
            if (Input.touchCount > 0)
            {
                touchDownTime = Time.time;
                touchDownPosition = Input.GetTouch(0).position;
            }
        }
        
        public void SetDragging(bool dragging)
        {
            isDragging = dragging;
        }
        
        /// <summary>
        /// Проверяет, был ли это быстрый клик (без перетаскивания камеры)
        /// </summary>
        public bool WasClick(bool wasCameraMoved)
        {
            if (!isDragging && Input.GetMouseButtonUp(0))
            {
                float clickDuration = Time.time - mouseDownTime;
                float mouseDistance = Vector3.Distance(Input.mousePosition, mouseDownPosition);
                
                return clickDuration <= maxClickTime && 
                       mouseDistance <= maxClickDistance && 
                       !wasCameraMoved;
            }
            
            return false;
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


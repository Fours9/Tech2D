using UnityEngine;

namespace CellNameSpace
{
    public class CellClick : MonoBehaviour
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            
        }

        // Update is called once per frame
        void Update()
        {
            
        }

        private void OnMouseDown()
        {
            Debug.Log($"Клик по гексу: {gameObject.name} (позиция: {transform.position})");
        }
    }
}

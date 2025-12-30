using UnityEngine;
using System.Collections.Generic;
using CellNameSpace;

/// <summary>
/// Утилита для объединения мешей клеток в чанки
/// Использует transform.localToWorldMatrix из каждого GameObject'а для корректного позиционирования
/// И пересчитывает UV координаты для работы с world-space шейдером или текстурой чанка
/// </summary>
public static class CellMeshCombiner
{
    /// <summary>
    /// Результат объединения мешей с текстурой
    /// </summary>
    public struct CombineResult
    {
        public Mesh mesh;
        public Texture2D chunkTexture;
        public Material baseMaterial; // Базовый материал для применения текстуры
        public Bounds chunkBounds; // Bounds чанка для UV координат
    }
    /// <summary>
    /// Объединяет меши клеток в один меш для чанка
    /// Использует transform.localToWorldMatrix из каждого GameObject'а для корректного позиционирования
    /// </summary>
    /// <param name="cells">Список GameObject клеток для объединения</param>
    /// <returns>Объединенный меш или null, если не удалось создать</returns>
    public static Mesh CombineCellMeshes(List<GameObject> cells)
    {
        if (cells == null || cells.Count == 0)
            return null;
        
        List<CombineInstance> combineInstances = new List<CombineInstance>();
        
        foreach (GameObject cell in cells)
        {
            if (cell == null)
                continue;
            
            MeshFilter meshFilter = cell.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                // Получаем оригинальную позицию клетки для правильного расчета UV
                CellInfo cellInfo = cell.GetComponent<CellInfo>();
                Vector3 originalPosition = cellInfo != null ? cellInfo.GetOriginalPosition() : cell.transform.position;
                
                // Создаем копию меша для модификации UV координат
                Mesh meshCopy = Object.Instantiate(meshFilter.sharedMesh);
                
                // Получаем вершины и UV координаты
                Vector3[] vertices = meshCopy.vertices;
                Vector2[] uvs = new Vector2[vertices.Length];
                
                // Пересчитываем UV координаты на основе мировых позиций вершин
                // Шейдер использует: uv = (_OriginalPosition.xy + worldOffset.xy) * _TextureScale
                // Где worldOffset - это смещение от центра объекта до вершины в мировых координатах
                // Для чанка мы устанавливаем _OriginalPosition = (0,0,0), поэтому UV должны быть равны worldOffset * _TextureScale
                // Но worldOffset вычисляется относительно центра чанка, поэтому нам нужно использовать мировые координаты вершин
                float textureScale = 1.0f;
                MeshRenderer cellRenderer = cell.GetComponent<MeshRenderer>();
                if (cellRenderer != null && cellRenderer.sharedMaterial != null)
                {
                    textureScale = cellRenderer.sharedMaterial.GetFloat("_TextureScale");
                    if (textureScale == 0) textureScale = 1.0f;
                }
                
                // Получаем мировую позицию центра клетки
                Vector3 cellWorldPos = cell.transform.position;
                
                for (int i = 0; i < vertices.Length; i++)
                {
                    // Преобразуем локальную вершину в мировую позицию
                    Vector3 worldVertex = cell.transform.TransformPoint(vertices[i]);
                    
                    // Вычисляем worldOffset как смещение от центра чанка (который будет в (0,0,0))
                    // Но так как мы используем localToWorldMatrix, вершины будут в правильных позициях
                    // А worldOffset будет вычисляться относительно центра чанка
                    // Поэтому UV должны быть равны мировым координатам вершин * textureScale
                    uvs[i] = new Vector2(worldVertex.x, worldVertex.y) * textureScale;
                }
                
                meshCopy.uv = uvs;
                
                CombineInstance combine = new CombineInstance();
                combine.mesh = meshCopy;
                combine.subMeshIndex = 0;
                // Используем localToWorldMatrix для преобразования вершин в мировое пространство
                combine.transform = cell.transform.localToWorldMatrix;
                combineInstances.Add(combine);
            }
        }
        
        if (combineInstances.Count == 0)
            return null;
        
        // Объединяем меши
        Mesh combinedMesh = new Mesh();
        combinedMesh.CombineMeshes(combineInstances.ToArray());
        
        // Очищаем временные копии мешей
        foreach (CombineInstance combine in combineInstances)
        {
            if (combine.mesh != null && Application.isPlaying)
            {
                Object.Destroy(combine.mesh);
            }
            else if (combine.mesh != null)
            {
                Object.DestroyImmediate(combine.mesh);
            }
        }
        
        return combinedMesh;
    }
    
    /// <summary>
    /// Объединяет меши клеток и создает текстуру чанка
    /// </summary>
    /// <param name="cells">Список GameObject клеток для объединения</param>
    /// <param name="textureResolution">Разрешение текстуры чанка</param>
    /// <returns>Результат объединения с мешем и текстурой</returns>
    public static CombineResult CombineCellMeshesWithTexture(List<GameObject> cells, int textureResolution)
    {
        CombineResult result = new CombineResult();
        
        if (cells == null || cells.Count == 0)
            return result;
        
        // Вычисляем bounds чанка
        result.chunkBounds = ChunkTextureBaker.CalculateChunkBounds(cells);
        
        if (result.chunkBounds.size.magnitude < 0.001f)
        {
            Debug.LogWarning("CellMeshCombiner: Некорректные bounds чанка");
            return result;
        }
        
        // Создаем текстуру чанка
        result.chunkTexture = ChunkTextureBaker.BakeChunkTexture(cells, result.chunkBounds, textureResolution);
        
        if (result.chunkTexture == null)
        {
            Debug.LogWarning("CellMeshCombiner: Не удалось создать текстуру чанка");
            return result;
        }
        
        // Объединяем меши клеток с новыми UV координатами для текстуры чанка
        List<CombineInstance> combineInstances = new List<CombineInstance>();
        
        foreach (GameObject cell in cells)
        {
            if (cell == null)
                continue;
            
            MeshFilter meshFilter = cell.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                // Создаем копию меша для модификации UV координат
                Mesh meshCopy = Object.Instantiate(meshFilter.sharedMesh);
                
                // Получаем вершины и пересчитываем UV координаты для текстуры чанка
                Vector3[] vertices = meshCopy.vertices;
                Vector2[] uvs = new Vector2[vertices.Length];
                
                // Пересчитываем UV координаты относительно текстуры чанка
                // UV = (worldPos.xy - chunkBounds.min.xy) / chunkBounds.size.xy
                for (int i = 0; i < vertices.Length; i++)
                {
                    // Преобразуем локальную вершину в мировую позицию
                    Vector3 worldVertex = cell.transform.TransformPoint(vertices[i]);
                    
                    // Вычисляем UV относительно bounds чанка
                    Vector3 relativePos = worldVertex - result.chunkBounds.min;
                    uvs[i] = new Vector2(
                        relativePos.x / result.chunkBounds.size.x,
                        relativePos.y / result.chunkBounds.size.y
                    );
                }
                
                meshCopy.uv = uvs;
                
                CombineInstance combine = new CombineInstance();
                combine.mesh = meshCopy;
                combine.subMeshIndex = 0;
                combine.transform = cell.transform.localToWorldMatrix;
                combineInstances.Add(combine);
            }
        }
        
        if (combineInstances.Count == 0)
        {
            Debug.LogWarning("CellMeshCombiner: Не удалось создать combine instances");
            return result;
        }
        
        // Объединяем меши
        result.mesh = new Mesh();
        result.mesh.CombineMeshes(combineInstances.ToArray());
        
        // Очищаем временные копии мешей
        foreach (CombineInstance combine in combineInstances)
        {
            if (combine.mesh != null && Application.isPlaying)
            {
                Object.Destroy(combine.mesh);
            }
            else if (combine.mesh != null)
            {
                Object.DestroyImmediate(combine.mesh);
            }
        }
        
        // Получаем базовый материал из первой клетки
        if (cells.Count > 0)
        {
            MeshRenderer firstCellRenderer = cells[0].GetComponent<MeshRenderer>();
            if (firstCellRenderer != null && firstCellRenderer.sharedMaterial != null)
            {
                result.baseMaterial = firstCellRenderer.sharedMaterial;
            }
        }
        
        return result;
    }
}


using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;

/// <summary>
/// Утилита для запекания текстур чанков из клеток через CommandBuffer (SRP/URP-friendly)
/// </summary>
public static class ChunkTextureBaker
{
    // Настройки
    private static float pixelsPerCell = 64f;
    
    /// <summary>
    /// Вычисляет разрешение текстуры один раз для всех чанков (они одинакового размера)
    /// </summary>
    public static int CalculateChunkTextureResolution(int estimatedCellsPerChunk)
    {
        int calculatedResolution = Mathf.RoundToInt(Mathf.Sqrt(estimatedCellsPerChunk) * pixelsPerCell);
        
        // Округляем до степени двойки
        calculatedResolution = Mathf.Clamp(
            Mathf.NextPowerOfTwo(calculatedResolution),
            256,
            Mathf.Min(4096, SystemInfo.maxTextureSize)
        );
        
        return calculatedResolution;
    }
    
    /// <summary>
    /// Запекает текстуру чанка из всех клеток через CommandBuffer (SRP/URP-safe)
    /// </summary>
    public static Texture2D BakeChunkTexture(
        List<GameObject> cells, 
        Bounds chunkBounds,
        int textureResolution)
    {
        if (cells == null || cells.Count == 0)
        {
            Debug.LogWarning("ChunkTextureBaker: Пустой список клеток для запекания");
            return null;
        }

        if (chunkBounds.size.magnitude < 0.001f)
        {
            Debug.LogWarning("ChunkTextureBaker: Некорректные bounds чанка");
            return null;
        }

        // Собираем информацию о мешах и материалах
        List<MeshRenderData> renderData = new List<MeshRenderData>();
        foreach (GameObject cell in cells)
        {
            if (cell == null) continue;

            MeshFilter mf = cell.GetComponent<MeshFilter>();
            MeshRenderer mr = cell.GetComponent<MeshRenderer>();
            if (mf == null || mf.sharedMesh == null || mr == null || mr.sharedMaterial == null) continue;

            MaterialPropertyBlock mpb = null;
            if (mr.HasPropertyBlock())
            {
                mpb = new MaterialPropertyBlock();
                mr.GetPropertyBlock(mpb);
            }

            renderData.Add(new MeshRenderData
            {
                mesh = mf.sharedMesh,
                material = mr.sharedMaterial,
                matrix = cell.transform.localToWorldMatrix,
                propertyBlock = mpb
            });
        }

        Debug.Log($"ChunkTextureBaker: Starting bake, tempRT = {textureResolution}x{textureResolution}, cells count = {cells.Count}, renderData count = {renderData.Count}, chunkBounds = {chunkBounds}");

        if (renderData.Count == 0)
        {
            Debug.LogWarning("ChunkTextureBaker: Нет валидных MeshRenderer/MeshFilter для запекания");
            return null;
        }

        RenderTexture tempRT = RenderTexture.GetTemporary(
            textureResolution,
            textureResolution,
            24,
            RenderTextureFormat.ARGB32
        );
        tempRT.filterMode = FilterMode.Bilinear;
        tempRT.wrapMode = TextureWrapMode.Clamp;

        // Настройка временной камеры ТОЛЬКО для матриц (мы не рендерим через Render())
        Vector3 center = chunkBounds.center;
        float zMin = chunkBounds.min.z;
        float zMax = chunkBounds.max.z;

        float camZ = zMin - 10f;

        GameObject tempCameraGO = new GameObject("TempBakeCamera_MatricesOnly");
        Camera cam = tempCameraGO.AddComponent<Camera>();
        cam.enabled = false;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.clear;
        cam.orthographic = true;
        cam.orthographicSize = Mathf.Max(chunkBounds.size.x, chunkBounds.size.y) * 0.5f;
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = (zMax - camZ) + 5f;

        tempCameraGO.transform.position = new Vector3(center.x, center.y, camZ);
        tempCameraGO.transform.rotation = Quaternion.identity; // смотрим вдоль +Z

        Debug.Log($"ChunkTextureBaker: Camera matrices - zMin={zMin}, zMax={zMax}, camZ={camZ}, near={cam.nearClipPlane}, far={cam.farClipPlane}, orthoSize={cam.orthographicSize}");

        // Командный буфер: SRP/URP-safe рендер в RT
        CommandBuffer cb = new CommandBuffer { name = "ChunkTextureBaker_Bake" };

        cb.SetRenderTarget(tempRT);
        cb.ClearRenderTarget(true, true, Color.clear);

        // ВАЖНО: задаём матрицы вида/проекции, иначе DrawMesh рисует "непонятно куда"
        Matrix4x4 view = cam.worldToCameraMatrix;
        Matrix4x4 proj = cam.projectionMatrix;
        cb.SetViewProjectionMatrices(view, proj);

        // Рисуем все меши
        for (int i = 0; i < renderData.Count; i++)
        {
            var d = renderData[i];
            if (d.mesh == null || d.material == null) continue;

            cb.DrawMesh(
                d.mesh,
                d.matrix,
                d.material,
                0,
                -1,
                d.propertyBlock
            );
        }

        // Возвращаем матрицы обратно, чтобы не ломать другие рендеры
        cb.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);

        Graphics.ExecuteCommandBuffer(cb);
        cb.Release();

        Object.DestroyImmediate(tempCameraGO);

        // Читаем пиксели
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = tempRT;

        Texture2D chunkTexture = new Texture2D(textureResolution, textureResolution, TextureFormat.RGBA32, false);
        chunkTexture.ReadPixels(new Rect(0, 0, textureResolution, textureResolution), 0, 0);
        chunkTexture.Apply(false, false);

        RenderTexture.active = prev;

        // Быстрая проверка "не пустая ли"
        Color[] samplePixels = chunkTexture.GetPixels(0, 0, Mathf.Min(10, textureResolution), Mathf.Min(10, textureResolution));
        bool hasNonTransparent = false;
        for (int i = 0; i < samplePixels.Length; i++)
        {
            if (samplePixels[i].a > 0.01f)
            {
                hasNonTransparent = true;
                break;
            }
        }

        Debug.Log($"ChunkTextureBaker: Texture read complete, hasNonTransparentPixels = {hasNonTransparent}, sample pixels count = {samplePixels.Length}");
        if (!hasNonTransparent)
            Debug.LogWarning("ChunkTextureBaker: WARNING - Texture appears to be empty/transparent after reading from RenderTexture!");

        RenderTexture.ReleaseTemporary(tempRT);
        return chunkTexture;
    }
    
    /// <summary>
    /// Структура данных для рендеринга меша
    /// </summary>
    private struct MeshRenderData
    {
        public Mesh mesh;
        public Material material;
        public Matrix4x4 matrix;
        public MaterialPropertyBlock propertyBlock;
    }
    
    /// <summary>
    /// Вычисляет bounds чанка на основе всех клеток
    /// </summary>
    public static Bounds CalculateChunkBounds(List<GameObject> cells)
    {
        if (cells == null || cells.Count == 0) return new Bounds();

        Bounds bounds = new Bounds();
        bool inited = false;

        foreach (GameObject cell in cells)
        {
            if (cell == null) continue;

            MeshFilter mf = cell.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;

            Bounds local = mf.sharedMesh.bounds;
            Vector3 worldCenter = cell.transform.TransformPoint(local.center);
            Vector3 worldSize = Vector3.Scale(local.size, cell.transform.lossyScale);
            Bounds world = new Bounds(worldCenter, worldSize);

            if (!inited) { bounds = world; inited = true; }
            else bounds.Encapsulate(world);
        }

        return bounds;
    }
}


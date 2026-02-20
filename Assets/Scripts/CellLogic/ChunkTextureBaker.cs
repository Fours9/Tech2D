using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;

/// <summary>
/// Утилита для запекания текстур чанков из клеток через временную камеру (SRP-safe)
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
    /// Запекает текстуру чанка из всех клеток через временную камеру и временные GameObjects
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

        // ВАЖНО: камера рисует КВАДРАТ max(x,y) — это должно совпадать с UV
        float squareSize = Mathf.Max(chunkBounds.size.x, chunkBounds.size.y);

        RenderTexture tempRT = RenderTexture.GetTemporary(
            textureResolution,
            textureResolution,
            24,
            RenderTextureFormat.ARGB32
        );

        // Собираем инфу для рендера
        List<MeshRenderData> renderData = new List<MeshRenderData>();
        foreach (GameObject cell in cells)
        {
            if (cell == null) continue;

            MeshFilter mf = cell.GetComponent<MeshFilter>();
            MeshRenderer mr = cell.GetComponent<MeshRenderer>();

            if (mf != null && mf.sharedMesh != null && mr != null && mr.sharedMaterial != null)
            {
                MaterialPropertyBlock pb = null;
            if (mr.HasPropertyBlock())
            {
                    pb = new MaterialPropertyBlock();
                    mr.GetPropertyBlock(pb);
            }

            renderData.Add(new MeshRenderData
            {
                mesh = mf.sharedMesh,
                material = mr.sharedMaterial,
                matrix = cell.transform.localToWorldMatrix,
                    propertyBlock = pb
            });
            }
        }

        Debug.Log($"ChunkTextureBaker: Starting bake, tempRT={textureResolution}x{textureResolution}, cells={cells.Count}, renderData={renderData.Count}, chunkBounds={chunkBounds}, squareSize={squareSize}");

        // Камера
        Vector3 center = chunkBounds.center;
        float cameraDistance = Mathf.Max(chunkBounds.size.z * 2f, 10f);

        GameObject tempCameraGO = new GameObject("TempBakeCamera");
        tempCameraGO.hideFlags = HideFlags.HideAndDontSave;

        Camera cam = tempCameraGO.AddComponent<Camera>();
        cam.enabled = false;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.clear;
        cam.orthographic = true;

        // ВАЖНО: орто-размер = squareSize / 2
        cam.orthographicSize = squareSize * 0.5f;

        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = cameraDistance * 2f + chunkBounds.size.z;
        cam.targetTexture = tempRT;
        cam.cullingMask = -1;
        cam.allowMSAA = false;
        cam.allowHDR = false;

        tempCameraGO.transform.position = new Vector3(center.x, center.y, center.z - cameraDistance);
        tempCameraGO.transform.rotation = Quaternion.identity;

        // Временные объекты в сцене (SRP)
        GameObject tempParent = new GameObject("TempRenderParent");
        tempParent.hideFlags = HideFlags.HideAndDontSave;
        tempParent.SetActive(true);

        List<GameObject> tempRenderObjects = new List<GameObject>();
        foreach (var data in renderData)
        {
            GameObject tempObj = new GameObject("TempRenderCell");
            tempObj.transform.SetParent(tempParent.transform, false);

            Vector3 pos = data.matrix.GetColumn(3);
            Quaternion rot = data.matrix.rotation;
            Vector3 scl = data.matrix.lossyScale;

            tempObj.transform.position = pos;
            tempObj.transform.rotation = rot;
            tempObj.transform.localScale = scl;

            MeshFilter tmf = tempObj.AddComponent<MeshFilter>();
            tmf.sharedMesh = data.mesh;

            MeshRenderer tmr = tempObj.AddComponent<MeshRenderer>();
            tmr.sharedMaterial = data.material;
            if (data.propertyBlock != null)
                tmr.SetPropertyBlock(data.propertyBlock);

            tempRenderObjects.Add(tempObj);
        }

        // --- РЕНДЕР В RT БЕЗ Camera.Render() (URP-safe) ---
        var cb = new UnityEngine.Rendering.CommandBuffer();
        cb.name = "ChunkBake";

        cb.SetRenderTarget(tempRT);
        cb.ClearRenderTarget(true, true, new Color(0,0,0,1));

        // Матрицы камеры
        cb.SetViewProjectionMatrices(cam.worldToCameraMatrix, cam.projectionMatrix);

        // Рисуем все меши
        for (int i = 0; i < renderData.Count; i++)
        {
            var d = renderData[i];
            if (d.mesh == null || d.material == null) continue;

            cb.DrawMesh(d.mesh, d.matrix, d.material, 0, -1, d.propertyBlock);
        }

        // Важно: вернуть матрицы, чтобы не ломать остальной рендер
        cb.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);

        Graphics.ExecuteCommandBuffer(cb);
        cb.Release();

        // Читаем пиксели
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = tempRT;

        Texture2D tex = new Texture2D(textureResolution, textureResolution, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, textureResolution, textureResolution), 0, 0);
        tex.Apply(false, false);

        // Защищаем текстуру от автоматической выгрузки через Resources.UnloadUnusedAssets()
        tex.hideFlags = HideFlags.DontUnloadUnusedAsset;

        RenderTexture.active = prev;

        // ВАЖНО: чтобы при UV чуть <0 или >1 не было повторов/полос
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Point;

        // Тест пикселя для проверки альфа-канала
        var c = tex.GetPixel(textureResolution / 2, textureResolution / 2);
        Debug.Log($"BAKE SAMPLE PIXEL: {c}"); // особенно смотри на alpha

        // Cleanup
        foreach (var o in tempRenderObjects)
        {
            if (o != null)
            {
                if (Application.isPlaying) Object.Destroy(o);
                else Object.DestroyImmediate(o);
            }
        }

        if (Application.isPlaying) Object.Destroy(tempParent);
        else Object.DestroyImmediate(tempParent);
        
        if (Application.isPlaying) Object.Destroy(tempCameraGO);
        else Object.DestroyImmediate(tempCameraGO);

        RenderTexture.ReleaseTemporary(tempRT);

        return tex;
    }
    
    private struct MeshRenderData
    {
        public Mesh mesh;
        public Material material;
        public Matrix4x4 matrix;
        public MaterialPropertyBlock propertyBlock;
    }
    
    /// <summary>
    /// Bounds чанка — лучше считать по Renderer.bounds (он уже в world и учитывает всё)
    /// </summary>
    public static Bounds CalculateChunkBounds(List<GameObject> cells)
    {
        if (cells == null || cells.Count == 0)
            return new Bounds();

        bool init = false;
        Bounds b = new Bounds();

        foreach (var cell in cells)
        {
            if (cell == null) continue;

            var mr = cell.GetComponent<MeshRenderer>();
            if (mr == null) continue;

            if (!init)
            {
                b = mr.bounds;
                init = true;
            }
            else
            {
                b.Encapsulate(mr.bounds);
            }
        }

        return b;
    }
}

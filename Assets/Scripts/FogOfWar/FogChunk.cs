using UnityEngine;
using System.Collections.Generic;
using CellNameSpace;

namespace FogOfWar
{
/// <summary>
/// Компонент чанка тумана войны. Рендерит туман shared mesh из CellChunk.
/// Режимы: ChunkHidden, ChunkExplored (один меш+материал) или Individual (fogOfWarRenderer по клеткам).
/// </summary>
public class FogChunk : MonoBehaviour
{
    private List<CellInfo> cellInfos = new List<CellInfo>();
    private CellChunk cellChunk;
    private MeshFilter meshFilter;
    private MeshRenderer fogRenderer;
    private bool isIndividualRenderingEnabled = false;

    /// <summary>
    /// Инициализирует FogChunk — shared mesh из CellChunk, материалы из FogOfWarManager.
    /// </summary>
    public void Initialize(List<GameObject> cellsInChunk, CellChunk chunk)
    {
        cellChunk = chunk;
        cellInfos = new List<CellInfo>();

        foreach (GameObject cell in cellsInChunk)
        {
            CellInfo info = cell.GetComponent<CellInfo>();
            if (info != null)
                cellInfos.Add(info);
        }

        meshFilter = GetComponent<MeshFilter>();
        fogRenderer = GetComponent<MeshRenderer>();

        if (meshFilter != null && cellChunk != null)
        {
            MeshFilter chunkMeshFilter = cellChunk.GetChunkMeshFilter();
            if (chunkMeshFilter != null && chunkMeshFilter.sharedMesh != null)
                meshFilter.sharedMesh = chunkMeshFilter.sharedMesh;
        }
    }

    /// <summary>
    /// Включает индивидуальный рендеринг тумана (fogOfWarRenderer по клеткам), выключает меш чанка.
    /// </summary>
    public void EnableIndividualRendering()
    {
        if (isIndividualRenderingEnabled)
            return;

        isIndividualRenderingEnabled = true;
        if (fogRenderer != null)
            fogRenderer.enabled = false;

        foreach (CellInfo c in cellInfos)
        {
            if (c != null)
                c.SetFogRendererState(false);
        }
    }

    /// <summary>
    /// Включает рендеринг через чанк, выключает fogOfWarRenderer по клеткам.
    /// Вызывает RefreshMode для определения режима.
    /// </summary>
    public void EnableChunkRendering()
    {
        if (!isIndividualRenderingEnabled)
            return;

        isIndividualRenderingEnabled = false;
        RefreshMode();
    }

    /// <summary>
    /// Пересчитывает режим по fogState клеток и включает нужные рендеры.
    /// </summary>
    public void RefreshMode()
    {
        if (fogRenderer == null || cellInfos.Count == 0)
            return;

        bool hasVisible = false;
        bool hasBoundary = false;
        bool allHidden = true;
        bool allExplored = true;

        foreach (CellInfo c in cellInfos)
        {
            if (c == null) continue;
            FogOfWarState state = c.GetFogOfWarState();
            if (state == FogOfWarState.Visible) hasVisible = true;
            if (c.IsFogBoundaryCell()) hasBoundary = true;
            if (state != FogOfWarState.Hidden) allHidden = false;
            if (state != FogOfWarState.Explored) allExplored = false;
        }

        bool useIndividual = hasVisible || hasBoundary || (!allHidden && !allExplored);

        if (useIndividual)
        {
            fogRenderer.enabled = false;
            foreach (CellInfo c in cellInfos)
            {
                if (c != null)
                    c.SetFogRendererState(false);
            }
        }
        else
        {
            fogRenderer.enabled = true;
            Material mat = allHidden
                ? (FogOfWarManager.Instance != null ? FogOfWarManager.Instance.GetFogUnseenChunkMaterial() : null)
                : (FogOfWarManager.Instance != null ? FogOfWarManager.Instance.GetFogExploredChunkMaterial() : null);
            if (mat != null)
                fogRenderer.sharedMaterial = mat;

            foreach (CellInfo c in cellInfos)
            {
                if (c != null)
                    c.SetFogRendererState(true);
            }
        }
    }

    /// <summary>
    /// Обновляет shared mesh из CellChunk (при RebuildMesh).
    /// </summary>
    public void RefreshMesh()
    {
        if (meshFilter == null || cellChunk == null)
            return;

        MeshFilter chunkMeshFilter = cellChunk.GetChunkMeshFilter();
        if (chunkMeshFilter != null && chunkMeshFilter.sharedMesh != null)
            meshFilter.sharedMesh = chunkMeshFilter.sharedMesh;
    }
}
}

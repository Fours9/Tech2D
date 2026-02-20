using UnityEngine;
using System.Collections.Generic;
using CellNameSpace;

/// <summary>
/// Компонент чанка клеток. Управляет объединенным мешем и состоянием рендеринга клеток в чанке.
/// Добавляется к GameObject чанка через AddComponent<CellChunk>().
/// </summary>
public class CellChunk : MonoBehaviour
{
    private List<GameObject> cells = new List<GameObject>(); // Список GameObject клеток в этом чанке
    private List<CellInfo> cellInfos = new List<CellInfo>(); // Список CellInfo клеток в этом чанке (абстрактные клетки)
    private MeshRenderer chunkRenderer; // MeshRenderer чанка (объединенный меш)
    private MeshFilter chunkMeshFilter; // MeshFilter чанка (для пересборки меша)
    private bool isIndividualRenderingEnabled = false; // Состояние: включен ли индивидуальный рендеринг клеток
    private bool isDirty = false; // Флаг: нужно ли пересобрать меш чанка (при изменении типа клетки)
    private Texture2D chunkTexture; // Текстура чанка (статичная, создается один раз)
    
    /// <summary>
    /// Инициализирует чанк - добавляет клетки (как GameObject, так и CellInfo) и получает ссылку на MeshRenderer
    /// </summary>
    public void Initialize(List<GameObject> cellsInChunk, MeshRenderer chunkMeshRenderer, MeshFilter chunkMeshFilterComponent)
    {
        cells = new List<GameObject>(cellsInChunk);
        cellInfos = new List<CellInfo>();
        
        // Извлекаем CellInfo из каждого GameObject для удобного доступа и устанавливаем ссылку на чанк
        foreach (GameObject cell in cells)
        {
            CellInfo cellInfo = cell.GetComponent<CellInfo>();
            if (cellInfo != null)
            {
                cellInfos.Add(cellInfo);
                cellInfo.SetChunk(this); // Устанавливаем ссылку на этот чанк в клетке
            }
        }
        
        chunkRenderer = chunkMeshRenderer;
        chunkMeshFilter = chunkMeshFilterComponent;
        isIndividualRenderingEnabled = false; // По умолчанию рендерим через чанк
        isDirty = false; // Чанк только что создан, не требует пересборки
    }
    
    /// <summary>
    /// Получить список GameObject клеток в чанке
    /// </summary>
    public List<GameObject> GetCells()
    {
        return cells;
    }
    
    /// <summary>
    /// Получить список CellInfo клеток в чанке (абстрактные клетки)
    /// </summary>
    public List<CellInfo> GetCellInfos()
    {
        return cellInfos;
    }
    
    /// <summary>
    /// Включает индивидуальный рендеринг клеток, выключает рендеринг чанка
    /// Используется для CellHoverElevator, когда нужно изменить transform.position клеток
    /// </summary>
    public void EnableIndividualRendering()
    {
        if (isIndividualRenderingEnabled)
            return; // Уже включено, ничего не делаем
        
        isIndividualRenderingEnabled = true;
        
        // Выключаем рендеринг чанка
        if (chunkRenderer != null)
            chunkRenderer.enabled = false;
        
        // Включаем индивидуальный рендеринг каждой клетки (используем кешированный список CellInfo)
        foreach (CellInfo cellInfo in cellInfos)
        {
            if (cellInfo != null)
            {
                cellInfo.SetMainRendererState(true); // Включаем индивидуальный рендеринг
            }
        }
    }
    
    /// <summary>
    /// Выключает индивидуальный рендеринг клеток, включает рендеринг чанка
    /// Используется когда курсор отдаляется от чанка
    /// </summary>
    public void EnableChunkRendering()
    {
        if (!isIndividualRenderingEnabled)
            return; // Уже выключено, ничего не делаем
        
        isIndividualRenderingEnabled = false;
        
        // Выключаем индивидуальный рендеринг каждой клетки (используем кешированный список CellInfo)
        foreach (CellInfo cellInfo in cellInfos)
        {
            if (cellInfo != null)
            {
                cellInfo.SetMainRendererState(false); // Отключаем индивидуальный рендеринг (рендерим через чанк)
            }
        }
        
        // Включаем рендеринг чанка
        if (chunkRenderer != null)
            chunkRenderer.enabled = true;
    }
    
    /// <summary>
    /// Проверяет, включен ли индивидуальный рендеринг
    /// </summary>
    public bool IsIndividualRenderingEnabled()
    {
        return isIndividualRenderingEnabled;
    }
    
    /// <summary>
    /// Помечает чанк как "грязный" - требуется пересборка меша (при изменении типа клетки)
    /// </summary>
    public void MarkDirty()
    {
        isDirty = true;
    }
    
    /// <summary>
    /// Проверяет, требуется ли пересборка меша чанка
    /// </summary>
    public bool IsDirty()
    {
        return isDirty;
    }
    
        /// <summary>
        /// Пересобирает меш чанка (вызывается из Grid при необходимости)
        /// </summary>
        /// <param name="textureResolution">Разрешение текстуры для пересоздания (если нужно)</param>
        public void RebuildMesh(int? textureResolution = null)
        {
            if (!isDirty)
                return; // Не требуется пересборка
            
            if (chunkMeshFilter == null || chunkRenderer == null)
                return;
            
            // Удаляем старую текстуру перед пересозданием (только если клетки изменились)
            if (chunkTexture != null)
            {
                DestroyImmediate(chunkTexture);
                chunkTexture = null;
            }
            
            // Если указано разрешение текстуры, используем новый метод с текстурой
            if (textureResolution.HasValue)
            {
                // Пересоздаем меш и текстуру
                CellMeshCombiner.CombineResult result = CellMeshCombiner.CombineCellMeshesWithTexture(cells, textureResolution.Value);
                
                if (result.mesh != null && result.chunkTexture != null)
                {
                    chunkMeshFilter.sharedMesh = result.mesh;
                    chunkTexture = result.chunkTexture;
                    
                    // Защищаем текстуру от автоматической выгрузки через Resources.UnloadUnusedAssets()
                    chunkTexture.hideFlags = HideFlags.DontUnloadUnusedAsset;
                    
                    // Для чанка с baked-текстурой используем простой URP Unlit шейдер
                    // WorldSpaceTexture шейдер не подходит, так как он использует world-space UV
                    // и игнорирует UV 0..1, которые мы специально вычислили для baked-текстуры
                    Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
                    if (unlitShader == null)
                    {
                        // Fallback на стандартный Unlit, если URP Unlit не найден
                        unlitShader = Shader.Find("Unlit/Texture");
                    }
                    
                    if (unlitShader != null)
                    {
                        // Используем общий материал (создаем один раз, если еще не создан)
                        if (chunkRenderer.sharedMaterial == null || chunkRenderer.sharedMaterial.shader != unlitShader)
                        {
                            // Создаем материал только если его еще нет или шейдер не совпадает
                            Material sharedChunkMaterial = new Material(unlitShader);
                            chunkRenderer.sharedMaterial = sharedChunkMaterial;
                        }
                        
                        // Настраиваем текстуру для правильного отображения
                        chunkTexture.wrapMode = TextureWrapMode.Clamp;
                        chunkTexture.filterMode = FilterMode.Bilinear;
                        
                        // Используем MaterialPropertyBlock для установки текстуры без создания нового материала
                        MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                        // URP Unlit ожидает _BaseMap
                        propertyBlock.SetTexture("_BaseMap", chunkTexture);
                        // На всякий случай устанавливаем и _MainTex (для совместимости)
                        propertyBlock.SetTexture("_MainTex", chunkTexture);
                        chunkRenderer.SetPropertyBlock(propertyBlock);
                    }
                    else
                    {
                        Debug.LogError("CellChunk: Не удалось найти URP Unlit или Unlit/Texture шейдер для baked-текстуры чанка!");
                    }
                    
                    isDirty = false; // Меш пересобран, сбрасываем флаг
                }
                else
                {
                    Debug.LogWarning($"Не удалось пересобрать меш и текстуру для чанка {gameObject.name}");
                }
            }
            else
            {
                // Старый метод без текстуры (для обратной совместимости)
                Mesh combinedMesh = CellMeshCombiner.CombineCellMeshes(cells);
                
                if (combinedMesh != null)
                {
                    chunkMeshFilter.sharedMesh = combinedMesh;
                    
                    // Обновляем материал и MaterialPropertyBlock из первой клетки (на случай если они изменились)
                    if (cells.Count > 0)
                    {
                        MeshRenderer firstCellRenderer = cells[0].GetComponent<MeshRenderer>();
                        if (firstCellRenderer != null && firstCellRenderer.sharedMaterial != null)
                        {
                            chunkRenderer.sharedMaterial = firstCellRenderer.sharedMaterial;
                            
                            // Применяем MaterialPropertyBlock из первой клетки к чанку
                            // Это необходимо, так как шейдер требует _OriginalPosition и другие параметры
                            MaterialPropertyBlock chunkPropertyBlock = new MaterialPropertyBlock();
                            firstCellRenderer.GetPropertyBlock(chunkPropertyBlock);
                            chunkRenderer.SetPropertyBlock(chunkPropertyBlock);
                        }
                    }
                    
                    isDirty = false; // Меш пересобран, сбрасываем флаг
                }
                else
                {
                    Debug.LogWarning($"Не удалось пересобрать меш для чанка {gameObject.name}");
                }
            }
        }
        
        /// <summary>
        /// Удаляет текстуру чанка при уничтожении объекта
        /// </summary>
        private void OnDestroy()
        {
            // Удаляем текстуру при уничтожении чанка
            if (chunkTexture != null)
            {
                DestroyImmediate(chunkTexture);
                chunkTexture = null;
            }
        }
}


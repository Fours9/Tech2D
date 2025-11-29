using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor скрипт для автоматической конвертации 3D модели клетки в спрайт
/// </summary>
public class ModelToSpriteConverter : EditorWindow
{
    private GameObject modelPrefab;
    private int textureSize = 512;
    private string outputPath = "Assets/Sprites/HexagonCell_Sprite";
    
    [MenuItem("Tools/Convert Model to Sprite")]
    public static void ShowWindow()
    {
        GetWindow<ModelToSpriteConverter>("Model to Sprite Converter");
    }
    
    void OnGUI()
    {
        GUILayout.Label("Model to Sprite Converter", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        modelPrefab = (GameObject)EditorGUILayout.ObjectField(
            "Model Prefab", 
            modelPrefab, 
            typeof(GameObject), 
            false);
        
        textureSize = EditorGUILayout.IntField("Texture Size", textureSize);
        outputPath = EditorGUILayout.TextField("Output Path", outputPath);
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Convert to Sprite"))
        {
            if (modelPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a model prefab!", "OK");
                return;
            }
            
            ConvertModelToSprite();
        }
    }
    
    private void ConvertModelToSprite()
    {
        // Создаем папку Sprites, если её нет
        if (!Directory.Exists("Assets/Sprites"))
        {
            Directory.CreateDirectory("Assets/Sprites");
            AssetDatabase.Refresh();
        }
        
        // Создаем RenderTexture с depth buffer
        RenderTexture renderTexture = new RenderTexture(textureSize, textureSize, 24, RenderTextureFormat.ARGB32);
        renderTexture.Create();
        
        // Создаем временную модель в сцене
        GameObject tempModel = PrefabUtility.InstantiatePrefab(modelPrefab) as GameObject;
        tempModel.transform.position = Vector3.zero;
        tempModel.transform.rotation = Quaternion.Euler(0, 180, 0); // Как в Grid.cs
        tempModel.transform.localScale = Vector3.one;
        
        // Применяем белый материал к модели ПЕРЕД вычислением bounds
        Material whiteMaterial = new Material(Shader.Find("Unlit/Color"));
        whiteMaterial.color = Color.white;
        
        // Находим все MeshRenderer в модели и применяем белый материал
        MeshRenderer[] renderers = tempModel.GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.material = whiteMaterial;
                renderer.enabled = true;
                renderer.gameObject.SetActive(true);
            }
        }
        
        // Также находим все MeshFilter для получения информации о мешах
        MeshFilter[] allMeshFilters = tempModel.GetComponentsInChildren<MeshFilter>();
        Debug.Log($"Found {renderers.Length} MeshRenderers and {allMeshFilters.Length} MeshFilters in model");
        
        // Вычисляем начальные bounds модели (ДО масштабирования)
        Bounds initialBounds = new Bounds();
        bool initialBoundsInitialized = false;
        
        // Пробуем получить bounds из MeshFilter (локальные bounds меша)
        foreach (MeshFilter meshFilter in allMeshFilters)
        {
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                Bounds meshBounds = meshFilter.sharedMesh.bounds;
                if (meshBounds.size.magnitude > 0.001f)
                {
                    if (!initialBoundsInitialized)
                    {
                        initialBounds = meshBounds;
                        initialBoundsInitialized = true;
                    }
                    else
                    {
                        initialBounds.Encapsulate(meshBounds);
                    }
                }
            }
        }
        
        // Если не получилось из MeshFilter, пробуем из MeshRenderer
        if (!initialBoundsInitialized)
        {
            foreach (MeshRenderer renderer in renderers)
            {
                if (renderer != null && renderer.enabled)
                {
                    Bounds rendererBounds = renderer.bounds;
                    if (rendererBounds.size.magnitude > 0.001f)
                    {
                        if (!initialBoundsInitialized)
                        {
                            initialBounds = rendererBounds;
                            initialBoundsInitialized = true;
                        }
                        else
                        {
                            initialBounds.Encapsulate(rendererBounds);
                        }
                    }
                }
            }
        }
        
        // Вычисляем размер модели и масштабируем при необходимости
        float initialMaxSize = Mathf.Max(initialBounds.size.x, initialBounds.size.y);
        float scaleMultiplier = 1f;
        
        if (initialMaxSize > 0.001f && initialMaxSize < 1.0f)
        {
            // Увеличиваем масштаб так, чтобы модель была примерно 1.5 единицы в размере
            float targetSize = 1.5f;
            scaleMultiplier = targetSize / initialMaxSize;
            tempModel.transform.localScale = Vector3.one * scaleMultiplier;
            Debug.Log($"Model is small ({initialMaxSize:F4}), scaling by {scaleMultiplier:F2}x to size ~{targetSize}");
        }
        
        // Сохраняем scaleMultiplier для использования позже
        float finalScaleMultiplier = scaleMultiplier;
        
        // Теперь вычисляем bounds ПОСЛЕ масштабирования
        // Используем MeshFilter для получения точных bounds с учетом масштаба
        Bounds modelBounds = new Bounds();
        bool boundsInitialized = false;
        
        foreach (MeshFilter meshFilter in allMeshFilters)
        {
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                Bounds meshBounds = meshFilter.sharedMesh.bounds;
                // Преобразуем локальные bounds в мировые через transform (с учетом масштаба)
                // Используем lossyScale для учета всех масштабов в иерархии
                Vector3 scale = meshFilter.transform.lossyScale;
                Vector3 worldCenter = meshFilter.transform.TransformPoint(meshBounds.center);
                Vector3 worldSize = new Vector3(
                    meshBounds.size.x * scale.x,
                    meshBounds.size.y * scale.y,
                    meshBounds.size.z * scale.z
                );
                Bounds worldBounds = new Bounds(worldCenter, worldSize);
                
                Debug.Log($"MeshFilter bounds: local={meshBounds.size}, scale={scale}, world={worldSize}");
                
                if (!boundsInitialized)
                {
                    modelBounds = worldBounds;
                    boundsInitialized = true;
                }
                else
                {
                    modelBounds.Encapsulate(worldBounds);
                }
            }
        }
        
        // Если не получилось, пробуем из MeshRenderer (после масштабирования)
        if (!boundsInitialized || modelBounds.size.magnitude < 0.01f)
        {
            // Принудительно обновляем bounds рендереров
            foreach (MeshRenderer renderer in renderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = false;
                    renderer.enabled = true;
                }
            }
            
            foreach (MeshRenderer renderer in renderers)
            {
                if (renderer != null && renderer.enabled)
                {
                    Bounds rendererBounds = renderer.bounds;
                    if (rendererBounds.size.magnitude > 0.001f)
                    {
                        if (!boundsInitialized)
                        {
                            modelBounds = rendererBounds;
                            boundsInitialized = true;
                        }
                        else
                        {
                            modelBounds.Encapsulate(rendererBounds);
                        }
                    }
                }
            }
        }
        
        // Если все еще не нашли bounds, используем значения по умолчанию
        if (!boundsInitialized || modelBounds.size.magnitude < 0.01f)
        {
            modelBounds = new Bounds(Vector3.zero, new Vector3(1.5f, 1.5f, 0.1f));
            Debug.LogWarning("Could not determine model bounds, using default size of 1.5");
        }
        
        // Создаем временную камеру
        GameObject cameraObj = new GameObject("TempRenderCamera");
        Camera renderCamera = cameraObj.AddComponent<Camera>();
        renderCamera.clearFlags = CameraClearFlags.SolidColor;
        renderCamera.backgroundColor = Color.clear; // Прозрачный фон, чтобы увидеть модель
        renderCamera.orthographic = true;
        
        // Вычисляем размер ортографической камеры на основе bounds модели
        float maxSize = Mathf.Max(modelBounds.size.x, modelBounds.size.y);
        
        // Если модель все еще слишком маленькая, используем размер с учетом масштабирования
        if (maxSize < 0.1f)
        {
            // Если мы масштабировали модель, используем ожидаемый размер после масштабирования
            if (finalScaleMultiplier > 1f)
            {
                maxSize = 1.5f; // Ожидаемый размер после масштабирования
                Debug.LogWarning($"Model bounds are very small after scaling ({maxSize:F4}), using expected scaled size of 1.5");
            }
            else
            {
                maxSize = 1.5f; // Используем размер по умолчанию
                Debug.LogWarning($"Model bounds are very small ({maxSize:F4}), using default size of 1.5");
            }
        }
        
        // Для ортографической камеры orthographicSize - это половина высоты видимой области
        // Увеличиваем немного, чтобы был запас по краям (примерно 10% запас)
        renderCamera.orthographicSize = maxSize * 0.55f;
        
        renderCamera.targetTexture = renderTexture;
        renderCamera.cullingMask = -1; // Все слои
        renderCamera.nearClipPlane = 0.01f; // Ближе к модели
        renderCamera.farClipPlane = 100f;
        
        // Убеждаемся, что модель находится в центре (0, 0, 0)
        tempModel.transform.position = Vector3.zero;
        
        // Размещаем камеру так, чтобы она смотрела на модель сверху (для 2D вида)
        // В Unity для 2D камера обычно смотрит вдоль оси Z (отрицательное направление)
        // Позиционируем камеру перед моделью по оси Z
        // Для плоской модели (Z=0) используем небольшое расстояние
        float cameraDistance = 5f;
        if (modelBounds.size.z > 0.01f)
        {
            cameraDistance = Mathf.Max(modelBounds.size.z * 2f, 5f);
        }
        
        cameraObj.transform.position = new Vector3(0, 0, -cameraDistance);
        cameraObj.transform.rotation = Quaternion.identity; // Смотрим вдоль оси Z
        
        // Добавляем направленный свет для освещения модели (если используется Standard шейдер)
        GameObject lightObj = new GameObject("TempRenderLight");
        Light renderLight = lightObj.AddComponent<Light>();
        renderLight.type = LightType.Directional;
        renderLight.color = Color.white;
        renderLight.intensity = 1f;
        renderLight.transform.rotation = Quaternion.Euler(50, -30, 0);
        lightObj.hideFlags = HideFlags.HideAndDontSave;
        
        // Отладочная информация
        Debug.Log($"Model Bounds: center={modelBounds.center}, size={modelBounds.size}");
        Debug.Log($"Camera Position: {cameraObj.transform.position}, Orthographic Size: {renderCamera.orthographicSize}, Distance: {cameraDistance}");
        Debug.Log($"Model Position: {tempModel.transform.position}, Scale: {tempModel.transform.localScale}, Rotation: {tempModel.transform.rotation.eulerAngles}");
        Debug.Log($"Renderers count: {renderers.Length}");
        
        // Убеждаемся, что все рендереры активны и видны
        foreach (MeshRenderer renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.enabled = true;
                renderer.gameObject.SetActive(true);
                Debug.Log($"Renderer: {renderer.name}, Enabled: {renderer.enabled}, Active: {renderer.gameObject.activeInHierarchy}, Bounds: {renderer.bounds.size}");
            }
        }
        
        // Рендерим
        renderCamera.Render();
        
        // Читаем текстуру из RenderTexture
        RenderTexture.active = renderTexture;
        Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false); // RGBA32 для поддержки прозрачности
        texture.ReadPixels(new Rect(0, 0, textureSize, textureSize), 0, 0);
        texture.Apply();
        RenderTexture.active = null;
        
        // Проверяем, есть ли что-то кроме прозрачного фона
        int nonTransparentPixels = 0;
        for (int x = 0; x < textureSize; x++)
        {
            for (int y = 0; y < textureSize; y++)
            {
                Color pixel = texture.GetPixel(x, y);
                if (pixel.a > 0.01f) // Если пиксель не полностью прозрачный
                {
                    nonTransparentPixels++;
                }
            }
        }
        Debug.Log($"Non-transparent pixels: {nonTransparentPixels} out of {textureSize * textureSize} ({(nonTransparentPixels * 100f / (textureSize * textureSize)):F2}%)");
        
        // Сохраняем как PNG
        byte[] pngData = texture.EncodeToPNG();
        string pngPath = outputPath + ".png";
        File.WriteAllBytes(pngPath, pngData);
        
        // Импортируем текстуру как спрайт
        AssetDatabase.Refresh();
        TextureImporter textureImporter = AssetImporter.GetAtPath(pngPath) as TextureImporter;
        if (textureImporter != null)
        {
            textureImporter.textureType = TextureImporterType.Sprite;
            textureImporter.spriteImportMode = SpriteImportMode.Single;
            textureImporter.spritePixelsPerUnit = 100;
            textureImporter.filterMode = FilterMode.Bilinear;
            textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
            textureImporter.mipmapEnabled = false;
            AssetDatabase.ImportAsset(pngPath, ImportAssetOptions.ForceUpdate);
        }
        
        // Очищаем временные объекты
        DestroyImmediate(tempModel);
        DestroyImmediate(cameraObj);
        DestroyImmediate(lightObj);
        DestroyImmediate(renderTexture);
        DestroyImmediate(texture);
        DestroyImmediate(whiteMaterial);
        
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("Success", 
            $"Sprite created successfully at:\n{pngPath}\n\nYou can now use it in CellInfo!", 
            "OK");
        
        // Выделяем созданный спрайт в Project
        Object sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
        if (sprite != null)
        {
            Selection.activeObject = sprite;
            EditorGUIUtility.PingObject(sprite);
        }
    }
}

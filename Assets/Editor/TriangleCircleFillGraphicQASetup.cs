#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Редакторские сценарии для TriangleCircleFillGraphic (маски, анимация, стабильный Progress).
/// Проверка multi-size: после Setup продублировать элемент, задать разные sizeDelta (например 100 / 200 / 320),
/// остановить анимацию и выставить на материале <c>_Progress</c> = 0.25 / 0.5 / 0.75 — доля дуги должна совпадать
/// (ступенчато по треугольникам, без сдвига порога между размерами).
/// </summary>
public static class TriangleCircleFillGraphicQASetup
{
    private const string RootName = "QA_TriangleCircleFillGraphicRoot";

    [MenuItem("QA/UI/TriangleCircleFillGraphic Setup")]
    public static void Setup()
    {
        var existing = GameObject.Find(RootName);
        if (existing != null)
            GameObject.DestroyImmediate(existing);

        var root = new GameObject(RootName);

        // Canvas
        var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(root.transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        // RectMask2D (non-square for anchor/pivot correctness)
        var maskGO = new GameObject("RectMask2D", typeof(RectTransform), typeof(RectMask2D));
        maskGO.transform.SetParent(canvasGO.transform, false);
        var maskRT = maskGO.GetComponent<RectTransform>();
        maskRT.anchorMin = new Vector2(0.5f, 0.5f);
        maskRT.anchorMax = new Vector2(0.5f, 0.5f);
        maskRT.pivot = new Vector2(0.5f, 0.5f);
        maskRT.anchoredPosition = Vector2.zero;
        maskRT.sizeDelta = new Vector2(340f, 210f);

        var rectMask = maskGO.GetComponent<RectMask2D>();
        rectMask.padding = Vector4.zero;

        // Graphic
        var graphicGO = new GameObject("TriangleCircleFillGraphic", typeof(RectTransform), typeof(TriangleCircleFillGraphic), typeof(Animation));
        graphicGO.transform.SetParent(maskGO.transform, false);

        var graphicRT = graphicGO.GetComponent<RectTransform>();
        graphicRT.anchorMin = new Vector2(0.5f, 0.5f);
        graphicRT.anchorMax = new Vector2(0.5f, 0.5f);
        graphicRT.pivot = new Vector2(0.5f, 0.5f);
        graphicRT.anchoredPosition = Vector2.zero;
        graphicRT.sizeDelta = maskRT.sizeDelta;

        var graphic = graphicGO.GetComponent<TriangleCircleFillGraphic>();

        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Models/LoadCircleMesh.fbx");
        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/CircleLoad.mat");
        if (mesh == null)
        {
            Debug.LogError("QA Setup: Mesh not found at Assets/Models/LoadCircleMesh.fbx");
            return;
        }
        if (mat == null)
        {
            Debug.LogError("QA Setup: Material not found at Assets/Materials/CircleLoad.mat");
            return;
        }

        graphic.SourceMesh = mesh;
        graphic.material = mat;
        mat.SetFloat("_Progress", 0f);

        // Play existing animation clip (should animate material._Progress).
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/TriangleCircleLoadAnimation.anim");
        var animation = graphicGO.GetComponent<Animation>();
        if (clip != null)
        {
            animation.AddClip(clip, clip.name);
            animation.clip = clip;
            animation.playAutomatically = true;
            animation.wrapMode = WrapMode.Loop;
        }
        else
        {
            Debug.LogWarning("QA Setup: AnimationClip not found at Assets/Animations/TriangleCircleLoadAnimation.anim");
        }

        Selection.activeGameObject = root;
        Debug.Log("TriangleCircleFillGraphic QA setup created.");
    }

    [MenuItem("QA/UI/TriangleCircleFillGraphic — Multi-size Progress checklist")]
    public static void LogMultiSizeProgressChecklist()
    {
        Debug.Log(
            "[TriangleCircleFill] Multi-size quantized Progress:\n" +
            "1) QA/UI/TriangleCircleFillGraphic Setup\n" +
            "2) Дублируйте Graphic 2 раза; задайте sizeDelta примерно 120 × 120, 200 × 200, 320 × 320 (одинаковый pivot/anchor).\n" +
            "3) Отключите Animation или зафиксируйте кадр; на общем материале выставьте _Progress = 0.25, затем 0.5, затем 0.75.\n" +
            "4) Ожидание: визуальная доля заполнения дуги совпадает на всех трёх (квантование по целым фасеткам допустимо).\n" +
            "Шейдер использует _FillPivot/_FillInvScale из TriangleCircleFillGraphic для нормализации fill space.");
    }
}
#endif


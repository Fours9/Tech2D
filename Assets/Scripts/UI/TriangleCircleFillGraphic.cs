using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// uGUI Graphic implementation that renders a pre-authored Mesh using <see cref="VertexHelper"/>.
/// Scales the mesh to current <see cref="RectTransform"/> (optionally preserving aspect) and keeps
/// it compatible with Mask/RectMask2D via the standard MaskableGraphic pipeline.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(CanvasRenderer))]
[RequireComponent(typeof(RectTransform))]
public class TriangleCircleFillGraphic : MaskableGraphic
{
    [Header("Geometry")]
    [SerializeField] private Mesh sourceMesh;
    [SerializeField] private bool preserveAspect = true; // "вписанный круг" => true
    [SerializeField] private bool usePositionAsUV = false; // default: use mesh UVs

    [Header("Animation Bridge")]
    [SerializeField] private bool syncProgressFromMaterial = true;
    [SerializeField] private float progressEpsilon = 0.0001f;
    [SerializeField] private bool autoObjectSpacePatternScale = true;
    [SerializeField] [Range(0.1f, 2f)] private float autoPatternScaleMultiplier = 0.66f;

    private Vector2 _lastRectSize;
    private bool _cacheValid;

    private Mesh _cachedSourceMesh;
    private bool _cachedPreserveAspect;
    private bool _cachedUsePositionAsUV;

    private Vector3[] _cachedPositions;
    private Vector2[] _cachedUVs;
    private int[] _cachedTriangles;
    private Vector3 _cachedMeshCenter;

    private Material _lastTargetMaterial;
    private float _lastSourceProgress;
    private bool _hasLastSourceProgress;
    private float _lastPatternScale;
    private bool _hasLastPatternScale;

    private Vector4 _lastFillPivot;
    private Vector4 _lastFillInvScale;
    private bool _hasLastFillSpace;

    public Mesh SourceMesh
    {
        get => sourceMesh;
        set
        {
            if (sourceMesh == value)
                return;
            sourceMesh = value;
            InvalidateCache();
            SetVerticesDirty();
        }
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        InvalidateCache();
        SetVerticesDirty();
    }

    protected override void Awake()
    {
        base.Awake();
        EnsureCanvasRendererExists();
    }

    protected override void OnRectTransformDimensionsChange()
    {
        base.OnRectTransformDimensionsChange();
        var rect = GetPixelAdjustedRect();
        var size = new Vector2(rect.width, rect.height);

        if (!_cacheValid || !Mathf.Approximately(size.x, _lastRectSize.x) || !Mathf.Approximately(size.y, _lastRectSize.y))
        {
            _lastRectSize = size;
            InvalidateCache();
            SetVerticesDirty();
        }
    }

    private void InvalidateCache()
    {
        _cacheValid = false;
        _cachedPositions = null;
        _cachedUVs = null;
        _cachedTriangles = null;
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (sourceMesh == null)
            return;

        var rect = GetPixelAdjustedRect();
        if (rect.width <= 0f || rect.height <= 0f)
            return;

        EnsureCache(rect);

        if (_cachedPositions == null || _cachedUVs == null || _cachedTriangles == null)
            return;

        var color32 = color;

        // Vertices must be added in the same order as triangle indices.
        for (int i = 0; i < _cachedPositions.Length; i++)
            vh.AddVert(_cachedPositions[i], color32, _cachedUVs[i]);

        for (int i = 0; i < _cachedTriangles.Length; i += 3)
            vh.AddTriangle(_cachedTriangles[i], _cachedTriangles[i + 1], _cachedTriangles[i + 2]);
    }

    private void EnsureCache(Rect rect)
    {
        var size = new Vector2(rect.width, rect.height);
        bool rectChanged = !_cacheValid || !Mathf.Approximately(size.x, _lastRectSize.x) || !Mathf.Approximately(size.y, _lastRectSize.y);

        if (!rectChanged &&
            _cachedSourceMesh == sourceMesh &&
            _cachedPreserveAspect == preserveAspect &&
            _cachedUsePositionAsUV == usePositionAsUV)
        {
            return; // cache is valid
        }

        _lastRectSize = size;
        _cachedSourceMesh = sourceMesh;
        _cachedPreserveAspect = preserveAspect;
        _cachedUsePositionAsUV = usePositionAsUV;

        var srcVertices = sourceMesh.vertices;
        var srcTriangles = sourceMesh.triangles;
        var srcBounds = sourceMesh.bounds;

        if (srcVertices == null || srcVertices.Length == 0 || srcTriangles == null || srcTriangles.Length < 3 || srcBounds.size.sqrMagnitude <= 0f)
            return;

        _cachedMeshCenter = srcBounds.center;

        float meshW = Mathf.Max(srcBounds.size.x, 1e-6f);
        float meshH = Mathf.Max(srcBounds.size.y, 1e-6f);

        float scaleX;
        float scaleY;

        if (preserveAspect)
        {
            float s = Mathf.Min(rect.width / meshW, rect.height / meshH);
            scaleX = s;
            scaleY = s;
        }
        else
        {
            scaleX = rect.width / meshW;
            scaleY = rect.height / meshH;
        }

        _cachedPositions = new Vector3[srcVertices.Length];
        _cachedUVs = new Vector2[srcVertices.Length];
        _cachedTriangles = srcTriangles;

        var srcUV = sourceMesh.uv;
        bool hasUV = !usePositionAsUV && srcUV != null && srcUV.Length == srcVertices.Length;

        var rectCenter = new Vector2(rect.center.x, rect.center.y);

        for (int i = 0; i < srcVertices.Length; i++)
        {
            Vector3 p = srcVertices[i] - _cachedMeshCenter;

            p.x *= scaleX;
            p.y *= scaleY;
            p.z = 0f; // UI expects 2D

            p.x += rectCenter.x;
            p.y += rectCenter.y;

            _cachedPositions[i] = p;

            if (usePositionAsUV)
            {
                // Shader can optionally use uv as a proxy for local coordinates.
                _cachedUVs[i] = new Vector2(p.x, p.y);
            }
            else
            {
                _cachedUVs[i] = hasUV ? srcUV[i] : Vector2.zero;
            }
        }

        _cacheValid = true;
    }

    private void LateUpdate()
    {
        var dst = materialForRendering;
        if (dst == null)
            return;

        SyncFillSpaceParams(dst);
        SyncObjectSpacePatternScale(dst);
        SyncProgress(dst);
        _lastTargetMaterial = dst;
    }

    private void SyncProgress(Material dst)
    {
        if (!syncProgressFromMaterial)
            return;

        var src = material;
        if (src == null)
            return;

        if (src == dst)
            return; // already the same instance; animation affects it directly

        const string prop = "_Progress";
        if (!src.HasProperty(prop) || !dst.HasProperty(prop))
            return;

        float srcValue = src.GetFloat(prop);
        float dstValue = dst.GetFloat(prop);

        bool targetChanged = dst != _lastTargetMaterial;
        bool sourceChanged = !_hasLastSourceProgress || Mathf.Abs(srcValue - _lastSourceProgress) > progressEpsilon;
        bool dstAlreadyMatchesSource = Mathf.Abs(dstValue - srcValue) <= progressEpsilon;

        // Important: do not overwrite runtime-animated materialForRendering every frame
        // when the source material value did not actually change.
        if (!targetChanged && !sourceChanged)
            return;

        if (!dstAlreadyMatchesSource)
            dst.SetFloat(prop, srcValue);

        _lastSourceProgress = srcValue;
        _hasLastSourceProgress = true;
    }

    /// <summary>
    /// Maps fragment positions from Graphic local space into mesh (fill) space so Progress / polar angles
    /// are size-invariant. Must match <see cref="EnsureCache"/> scale and pivot.
    /// </summary>
    private void SyncFillSpaceParams(Material dst)
    {
        const string pivotProp = "_FillPivot";
        const string invProp = "_FillInvScale";
        if (!dst.HasProperty(pivotProp) || !dst.HasProperty(invProp))
            return;

        if (sourceMesh == null)
            return;

        var rect = GetPixelAdjustedRect();
        if (rect.width <= 0f || rect.height <= 0f)
            return;

        var srcBounds = sourceMesh.bounds;
        float meshW = Mathf.Max(1e-6f, srcBounds.size.x);
        float meshH = Mathf.Max(1e-6f, srcBounds.size.y);
        float rectW = Mathf.Max(0.0001f, rect.width);
        float rectH = Mathf.Max(0.0001f, rect.height);

        float scaleX;
        float scaleY;
        if (preserveAspect)
        {
            float s = Mathf.Min(rectW / meshW, rectH / meshH);
            scaleX = s;
            scaleY = s;
        }
        else
        {
            scaleX = rectW / meshW;
            scaleY = rectH / meshH;
        }

        var pivot = new Vector4(rect.center.x, rect.center.y, 0f, 0f);
        var invScale = new Vector4(
            1f / Mathf.Max(1e-6f, scaleX),
            1f / Mathf.Max(1e-6f, scaleY),
            0f,
            0f);

        bool targetChanged = dst != _lastTargetMaterial;
        bool changed = targetChanged
                       || !_hasLastFillSpace
                       || (pivot - _lastFillPivot).sqrMagnitude > 1e-10f
                       || (invScale - _lastFillInvScale).sqrMagnitude > 1e-10f;

        if (!changed)
            return;

        dst.SetVector(pivotProp, pivot);
        dst.SetVector(invProp, invScale);
        _lastFillPivot = pivot;
        _lastFillInvScale = invScale;
        _hasLastFillSpace = true;
    }

    private void SyncObjectSpacePatternScale(Material dst)
    {
        if (!autoObjectSpacePatternScale)
            return;

        const string prop = "_ObjectSpacePatternScale";
        if (!dst.HasProperty(prop))
            return;

        if (sourceMesh == null)
            return;

        var rect = GetPixelAdjustedRect();

        // Match the shader's expected object-space scale by undoing the mesh scaling we apply in EnsureCache().
        // This makes low-poly "cell" size stable across RectTransform sizes.
        var srcBounds = sourceMesh.bounds;
        float meshW = Mathf.Max(1e-6f, srcBounds.size.x);
        float meshH = Mathf.Max(1e-6f, srcBounds.size.y);

        float rectW = Mathf.Max(0.0001f, rect.width);
        float rectH = Mathf.Max(0.0001f, rect.height);

        float scaleX;
        float scaleY;

        if (preserveAspect)
        {
            float s = Mathf.Min(rectW / meshW, rectH / meshH);
            scaleX = s;
            scaleY = s;
        }
        else
        {
            scaleX = rectW / meshW;
            scaleY = rectH / meshH;
        }

        // Use an uniform scale as the best compromise for shader math that assumes isotropic object space.
        float uniformScale = preserveAspect ? scaleX : Mathf.Min(scaleX, scaleY);
        float patternScale = (1f / Mathf.Max(1e-6f, uniformScale)) * Mathf.Max(0.1f, autoPatternScaleMultiplier);

        bool targetChanged = dst != _lastTargetMaterial;
        bool valueChanged = !_hasLastPatternScale || Mathf.Abs(patternScale - _lastPatternScale) > 1e-6f;
        if (!targetChanged && !valueChanged)
            return;

        dst.SetFloat(prop, patternScale);
        _lastPatternScale = patternScale;
        _hasLastPatternScale = true;
    }

    private void EnsureCanvasRendererExists()
    {
        if (GetComponent<CanvasRenderer>() == null)
            gameObject.AddComponent<CanvasRenderer>();
    }
}


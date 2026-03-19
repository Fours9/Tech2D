Shader "Custom/TriangleCircleFill"
{
    Properties
    {
        _PaletteTex ("Palette Texture (1xN)", 2D) = "white" {}
        _PaletteCount ("Palette Count", Float) = 8
        _RandomSeed ("Random Seed", Float) = 1

        [HDR] _ColorEmpty ("Empty Color", Color) = (0.03, 0.08, 0.25, 1)
        [HDR] _ColorFilledFallback ("Filled Fallback Color", Color) = (0.1, 0.3, 1.0, 1)
        [HDR] _ColorHighlight ("Highlight Color", Color) = (0.8, 0.2, 1.0, 1)

        [Range(0, 1)] _Progress ("Progress", Float) = 0
        _SegmentCount ("Segment Count", Float) = 18
        _HighlightSegment ("Highlight Segment (-1 off)", Integer) = -1

        _StartAngleDeg ("Start Angle Deg", Float) = 90
        [Toggle] _FillClockwise ("Fill Clockwise", Float) = 1

        _CellDensity ("LowPoly Cell Density", Float) = 14
        [Toggle] _UseObjectSpacePattern ("Use Object Space Pattern", Float) = 1
        [Range(0, 0.45)] _VertexJitter ("Vertex Jitter", Float) = 0.22
        [Range(0.001, 0.3)] _FacetEdgeWidth ("Facet Edge Width", Float) = 0.045
        _FacetEdgeColor ("Facet Edge Color", Color) = (0, 0, 0, 1)
        [Range(0, 1)] _UnfilledDarken ("Unfilled Darken", Float) = 0.4

        [Toggle] _UseRadialMask ("Use Radial Mask", Float) = 0
        [Range(0, 1)] _InnerRadius ("Inner Radius", Float) = 0
        [Range(0, 1)] _OuterRadius ("Outer Radius", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Tags { "LightMode" = "SRPDefaultUnlit" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float2 positionOS  : TEXCOORD1;
            };

            TEXTURE2D(_PaletteTex);
            SAMPLER(sampler_PaletteTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _ColorEmpty;
                float4 _ColorFilledFallback;
                float4 _ColorHighlight;
                float4 _PaletteTex_ST;

                float _PaletteCount;
                float _RandomSeed;
                float _Progress;
                float _SegmentCount;
                int _HighlightSegment;
                float _StartAngleDeg;
                float _FillClockwise;
                float _CellDensity;
                float _UseObjectSpacePattern;
                float _VertexJitter;
                float _FacetEdgeWidth;
                float4 _FacetEdgeColor;
                float _UnfilledDarken;
                float _UseRadialMask;
                float _InnerRadius;
                float _OuterRadius;
            CBUFFER_END

            // Deterministic integer hash for stable per-segment color picking.
            uint HashUint(uint s)
            {
                s ^= 2747636419u;
                s *= 2654435769u;
                s ^= s >> 16;
                s *= 2654435769u;
                s ^= s >> 16;
                s *= 2654435769u;
                return s;
            }

            float Hash11(float n)
            {
                return frac(sin(n) * 43758.5453123);
            }

            float2 Hash22(float2 p)
            {
                float x = dot(p, float2(127.1, 311.7));
                float y = dot(p, float2(269.5, 183.3));
                return frac(sin(float2(x, y)) * 43758.5453123);
            }

            float2 JitteredVertex(float2 gridId)
            {
                float2 rnd = Hash22(gridId + _RandomSeed * 17.31) * 2.0 - 1.0;
                float jitterSafe = min(max(_VertexJitter, 0.0), 0.65);
                // Jitter in Cartesian space for irregular triangle side lengths.
                return rnd * jitterSafe * 0.48;
            }

            float2 LatticeToCartesian(float2 lattice)
            {
                const float k = 0.86602540378; // sqrt(3)/2
                return float2(lattice.x + 0.5 * lattice.y, lattice.y * k);
            }

            // Same angular convention as fragment uses for mesh position (polar normalize).
            // 0..360° after _StartAngleDeg / _FillClockwise (same convention as fragment polar angle).
            float GetAngleDegForPos(float2 pos2d)
            {
                float len = max(length(pos2d), 1e-5);
                float2 polarPos = pos2d / len;
                float angleDeg = degrees(atan2(polarPos.y, polarPos.x));
                angleDeg = angleDeg < 0.0 ? angleDeg + 360.0 : angleDeg;
                angleDeg = fmod(angleDeg - _StartAngleDeg + 360.0, 360.0);
                if (_FillClockwise > 0.5)
                {
                    angleDeg = fmod(360.0 - angleDeg, 360.0);
                }
                return angleDeg;
            }

            float AngleDelta(float aDeg, float refDeg)
            {
                float d = aDeg - refDeg;
                if (d > 180.0) d -= 360.0;
                if (d < -180.0) d += 360.0;
                return d;
            }

            bool PointInTriangle(float2 p, float2 a, float2 b, float2 c, out float3 bary)
            {
                float2 v0 = b - a;
                float2 v1 = c - a;
                float2 v2 = p - a;
                float den = v0.x * v1.y - v1.x * v0.y;
                if (abs(den) < 1e-6)
                {
                    bary = float3(0, 0, 0);
                    return false;
                }

                float invDen = 1.0 / den;
                float u = (v2.x * v1.y - v1.x * v2.y) * invDen;
                float v = (v0.x * v2.y - v2.x * v0.y) * invDen;
                float w = 1.0 - u - v;
                bary = float3(w, u, v);
                const float eps = -1e-4;
                return (u >= eps && v >= eps && w >= eps);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.positionOS = IN.positionOS.xy;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 meshPos = IN.positionOS;
                float meshLen = max(length(meshPos), 1e-5);
                float2 polarPos = meshPos / meshLen;

                float radius = length(polarPos);

                float outerR = saturate(_OuterRadius);
                float innerR = saturate(_InnerRadius);
                innerR = min(innerR, outerR);

                if (_UseRadialMask > 0.5 && (radius < innerR || radius > outerR))
                {
                    return half4(0, 0, 0, 0);
                }

                float angleDeg = degrees(atan2(polarPos.y, polarPos.x));
                angleDeg = angleDeg < 0.0 ? angleDeg + 360.0 : angleDeg;
                angleDeg = fmod(angleDeg - _StartAngleDeg + 360.0, 360.0);

                if (_FillClockwise > 0.5)
                {
                    angleDeg = fmod(360.0 - angleDeg, 360.0);
                }

                float progress = saturate(_Progress);
                const float progressEps = 1e-3;

                int segmentCount = max(1, (int)round(_SegmentCount));
                float segmentSize = 360.0 / (float)segmentCount;
                int segmentIndex = min(segmentCount - 1, (int)floor(angleDeg / segmentSize));

                half4 colorOut = _ColorEmpty;
                int paletteCount = max(1, (int)round(_PaletteCount));
                bool triFullyFilled = false;

                if (_PaletteCount >= 1.0)
                {
                    float density = max(1.0, _CellDensity);
                    float invDensity = 1.0 / density;
                    float2 facetCoords = (_UseObjectSpacePattern > 0.5) ? IN.positionOS : IN.uv;
                    float2 p = facetCoords * density;

                    float2 g = float2(p.x - p.y * 0.57735026919, p.y * 1.15470053838);
                    float2 baseCell = floor(g);

                    float3 bary;
                    float2 triId;
                    float2 bestVa = float2(0, 0);
                    float2 bestVb = float2(0, 0);
                    float2 bestVc = float2(0, 0);
                    bool allowEdgeColor = true;
                    float bestDepth = -1e10;
                    float bestFallbackDist = 1e20;
                    float2 bestFallbackTriId = baseCell * 2.0;
                    float2 fbVa = float2(0, 0);
                    float2 fbVb = float2(0, 0);
                    float2 fbVc = float2(0, 0);
                    bool foundValid = false;

                    [unroll]
                    for (int oy = -1; oy <= 1; oy++)
                    {
                        [unroll]
                        for (int ox = -1; ox <= 1; ox++)
                        {
                            float2 cell = baseCell + float2((float)ox, (float)oy);

                            float2 la1 = cell;
                            float2 lb1 = cell + float2(1, 0);
                            float2 lc1 = cell + float2(0, 1);
                            float2 triId1 = cell * 2.0 + float2(0, 0);

                            float2 va1 = LatticeToCartesian(la1) + JitteredVertex(la1);
                            float2 vb1 = LatticeToCartesian(lb1) + JitteredVertex(lb1);
                            float2 vc1 = LatticeToCartesian(lc1) + JitteredVertex(lc1);

                            float3 testBary1;
                            bool valid1 = PointInTriangle(p, va1, vb1, vc1, testBary1);
                            float depth1 = min(testBary1.x, min(testBary1.y, testBary1.z));
                            float2 c1 = (va1 + vb1 + vc1) / 3.0;
                            float d1 = dot(p - c1, p - c1);

                            if (valid1 && depth1 > bestDepth)
                            {
                                bestDepth = depth1;
                                bary = testBary1;
                                triId = triId1;
                                bestVa = va1;
                                bestVb = vb1;
                                bestVc = vc1;
                                foundValid = true;
                            }

                            if (d1 < bestFallbackDist)
                            {
                                bestFallbackDist = d1;
                                bestFallbackTriId = triId1;
                                fbVa = va1;
                                fbVb = vb1;
                                fbVc = vc1;
                            }

                            float2 la2 = cell + float2(1, 1);
                            float2 lb2 = cell + float2(0, 1);
                            float2 lc2 = cell + float2(1, 0);
                            float2 triId2 = cell * 2.0 + float2(1, 0);

                            float2 va2 = LatticeToCartesian(la2) + JitteredVertex(la2);
                            float2 vb2 = LatticeToCartesian(lb2) + JitteredVertex(lb2);
                            float2 vc2 = LatticeToCartesian(lc2) + JitteredVertex(lc2);

                            float3 testBary2;
                            bool valid2 = PointInTriangle(p, va2, vb2, vc2, testBary2);
                            float depth2 = min(testBary2.x, min(testBary2.y, testBary2.z));
                            float2 c2 = (va2 + vb2 + vc2) / 3.0;
                            float d2 = dot(p - c2, p - c2);

                            if (valid2 && depth2 > bestDepth)
                            {
                                bestDepth = depth2;
                                bary = testBary2;
                                triId = triId2;
                                bestVa = va2;
                                bestVb = vb2;
                                bestVc = vc2;
                                foundValid = true;
                            }

                            if (d2 < bestFallbackDist)
                            {
                                bestFallbackDist = d2;
                                bestFallbackTriId = triId2;
                                fbVa = va2;
                                fbVb = vb2;
                                fbVc = vc2;
                            }
                        }
                    }

                    if (!foundValid)
                    {
                        bary = float3(0.3333, 0.3333, 0.3333);
                        triId = bestFallbackTriId;
                        bestVa = fbVa;
                        bestVb = fbVb;
                        bestVc = fbVc;
                        allowEdgeColor = false;
                    }

                    const float progressActiveMin = 1e-5;
                    if (progress <= progressActiveMin)
                    {
                        triFullyFilled = false;
                    }
                    else
                    {
                        float progressDeg = progress * 360.0;
                        float ringR = (_UseObjectSpacePattern > 0.5)
                            ? max(length(meshPos), 1e-4)
                            : max(length(facetCoords - 0.5), 0.02);
                        float cellWorld = 1.0 / density;
                        float angularSlopDeg = degrees(atan(cellWorld / ringR)) * 1.25;
                        angularSlopDeg = max(angularSlopDeg, progressEps);
                        float fillBound = progressDeg + progressEps + angularSlopDeg;
                        fillBound = max(fillBound, 0.0);
                        fillBound = min(fillBound, 360.0);

                        float2 posA = bestVa * invDensity;
                        float2 posB = bestVb * invDensity;
                        float2 posC = bestVc * invDensity;
                        float a0 = GetAngleDegForPos(posA);
                        float a1 = GetAngleDegForPos(posB);
                        float a2 = GetAngleDegForPos(posC);
                        float d0 = AngleDelta(a0, 0.0);
                        float d1 = AngleDelta(a1, 0.0);
                        float d2 = AngleDelta(a2, 0.0);
                        bool inside0 = (d0 >= -progressEps && d0 <= fillBound);
                        bool inside1 = (d1 >= -progressEps && d1 <= fillBound);
                        bool inside2 = (d2 >= -progressEps && d2 <= fillBound);
                        float minD = min(d0, min(d1, d2));
                        float maxD = max(d0, max(d1, d2));
                        bool wrapCase = (maxD - minD > 180.0);
                        bool wrapValid = wrapCase && (maxD <= fillBound) && (minD >= -progressEps);
                        triFullyFilled = (inside0 && inside1 && inside2) || wrapValid;
                    }

                    float edgeMetric = min(bary.x, min(bary.y, bary.z));
                    float triSeed = dot(triId, float2(12.9898, 78.233)) + _RandomSeed * 57.31;
                    float cellNoise = Hash11(triSeed);
                    int paletteIndex = min(paletteCount - 1, (int)floor(cellNoise * paletteCount));
                    float paletteU = ((float)paletteIndex + 0.5) / (float)paletteCount;
                    float2 paletteUV = float2(paletteU, 0.5);
                    half4 paletteColor = SAMPLE_TEXTURE2D(_PaletteTex, sampler_PaletteTex, paletteUV);

                    float edgeWidth = max(0.001, _FacetEdgeWidth);
                    bool isEdge = allowEdgeColor && (edgeMetric < edgeWidth);
                    colorOut = isEdge
                        ? half4(_FacetEdgeColor.rgb, _ColorFilledFallback.a)
                        : half4(paletteColor.rgb, _ColorFilledFallback.a);

                    float darken = saturate(_UnfilledDarken);
                    if (!triFullyFilled)
                    {
                        colorOut.rgb *= darken;
                    }
                }
                else
                {
                    colorOut = half4(_ColorFilledFallback.rgb, _ColorFilledFallback.a);
                    const float progressActiveMin = 1e-5;
                    if (progress <= progressActiveMin)
                    {
                        triFullyFilled = false;
                    }
                    else
                    {
                        float progressDeg = progress * 360.0;
                        float ringR = max(length(meshPos), 1e-4);
                        float cellWorld = 1.0 / max(1.0, _CellDensity);
                        float angularSlopDeg = degrees(atan(cellWorld / ringR)) * 1.25;
                        angularSlopDeg = max(angularSlopDeg, progressEps);
                        float fillBound = progressDeg + progressEps + angularSlopDeg;
                        fillBound = max(fillBound, 0.0);
                        fillBound = min(fillBound, 360.0);
                        triFullyFilled = (angleDeg <= fillBound);
                    }
                    if (!triFullyFilled)
                    {
                        colorOut.rgb *= saturate(_UnfilledDarken);
                    }
                }

                if (triFullyFilled && _HighlightSegment >= 0 && segmentIndex == _HighlightSegment)
                {
                    colorOut = _ColorHighlight;
                }

                return colorOut;
            }
            ENDHLSL
        }
    }
}

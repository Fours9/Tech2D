Shader "Custom/TintingLayerOutline"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [Range(0.0, 1.0)] _EdgeRadius ("Edge Radius", Float) = 0.9
        [Range(0.0, 1.0)] _EdgeDarkening ("Edge Darkening", Float) = 0.9
        _EdgeColor ("Edge Color", Color) = (0,0,0,1)
    }
    
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }
        
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _EdgeRadius;
            float _EdgeDarkening;
            fixed4 _EdgeColor;
            
            // Полная SDF для pointy-top шестиугольника (копия из WorldSpaceTexture.shader)
            // r - расстояние от центра до вершины
            float hexSDF(float2 p, float r)
            {
                const float k = sqrt(3.0);
                p = abs(p);
                
                float flatSideDist = r * k * 0.5;
                float distToFlat = p.x - flatSideDist;
                
                float distToDiag = (p.x + k * p.y - k * r) * 0.5;
                
                return max(distToFlat, distToDiag);
            }
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * _Color * i.color;
                
                // UV в нормализованное пространство: центр (0.5,0.5) -> (0,0), края -> ±1
                // Для pointy-top hex в квадратной текстуре: вершины сверху/снизу на краях, r=1
                float2 uvCentered = (i.uv - 0.5) * 2.0;
                float hexRadius = 1.0; // В нормализованном UV-пространстве
                
                float hexSDFValue = hexSDF(uvCentered, hexRadius);
                
                // Нормализуем: 0 в центре, 1 на границе (как в WorldSpaceTexture)
                float minSDF = -hexRadius * sqrt(3.0) * 0.5;
                float hexDist01 = (hexSDFValue - minSDF) / (0.0 - minSDF);
                hexDist01 = saturate(hexDist01);
                
                // Фактор затемнения
                float darkeningFactor = 0.0;
                if (hexDist01 > _EdgeRadius)
                {
                    darkeningFactor = (hexDist01 - _EdgeRadius) / (1.0 - _EdgeRadius);
                    darkeningFactor = saturate(darkeningFactor);
                }
                
                float edgeBlendFactor = darkeningFactor * _EdgeDarkening;
                col.rgb = lerp(col.rgb, _EdgeColor.rgb, edgeBlendFactor);
                col.a = lerp(col.a, _EdgeColor.a, edgeBlendFactor); // Обводка сохраняет непрозрачность при прозрачном тинте
                
                return col;
            }
            ENDCG
        }
    }
    
    FallBack "Sprites/Default"
}

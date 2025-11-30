Shader "Custom/FogOfWarNoise"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _TextureScale ("Texture Scale", Float) = 1.0
        _OriginalPosition ("Original Position", Vector) = (0,0,0,0)
    }
    
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        
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
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _TextureScale;
            float4 _OriginalPosition; // Изначальная позиция клетки (x, y, z, w)
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                // Используем изначальную позицию клетки как базовую точку для расчета UV
                // Вычисляем смещение от центра объекта до вершины в локальных координатах
                // Преобразуем это смещение в мировые координаты (только поворот и масштаб, без перемещения)
                float3 localVertex = v.vertex.xyz; // Локальные координаты вершины (относительно центра объекта)
                float3x3 objectToWorldRotationScale = (float3x3)unity_ObjectToWorld; // Матрица поворота и масштаба (без перемещения)
                float3 worldOffset = mul(objectToWorldRotationScale, localVertex); // Смещение в мировых координатах
                // Используем originalPosition как базовую точку и добавляем мировое смещение
                o.uv = (_OriginalPosition.xy + worldOffset.xy) * _TextureScale;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Используем UV на основе изначальной позиции клетки для текстуры с тилированием
                // UV уже рассчитаны в вершинном шейдере на основе _OriginalPosition
                // Применяем масштаб и смещение из _MainTex_ST, затем тилируем через frac
                float2 uv = i.uv * _MainTex_ST.xy + _MainTex_ST.zw;
                // Тиллируем текстуру (повторяем)
                uv = frac(uv);
                fixed4 col = tex2D(_MainTex, uv) * _Color;
                return col;
            }
            ENDCG
        }
    }
    
    FallBack "Diffuse"
}

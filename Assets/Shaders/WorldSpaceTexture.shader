Shader "Custom/WorldSpaceTexture"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _TextureScale ("Texture Scale", Float) = 1.0
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        
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
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                // Используем мировые координаты для UV с учетом масштаба
                o.uv = o.worldPos.xy * _TextureScale;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Используем world-space UV для текстуры с тилированием
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



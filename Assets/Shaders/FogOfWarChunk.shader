Shader "Custom/FogOfWarChunk"
{
    // Упрощённая версия FogOfWarNoise для чанков: без рваных краёв, без _OriginalPosition,
    // без анимаций переходов. UV по мировым координатам вершин.
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _TextureScale ("Texture Scale", Float) = 1.0
        [Header(Explored Clouds)]
        [Toggle] _CloudsEnabled ("Clouds Enabled", Float) = 0.0
        [Range(0.1, 10.0)] _CloudsScale ("Clouds Scale", Float) = 1.5
        [Range(0.0, 1.0)] _CloudsIntensity ("Clouds Intensity", Float) = 0.4
        [Range(0.0, 5.0)] _CloudsSpeed ("Clouds Speed", Float) = 0.3
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
            #define CLOUD_QUANTIZE 1000.0
            
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
            float _CloudsEnabled;
            float _CloudsScale;
            float _CloudsIntensity;
            float _CloudsSpeed;
            
            float noise2D(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }
            
            float smoothNoise2D(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = noise2D(i);
                float b = noise2D(i + float2(1.0, 0.0));
                float c = noise2D(i + float2(0.0, 1.0));
                float d = noise2D(i + float2(1.0, 1.0));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.uv = o.worldPos.xy * _TextureScale;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv * _MainTex_ST.xy + _MainTex_ST.zw;
                uv = uv - floor(uv);
                fixed4 col = tex2D(_MainTex, uv) * _Color;
                
                if (_CloudsEnabled > 0.5)
                {
                    float2 rawCoord = i.worldPos.xy * _CloudsScale;
                    float2 baseCloudCoord = floor(rawCoord * CLOUD_QUANTIZE + 0.5) / CLOUD_QUANTIZE;
                    float t = _Time.y * _CloudsSpeed;
                    float n1 = smoothNoise2D(baseCloudCoord + float2(0.0, t));
                    float n2 = smoothNoise2D(baseCloudCoord * 2.0 + float2(t, 0.0));
                    float cloudsRaw = n1 * 0.6 + n2 * 0.4;
                    float cloudsMask = saturate((cloudsRaw - 0.4) / 0.6);
                    float cloudsStrength = _CloudsIntensity * cloudsMask;
                    float3 foggedColor = col.rgb * 0.6 + float3(1.0, 1.0, 1.0) * 0.4;
                    col.rgb = lerp(col.rgb, foggedColor, cloudsStrength);
                }
                
                return col;
            }
            ENDCG
        }
    }
    
    FallBack "Diffuse"
}

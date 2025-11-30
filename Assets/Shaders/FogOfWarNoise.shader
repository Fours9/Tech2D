Shader "Custom/FogOfWarNoise"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _TextureScale ("Texture Scale", Float) = 1.0
        _OriginalPosition ("Original Position", Vector) = (0,0,0,0)
        // _HexRadius устанавливается автоматически через код, не отображается в инспекторе
        [Range(0.0, 1.0)] _EdgeRadius ("Edge Radius", Float) = 0.7
        [Range(0.0, 1.0)] _EdgeDarkening ("Edge Darkening", Float) = 0.4
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
                float2 localPos : TEXCOORD2;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _TextureScale;
            float4 _OriginalPosition; // Изначальная позиция клетки (x, y, z, w)
            float _HexRadius; // Радиус гексагона для расчета виньетки
            float _EdgeRadius; // От какого расстояния от центра начинать затемнение (0-1)
            float _EdgeDarkening; // Насколько сильно затемнять у самого края (0-1)
            
            // Полная SDF для pointy-top шестиугольника
            // r - расстояние от центра до вершины (_HexRadius)
            // Учитывает все 6 граней: 2 плоские вертикальные стороны и 4 диагональные
            float hexSDF(float2 p, float r)
            {
                const float k = sqrt(3.0);
                // Используем симметрию - работаем только с первым квадрантом
                p = abs(p);
                
                // Расстояние до плоской вертикальной стороны
                // Для pointy-top hex: плоские стороны вертикальные, расстояние = r * sqrt(3) / 2
                float flatSideDist = r * k * 0.5;
                float distToFlat = p.x - flatSideDist;
                
                // Расстояние до диагональных сторон
                // Правильная формула для диагональной грани pointy-top hex:
                // Линия: x + √3·y = √3·r
                // Signed distance: d = 0.5 * x + 0.5 * √3 * (y - r)
                // Или: d = (x + √3·y - √3·r) / 2
                float distToDiag = (p.x + k * p.y - k * r) * 0.5;
                
                // Берем максимум - это и есть расстояние до ближайшей грани
                // Отрицательное значение = внутри, 0 = на границе, положительное = снаружи
                return max(distToFlat, distToDiag);
            }
            
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
                // Сохраняем локальную позицию для расчета виньетки
                o.localPos = v.vertex.xy;
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
                
                // Расчет виньетки на основе полной SDF правильного pointy-top шестиугольника
                // _HexRadius - расстояние от центра до вершины
                float hexSDFValue = hexSDF(i.localPos, _HexRadius);
                
                // Нормализуем: 0 в центре, 1 на границе
                // В центре hexSDF отрицательное (для pointy-top hex в центре SDF = -r*sqrt(3)/2 до плоской стороны)
                // На границе hexSDF = 0
                // Для pointy-top hex минимальное значение в центре = -r*sqrt(3)/2 (до плоской стороны)
                float minSDF = -_HexRadius * sqrt(3.0) * 0.5; // Минимальное значение в центре
                
                // Нормализуем: (sdf - minSDF) / (0 - minSDF)
                // При sdf = minSDF (центр) → 0
                // При sdf = 0 (граница) → 1
                float hexDist01 = (hexSDFValue - minSDF) / (0.0 - minSDF);
                hexDist01 = saturate(hexDist01); // Ограничиваем 0-1
                
                // Вычисляем фактор затемнения:
                // Если hexDist01 < _EdgeRadius → фактор = 0 (не затемняем)
                // Если hexDist01 >= _EdgeRadius → фактор плавно растет от 0 до 1
                float darkeningFactor = 0.0;
                if (hexDist01 > _EdgeRadius)
                {
                    // Нормализуем на оставшийся промежуток от _EdgeRadius до 1.0
                    darkeningFactor = (hexDist01 - _EdgeRadius) / (1.0 - _EdgeRadius);
                    darkeningFactor = saturate(darkeningFactor); // Ограничиваем 0-1
                }
                
                // Множитель яркости:
                // При факторе 0 → яркость = 1.0 (ничего не меняем)
                // При факторе 1 → яркость = 1.0 - _EdgeDarkening
                float brightnessMultiplier = lerp(1.0, 1.0 - _EdgeDarkening, darkeningFactor);
                
                // Применяем виньетку к цвету
                col.rgb *= brightnessMultiplier;
                
                return col;
            }
            ENDCG
        }
    }
    
    FallBack "Diffuse"
}

Shader "Custom/FogOfWarNoise"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _TextureScale ("Texture Scale", Float) = 1.0
        _OriginalPosition ("Original Position", Vector) = (0,0,0,0)
        // _HexRadius устанавливается автоматически через код, не отображается в инспекторе
        [Header(Vignette)]
        [Toggle] _VignetteEnabled ("Vignette Enabled", Float) = 1.0
        [Range(0.0, 1.0)] _VignetteHexRadius ("Vignette Hex Radius", Float) = 1.0
        [Range(0.0, 1.0)] _EdgeRadius ("Edge Radius", Float) = 0.7
        [Range(0.0, 1.0)] _EdgeDarkening ("Edge Darkening", Float) = 0.4
        [Header(Ragged Edges)]
        [Range(0.0, 1.0)] _RaggedEdgesIntensity ("Ragged Edges Intensity", Float) = 0.1
        [Range(0.1, 10.0)] _RaggedEdgesScale ("Ragged Edges Scale", Float) = 2.0
        [Header(Explored Clouds)]
        [Toggle] _CloudsEnabled ("Clouds Enabled", Float) = 0.0
        [Range(0.1, 10.0)] _CloudsScale ("Clouds Scale", Float) = 1.5
        [Range(0.0, 1.0)] _CloudsIntensity ("Clouds Intensity", Float) = 0.4
        [Range(0.0, 5.0)] _CloudsSpeed ("Clouds Speed", Float) = 0.3
        [Header(Glowing Edge)]
        _GlowColor ("Glow Color", Color) = (1.0, 0.7, 0.4, 1.0)
        [Range(0.0, 1.0)] _GlowIntensity ("Glow Intensity", Float) = 0.6
        [Range(0.0, 0.1)] _GlowWidth ("Glow Width", Float) = 0.05
        [Range(0.0, 5.0)] _GlowFlickerSpeed ("Glow Flicker Speed", Float) = 1.5
        [Header(Ragged Edges Per Face)]
        [Toggle] _RaggedEdgeTopLeft ("Top Left", Float) = 1.0
        [Toggle] _RaggedEdgeFlatLeft ("Top Right", Float) = 1.0
        [Toggle] _RaggedEdgeBottomLeft ("Flat Right", Float) = 1.0
        [Toggle] _RaggedEdgeBottomRight ("Bottom Right", Float) = 1.0
        [Toggle] _RaggedEdgeFlatRight ("Bottom Left", Float) = 1.0
        [Toggle] _RaggedEdgeTopRight ("Flat Left", Float) = 1.0
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
                // Смещение вершины в мировых координатах (только поворот+масштаб, без переноса),
                // чтобы использовать его вместе с _OriginalPosition так же, как для основной текстуры.
                float2 worldOffset : TEXCOORD3;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _TextureScale;
            float4 _OriginalPosition; // Изначальная позиция клетки (x, y, z, w)
            float _HexRadius; // Радиус гексагона для расчета неровных краев (устанавливается автоматически)
            float _VignetteHexRadius; // Множитель радиуса для виньетки (0-1, умножается на _HexRadius)
            float _EdgeRadius; // От какого расстояния от центра начинать затемнение (0-1)
            float _EdgeDarkening; // Насколько сильно затемнять у самого края (0-1)
            float _VignetteEnabled; // Включена ли виньетка (0 или 1)
            float _RaggedEdgesIntensity; // Интенсивность неровных краев (0-1)
            float _RaggedEdgesScale; // Масштаб шума для неровных краев
            float _CloudsEnabled; // Включены ли облака (0 или 1)
            float _CloudsScale;   // Масштаб шума для облаков
            float _CloudsIntensity; // Интенсивность облаков (0-1)
            float _CloudsSpeed;   // Скорость движения облаков
            float _RaggedEdgesEnabled; // Включены ли неровные края (0 или 1)
            float _RaggedEdgeTopRight; // Включена ли неровность для Top Right (330-30°) (0 или 1)
            float _RaggedEdgeTopLeft; // Включена ли неровность для Top Left (30-90°) (0 или 1)
            float _RaggedEdgeFlatLeft; // Включена ли неровность для Flat Left (90-150°) (0 или 1)
            float _RaggedEdgeBottomLeft; // Включена ли неровность для Bottom Left (150-210°) (0 или 1)
            float _RaggedEdgeBottomRight; // Включена ли неровность для Bottom Right (210-270°) (0 или 1)
            float _RaggedEdgeFlatRight; // Включена ли неровность для Flat Right (270-330°) (0 или 1)
            fixed4 _GlowColor; // Цвет тления
            float _GlowIntensity; // Интенсивность тления (0-1)
            float _GlowWidth; // Ширина зоны тления (0-0.1)
            float _GlowFlickerSpeed; // Скорость мерцания (0-5)
            
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
            
            // Простая функция шума для создания неровных краев
            // Использует дробную часть синуса для создания псевдослучайного значения
            float noise2D(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }
            
            // Сглаженный шум (fractal noise)
            float smoothNoise2D(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f); // Smoothstep для интерполяции
                
                float a = noise2D(i);
                float b = noise2D(i + float2(1.0, 0.0));
                float c = noise2D(i + float2(0.0, 1.0));
                float d = noise2D(i + float2(1.0, 1.0));
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }
            
            // Определяет, какая грань ближе всего к точке p
            // Для flat-top hex (сверху грань, а не вершина)
            // Возвращает: 0=top (flat), 1=top-right, 2=bottom-right, 3=bottom (flat), 4=bottom-left, 5=top-left
            int getClosestFace(float2 p, float r)
            {
                float angle = atan2(p.y, p.x); // Угол в радианах от -PI до PI
                
                // Нормализуем угол к диапазону [0, 2*PI]
                if (angle < 0.0) angle += 6.28318530718; // 2*PI
                
                // 6 равных секторов по 60° (π/3 = 1.0472 rad)
                // Сектора сдвинуты на 30° (π/6 = 0.5236 rad) относительно стандартных
                const float pi_6 = 0.5235987756;   // π/6 = 30°
                const float pi_2 = 1.5707963268;   // π/2 = 90°
                const float pi_5_6 = 2.6179938780; // 5π/6 = 150°
                const float pi_7_6 = 3.6651914292; // 7π/6 = 210°
                const float pi_3_2 = 4.7123889804; // 3π/2 = 270°
                const float pi_11_6 = 5.7595865316; // 11π/6 = 330°
                
                // Top-Left: 30-90° (центр 60°)
                // Flat Left (Top flat): 90-150° (центр 120°)
                // Bottom-Left: 150-210° (центр 180°)
                // Bottom-Right: 210-270° (центр 240°)
                // Flat Right (Bottom flat): 270-330° (центр 300°)
                // Top-Right: 330-30° (центр 0°/360°)
                if (angle >= pi_11_6 || angle < pi_6) return 1; // 330-30°: Top-Right
                else if (angle < pi_2) return 5; // 30-90°: Top-Left
                else if (angle < pi_5_6) return 0; // 90-150°: Flat Left (Top flat)
                else if (angle < pi_7_6) return 4; // 150-210°: Bottom-Left
                else if (angle < pi_3_2) return 2; // 210-270°: Bottom-Right
                else return 3; // 270-330°: Flat Right (Bottom flat)
            }
            
            // Получает значение включения для грани по индексу
            float getFaceEnabled(int faceIndex)
            {
                if (faceIndex == 0) return _RaggedEdgeFlatLeft; // Flat Left (90-150°)
                else if (faceIndex == 1) return _RaggedEdgeTopRight; // Top Right (330-30°)
                else if (faceIndex == 2) return _RaggedEdgeBottomRight; // Bottom Right (210-270°)
                else if (faceIndex == 3) return _RaggedEdgeFlatRight; // Flat Right (270-330°)
                else if (faceIndex == 4) return _RaggedEdgeBottomLeft; // Bottom Left (150-210°)
                else return _RaggedEdgeTopLeft; // Top Left (30-90°), faceIndex == 5
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
                // Сохраняем worldOffset отдельно, чтобы использовать те же координаты для шума неровных краёв и огня
                o.worldOffset = worldOffset.xy;
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
                // Применяем только если виньетка включена
                if (_VignetteEnabled > 0.5)
                {
                    // Используем отдельный радиус для виньетки: _VignetteHexRadius * _HexRadius
                    // _VignetteHexRadius - множитель от 0 до 1, _HexRadius - базовый радиус из mesh
                    float vignetteRadius = _VignetteHexRadius * _HexRadius;
                    float hexSDFValue = hexSDF(i.localPos, vignetteRadius);
                    
                    // Нормализуем: 0 в центре, 1 на границе
                    // В центре hexSDF отрицательное (для pointy-top hex в центре SDF = -r*sqrt(3)/2 до плоской стороны)
                    // На границе hexSDF = 0
                    // Для pointy-top hex минимальное значение в центре = -r*sqrt(3)/2 (до плоской стороны)
                    float minSDF = -vignetteRadius * sqrt(3.0) * 0.5; // Минимальное значение в центре
                    
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
                }
                
                // Облачный туман (визуально больше подходит для Explored-материала,
                // но управляется чисто параметрами материала)
                if (_CloudsEnabled > 0.5)
                {
                    // Берём те же базовые координаты, что и для бумаги/рваных краёв,
                    // без tiling через frac, чтобы облака "лежали" на карте стабильно.
                    float2 baseCloudCoord = (_OriginalPosition.xy + i.worldOffset) * _CloudsScale;
                    
                    // Два слоя шума с разным масштабом и смещением по времени
                    float t = _Time.y * _CloudsSpeed;
                    float n1 = smoothNoise2D(baseCloudCoord + float2(0.0, t));
                    float n2 = smoothNoise2D(baseCloudCoord * 2.0 + float2(t, 0.0));
                    
                    // Смешиваем слои в одно поле облаков
                    float cloudsRaw = n1 * 0.6 + n2 * 0.4;
                    
                    // Выделяем "облачные" участки — порог + плавный переход
                    // Чем ближе к 1, тем плотнее облако.
                    float cloudsMask = saturate((cloudsRaw - 0.4) / 0.6);
                    
                    // Итоговая сила облака (маска * интенсивность)
                    float cloudsStrength = _CloudsIntensity * cloudsMask;
                    
                    // Немного поднимаем "дымку": приглушаем исходный цвет и подмешиваем светлый туман
                    float3 foggedColor = col.rgb * 0.6 + float3(1.0, 1.0, 1.0) * 0.4;
                    col.rgb = lerp(col.rgb, foggedColor, cloudsStrength);
                }
                
                // Вычисляем SDF для определения расстояния до края (нужно для тления)
                float hexSDFValue = hexSDF(i.localPos, _HexRadius);
                float modifiedSDF = hexSDFValue;
                float edgeFade = 1.0;
                bool isRaggedEdgeActive = false;
                
                // Применяем неровные края, если эффект включен
                if (_RaggedEdgesEnabled > 0.5)
                {
                    // Определяем ближайшую грань
                    int closestFace = getClosestFace(i.localPos, _HexRadius);
                    
                    // Проверяем, включен ли эффект для этой грани
                    float faceEnabled = getFaceEnabled(closestFace);
                    
                    // Применяем эффект только если он включен для этой грани
                    if (faceEnabled > 0.5)
                    {
                        isRaggedEdgeActive = true;
                        
                        // Генерируем шум на основе тех же координат, что и основная текстура:
                        // _OriginalPosition + worldOffset (без tiling/frac), чтобы рисунок рваных краёв
                        // и огня был привязан к карте так же, как бумага.
                        float2 baseCoord = (_OriginalPosition.xy + i.worldOffset) * _RaggedEdgesScale;
                        float2 noiseCoord = baseCoord;
                        float noiseValue = smoothNoise2D(noiseCoord);
                        
                        // Преобразуем шум из [0, 1] в [-intensity, +intensity]
                        // Это создаст неровности, которые будут "вдавливать" и "выдавливать" края
                        float noiseOffset = (noiseValue - 0.5) * 2.0 * _RaggedEdgesIntensity * _HexRadius;
                        
                        // Применяем шум к SDF: если SDF + offset < 0, пиксель видим
                        // Если SDF + offset >= 0, пиксель обрезаем (альфа = 0)
                        modifiedSDF = hexSDFValue + noiseOffset;
                        
                        // Если модифицированный SDF положительный (снаружи с учетом шума), обрезаем пиксель
                        if (modifiedSDF > 0.0)
                        {
                            col.a = 0.0;
                        }
                        else
                        {
                            // Плавный переход на краю для более мягкого обрезания
                            // Используем smoothstep для создания плавного перехода
                            edgeFade = smoothstep(0.0, -_RaggedEdgesIntensity * _HexRadius * 0.5, modifiedSDF);
                            col.a *= edgeFade;
                        }
                    }
                }
                
                // Применяем эффект тления вдоль рваного края
                // Эффект работает только если неровные края включены и пиксель видим
                if (_RaggedEdgesEnabled > 0.5 && isRaggedEdgeActive && col.a > 0.001)
                {
                    // Определяем расстояние до модифицированного края (modifiedSDF)
                    // Используем отрицательное значение modifiedSDF (внутри гекса с учетом шума) для определения близости к неровному краю
                    float distToRaggedEdge = -modifiedSDF; // Расстояние от неровного края внутрь (положительное значение)
                    
                    // Нормализуем расстояние: 0 на неровном краю (где modifiedSDF = 0), увеличивается внутрь
                    // Используем _GlowWidth для определения зоны тления
                    float glowZone = _GlowWidth * _HexRadius;
                    
                    // Вычисляем фактор тления: максимален на неровном краю, затухает внутрь
                    // Используем smoothstep для более плавного перехода
                    float glowFactor = smoothstep(glowZone, 0.0, distToRaggedEdge);
                    
                    // Добавляем мерцание на основе времени и шума
                    // Используем те же базовые координаты, что и для неровных краёв / основной текстуры,
                    // чтобы огонь был привязан к оригинальным координатам клетки.
                    float2 baseCoord = (_OriginalPosition.xy + i.worldOffset) * _RaggedEdgesScale;
                    float2 flickerCoord = baseCoord + _Time.y * _GlowFlickerSpeed;
                    float flickerNoise = smoothNoise2D(flickerCoord);
                    
                    // Мерцание: плавное колебание от 0.7 до 1.0
                    float flicker = lerp(0.7, 1.0, flickerNoise);
                    
                    // Добавляем дополнительное мерцание на основе времени
                    float timeFlicker = sin(_Time.y * _GlowFlickerSpeed * 2.0) * 0.15 + 0.85;
                    flicker *= timeFlicker;
                    
                    // Применяем эффект тления с плавным затуханием
                    float glowStrength = glowFactor * _GlowIntensity * flicker;
                    
                    // Смешиваем цвет тления с основным цветом (аддитивное смешивание)
                    col.rgb += _GlowColor.rgb * glowStrength;
                }
                
                return col;
            }
            ENDCG
        }
    }
    
    FallBack "Diffuse"
}

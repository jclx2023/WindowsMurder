Shader "Custom/JitterPixelSprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Pixelation Settings)]
        _PixelResolution ("Pixel Resolution", Float) = 64.0
        
        [Header(Jitter Settings)]
        _JitterStrength ("Jitter Strength", Range(0, 2.0)) = 0.5
        _JitterSpeedX ("Jitter Speed X", Float) = 20.0
        _JitterSpeedY ("Jitter Speed Y", Float) = 25.0
        _SpatialFreqX ("Spatial Frequency X", Float) = 100.0
        _SpatialFreqY ("Spatial Frequency Y", Float) = 100.0
        
        [Header(Advanced)]
        [Toggle] _UsePixelSnap ("Use Pixel Snap", Float) = 0
        [Toggle] _EnableJitter ("Enable Jitter", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
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
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // 材质属性
            sampler2D _MainTex;
            sampler2D _AlphaTex;
            fixed4 _Color;
            float _PixelResolution;
            float _JitterStrength;
            float _JitterSpeedX;
            float _JitterSpeedY;
            float _SpatialFreqX;
            float _SpatialFreqY;
            float _UsePixelSnap;
            float _EnableJitter;
            float4 _MainTex_TexelSize;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                
                #ifdef PIXELSNAP_ON
                if (_UsePixelSnap > 0.5)
                {
                    OUT.vertex = UnityPixelSnap(OUT.vertex);
                }
                #endif

                return OUT;
            }

            // 像素化函数
            float2 pixelateUV(float2 uv, float resolution)
            {
                return floor(uv * resolution) / resolution;
            }

            // 简单随机数生成器
            float random(float2 st)
            {
                return frac(sin(dot(st.xy, float2(12.9898, 78.233))) * 43758.5453123);
            }

            // 简化的2D噪声函数
            float simpleNoise(float2 st)
            {
                float2 i = floor(st);
                float2 f = frac(st);

                // 四个角的随机值
                float a = random(i);
                float b = random(i + float2(1.0, 0.0));
                float c = random(i + float2(0.0, 1.0));
                float d = random(i + float2(1.0, 1.0));

                // 平滑插值
                float2 u = smoothstep(0.0, 1.0, f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            // 不规则抖动偏移计算
            float2 calculateJitter(float2 uv, float time)
            {
                if (_EnableJitter < 0.5)
                    return float2(0, 0);

                // 多个不同频率的噪声层
                float timeScale = time * 0.1;
                
                // 主要抖动 - 基于噪声
                float2 seed1 = uv * 8.0 + timeScale * float2(_JitterSpeedX, _JitterSpeedY);
                float noiseX = simpleNoise(seed1) - 0.5;
                float noiseY = simpleNoise(seed1 + float2(100.0, 200.0)) - 0.5;
                
                // 细节抖动 - 更高频率
                float2 seed2 = uv * 20.0 + timeScale * float2(_JitterSpeedY, _JitterSpeedX) * 1.3;
                float detailX = (simpleNoise(seed2) - 0.5) * 0.3;
                float detailY = (simpleNoise(seed2 + float2(300.0, 400.0)) - 0.5) * 0.3;
                
                // 组合原有的sin/cos（降低权重）以保持一些可预测性
                float sinJitter = sin(time * _JitterSpeedX * 0.5 + uv.y * _SpatialFreqY * 0.01) * 0.2;
                float cosJitter = cos(time * _JitterSpeedY * 0.5 + uv.x * _SpatialFreqX * 0.01) * 0.2;
                
                // 最终组合：噪声为主，sin/cos为辅
                float2 jitter = float2(
                    noiseX * 0.6 + detailX + sinJitter,
                    noiseY * 0.6 + detailY + cosJitter
                );

                // 标准化抖动强度
                jitter *= _JitterStrength / _PixelResolution;
                
                return jitter;
            }

            // 安全纹理采样函数
            fixed4 sampleTextureSafe(sampler2D tex, float2 uv)
            {
                // 将UV坐标钳制在[0,1]范围内，避免采样越界
                uv = saturate(uv);
                return tex2D(tex, uv);
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.texcoord;
                
                // 第一步：像素化处理
                float2 pixelatedUV = pixelateUV(uv, _PixelResolution);
                
                // 第二步：添加抖动偏移
                float2 jitterOffset = calculateJitter(pixelatedUV, _Time.y);
                float2 finalUV = pixelatedUV + jitterOffset;
                
                // 第三步：安全采样纹理
                fixed4 texColor = sampleTextureSafe(_MainTex, finalUV);
                
                #if ETC1_EXTERNAL_ALPHA
                // 处理ETC1格式的外部Alpha通道
                fixed4 alpha = sampleTextureSafe(_AlphaTex, finalUV);
                texColor.a = alpha.r;
                #endif
                
                // 应用颜色tint
                texColor *= IN.color;
                
                return texColor;
            }
            ENDCG
        }
    }
    
    Fallback "Sprites/Default"
}
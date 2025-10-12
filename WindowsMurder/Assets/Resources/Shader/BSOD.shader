Shader "Custom/BSOD_CRT"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _TimeScale ("Time Scale", Float) = 1.0
        _ScanlineIntensity ("Scanline Intensity", Range(0,1)) = 0.2
        _ChromaticAberration ("RGB Offset", Range(0,3)) = 1.0
        _FlickerIntensity ("Flicker Intensity", Range(0,1)) = 0.1
        _BlurAmount ("Blur Amount", Range(0,2)) = 0.3
        _BlueColor ("Base Blue", Color) = (0,0.2,0.8,1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            ZTest Always Cull Off ZWrite Off

            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _TimeScale;
            float _ScanlineIntensity;
            float _ChromaticAberration;
            float _FlickerIntensity;
            float _BlurAmount;
            float4 _BlueColor;

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 uv = i.uv;

                // 模拟轻微的扫描线干扰
                float scan = sin(uv.y * 800.0 + _Time * 50.0 * _TimeScale);
                float flicker = (sin(_Time * 120.0) * 0.5 + 0.5) * _FlickerIntensity;

                // 模拟RGB偏移
                float2 offset = float2(_ChromaticAberration * 0.001, 0);
                float r = tex2D(_MainTex, uv + offset).r;
                float g = tex2D(_MainTex, uv).g;
                float b = tex2D(_MainTex, uv - offset).b;

                // 模糊（邻域取样）
                float4 sum = 0;
                float2 blurStep = _MainTex_TexelSize.xy * _BlurAmount;
                [unroll]
                for(int x=-1; x<=1; x++)
                {
                    [unroll]
                    for(int y=-1; y<=1; y++)
                    {
                        sum += tex2D(_MainTex, uv + float2(x,y)*blurStep);
                    }
                }
                sum /= 9.0;

                // 蓝色主调
                float3 blueTint = _BlueColor.rgb;
                float3 col = lerp(sum.rgb, float3(r,g,b), 0.5);
                col *= (blueTint + flicker * 0.1);
                col *= 1.0 - _ScanlineIntensity * (0.5 + 0.5 * scan);

                return float4(col, 1.0);
            }
            ENDCG
        }
    }
}

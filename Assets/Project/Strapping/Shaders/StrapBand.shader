Shader "Project/Strapping/StrapBand"
{
    Properties
    {
        _BaseColor ("带体颜色", Color) = (0.12, 0.35, 0.72, 1)
        _Smoothness ("光滑度", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _Smoothness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half3 normalWS : TEXCOORD0;
                half3 color : TEXCOORD1;
            };

            Varyings vert(Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                // 纵向条纹感（PP 打包带）
                half stripe = step(0.5, frac(i.uv.x * 24.0));
                o.color = _BaseColor.rgb * lerp(0.92h, 1.08h, stripe);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                Light mainLight = GetMainLight();
                half ndl = saturate(dot(normalize(i.normalWS), mainLight.direction));
                half3 lit = i.color * (0.35h + ndl * 0.65h);
                return half4(lit, 1);
            }
            ENDHLSL
        }
    }
}

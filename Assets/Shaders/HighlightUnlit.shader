Shader "Custom/HighlightOutline"
{
    Properties
    {
        _Color ("Outline Color", Color) = (1,1,0,1)
        _Thickness ("Thickness", Float) = 0.03
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Overlay" }

        Pass
        {
            ZWrite Off
            ZTest Always
            Cull Front            // рисуем задние грани, чтобы получить ободок
            Blend SrcAlpha One

            // рисуем только там, где stencil != 1 (т.е. вне оригинального силуэта)
            Stencil
            {
                Ref 1
                Comp NotEqual
                Pass Keep
                ReadMask 255
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _Color;
            float _Thickness;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            Varyings vert (Attributes v)
            {
                Varyings o;
                float3 n = normalize(v.normalOS);
                float3 expanded = v.positionOS.xyz + n * _Thickness;
                o.positionHCS = TransformObjectToHClip(expanded);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                return half4(_Color.rgb, _Color.a);
            }
            ENDHLSL
        }
    }
}

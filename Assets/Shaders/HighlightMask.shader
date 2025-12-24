Shader "Custom/HighlightMask"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Overlay" }

        Pass
        {
            // не записываем цвет
            ColorMask 0

            // хотим пометить всю проекцию объекта (видно сквозь всЄ)
            ZWrite Off
            ZTest Always

            // пишем 1 в stencil дл€ всех пикселей объекта
            Stencil
            {
                Ref 1
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings  { float4 positionHCS : SV_POSITION; };

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                // ColorMask 0 Ч цвет не пишетс€, но фрагмент должен возвращать что-то
                return half4(0,0,0,0);
            }
            ENDHLSL
        }
    }
}

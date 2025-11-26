Shader "Custom/AspectFitUnlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BackgroundColor ("Background Color", Color) = (0,0,0,1)
        _PlaneAspect ("Plane Aspect", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            ZWrite On
            Cull Off
            Lighting Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize; // z = tex width, w = tex height
            fixed4 _BackgroundColor;
            float _PlaneAspect;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float imgW = _MainTex_TexelSize.z;
                float imgH = _MainTex_TexelSize.w;

                if (imgW <= 0 || imgH <= 0) { imgW = 1; imgH = 1; }

                float imageAspect = imgW / imgH;
                float planeAspect = max(0.0001, _PlaneAspect);

                float2 uv = i.uv;

                if (imageAspect > planeAspect)
                {
                    float imageFracV = planeAspect / imageAspect;
                    float borderV = (1.0 - imageFracV) * 0.5;

                    if (uv.y < borderV || uv.y > 1.0 - borderV)
                    {
                        return _BackgroundColor;
                    }

                    float v = (uv.y - borderV) / imageFracV;
                    float u = uv.x;
                    fixed4 col = tex2D(_MainTex, float2(u, v));
                    return col;
                }
                else
                {
                    float imageFracU = imageAspect / planeAspect;
                    float borderU = (1.0 - imageFracU) * 0.5;

                    if (uv.x < borderU || uv.x > 1.0 - borderU)
                    {
                        return _BackgroundColor;
                    }
                    float u = (uv.x - borderU) / imageFracU;
                    float v = uv.y;
                    fixed4 col = tex2D(_MainTex, float2(u, v));
                    return col;
                }
            }
            ENDCG
        }
    }
}

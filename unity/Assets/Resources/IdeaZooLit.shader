Shader "IdeaZoo/RuntimeLit"
{
    Properties
    {
        _BaseMap ("Pattern", 2D) = "white" {}
        _BaseColor ("Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite On

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
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _BaseMap;
            float4 _BaseMap_ST;
            fixed4 _BaseColor;

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                return tex2D(_BaseMap, input.uv) * _BaseColor;
            }
            ENDCG
        }
    }
}

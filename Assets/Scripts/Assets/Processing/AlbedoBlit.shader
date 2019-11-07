/*
 * Shader for processing the Albedo Compact Material Map
 */
Shader "Processing/AlbedoBlit"
{
    Properties
    {
        _AlbedoMap ("Albedo Map", 2D) = "white" {}
        _AlbedoColor( "Albedo Color", Color) = (0,0,0,0)
        _EmissionMap ("Emission Map", 2D) = "white" {}
        _EmissionColor( "Emission Color", Color) = (0,0,0,0)
        _OcclusionMap ("Occlusion Map", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "./ProcessingHelper.cginc"

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
                o.uv = v.uv;
                return o;
            }

            sampler2D _AlbedoMap, _EmissionMap, _OcclusionMap;
            float4 _AlbedoColor, _EmissionColor;

            fixed4 frag (v2f i) : SV_Target
            {
                //return float4(0,0,0,1);
                fixed4 albedo = tex2D(_AlbedoMap, i.uv) * _AlbedoColor * grayscale(tex2D(_OcclusionMap, i.uv));
                fixed4 emission = tex2D(_EmissionMap, i.uv) * _EmissionColor;
                float emissionStrength = grayscale(_EmissionColor);
                float3 color = (albedo + emission * emissionStrength) / (1 + emissionStrength);
                return float4(color, emissionStrength);
            }
            ENDCG
        }
    }
}

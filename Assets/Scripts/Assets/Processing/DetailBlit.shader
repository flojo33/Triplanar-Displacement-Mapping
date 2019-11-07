/*
 * Shader for processing the Detail Compact Material Map
 */
Shader "Processing/DetailBlit"
{
    Properties
    {
        //Red
        _SpecularMap ("Specular Map", 2D) = "white" {}
        _SpecularStrength( "Specular Strength", Float) = 1
        _SpecularPower( "Specular Power", Float) = 1
        //Green
        _MetallicMap ("Metallic Map", 2D) = "white" {}
        _MetallicStrength( "Metallic Strength", Float) = 1
        
        //Blue + Alpha
        _DetailNormalStrength( "Detail normal Strength", Float) = 1
        _DetailNormalMap ("DetailNormalMap", 2D) = "normal" {}
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

            sampler2D _SpecularMap, _MetallicMap, _DetailNormalMap;
            float _SpecularStrength, _SpecularPower,_MetallicStrength, _DetailNormalStrength;

            fixed4 frag (v2f i) : SV_Target
            {
                float3 detailNormalSample = UnpackNormal( tex2Dlod(_DetailNormalMap, float4(i.uv,0,0)));
                detailNormalSample.xy *= _DetailNormalStrength;
                detailNormalSample = normalize(detailNormalSample);
                float specular = pow(grayscale(tex2D(_SpecularMap, i.uv) * _SpecularStrength), _SpecularPower);
                float metallic = grayscale(tex2D(_MetallicMap, i.uv) * _MetallicStrength);
                return float4(specular, metallic, detailNormalSample.x * 0.5f + 0.5f, detailNormalSample.y * 0.5f + 0.5f);
            }
            ENDCG
        }
    }
}

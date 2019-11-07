/*
 * Shader for processing the Surface Compact Material Map
 */
Shader "Processing/SurfaceBlit"
{
    Properties
    {
        _HeightMap ("Height Map", 2D) = "white" {}
        _HeightStrength ("Height Strength", Float) = 1.0
        _HeightOffset ("Height Offset", Float) = 1.0
        _HeightMax ("Height Max", Float) = 1.0
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

            sampler2D _HeightMap;
            float4 _HeightMap_TexelSize;
            float _HeightStrength, _HeightOffset, _HeightMax;

            float4 frag (v2f i) : SV_Target
            {
                float height = min(1,max(0,lerp(0.5,grayscale(tex2Dlod(_HeightMap, float4(i.uv,0,0))),_HeightStrength) + _HeightOffset));
                float3 mainNormalVector;
                if(_HeightStrength <= 0) {
                    mainNormalVector = float3(0,1,0);    
                }
                else
                {
                    float halfTexelX = _HeightMap_TexelSize.x / 2.0;
                    float halfTexelY = _HeightMap_TexelSize.y / 2.0;
                    uint sizeX = _HeightMap_TexelSize.z;
                    uint sizeY = _HeightMap_TexelSize.w;
                    uint curX = uint((i.uv.x - halfTexelX) / _HeightMap_TexelSize.x) + sizeX;
                    uint curY = uint((i.uv.y - halfTexelY) / _HeightMap_TexelSize.y) + sizeY;
                    
                    float tl = grayscale(tex2Dlod(_HeightMap, float4(((curX + 1) % sizeX) * _HeightMap_TexelSize.x + halfTexelX, ((curY + 1) % sizeY) * _HeightMap_TexelSize.y + halfTexelY,0, 0)));
                    float l =  grayscale(tex2Dlod(_HeightMap, float4(((curX + 1) % sizeX) * _HeightMap_TexelSize.x + halfTexelX, ((curY    ) % sizeY) * _HeightMap_TexelSize.y + halfTexelY,0, 0)));
                    float bl = grayscale(tex2Dlod(_HeightMap, float4(((curX + 1) % sizeX) * _HeightMap_TexelSize.x + halfTexelX, ((curY - 1) % sizeY) * _HeightMap_TexelSize.y + halfTexelY,0, 0)));
                    float b =  grayscale(tex2Dlod(_HeightMap, float4(((curX    ) % sizeX) * _HeightMap_TexelSize.x + halfTexelX, ((curY - 1) % sizeY) * _HeightMap_TexelSize.y + halfTexelY,0, 0)));
                    float br = grayscale(tex2Dlod(_HeightMap, float4(((curX - 1) % sizeX) * _HeightMap_TexelSize.x + halfTexelX, ((curY - 1) % sizeY) * _HeightMap_TexelSize.y + halfTexelY,0, 0)));
                    float r =  grayscale(tex2Dlod(_HeightMap, float4(((curX - 1) % sizeX) * _HeightMap_TexelSize.x + halfTexelX, ((curY    ) % sizeY) * _HeightMap_TexelSize.y + halfTexelY,0, 0)));
                    float tr = grayscale(tex2Dlod(_HeightMap, float4(((curX - 1) % sizeX) * _HeightMap_TexelSize.x + halfTexelX, ((curY + 1) % sizeY) * _HeightMap_TexelSize.y + halfTexelY,0, 0)));
                    float t =  grayscale(tex2Dlod(_HeightMap, float4(((curX    ) % sizeX) * _HeightMap_TexelSize.x + halfTexelX, ((curY + 1) % sizeY) * _HeightMap_TexelSize.y + halfTexelY,0, 0)));
                    
                    float dX = tr + 2 * r + br - tl - 2 * l - bl;
    
                    // Compute dy using Sobel:
                    //           -1 -2 -1 
                    //            0  0  0
                    //            1  2  1
                    float dY = bl + 2 * b + br - tl - 2 * t - tr;
                   
                    mainNormalVector = normalize(float3(dX,  1/(_HeightStrength*_HeightMax), dY));
                }
                return float4(mainNormalVector.x * 0.5f + 0.5f, mainNormalVector.z * 0.5f + 0.5f,height,0);
            }
            ENDCG
        }
    }
}

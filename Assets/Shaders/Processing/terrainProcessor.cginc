#include "./noise.cginc"
#include "UnityPBSLighting.cginc"
#include "AutoLight.cginc"
#include "./../Utilities.cginc"
#include "./../SplatUtilities.cginc"

float2 _offset;
float _pow1, _pow2, _pow3, _pow4, _pow5, _pow6, _pow7;
float _scale1, _scale2, _scale3, _scale4, _scale5, _scale6, _scale7;
float _height1, _height3, _height4, _height5;
float _scaleMultiplier, _weightMultiplier;

float _temperatureScale, _temperatureBlend, _temperatureRandom;
float _sandRandom, _sandBlend;
float _snowRandom, _snowBlend;
float4 _maxTessellationStrengths[4];

//Base shader requirements...
float _DisplacementStrength;
float _DisplacementDistance;

float Perlin(float2 position, float2 offset, float scale, float power) {
    return pow(abs(cnoise(position * scale + offset) / 2 + 0.5), power);
}

float GetHeightAtPoint(float2 position) {
    float h1 = Perlin(position, _offset, _scale1, _pow1) * 
             Perlin(position, _offset, _scale2, _pow2) * _height1;
    float h3 = Perlin(position, _offset, _scale3, _pow3) * _height3;
    float h4 = 0.5f * (Perlin(position, _offset, _scale4, _pow4) + 
                     Perlin(position, _offset, _scale4 * 2, _pow4)) * _height4;
    float h5 = Perlin(position, _offset, _scale5, _pow5) * _height5;
    float mul1 = Perlin(position, _offset, _scale6, _pow6);
    float mul2 = Perlin(position, _offset, _scale7, _pow7) * _weightMultiplier;
    return (h1 + h3 + h4 + h5) * (1 + (mul1 + mul2) / (1 + _weightMultiplier) * _scaleMultiplier);
}

float BlendSplat(float input, float factor, float offset)
{
    return max(0,min(1,((input - offset) * factor + 0.5)));
}

float4 GetSplatAtPoint(float2 position, float height) {
    //return float4(0,1,0,0);
    float maxHeight = _height1 + _height3 + _height4 + _height5;
    
    float sandStrength = BlendSplat(1 - height/maxHeight + Perlin(position, _offset, _scale4 * 2, _pow4) * _sandRandom, _sandBlend, 0.99);
    float snowStrength = (1-sandStrength) * BlendSplat(height/maxHeight + Perlin(position, _offset, _scale4 * 2, _pow4) * _snowRandom, _snowBlend, 0.6);

    float temperature = BlendSplat((Perlin(position, _offset, _temperatureScale, 1) * ((1 + (Perlin(position, _offset, _temperatureScale * 20, 1) - 0.5) * _temperatureRandom))), _temperatureBlend, 0.5);
    float remainingStrength = (1 - (snowStrength + sandStrength));
    return float4(remainingStrength * temperature, remainingStrength * (1 - temperature), sandStrength, snowStrength);
}

float GetTessellationStrength(float4 splat, float3 normal) {
    float3 blend = GetTriplanarBlend(normal);
    float m = 0;
    for(int i = 0; i < 4; i++)
    {
        if(splat[i] > 0) 
        {
            if(blend.y != 0) 
            {
                if(sign(blend.y) > 0) 
                {
                    m = max(m, blend.y * _maxTessellationStrengths[i].x);
                }
                else 
                {
                    m = max(m, blend.y * _maxTessellationStrengths[i].z);
                }
            }
            if(blend.x != 0 || blend.z != 0)
            {
                m = max(m, (blend.x + blend.z) * 0.5f * _maxTessellationStrengths[i].y);
            }
        }
    }
	return m;
}

float GetDisplacementAtPoint(float3 position, float3 normal, float4 splat) {
    triplanarUv tUv = getTriplanarVertexUv(position, normal);
    float4 splatX = float4(0,0,0,0);
    float4 splatY = float4(0,0,0,0);
    float4 splatZ = float4(0,0,0,0);
    float4 splatBlend = GetSplatBlend(splat);
    float height = sampleVertexHeight(splatBlend, normal, tUv, splatX, splatY, splatZ, 0);
	return scaleDisplacementValue(height, _DisplacementStrength * _DisplacementDistance);
}
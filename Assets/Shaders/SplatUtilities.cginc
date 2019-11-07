#if !defined( SPLAT_UTILITIES_LOADED )
#define SPLAT_UTILITIES_LOADED

//--------------------------------------------------------------------------
// Splat input Properties set by Terrain Generator
//--------------------------------------------------------------------------
//#define TRIPLANAR_DEMO
#if defined(TRIPLANAR_DEMO)
    #define side_x 0
    #define side_y_top 1
    #define side_y_bottom 1
    #define side_z 2
#else
    #define side_x 1
    #define side_y_top 0
    #define side_y_bottom 2
    #define side_z 1
#endif

//2D texture containing the splat rgba for the current tile
sampler2D _SplatMap;
float4 _SplatMap_ST;

//Texture2DArray containing albedo, detail and normal maps of every splat
UNITY_DECLARE_TEX2DARRAY(_SplatArray);

//Offsets of every side (3) of every splat (4) for both texture types (2)
float4 _SplatArrayOffsets[24];

float _TriplanarHeightBlendStrength;
float _SplatHeightBlendStrength;

float4 SampleSplatTexture(float2 uv) {
    return GetSplatBlend(tex2Dlod(_SplatMap, float4(uv,0,0)));
}
//--------------------------------------------------------------------------
// type: 0 = albedo, 1 = detail, 2 = normal
// side: 0 = top, 1 = sides, 2 = bottom
// uv: 2D uv coordinate to sample from
// sampleLod: sample using mipMaps or using the base texture
//--------------------------------------------------------------------------
float3 GetTextureOffset(int type, int splat, int side, float2 uv) {
    int offsetId = splat * 3 + side;
    int dispalcementOffset = type==2?0:1;
    int offsetTexture = splat * 6 + side * 2 + dispalcementOffset;
    float2 offset = _SplatArrayOffsets[offsetTexture].zw;
    float2 scale = _SplatArrayOffsets[offsetTexture].xy;
    return float3(uv * scale + offset, offsetId * 3 + type);
}

float4 SampleTextureArray(int type, int splat, int side, float2 uv, inout uint sampleCount) {
    sampleCount ++;
    return UNITY_SAMPLE_TEX2DARRAY(_SplatArray, GetTextureOffset(type, splat, side, uv));
}

float4 SampleTextureArrayLod(int type, int splat, int side, float2 uv, float lod) {
    return UNITY_SAMPLE_TEX2DARRAY_LOD(_SplatArray, GetTextureOffset(type, splat, side, uv), lod);
}

float BlendHeight(float factor, float4 heights, float4 contributions, inout float x, inout float y, inout float z, inout float w) {
     float4 combinedStrength = heights + contributions;
     float ma = max(max(max(combinedStrength.x,combinedStrength.y),combinedStrength.z),combinedStrength.w) - factor;
     x = max(combinedStrength.r - ma, 0);
     y = max(combinedStrength.g - ma, 0);
     z = max(combinedStrength.b - ma, 0);
     w = max(combinedStrength.a - ma, 0);
     float sum = x+y+z+w;
     if(sum > 0) {
         x /= sum;
         y /= sum;
         z /= sum;
         w /= sum;
     }
     return heights.x * x + heights.y * y + heights.z * z + heights.w * w;
}

float3 UnpackNormal(float2 compressedNormal) {
    compressedNormal = (compressedNormal * 2) - 1;
    float y = sqrt(1.0 - saturate(dot(compressedNormal.xy, compressedNormal.xy)));
    return float3(compressedNormal.x, compressedNormal.y, y);
}

//---------------------------------------------------------------------
// Reoriented Normal Mapping as described in
// http://blog.selfshadow.com/publications/blending-in-detail/
//---------------------------------------------------------------------
float3 ReorientedNormalBlend(float3 baseNormal, float3 detailNormal)
{
    baseNormal.z += 1;
    detailNormal.xy = -detailNormal.xy;

    return baseNormal * dot(baseNormal, detailNormal) / baseNormal.z - detailNormal;
}

//---------------------------------------------------------------------
// Triplanar Reoriented Normal Mapping as described in
// https://medium.com/@bgolus/normal-mapping-for-a-triplanar-shader-10bf39dca05a
//---------------------------------------------------------------------
float3 TriplanarReorientedNormalMapping(float3 blend, float3 baseNormal, float3 axisSigns, float3 tangentNormalX, float3 tangentNormalY, float3 tangentNormalZ) {
    float3 absVertNormal = abs(baseNormal);
    
    //swizzle world normals to match tangent space and apply reoriented normal blend    
    tangentNormalX = ReorientedNormalBlend(float3(baseNormal.zy, absVertNormal.x), tangentNormalX);
    tangentNormalY = ReorientedNormalBlend(float3(baseNormal.xz, absVertNormal.y), tangentNormalY);
    tangentNormalZ = ReorientedNormalBlend(float3(baseNormal.xy, absVertNormal.z), tangentNormalZ);

    // apply world space sign to tangent space Z
    tangentNormalX.z *= axisSigns.x;
    tangentNormalY.z *= axisSigns.y;
    tangentNormalZ.z *= axisSigns.z;

    // sizzle tangent normals to match world normal and blend together
    return normalize(
        tangentNormalX.zyx * blend.x + 
        tangentNormalY.xzy * blend.y + 
        tangentNormalZ.xyz * blend.z 
        //+ baseNormal
    );
}

inline float sampleVertexHeightTriplanar(triplanarUv uvs, int splat, inout float4 x, inout float4 y, inout float4 z, float lod) {
    float3 heights;
    heights.x = SampleTextureArrayLod(2, splat, side_x, uvs.x, lod).b;
    if(uvs.axisSigns.y > 0) {
        heights.y = SampleTextureArrayLod(2, splat, side_y_top, uvs.y, lod).b;
    } else {
        heights.y = SampleTextureArrayLod(2, splat, side_y_bottom, uvs.y, lod).b;
    }
    heights.z = SampleTextureArrayLod(2, splat, side_z, uvs.z, lod).b;
    float w; //dummy for using the blend function with four values
    return BlendHeight(_TriplanarHeightBlendStrength, float4(heights,0), float4(uvs.strength,0), x[splat], y[splat], z[splat], w);
}

inline float sampleVertexHeight(inout float4 splat, float3 normal, triplanarUv tUv, inout float4 x, inout float4 y, inout float4 z, float lod) {
    float4 heights = float4(0,0,0,0);
    if(splat.r > 0) {
        heights.r = sampleVertexHeightTriplanar(tUv, 0, x, y, z, lod);
    }
    if(splat.g > 0) {
        heights.g = sampleVertexHeightTriplanar(tUv, 1, x, y, z, lod);
    }
    if(splat.b > 0) {
        heights.b = sampleVertexHeightTriplanar(tUv, 2, x, y, z, lod);
    }
    if(splat.a > 0) {
        heights.a = sampleVertexHeightTriplanar(tUv, 3, x, y, z, lod);
    }
    return BlendHeight(_SplatHeightBlendStrength, heights, splat, splat.x, splat.y, splat.z, splat.w);
}

void sampleNormalBaseTriplanar(inout float3 fullNormal, triplanarUv uvs, int splat, float3 surfaceNormal, float splatStrength, float x, float y, float z, inout uint sampleCount) {
    if(splatStrength > 0) {
        float3 tnormalX = float3(0,0,0);
        float3 tnormalY = float3(0,0,0);
        float3 tnormalZ = float3(0,0,0);
        float3 blend = float3(x,y,z);
        if(blend.x > 0) {
            tnormalX = UnpackNormal(SampleTextureArray(2, splat, side_x, uvs.x, sampleCount).xy);
        }
        if(blend.y > 0) {
            if(uvs.axisSigns.y > 0) {
                tnormalY = UnpackNormal(SampleTextureArray(2, splat, side_y_top, uvs.y, sampleCount).xy);
            } else {
                tnormalY = UnpackNormal(SampleTextureArray(2, splat, side_y_bottom, uvs.y, sampleCount).xy);
            }
        }
        if(blend.z > 0) {
            tnormalZ = UnpackNormal(SampleTextureArray(2, splat, side_z, uvs.z, sampleCount).xy);
        }
        
        // sizzle tangent normals to match world normal and blend together
        fullNormal += splatStrength * TriplanarReorientedNormalMapping(blend, surfaceNormal, uvs.axisSigns, tnormalX, tnormalY, tnormalZ);
    }
}

float3 sampleNormalBase(triplanarUv uvs, float3 normal, float4 splat, float4 splatX, float4 splatY, float4 splatZ, inout uint sampleCount) {
    // tangent space normal maps
    float3 n = float3(0,0,0);
    sampleNormalBaseTriplanar(n, uvs, 0, normal, splat.r, splatX.r, splatY.r, splatZ.r, sampleCount);
    sampleNormalBaseTriplanar(n, uvs, 1, normal, splat.g, splatX.g, splatY.g, splatZ.g, sampleCount);
    sampleNormalBaseTriplanar(n, uvs, 2, normal, splat.b, splatX.b, splatY.b, splatZ.b, sampleCount);
    sampleNormalBaseTriplanar(n, uvs, 3, normal, splat.a, splatX.a, splatY.a, splatZ.a, sampleCount);
    return normalize(n);
}
#if defined(SINGLE_BLEND_ON)
void sampleMaterialMapsTriplanar(inout float4 albedo, inout float4 details, inout float3 normal, float4 triplanarWeight, float4 splatWeights, float2 uv, int side, inout uint sampleCount) {
    float4 tempDetails;
    float4 blendStrengths = triplanarWeight * splatWeights;
    if(blendStrengths.r > 0) {
        albedo += blendStrengths.r * SampleTextureArray(0, 0, side, uv, sampleCount);
        tempDetails = SampleTextureArray(1, 0, side, uv, sampleCount);
        details += float4(blendStrengths.r * tempDetails.xy,0,0);
        normal += blendStrengths.r * UnpackNormal(tempDetails.zw);
    }
    if(blendStrengths.g > 0) {
        albedo += blendStrengths.g * SampleTextureArray(0, 1, side, uv, sampleCount);
        tempDetails = SampleTextureArray(1, 1, side, uv, sampleCount);
        details += float4(blendStrengths.g * tempDetails.xy,0,0);
        normal += blendStrengths.g * UnpackNormal(tempDetails.zw);
    }
    if(blendStrengths.b > 0) {
        albedo += blendStrengths.b * SampleTextureArray(0, 2, side, uv, sampleCount);
        tempDetails = SampleTextureArray(1, 2, side, uv, sampleCount);
        details += float4(blendStrengths.b * tempDetails.xy,0,0);
        normal += blendStrengths.b * UnpackNormal(tempDetails.zw);
    }
    if(blendStrengths.a > 0) {
        albedo += blendStrengths.a * SampleTextureArray(0, 3, side, uv, sampleCount);
        tempDetails = SampleTextureArray(1, 3, side, uv, sampleCount);
        details += float4(blendStrengths.a * tempDetails.xy,0,0);
        normal += blendStrengths.a * UnpackNormal(tempDetails.zw);
    }
}

void sampleMaterialMaps(triplanarUv uvs, float4 splatWeights, float4 splatX, float4 splatY, float4 splatZ, inout float4 albedo, inout float4 details, inout float3 normal, inout uint sampleCount) {
    float3 tnormalX = float3(0,0,0);
    float3 tnormalY = float3(0,0,0);
    float3 tnormalZ = float3(0,0,0);
    sampleMaterialMapsTriplanar(albedo, details, tnormalX, splatX, splatWeights, uvs.x, side_x, sampleCount);
    if(uvs.axisSigns.y > 0) {
        sampleMaterialMapsTriplanar(albedo, details, tnormalY, splatY, splatWeights, uvs.y, side_y_top, sampleCount);
    } else {
        sampleMaterialMapsTriplanar(albedo, details, tnormalY, splatY, splatWeights, uvs.y, side_y_bottom, sampleCount);
    }
    sampleMaterialMapsTriplanar(albedo, details, tnormalZ, splatZ, splatWeights, uvs.z, side_z, sampleCount);
    
    normal = TriplanarReorientedNormalMapping(uvs.strength, normal, uvs.axisSigns, tnormalX, tnormalY, tnormalZ);
}
#else
void sampleMaterialMapsTriplanar(inout float4 albedo, inout float4 details, inout float3 normal, float triplanarWeight, float4 splatWeights, float2 uv, int side, inout uint sampleCount) {
    float4 tempDetails;
    if(splatWeights.r > 0) {
        albedo += triplanarWeight * splatWeights.r * SampleTextureArray(0, 0, side, uv, sampleCount);
        tempDetails = SampleTextureArray(1, 0, side, uv, sampleCount);
        details += float4(triplanarWeight * splatWeights.r * tempDetails.xy,0,0);
        normal += triplanarWeight * splatWeights.r * UnpackNormal(tempDetails.zw);
    }
    if(splatWeights.g > 0) {
        albedo += triplanarWeight * splatWeights.g * SampleTextureArray(0, 1, side, uv, sampleCount);
        tempDetails = SampleTextureArray(1, 1, side, uv, sampleCount);
        details += float4(triplanarWeight * splatWeights.g * tempDetails.xy,0,0);
        normal += triplanarWeight * splatWeights.g * UnpackNormal(tempDetails.zw);
    }
    if(splatWeights.b > 0) {
        albedo += triplanarWeight * splatWeights.b * SampleTextureArray(0, 2, side, uv, sampleCount);
        tempDetails = SampleTextureArray(1, 2, side, uv, sampleCount);
        details += float4(triplanarWeight * splatWeights.b * tempDetails.xy,0,0);
        normal += triplanarWeight * splatWeights.b * UnpackNormal(tempDetails.zw);
    }
    if(splatWeights.a > 0) {
        albedo += triplanarWeight * splatWeights.a * SampleTextureArray(0, 3, side, uv, sampleCount);
        tempDetails = SampleTextureArray(1, 3, side, uv, sampleCount);
        details += float4(triplanarWeight * splatWeights.a * tempDetails.xy,0,0);
        normal += triplanarWeight * splatWeights.a * UnpackNormal(tempDetails.zw);
    }
}

void sampleMaterialMaps(triplanarUv uvs, float4 splatWeights, inout float4 albedo, inout float4 details, inout float3 normal, inout uint sampleCount) {
    float3 tnormalX = float3(0,0,0);
    float3 tnormalY = float3(0,0,0);
    float3 tnormalZ = float3(0,0,0);
    if(uvs.strength.x > 0){
    sampleMaterialMapsTriplanar(albedo, details, tnormalX, uvs.strength.x, splatWeights, uvs.x, side_x, sampleCount);
    }
    if(uvs.strength.y > 0){
        if(uvs.axisSigns.y > 0) {
            sampleMaterialMapsTriplanar(albedo, details, tnormalY, uvs.strength.y, splatWeights, uvs.y, side_y_top, sampleCount);
        } else {
            sampleMaterialMapsTriplanar(albedo, details, tnormalY, uvs.strength.y, splatWeights, uvs.y, side_y_bottom, sampleCount);
        }
    }
    if(uvs.strength.z > 0){
        sampleMaterialMapsTriplanar(albedo, details, tnormalZ, uvs.strength.z, splatWeights, uvs.z, side_z, sampleCount);
    }
    
    normal = TriplanarReorientedNormalMapping(uvs.strength, normal, uvs.axisSigns, tnormalX, tnormalY, tnormalZ);
}
#endif
#endif //!defined( SPLAT_UTILITIES_LOADED )
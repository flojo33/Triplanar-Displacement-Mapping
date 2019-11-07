#if !defined( CUSTOM_UTILITIES_LOADED )
#define CUSTOM_UTILITIES_LOADED
    //--------------------------------------------------------------------------
    // This include file contains helper functions that are used in the 
    // TriplanarDisplacmentMapping shaders
    //--------------------------------------------------------------------------
    
    float _ShadowTessellationFalloffStartDistance;
    float _ShadowTessellationFalloffDistance;
    float _TessellationFalloffStartDistance;
    float _TessellationFalloffDistance;
    float _BlendExponent;
    float _BlendOffset;
    sampler2D _TessellationMap;
   
    //---------------------------------------------------------------------
    //Struct containing triplanar infomration about the current point
    //---------------------------------------------------------------------
    struct triplanarUv {
        float2 x;
        float2 y;
        float2 z;
        float3 strength;
        float3 axisSigns;
    };
    
    //---------------------------------------------------------------------
    // Get the amount of each triplanar plane that is to be sampled.
    // This function tries to minimize the overlaping areas to reduce the
    // amount of samples required.
    //---------------------------------------------------------------------
    float3 GetTriplanarBlend(float3 normal) {
        float3 blend = max(abs(normal) - _BlendOffset,0);
        blend = pow(blend,_BlendExponent);
        return blend / (blend.x + blend.y + blend.z);
    }
     float3 GetTriplanarVertexBlend(float3 normal) {
        float3 blend = abs(normal);
        return blend / (blend.x + blend.y + blend.z);
    }
     float4 GetSplatBlend(float4 splat) {
        float4 blend = pow(splat,4);
        return blend / (blend.x + blend.y + blend.z + blend.w);
    }
    
    float GetVertexMip(float tessellationStrength) {
        return pow(1-tessellationStrength,1) * 4;
    }
    
    //---------------------------------------------------------------------
    // Get the direction of the normal for each axis
    //---------------------------------------------------------------------
    float3 GetAxisSign(float3 normal) {
        return sign(normal);
    }
    
    //---------------------------------------------------------------------
    // Obtain all triplanar data of a point given its normal and the blend 
    // cutoff value
    //---------------------------------------------------------------------
    triplanarUv getTriplanarUv(float3 position, float3 normal) {
        triplanarUv output;
        output.x = position.zy;
        output.y = position.xz;
        output.z = position.xy;
        output.strength = GetTriplanarBlend(normal);
        output.axisSigns = GetAxisSign(normal);
        return output;
    }
    
     triplanarUv getTriplanarVertexUv(float3 position, float3 normal) {
        triplanarUv output;
        output.x = position.zy;
        output.y = position.xz;
        output.z = position.xy;
        output.strength = GetTriplanarVertexBlend(normal);
        output.axisSigns = GetAxisSign(normal);
        return output;
    }
    
    //---------------------------------------------------------------------
    //Moves a displacement value from range 0 - 1 to range -strength/2
    // strength/2
    //---------------------------------------------------------------------
    float scaleDisplacementValue(float displacement, float strength) {
        return (displacement - 0.5) * strength;
    }
    
    //---------------------------------------------------------------------
    // Obtain the amount of tessellation a vertex sould obtain in relation
    // to its distance from the camera.
    //---------------------------------------------------------------------
    float obtainTesselationStrength(float3 position) {
        float3 v = position - _WorldSpaceCameraPos;
        float zeroDistance = _TessellationFalloffStartDistance + _TessellationFalloffDistance;
        if(dot(v,v) > zeroDistance * zeroDistance) {
            return 0;
        }
        else
        {
            float d = (distance(position, _WorldSpaceCameraPos) - _TessellationFalloffStartDistance) / _TessellationFalloffDistance;
            return max(0,min(1, 1 - d ));
        }
    }

#endif //!defined( CUSTOM_UTILITIES_LOADED )
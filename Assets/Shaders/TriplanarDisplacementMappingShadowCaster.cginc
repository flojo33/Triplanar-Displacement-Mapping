//---------------------------------------------------------------------------
// Shadow Shader Pass.
//---------------------------------------------------------------------------
#if !defined(SHADOWS_INCLUDED)
#define SHADOWS_INCLUDED
#define VertexProgram ShadowVertexProgram
#include "UnityCG.cginc"
#include "./Utilities.cginc"
#include "./SplatUtilities.cginc"

//---------------------------------------------------------------------
// Shadow Specific Variables
//---------------------------------------------------------------------
float _DisplacementStrength;
float _DisplacementDistance;
float _DisplacementTessellationStrength;
float _TessellationShadowBackfaceFactor;

//---------------------------------------------------------------------
// Shadow Vertex program input struct
//---------------------------------------------------------------------
struct VertexData {
	UNITY_VERTEX_INPUT_INSTANCE_ID
	float4 vertex : POSITION;
	float3 normal : NORMAL;
    float4 splat : COLOR;
};

//---------------------------------------------------------------------
// Shadow Vertex to Fragment program interpolation struct
//---------------------------------------------------------------------
struct Interpolators {
	float4 position : SV_POSITION;
	#if defined(SHADOWS_CUBE)
		float3 lightVec : TEXCOORD0;
	#endif
};

//---------------------------------------------------------------------
// Shadow Vertex Program
//---------------------------------------------------------------------
Interpolators ShadowVertexProgram (VertexData v) {
	Interpolators i;
	v.normal = normalize(v.normal);
    float3 initialPosition = mul(unity_ObjectToWorld, v.vertex).xyz;
    
	float mipId = GetVertexMip(obtainTesselationStrength(initialPosition));
    triplanarUv tUv = getTriplanarVertexUv(initialPosition, v.normal);
    
    #ifdef ENABLE_SPLAT
        float4 splat = v.splat;
    #else
        float4 splat = float4(1,0,0,0);
    #endif
    float4 splatX = float4(0,0,0,0);
    float4 splatY = float4(0,0,0,0);
    float4 splatZ = float4(0,0,0,0);
    float height = sampleVertexHeight(splat, v.normal , tUv, splatX, splatY, splatZ, mipId);
    
	float displacement = scaleDisplacementValue(height, _DisplacementStrength * _DisplacementDistance);
	
	v.vertex.xyz += v.normal * displacement;
	
	#if defined(SHADOWS_CUBE)
		i.position = UnityObjectToClipPos(v.vertex);
		i.lightVec =
			mul(unity_ObjectToWorld, v.vertex).xyz - _LightPositionRange.xyz;
	#else
		i.position = UnityClipSpaceShadowCasterPos(v.vertex.xyz, v.normal);
		i.position = UnityApplyLinearShadowBias(i.position);
	#endif
	return i;
}

//---------------------------------------------------------------------
// Shadow Fragment Program
// Only required for point lights in order to write their depth to
// interpolate the strength falloff
//---------------------------------------------------------------------
float4 ShadowFragmentProgram (Interpolators i) : SV_TARGET {
	#if defined(SHADOWS_CUBE)
		float depth = length(i.lightVec) + unity_LightShadowBias.x;
		depth *= _LightPositionRange.w;
		return UnityEncodeCubeShadowDepth(depth);
	#else
		return 0;
	#endif
}
#endif
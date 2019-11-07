#if !defined(CUSTOM_BASE_LOADED)

#define CUSTOM_BASE_LOADED

#define TESSELLATION_TANGENT 1

//--------------------------------------------------------------------------
// Include Unity standard shader code
//--------------------------------------------------------------------------
#include "./Utilities.cginc"
#include "./SplatUtilities.cginc"
#include "UnityPBSLighting.cginc"
#include "AutoLight.cginc"

//--------------------------------------------------------------------------
// Input Properties like texture samplers and vectors
//--------------------------------------------------------------------------
float _DisplacementStrength;
float _DisplacementDistance;
float _DisplacementTessellationStrength;
float _TessellationShadowBackfaceFactor;
float _divergenceOffset;

//--------------------------------------------------------------------------
// Structs for passing data between programs
//--------------------------------------------------------------------------
struct VertexData {
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float2 uv : TEXCOORD0;
};

struct Interpolators {
    float4 pos : SV_POSITION;
    float3 initialPosition : TEXCOORD0;
	float3 worldPos : TEXCOORD1;
	SHADOW_COORDS(2)
	float3 localNormal : TEXCOORD3;
	float4 splatX : TEXCOORD4;
	float4 splatY : TEXCOORD5;
	float4 splatZ : TEXCOORD6;
	float4 splatWeights : TEXCOORD7;
	#if defined(VERTEXLIGHT_ON)
		float3 vertexLightColor : TEXCOORD8;
	#endif
};

struct FragmentOutput {
	#if defined(DEFERRED_PASS)
		float4 gBuffer0 : SV_Target0;
		float4 gBuffer1 : SV_Target1;
		float4 gBuffer2 : SV_Target2;
		float4 gBuffer3 : SV_Target3;
	#else
		float4 color : SV_Target;
	#endif
};

//--------------------------------------------------------------------------
// Compute vertex light colors
//--------------------------------------------------------------------------
void ComputeVertexLightColor (inout Interpolators input) {
	#if defined(VERTEXLIGHT_ON)
		input.vertexLightColor = Shade4PointLights(
			unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
			unity_LightColor[0].rgb, unity_LightColor[1].rgb,
			unity_LightColor[2].rgb, unity_LightColor[3].rgb,
			unity_4LightAtten0, input.initialPosition, input.localNormal
		);
	#endif
}

//--------------------------------------------------------------------------
// 1. Get vertex data
//--------------------------------------------------------------------------
Interpolators VertexProgram (VertexData v) {
    Interpolators i;
    
    v.normal = normalize(v.normal);
    
	float3 normal = v.normal;
	i.initialPosition = mul(unity_ObjectToWorld, v.vertex).xyz;
	
	float mipId = GetVertexMip(obtainTesselationStrength(i.initialPosition));
	
    triplanarUv tUv = getTriplanarVertexUv(i.initialPosition, normal);
    #ifdef ENABLE_SPLAT
        i.splatWeights = SampleSplatTexture(v.uv);
    #else
        i.splatWeights = float4(1,0,0,0);
    #endif
    
    i.splatX = float4(0,0,0,0);
    i.splatY = float4(0,0,0,0);
    i.splatZ = float4(0,0,0,0);
    float height = sampleVertexHeight(i.splatWeights, normal, tUv, i.splatX, i.splatY, i.splatZ, mipId);
    
    i.splatX *= i.splatWeights;
    i.splatY *= i.splatWeights;
    i.splatZ *= i.splatWeights;
    
	float displacement = scaleDisplacementValue(height, _DisplacementStrength * _DisplacementDistance);
	
	v.vertex.xyz += v.normal * displacement;
	
	i.worldPos = mul(unity_ObjectToWorld, v.vertex);
	
    i.pos = UnityObjectToClipPos(v.vertex);
    
	i.localNormal = normal;
	
	TRANSFER_SHADOW(i);
	
	ComputeVertexLightColor(i);
	
	
    return i;
}

//--------------------------------------------------------------------------
// 2. Interpolation Step: (Invisible)
//--------------------------------------------------------------------------
// Interpolate values of each triangle between its three
// vertices and input the interpolated values onto the Fragment Program

//--------------------------------------------------------------------------
// Create light from Interpolated Data
//--------------------------------------------------------------------------
UnityLight CreateLight (Interpolators input, float3 normal) {
	UnityLight light;
	#if defined(DEFERRED_PASS)
		light.dir = float3(0, 1, 0);
		light.color = 0;
	#else
        #if defined(POINT) || defined(POINT_COOKIE) || defined(SPOT)
            light.dir = normalize(_WorldSpaceLightPos0.xyz - input.worldPos);
        #else
            light.dir = _WorldSpaceLightPos0.xyz;
            
        #endif
        
        UNITY_LIGHT_ATTENUATION(attenuation, input, input.worldPos);
        
        light.color = _LightColor0.rgb * attenuation;
        light.ndotl = DotClamped(normal, light.dir);
    #endif
	return light;
}

//--------------------------------------------------------------------------
// Create indirect from Interpolated Data
//--------------------------------------------------------------------------
UnityIndirect CreateIndirectLight (Interpolators input, float3 normal) {
	UnityIndirect indirectLight;
	indirectLight.diffuse = 0;
	indirectLight.specular = 0;

	#if defined(VERTEXLIGHT_ON)
		indirectLight.diffuse = input.vertexLightColor;
	#endif
	
	#if defined(FORWARD_BASE_PASS) || defined(DEFERRED_PASS)
		indirectLight.diffuse += max(0, ShadeSH9(float4(normal, 1)));
	#endif
	
	return indirectLight;
}

float4 CreateEmission(float4 baseColor, float emissionStrength, float4 baseMaterial) {
    return baseColor * emissionStrength * 10.0f + baseMaterial * (1-emissionStrength);
}

FragmentOutput BlackFragmentProgram (Interpolators input) {
    FragmentOutput fragmentOutput;
	#if defined(DEFERRED_PASS)
	    fragmentOutput.gBuffer0.rgb = float3(0,0,0);//albedo
		fragmentOutput.gBuffer0.a = 0;//Occlusion ?
		fragmentOutput.gBuffer1.rgb = 0;
		fragmentOutput.gBuffer1.a = 0;// Smoothness
		fragmentOutput.gBuffer2 = float4(0,0,0,0);
		fragmentOutput.gBuffer3 = float4(0,0,0,0);
	#else
		fragmentOutput.color = float4(0,0,0,0);
	#endif
	return fragmentOutput;
}
//--------------------------------------------------------------------------
// 3. Output the color of each fragment
//--------------------------------------------------------------------------
FragmentOutput FragmentProgram (Interpolators input) {

    #ifdef FLAT_SHADING
        float3 nX = ddx(input.worldPos);
        float3 nY = ddy(input.worldPos);
        float3 normalDerivative = normalize(-cross(nX, nY));
	#endif
	
    int sampleCount = 0;
    triplanarUv tUv = getTriplanarUv(input.initialPosition,  input.localNormal);
    
    #ifdef FLAT_SHADING
        float3 normal = normalDerivative;
    #else
        float3 normal = sampleNormalBase(tUv, input.localNormal, input.splatWeights, input.splatX, input.splatY, input.splatZ, sampleCount);
    #endif
    
    float4 color = float4(0,0,0,0);//((GetTriplanarBlend(normal)+1)/2,1);
    float4 details = float4(0,0,0,0);//((GetTriplanarBlend(normal)+1)/2,1);
    
    #ifdef SINGLE_BLEND
        float3 dummy;
        sampleMaterialMaps(tUv, input.splatWeights, color, details, normal, sampleCount);
    #else
        triplanarUv tUv2 = getTriplanarUv(input.worldPos, normal);
        #ifdef DETAIL_NORMAL
            float3 normalsPreBlend = normal;
            sampleMaterialMaps(tUv2, input.splatWeights, color, details, normal, sampleCount);
            normal = lerp(normalsPreBlend,normal,obtainTesselationStrength(input.initialPosition));
        #else
            float3 dummy;
            sampleMaterialMaps(tUv2, input.splatWeights, color, details, dummy, sampleCount);
        #endif
    #endif
    
    //Shade the entire terrain in white instead of using textures.
    #ifdef SHADE_WHITE
        details.r = 0;
        details.g = 0;
        color = float4(1,1,1,0);
    #endif
    
    float smoothness = details.r;
    float metallic = details.g;
    float emissionStrength = color.a;
    
    #ifdef DISPLAY_NORMAL
        color = float4((normal + 1)/2,1);
        smoothness = 0;
        metallic = 0;
        emissionStrength = 0;
    #endif
    
    #ifdef DISPLAY_SPLAT_MAP
        color += input.splatWeights;
    #endif
    
    #ifdef DISPLAY_SAMPLE_COUNT
        color = lerp(float4(0,1,0,0), float4(1,0,0,0), sampleCount/36.0);
    #endif
    
    float3 specularTint;
    
    float oneMinusReflectivity;
    
    float3 albedo = DiffuseAndSpecularFromMetallic(
    	color, metallic, specularTint, oneMinusReflectivity
    );
    
    UnityIndirect indirectLight = CreateIndirectLight(input, normal);
    
    float4 pbs = UNITY_BRDF_PBS(
    	albedo, specularTint,
    	oneMinusReflectivity, smoothness,
    	normal, normalize(_WorldSpaceCameraPos - input.worldPos),
    	CreateLight(input, normal), indirectLight
    );
    
    float4 col = CreateEmission(color, emissionStrength, pbs);
    
    FragmentOutput fragmentOutput;
	#if defined(DEFERRED_PASS)
	    fragmentOutput.gBuffer0.rgb = albedo.rgb;//albedo
		fragmentOutput.gBuffer0.a = 0;//Occlusion ?
		fragmentOutput.gBuffer1.rgb = specularTint;
		fragmentOutput.gBuffer1.a = smoothness;// Smoothness
		fragmentOutput.gBuffer2 = float4(normal * 0.5 + 0.5, 1);
		fragmentOutput.gBuffer3 = col;
	#else
		fragmentOutput.color = col;
	#endif
	return fragmentOutput;
}

#endif
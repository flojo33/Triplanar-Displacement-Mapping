//--------------------------------------------------------------------------
// Triplanar Displacement Mapping Shader for Unity
// Created by Florian Bayer
// 13.05.2019
//--------------------------------------------------------------------------
Shader "TriplanarDisplacementMapping/TriplanarDisplacementMappingWireframe" {
    
    Properties {
        //--------------------------------------------------------------------------
        // Blending
        //--------------------------------------------------------------------------
		_BlendExponent ("Texture Blend Exponent", Range(1, 16)) = 4
		_BlendOffset ("Texture Blend Offset", Range(0, 0.576)) = 0
		
		//_SplatBlendExponent ("Splat Blend Exponent", Range(1, 16)) = 4
		_SplatHeightBlendStrength ("Splat Height Blend Strength", Range(0, 1)) = 0.5
		_TriplanarHeightBlendStrength ("Splat triplanar Height Blend Strength", Range(0, 1)) = 0.5
		
        //--------------------------------------------------------------------------
        // Displacement
        //--------------------------------------------------------------------------
		_DisplacementStrength ("Displacement Strength", Range(0, 1.0)) = 0.5
		_DisplacementDistance ("Displacement Distance", float) = 1.0
		_DisplacementTessellationStrength ("Displacement Tesselation Strength", Range(0, 1.0)) = 0.5
		
        //--------------------------------------------------------------------------
        // Tessellation
        //--------------------------------------------------------------------------
		_TessellationShadowEdgeLength ("Shadow Tesselation Edge Length", float) = 0.75
	    _TessellationEdgeLength ("Tessellation Edge Length", float) = 1.5
	    _ShadowTessellationEdgeLength ("Tessellation Edge Length", float) = 1.5
	    _TessellationFalloffStartDistance ("Tessellation Falloff Start Distance", float) = 200.0
	    _TessellationFalloffDistance ("Tessellation Falloff Distance", float) = 100.0
	    
	    _TessellationShadowBackfaceFactor ("Tessellation Shadow Backface Factor", Range(0.0, 1.0)) = 0.707
	    
		//_mipMapCount ("MipMapCount", int) = 10
		
		//_vertexMipStartDistance ("mip start distance", float) = 100
		//_vertexMipEndDistance ("mip end distance", float) = 300
	    
	    _divergenceOffset  ("Divergence Strength", Range(0, 32.0)) = 0.5
		
        //--------------------------------------------------------------------------
        // Splat Mapping
        //--------------------------------------------------------------------------
		_SplatMap ("Splat Map", 2D) = "white" {}
		_SplatArray ("Splat Array", 2DArray) = "" {}
		_TessellationMap ("Tessellation Map", 2D) = "white" {}
    }

    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200
        
        //First Directional Light
		Pass {
			Tags {
				"LightMode" = "ForwardBase"
			}
			
            CGPROGRAM
            //#pragma enable_d3d11_debug_symbols
            #pragma target 4.6
            
            #pragma shader_feature ENABLE_SPLAT
            
			#pragma multi_compile _ SHADOWS_SCREEN
			
			#define FORWARD_BASE_PASS
			
            //--------------------------------------------------------------------------
            // Define Shader Programs
            //--------------------------------------------------------------------------
            #pragma vertex TessellationVertexProgram
			#pragma fragment BlackFragmentProgram
			#pragma hull HullProgram
			#pragma domain DomainProgram
			
            //--------------------------------------------------------------------------
            // Load Base Functionality...
            //--------------------------------------------------------------------------
			#include "./TriplanarDisplacementMappingBase.cginc"
			#include "./Tessellation.cginc"
			
			ENDCG
		}
		
		//Deferred Rendering
		Pass {
			Tags {
				"LightMode" = "Deferred"
			}
			
            CGPROGRAM
            //#pragma enable_d3d11_debug_symbols
            #pragma target 4.6
            #pragma exclude_renderers nomrt
            
            #pragma shader_feature ENABLE_SPLAT
            
			#define DEFERRED_PASS
			
            //--------------------------------------------------------------------------
            // Define Shader Programs
            //--------------------------------------------------------------------------
            #pragma vertex TessellationVertexProgram
			#pragma fragment BlackFragmentProgram
			#pragma hull HullProgram
			#pragma domain DomainProgram
			
            //--------------------------------------------------------------------------
            // Load Base Functionality...
            //--------------------------------------------------------------------------
			#include "./TriplanarDisplacementMappingBase.cginc"
			#include "./Tessellation.cginc"
			
			ENDCG
		}
	}
}
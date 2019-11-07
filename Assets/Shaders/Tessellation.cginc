//---------------------------------------------------------------------------
// Tessellation Programs. General function structure obtained from 
// https://catlikecoding.com/unity/tutorials/advanced-rendering/tessellation/
//---------------------------------------------------------------------------
#if !defined(TESSELLATION_INCLUDED)
#define TESSELLATION_INCLUDED
#include "./Utilities.cginc"

    float _TessellationEdgeLength;
    float _ShadowTessellationEdgeLength;
    
    //---------------------------------------------------------------------
    // Tessellation Vertex program input struct
    //---------------------------------------------------------------------
    struct TessellationVertexData {
    	float4 vertex : POSITION;
    	float3 normal : NORMAL;
    	float2 uv : TEXCOORD0;
    };
    
    struct TessellationFactors {
        float edge[3] : SV_TessFactor;
        float inside : SV_InsideTessFactor;
    };
    
    struct TessellationControlPoint {
        float4 vertex : INTERNALTESSPOS;
        float3 normal : NORMAL;
        float2 uv : TEXCOORD0;
        float3 tessellationStrength : TEXCOORD1;
    };
    
    float TessellationEdgeFactor (float3 p0, float3 p1, float fac0, float fac1) {
        
		float edgeLength = distance(p0, p1);
		float3 edgeCenter = (p0 + p1) * 0.5;
		float factor = (fac0 + fac1) * 0.5;
		
	    float tesselationAmount = obtainTesselationStrength(edgeCenter);
        if(tesselationAmount <= 0) {
            return 1;
        } else {
		    return (1-factor) + factor * max(1,(edgeLength / _TessellationEdgeLength) * tesselationAmount);
        }
    }
    
    //---------------------------------------------------------------------
    // Functions used for culling triangles
    //---------------------------------------------------------------------
    bool TriangleIsBelowClipPlane (float3 p0, float3 p1, float3 p2, int planeIndex, float bias) {
        float4 plane = unity_CameraWorldClipPlanes[planeIndex];
        return
            dot(float4(p0, 1), plane) < bias &&
            dot(float4(p1, 1), plane) < bias &&
            dot(float4(p2, 1), plane) < bias;
    }
    
    bool TriangleIsOutsideViewFrustrum(float3 p0, float3 p1, float3 p2, int planeIndex, float bias) {
        if(TriangleIsBelowClipPlane(p0, p1, p2, 0, bias)) {
            return true;
        } else {
            if(TriangleIsBelowClipPlane(p0, p1, p2, 1, bias)) {
                return true;
            } else {
                if(TriangleIsBelowClipPlane(p0, p1, p2, 2, bias)) {
                    return true;
                } else {
                    if(TriangleIsBelowClipPlane(p0, p1, p2, 3, bias)) {
                        return true;
                    } else {
                        return false;
                    }
                }
            }
        }
    }
    
    float Di(float3 m, float3 n, float3 o, float3 p)
    {
        return (m.x - n.x) * (o.x - p.x) + (m.y - n.y) * (o.y - p.y) + (m.z - n.z) * (o.z - p.z);
    }
    
    bool TriangleIsCulled (float3 v0, float3 v1, float3 v2)
    {
        float bias = -0.5 * (_DisplacementStrength * _DisplacementDistance);
        #if defined(SHADOWS_CUBE)
            float3 center = (v0+v1+v2)/3.0;
            float3 lightVec = center - _LightPositionRange.xyz;
            float depth = length(lightVec) + unity_LightShadowBias.x;
            depth *= _LightPositionRange.w;
            return (depth) > 1? true : false;
        #else //<-- !defined(SHADOWS_CUBE)
            return TriangleIsOutsideViewFrustrum(v0, v1, v2, 0, bias);
        #endif//<-- !defined(SHADOWS_CUBE)
    }
    
    
    //---------------------------------------------------------------------
    // Tessellation Vertex program. Passes vertex data to the hull shader.
    //---------------------------------------------------------------------
    TessellationControlPoint TessellationVertexProgram (TessellationVertexData v) {
        TessellationControlPoint p;
        p.vertex = v.vertex;
        p.normal = v.normal;
        p.uv = v.uv;
        p.tessellationStrength = tex2Dlod(_TessellationMap, float4(p.uv,0,0)); //Sample Tessellation Strength Map
        return p;
    }
    
    //---------------------------------------------------------------------
    // Tessellation control shader or hull Program. Defines the geometry 
    // that the tessellation stage should output.
    //---------------------------------------------------------------------
    [UNITY_domain("tri")] //Output triangle geometry
    [UNITY_outputcontrolpoints(3)] //Output three points
    [UNITY_outputtopology("triangle_cw")] //Triangle Clockwise
    [UNITY_partitioning("integer")] //How the output geometry is partitioned (alternatives: "integer", "fractional_even")
    [UNITY_patchconstantfunc("PatchConstantFunction")] //Function that defines the amount of partitioning a patch should receive
    TessellationControlPoint HullProgram (InputPatch<TessellationControlPoint, 3> patch, uint id : SV_OutputControlPointID) {
        return patch[id];
    }
    
    //---------------------------------------------------------------------
    // Defines the amount of subdivision each edge should receive in 
    // addition to the amount of inner triangels that should be created.
    //---------------------------------------------------------------------
    TessellationFactors PatchConstantFunction (InputPatch<TessellationControlPoint, 3> patch) {
        TessellationFactors f;
        float3 p0 = mul(unity_ObjectToWorld, patch[0].vertex).xyz;
        float3 p1 = mul(unity_ObjectToWorld, patch[1].vertex).xyz;
        float3 p2 = mul(unity_ObjectToWorld, patch[2].vertex).xyz;
        if (TriangleIsCulled(p0, p1, p2)) {
            f.edge[0] = f.edge[1] = f.edge[2] = f.inside = 0;
        }
        else {
            float fac0 = patch[0].tessellationStrength.x;
            float fac1 = patch[1].tessellationStrength.x;
            float fac2 = patch[2].tessellationStrength.x;
            f.edge[0] = TessellationEdgeFactor(p1, p2, fac1, fac2);
            f.edge[1] = TessellationEdgeFactor(p2, p0, fac2, fac0);
            f.edge[2] = TessellationEdgeFactor(p0, p1, fac0, fac1);
            f.inside = (f.edge[0] + f.edge[1] + f.edge[2]) * (1 / 3.0);
        }
        return f;
    }
    
    //---------------------------------------------------------------------
    // Tessellation evaluation shader or domain Program. Invoked once per 
    // output vertex.
    //---------------------------------------------------------------------
    [UNITY_domain("tri")]
    Interpolators DomainProgram (TessellationFactors factors, OutputPatch<TessellationControlPoint, 3> patch, float3 barycentricCoordinates : SV_DomainLocation) {
        VertexData data;
        #define InterpolateBarycentricAndStore(fieldName) data.fieldName = \
            patch[0].fieldName * barycentricCoordinates.x + \
            patch[1].fieldName * barycentricCoordinates.y + \
            patch[2].fieldName * barycentricCoordinates.z;
        InterpolateBarycentricAndStore(vertex)
        InterpolateBarycentricAndStore(normal)
        InterpolateBarycentricAndStore(uv)
        return VertexProgram(data); //Call the main shaders vertex program for every vertex generated by the tessellation stage.
    }
    
#endif
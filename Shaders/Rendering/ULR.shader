/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org



/// <summary> 
/// Blends the source data based on the per-vertex Unstructured Lumigraph Rendering (ULR) algorithm.
/// This is the FPS-friendly but lower-quality option: camera blending weights are computed in the vertex shader instead of the fragment shader.
///
/// This shader is inspired by an algorithm presented in:
/// Buehler et al., Unstructured Lumigraph Rendering, 2001, https://doi.org/10.1145/383259.383309.
/// </summary>
Shader "COLIBRIVR/Rendering/ULR"
{
    Properties
    {
        _ColorData ("Color data", 2DArray) = "white" {}
        _DepthData ("Depth data", 2DArray) = "white" {}
        _GlobalTextureMap ("Global texture map", 2D) = "black" {}
        _LossyScale ("Lossy scale", Vector) = (0, 0, 0)
        _SourceCamCount ("Number of source cameras", int) = 1
        _BlendCamCount ("Number of source cameras that will have a non-zero blending weight for a given fragment", int) = 1
        _MaxBlendAngle ("Maximum angle difference (degrees) between source ray and view ray for the color value to be blended", float) = 180.0
        _ResolutionWeight ("Relative impact of the {resolution} factor compared to the {angle difference} factor in the ULR algorithm", float) = 0.2
        _DepthCorrectionFactor ("Factor for depth correction", float) = 0.1
        _GlobalTextureMapWeight ("Relative weight of the global texture map, compared to the estimated color.", float) = 0.1
        _IsColorSourceCamIndices ("Whether the displayed colors help visualize the source camera indices instead of the actual texture colors.", int) = 0
        _BlendFieldComputationParams ("Parameters defining which vertices will compute their part of the blending field", Vector) = (0, 0, 0)
        _ExcludedSourceView ("Excluded source camera index", int) = -1
    }

    SubShader
    {

/// PASS
        Pass
        {
    /// HEADER
            Name "Draw"
            CGPROGRAM
            #pragma vertex draw_vert
            #pragma fragment draw_frag
            #pragma geometry draw_geom
            #pragma require geometry
            #pragma require 2darray
            #pragma target 4.5
            #include "./../CGIncludes/ULRCG.cginc"
    /// ENDHEADER

    /// STRUCTS
            struct draw_vIN
            {
                uint vertexID : SV_VertexID;
                float4 objectXYZW : POSITION;
                float3 objectNormalXYZ : NORMAL;
                float2 texUV : TEXCOORD0;
            };

            struct draw_v2g
            {
                float4 clipXYZW : SV_POSITION;
                float2 texUV : TEXCOORD0;
                uint vertexID : TEXCOORD1;
                float3 worldXYZ : TEXCOORD2;
            };

            struct draw_g2f
            {
                float4 clipXYZW : SV_POSITION;
                float2 texUV : TEXCOORD0;
                float3 worldXYZ : TEXCOORD2;
                SourceCamContribution sourceCamContributions[_MaxBlendCamCount] : TEXCOORD3;
            };
    /// ENDSTRUCTS

    /// VERTEX
            draw_v2g draw_vert(draw_vIN i)
            {
                draw_v2g o;
                o.clipXYZW = UnityObjectToClipPos(i.objectXYZW);
                o.texUV = i.texUV;
                o.vertexID = i.vertexID;
                o.worldXYZ = mul(unity_ObjectToWorld, i.objectXYZW).xyz;
                if(ShouldBlendFieldBeComputedInVertex(o.vertexID))
                {
                    float4 worldNormalXYZ = mul(unity_ObjectToWorld, i.objectNormalXYZ);
                    SourceCamContribution sourceCamContributions[_MaxBlendCamCount];
                    ComputeCamWeightsForVertex(o.worldXYZ, worldNormalXYZ, true, sourceCamContributions);
                    TransferArraysToBuffers(o.vertexID, sourceCamContributions);
                }
                return o;
            }
    /// ENDVERTEX

    /// GEOMETRY
            [maxvertexcount(3)]
            void draw_geom (triangle draw_v2g i[3], inout TriangleStream<draw_g2f> triangleStream)
            {
                draw_g2f o;
                float3 vertexIDs = uint3(i[0].vertexID, i[1].vertexID, i[2].vertexID);
                SourceCamContributionsForTriangle sourceCamContributionsForTriangle;
                SortIndexWeightsListForTriangle(vertexIDs, sourceCamContributionsForTriangle);
                uint blendCamCount = GetBlendCamCount();
                [unroll]
                for(uint iter = 0; iter < 3; iter++)
                {
                    draw_v2g iterVert = i[iter];
                    o.clipXYZW = iterVert.clipXYZW;
                    o.texUV = iterVert.texUV;
                    o.worldXYZ = iterVert.worldXYZ;
                    [unroll]
                    for(uint blendCamIndex = 0; blendCamIndex < blendCamCount; blendCamIndex++)
                    {
                        uint sourceCamIndex = sourceCamContributionsForTriangle.indexList[blendCamIndex];
                        o.sourceCamContributions[blendCamIndex].index = sourceCamIndex;
                        o.sourceCamContributions[blendCamIndex].texUV = ComputeSourceTexUV(iterVert.worldXYZ, sourceCamIndex);
                        o.sourceCamContributions[blendCamIndex].weight = sourceCamContributionsForTriangle.weightList[blendCamIndex][iter];
                    }
                    triangleStream.Append(o);
                }
            }
    /// ENDGEOMETRY

    /// FRAGMENT
            float_fOUT draw_frag (draw_g2f i)
            {
                float_fOUT o;
                o.color = ComputeColorFromVertexCamWeights(i.sourceCamContributions, true, i.worldXYZ);
                NormalizeByAlpha(o.color);
                if(_IsColorSourceCamIndices == 0)
                    MergeColorWithGlobalTextureMap(o.color, i.texUV);
                if(o.color.a == 0)
                    clip(-1);
                return o;
            }
    /// ENDFRAGMENT
            ENDCG
        }
/// ENDPASS
    }
}

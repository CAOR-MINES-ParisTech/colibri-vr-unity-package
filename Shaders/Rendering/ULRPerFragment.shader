/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org



/// <summary> 
/// Blends the source data based on the per-fragment Unstructured Lumigraph Rendering (ULR) algorithm.
/// This is the high-quality but FPS-intensive option: it performs all computations in the fragment shader.
///
/// This shader is inspired by an algorithm presented in:
/// Buehler et al., Unstructured Lumigraph Rendering, 2001, https://doi.org/10.1145/383259.383309.
/// </summary>
Shader "COLIBRIVR/Rendering/ULRPerFragment"
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
        _ExcludedSourceView ("Excluded source camera index", int) = -1
    }

    SubShader
    {

/// PASS
        Pass
        {
    /// HEADER
            Name "DrawMesh-PerFragment"
            CGPROGRAM
            #pragma vertex drawMesh_vert
            #pragma fragment drawMesh_frag
            #pragma require 2darray
            #pragma require compute
            #include "./../CGIncludes/ULRCG.cginc"
    /// ENDHEADER

    /// STRUCTS
            struct drawMesh_vIN
            {
                float4 objectXYZW : POSITION;
                float3 objectNormalXYZ : NORMAL;
                float2 texUV : TEXCOORD0;
            };

            struct drawMesh_v2f
            {
                float2 texUV : TEXCOORD0;
                float3 worldXYZ : TEXCOORD1;
                float3 worldNormalXYZ : TEXCOORD2;
            };
    /// ENDSTRUCTS

    /// VERTEX
            drawMesh_v2f drawMesh_vert(drawMesh_vIN i, out float4 clipXYZW : SV_POSITION)
            {
                clipXYZW = UnityObjectToClipPos(i.objectXYZW);
                drawMesh_v2f o;
                o.texUV = i.texUV;
                o.worldXYZ = mul(unity_ObjectToWorld, i.objectXYZW).xyz;
                o.worldNormalXYZ = mul(unity_ObjectToWorld, i.objectNormalXYZ);
                return o;
            }
    /// ENDVERTEX

    /// FRAGMENT
            float_fOUT drawMesh_frag (drawMesh_v2f i)
            {
                float_fOUT o;
                SourceCamContribution sourceCamContributions[_MaxBlendCamCount];
                ComputeCamWeightsForVertex(i.worldXYZ, i.worldNormalXYZ, true, sourceCamContributions);
                o.color = ComputeColorFromVertexCamWeights(sourceCamContributions, false, 0);
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

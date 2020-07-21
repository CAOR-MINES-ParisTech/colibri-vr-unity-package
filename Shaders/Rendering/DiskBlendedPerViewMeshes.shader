/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org



/// <summary> 
/// Blends the instantiated per-view meshes using disk-based blending.
///
/// This shader is inspired by an algorithm presented in:
/// Overbeck et al., A System for Acquiring, Processing, and Rendering Panoramic Light Field Stills for Virtual Reality, 2018, https://doi.org/10.1145/3272127.3275031.
/// </summary>
Shader "COLIBRIVR/Rendering/DiskBlendedPerViewMeshes"
{
    Properties
    {
        _ColorData ("Color data", 2DArray) = "white" {}
        _MaxBlendAngle ("Max blend angle", float) = 0.0
        _ClipNullValues ("Clip null values", int) = 0
        _SourceCamCount ("Number of source cameras", int) = 1
        _FocalLength ("Focal length", float) = 1.0
        _StoredColorTexture ("Stored color texture", 2D) = "white" {}
        _StoredDepthTexture ("Stored depth texture", 2D) = "white" {}
        _ExcludedSourceView ("Excluded source camera index", int) = -1
        [PerRendererData] _SourceCamIndex ("Source camera index", int) = 0
        [PerRendererData] _SourceCamPosXYZ ("Source camera position", Vector) = (0, 0, 0)
        [PerRendererData] _SourceCamIsOmnidirectional ("Source camera is omnidirectional", int) = 0
    }

    SubShader
    {
/// PASS
        Pass
        {
    /// <summary>
    /// This pass draws and blends the per-view meshes.
    /// It is to be called once per mesh.
    /// </summary>

    /// HEADER
            Name "DiskBlendedPerViewMeshes_Draw"
            ZWrite On
            ZTest LEqual
            CGPROGRAM
            #include "./../CGIncludes/DiskBlendingCG.cginc"
            #pragma vertex draw_vert
            #pragma fragment draw_frag
            #pragma require 2darray
    /// ENDHEADER

    /// STRUCTS
            struct draw_v2f
            {
                float3 texArrayUVZ : TEXCOORD0;
                float3 viewCamToSourceCamWorldXYZ : TEXCOORD1;
                float4 worldXYZW : TEXCOORD2;
                uint isOmnidirectional : TEXCOORD3;
            };
    /// ENDSTRUCTS

    /// VERTEX
            draw_v2f draw_vert (base_vIN i, out float4 clipXYZW : SV_POSITION)
            {
                draw_v2f o;
                // Scale the depth buffer values so that overlapping per-view meshes are not discarded automatically due to the ZTest.
                // This is used as an alternative to methods based on ZTest Always or Blend One One, which were problematic for objects with self-occlusions. 
                clipXYZW = UnityObjectToClipPos(i.objectXYZW);
                uint sourceCamIndex = UNITY_ACCESS_INSTANCED_PROP(InstanceProperties, _SourceCamIndex);
                float normalizedDeviceZ = clipXYZW.z / clipXYZW.w;
                clipXYZW.z = clipXYZW.w * Traditional01ToNDC(ScaleZ01(sourceCamIndex, NDCToTraditional01(normalizedDeviceZ)));
                // Store other values in the output element for use during the fragment step.
                o.texArrayUVZ = float3(i.texUV, sourceCamIndex);
                o.viewCamToSourceCamWorldXYZ = UNITY_ACCESS_INSTANCED_PROP(InstanceProperties, _SourceCamPosXYZ) - _WorldSpaceCameraPos.xyz;
                o.worldXYZW = mul(unity_ObjectToWorld, i.objectXYZW);
                o.isOmnidirectional = UNITY_ACCESS_INSTANCED_PROP(InstanceProperties, _SourceCamIsOmnidirectional);
                return o;
            }
    /// ENDVERTEX
            
    /// FRAGMENT
            base_fOUT draw_frag (draw_v2f i, float4 viewportXYZW : SV_POSITION)
            {
                base_fOUT o;
                // If the view being rendered should be excluded, exit.
                uint sourceCamIndex = round(i.texArrayUVZ.z);
                if(sourceCamIndex == _ExcludedSourceView)
                    clip(-1);
                // Compute the blending weight.
                float weight = GetViewpointWeightForFragment(i.worldXYZW, i.viewCamToSourceCamWorldXYZ, i.isOmnidirectional);
                // If the weight is below the minimum threshold, exit.
                if(_ClipNullValues && weight <= _MinWeight)
                    clip(-1);
                // Get the color of the element being drawn.
                fixed3 colorRGB;
                if(_IsColorSourceCamIndices == 1)
                    colorRGB = GetColorForIndex(sourceCamIndex, _SourceCamCount);
                else
                    colorRGB = UNITY_SAMPLE_TEX2DARRAY_LOD(_ColorData, i.texArrayUVZ, 0).rgb;
                // Multiply this color by the blending weight.
                fixed4 meshColor = weight * fixed4(colorRGB, 1);
                // Set the output color as a blend between this weighted color and the existing output color (stored in a texture).
                o.color = ComputeBlendedColor(viewportXYZW, meshColor);
                return o;
            }
    /// ENDFRAGMENT
            ENDCG
        }
/// ENDPASS

/// PASS
        Pass
        {
    /// <summary>
    /// This pass writes to the output color and depth buffers, and normalizes the output color values.
    /// It is to be called once.
    /// </summary>

    /// HEADER
            Name "DiskBlendedPerViewMeshes_WriteToCameraTarget"
            ZWrite On
            CGPROGRAM
            #include "./../CGIncludes/DiskBlendingCG.cginc"
            #pragma vertex base_vert
            #pragma fragment output_frag
    /// ENDHEADER

    /// FRAGMENT
            depth_fOUT output_frag (base_v2f i)
            {
                depth_fOUT o;
                // Normalize the color in the stored color texture by its alpha value.
                o.color = tex2D(_StoredColorTexture, i.texUV);
                NormalizeByAlpha(o.color);
                // Get the stored depth.
                float frameDepth = tex2D(_StoredDepthTexture, i.texUV).r;
                // By the previous steps, this depth can only be equal to zero if nothing has been written in this fragment.
                // If this is not the case, something has thus been written.
                if(frameDepth != ReverseDepthIfReversedBuffer(1.0))
                {
                    // If what has been written has a weight above the minimum threshold, write this element at the stored depth.
                    if(!_ClipNullValues || o.color.a > _MinWeight)
                    {
                        o.depth = ScaledDepthBufferToNDC(frameDepth);
                    }
                }
                // Otherwise, nothing has been written, and the stored color is the background color.
                // In this case, write this element at the background depth.
                else
                {
                    o.depth = Traditional01ToNDC(1.0);
                }
                return o;
            }
    /// ENDFRAGMENT
            ENDCG
        }
/// ENDPASS
    }
}

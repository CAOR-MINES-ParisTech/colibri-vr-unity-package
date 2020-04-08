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
        _MainTex ("Current camera target", 2D) = "white" {}
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
                float viewZ : TEXCOORD2;
                float4 worldXYZW : TEXCOORD3;
                uint isOmnidirectional : TEXCOORD4;
                uint sourceCamIndex : TEXCOORD5;
            };
    /// ENDSTRUCTS

    /// VERTEX
            draw_v2f draw_vert (base_vIN i, out float4 clipXYZW : SV_POSITION)
            {
                uint sourceCamIndex = UNITY_ACCESS_INSTANCED_PROP(InstanceProperties, _SourceCamIndex);
                clipXYZW = UnityObjectToClipPos(i.objectXYZW);
                float normalizedDeviceZ = saturate(clipXYZW.z / clipXYZW.w);
                clipXYZW.z = clipXYZW.w * ScaleNormalizedDeviceZ(sourceCamIndex, normalizedDeviceZ);
                draw_v2f o;
                o.sourceCamIndex = sourceCamIndex;
                o.texArrayUVZ = float3(i.texUV, sourceCamIndex);
                o.viewCamToSourceCamWorldXYZ = UNITY_ACCESS_INSTANCED_PROP(InstanceProperties, _SourceCamPosXYZ) - _WorldSpaceCameraPos.xyz;
                o.viewZ = abs(UnityObjectToViewPos(i.objectXYZW).z);
                o.worldXYZW = mul(unity_ObjectToWorld, i.objectXYZW);
                o.isOmnidirectional = UNITY_ACCESS_INSTANCED_PROP(InstanceProperties, _SourceCamIsOmnidirectional);
                return o;
            }
    /// ENDVERTEX
            
    /// FRAGMENT
            base_fOUT draw_frag (draw_v2f i, float4 viewportXYZW : SV_POSITION)
            {
                base_fOUT o;
                if(i.texArrayUVZ.z == _ExcludedSourceView)
                    clip(-1);
                float weight = GetViewpointWeightForFragment(i.worldXYZW, i.viewCamToSourceCamWorldXYZ, i.isOmnidirectional);
                if(_ClipNullValues && weight <= _MinWeight)
                    clip(-1);
                float2 screenUV = viewportXYZW.xy / _ScreenParams.xy;
                float meshViewZ = i.viewZ;
                fixed3 colorRGB;
                if(_IsColorSourceCamIndices == 1)
                    colorRGB = GetColorForIndex(i.sourceCamIndex, _SourceCamCount);
                else
                    colorRGB = UNITY_SAMPLE_TEX2DARRAY(_ColorData, i.texArrayUVZ).rgb;
                fixed4 meshColor = weight * fixed4(colorRGB, 1);
                o.color = ComputeBlendedColor(screenUV, meshViewZ, meshColor);
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

    /// PROPERTIES
            sampler2D _MainTex;
    /// ENDPROPERTIES

    /// FRAGMENT
            depth_fOUT output_frag (base_v2f i)
            {
                depth_fOUT o;
                o.color = tex2D(_StoredColorTexture, i.texUV);
                if(o.color.a == 0 || (_ClipNullValues && o.color.a == _MinWeight))
                    o.color = tex2D(_MainTex, i.texUV);
                else
                    NormalizeByAlpha(o.color);
                o.depth = UnscaleNormalizedDeviceZ(tex2D(_StoredDepthTexture, i.texUV).r);
                return o;
            }
    /// ENDFRAGMENT
            ENDCG
        }
/// ENDPASS
    }
}

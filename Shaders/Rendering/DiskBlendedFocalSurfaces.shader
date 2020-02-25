/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org



/// <summary> 
/// Blends the instantiated focal surfaces using disk-based blending.
///
/// This shader is inspired by an algorithm presented in:
/// Overbeck et al., A System for Acquiring, Processing, and Rendering Panoramic Light Field Stills for Virtual Reality, 2018, https://doi.org/10.1145/3272127.3275031.
/// </summary>
Shader "COLIBRIVR/Rendering/DiskBlendedFocalSurfaces"
{
    Properties
    {
        _MainTex ("Current camera target", 2D) = "white" {}
        _ColorData ("Color data", 2DArray) = "white" {}
        _MaxBlendAngle ("Max blend angle", float) = 0.0
        _ClipNullValues ("Clip null values", int) = 0
        _SourceCamCount ("Number of source cameras", int) = 1
        _FocalLength ("Focal length", float) = 1.0
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
    /// This pass draws and blends the focal surfaces.
    /// It is to be called once per focal surface.
    /// </summary>

    /// HEADER
            Name "DiskBlendedFocalSurfaces_Draw"
            ZWrite On
            ZTest Always
            Blend One One
            CGPROGRAM
            #include "./../CGIncludes/DiskBlendingCG.cginc"
            #pragma vertex draw_vert
            #pragma fragment draw_frag
            #pragma multi_compile_instancing
            #pragma require 2darray
    /// ENDHEADER

    /// STRUCTS
            struct draw_vIN
            {
                float4 objectXYZW : POSITION;
                float2 texUV : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct draw_v2f
            {
                float3 texArrayUVZ : TEXCOORD0;
                float3 viewCamToSourceCamWorldXYZ : TEXCOORD1;
                float4 worldXYZW : TEXCOORD2;
                uint isOmnidirectional : TEXCOORD3;
            };
    /// ENDSTRUCTS

    /// VERTEX
            draw_v2f draw_vert (draw_vIN i, out float4 clipXYZW : SV_POSITION)
            {
                UNITY_SETUP_INSTANCE_ID(i);
                clipXYZW = UnityObjectToClipPos(i.objectXYZW);
                draw_v2f o;
                o.texArrayUVZ = float3(i.texUV, UNITY_ACCESS_INSTANCED_PROP(InstanceProperties, _SourceCamIndex));
                o.viewCamToSourceCamWorldXYZ = UNITY_ACCESS_INSTANCED_PROP(InstanceProperties, _SourceCamPosXYZ) - _WorldSpaceCameraPos.xyz;
                o.worldXYZW = mul(unity_ObjectToWorld, i.objectXYZW);
                o.isOmnidirectional = UNITY_ACCESS_INSTANCED_PROP(InstanceProperties, _SourceCamIsOmnidirectional);
                return o;
            }
    /// ENDVERTEX
            
    /// FRAGMENT
            base_fOUT draw_frag (draw_v2f i)
            {
                base_fOUT o;
                if(i.texArrayUVZ.z == _ExcludedSourceView)
                    clip(-1);
                float weight = GetViewpointWeightForFragment(i.worldXYZW, i.viewCamToSourceCamWorldXYZ, i.isOmnidirectional);
                if(weight <= _MinWeight)
                {
                    if(_ClipNullValues)
                        clip(-1);
                    else
                        o.color = 0;
                }
                else
                {
                    fixed4 meshColor = weight * fixed4(UNITY_SAMPLE_TEX2DARRAY(_ColorData, i.texArrayUVZ).rgb, 1);
                    o.color = meshColor;
                }
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
    /// This pass normalizes the output values.
    /// It is to be called once.
    /// </summary>

    /// HEADER
            Name "DiskBlendedFocalSurfaces_NormalizeAlpha"
            ZWrite Off
            CGPROGRAM
            #include "./../CGIncludes/DiskBlendingCG.cginc"
            #pragma vertex base_vert
            #pragma fragment normalize_frag
    /// ENDHEADER

    /// PROPERTIES
            sampler2D _MainTex;
    /// ENDPROPERTIES

    /// FRAGMENT
            base_fOUT normalize_frag (base_v2f i)
            {
                base_fOUT o;
                o.color = tex2D(_MainTex, i.texUV);
                NormalizeByAlpha(o.color);
                return o;
            }
    /// ENDFRAGMENT
            ENDCG
        }
/// ENDPASS
    }
}

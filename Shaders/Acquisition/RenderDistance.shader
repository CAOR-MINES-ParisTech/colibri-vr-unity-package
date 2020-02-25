/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org



/// <summary> 
/// Renders objects' distance into an RFloat texture.
/// </summary>
Shader "COLIBRIVR/Acquisition/RenderDistance"
{
    Properties
    {
        _CameraWorldXYZ("The world position (XYZ) of the camera", Vector) = (0, 0, 0, 0)
        _DistanceRange("The distance range, to clamp distance values in a 0-1 range", Vector) = (0, 0, 0, 0)
    }

    SubShader
    {
/// PASS
        Pass
        {
    /// HEADER
            CGPROGRAM
            #include "./../CGIncludes/CoreCG.cginc"
            #include "./../CGIncludes/CameraCG.cginc"
            #pragma vertex renderDistance_vert
            #pragma fragment renderDistance_frag
    ///ENDHEADER

    ///STRUCTS
            struct renderDistance_v2f
            {
                float2 texUV : TEXCOORD0;
                float3 worldXYZ : TEXCOORD1;
            };
    ///ENDSTRUCTS

    /// PROPERTIES
            float3 _CameraWorldXYZ;
            float2 _DistanceRange;
    ///ENDPROPERTIES

    /// VERTEX
            renderDistance_v2f renderDistance_vert(base_vIN i, out float4 clipXYZW : SV_POSITION)
            {
                clipXYZW = UnityObjectToClipPos(i.objectXYZW);
                renderDistance_v2f o;
                o.texUV = i.texUV;
                o.worldXYZ = mul(unity_ObjectToWorld, i.objectXYZW).xyz;
                return o;
            }
    /// ENDVERTEX

    /// FRAGMENT
            float_fOUT renderDistance_frag (renderDistance_v2f i)
            {
                float_fOUT o;
                float distanceFromCam = length(_CameraWorldXYZ - i.worldXYZ);
                o.color = EncodeClampedValueAs01NonLinear(distanceFromCam, _DistanceRange);
                return o;
            }
    /// ENDFRAGMENT
            ENDCG
        }
/// ENDPASS
    }
}

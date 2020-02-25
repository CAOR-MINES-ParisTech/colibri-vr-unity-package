/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org



/// <summary> 
/// 
/// </summary>
Shader "COLIBRIVR/Processing/GlobalTextureMap"
{
    Properties
    {
        _FocalLength ("Focal length", Float) = 0.0
        _ColorData ("Color data", 2DArray) = "white" {}
        _DepthData ("Depth data", 2DArray) = "white" {}
        _LocalScale ("Local scale", Vector) = (1, 1, 1)
        _SourceCamCount ("Number of source cameras", int) = 1
        _BlendCamCount ("Number of source cameras that will have a non-zero blending weight for a given fragment", int) = 1
        _MaxBlendAngle ("Maximum angle difference (degrees) between source ray and view ray for the color value to be blended", float) = 180.0
        _ResolutionWeight ("Relative impact of the {resolution} factor compared to the {angle difference} factor in the ULR algorithm", float) = 0.2
        _DepthCorrectionFactor ("Factor for depth correction", float) = 0.1
        _ExcludedSourceView ("Excluded source camera index", int) = -1
    }

    SubShader
    {

/// PASS
        Pass
        {
    /// HEADER
            CGPROGRAM
            #include "./../CGIncludes/ULRCG.cginc"
            #pragma vertex textureMap_vert
            #pragma fragment textureMap_frag
    /// ENDHEADER

    
    /// STRUCTS
        struct textureMap_vIN
        {
            float4 objectXYZW : POSITION;
            float3 objectNormalXYZ : NORMAL;
            float2 texUV : TEXCOORD0;
        };

        struct textureMap_v2f
        {
            float3 worldXYZ : TEXCOORD1;
            float3 worldNormalXYZ : TEXCOORD2;
        };
    /// ENDSTRUCTS

    /// PROPERTIES
        float _FocalLength;
    /// ENDPROPERTIES

    /// VERTEX
        textureMap_v2f textureMap_vert(textureMap_vIN i, out float4 clipXYZW : SV_POSITION)
        {
            textureMap_v2f o;
            o.worldXYZ = mul(unity_ObjectToWorld, i.objectXYZW).xyz;
            o.worldNormalXYZ = mul(unity_ObjectToWorld, i.objectNormalXYZ);
            float4 newWorldXYZW = float4(i.texUV.x - 0.5, i.texUV.y - 0.5, _FocalLength, 1);
            clipXYZW = UnityWorldToClipPos(newWorldXYZW);
            return o;
        }
    /// ENDVERTEX

    /// FRAGMENT
            base_fOUT textureMap_frag (textureMap_v2f i)
            {
                base_fOUT o;
                o.color = ComputeGlobalBlendedColor(i.worldXYZ, i.worldNormalXYZ);
                NormalizeByAlpha(o.color);
                if(o.color.a == 0)
                    clip(-1);
                return o;
            }
    /// ENDFRAGMENT
            ENDCG
        }
///ENDPASS
    }
}

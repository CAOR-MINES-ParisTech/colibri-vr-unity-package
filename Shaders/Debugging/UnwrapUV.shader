/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org



/// <summary> 
/// 
/// </summary>
Shader "COLIBRIVR/Debug/UnwrapUV"
{
    Properties
    {
        _TextureMap ("Texture map", 2D) = "white" {}
        _ColorLerpValue ("Color lerp value between white and texture map color", float) = 0
        _UnwrapLerpValue ("Unwrap lerp value between 3D position and 2D UV position", float) = 0
    }

    SubShader
    {

/// PASS
        Pass
        {
    /// HEADER
            CGPROGRAM
            #include "./../CGIncludes/CoreCG.cginc"
            #pragma vertex unwrap_vert
            #pragma fragment unwrap_frag
    /// ENDHEADER

    /// PROPERTIES
        sampler2D _TextureMap;
        float _ColorLerpValue;
        float _UnwrapLerpValue;
    /// ENDPROPERTIES

    /// VERTEX
        base_v2f unwrap_vert(base_vIN i, out float4 clipXYZW : SV_POSITION)
        {
            base_v2f o;
            o.texUV = i.texUV;
            float4 worldXYZW = mul(unity_ObjectToWorld, i.objectXYZW);
            float4 newWorldXYZW = float4(i.texUV.x - 0.5, i.texUV.y - 0.5, 0, 1);
            float lerpValue = saturate(_UnwrapLerpValue);
            newWorldXYZW = lerpValue * newWorldXYZW + (1 - lerpValue) * worldXYZW;
            clipXYZW = UnityWorldToClipPos(newWorldXYZW);
            return o;
        }
    /// ENDVERTEX

    /// FRAGMENT
            base_fOUT unwrap_frag (base_v2f i)
            {
                base_fOUT o;
                float lerpValue = saturate(_ColorLerpValue);
                o.color = lerpValue * tex2D(_TextureMap, i.texUV) + (1 - lerpValue) * 1;
                return o;
            }
    /// ENDFRAGMENT
            ENDCG
        }
///ENDPASS
    }
}

/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org



/// <summary> 
/// Converts a texture in the RFloat format to an ARGB color texture.
/// </summary>
Shader "COLIBRIVR/Acquisition/Convert01ToColor"
{
    Properties
    {
        _MainTex("The RFloat texture to be converted", 2D) = "white" {}
        _IsPrecise("Whether the color should be precise or just for visualization", int) = 1
    }

    SubShader
    {

/// PASS
        Pass
        {
    /// HEADER
            CGPROGRAM
            #include "./../CGIncludes/CoreCG.cginc"
            #include "./../CGIncludes/ColorCG.cginc"
            #pragma vertex base_vert
            #pragma fragment convertToColor_frag
    /// ENDHEADER

    /// PROPERTIES
            sampler2D _MainTex;
            uint _IsPrecise;
    /// ENDPROPERTIES

    /// FRAGMENT
            base_fOUT convertToColor_frag (base_v2f i)
            {
                base_fOUT o;
                float distanceNonlinear01 = tex2D(_MainTex, i.texUV).r;
                if(_IsPrecise == 1)
                    o.color = Encode01AsPreciseColor(distanceNonlinear01);
                else
                    o.color = Encode01AsPlasma(distanceNonlinear01);
                return o;
            }
    /// ENDFRAGMENT
            ENDCG
        }
///ENDPASS
    }
}

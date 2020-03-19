/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org



/// <summary> 
/// Converts a depth map encoding from precise to visualization.
/// </summary>
Shader "COLIBRIVR/Debugging/ConvertPreciseToVisualization"
{
    Properties
    {
        _MainTex("The depth map, precisely encoded as a color texture, to be converted", 2D) = "white" {}
        _MinMax01("The min and max distance values in the texture, to adjust the visualization factor", Vector) = (0, 1, 0, 0)
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
            #pragma fragment convertToVisualization_frag
    /// ENDHEADER

    /// PROPERTIES
            sampler2D _MainTex;
            float2 _MinMax01;
    /// ENDPROPERTIES

    /// FRAGMENT
            base_fOUT convertToVisualization_frag (base_v2f i)
            {
                base_fOUT o;
                float distanceNonlinear01 = Decode01FromPreciseColor(tex2D(_MainTex, i.texUV));
                float adjustedDistance01 = (distanceNonlinear01 - _MinMax01.x) / (_MinMax01.y - _MinMax01.x);
                o.color = Encode01AsPlasma(adjustedDistance01);
                return o;
            }
    /// ENDFRAGMENT
            ENDCG
        }
///ENDPASS
    }
}

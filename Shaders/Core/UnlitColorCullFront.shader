/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org



/// <summary> 
/// Unlit color shader with Cull Front.
/// </summary>
Shader "COLIBRIVR/Core/UnlitColorCullFront"
{
    Properties
    {
        _MainColor ("Color", Color) = (0, 0, 0, 0)
    }

    SubShader
    {

/// PASS
        Pass
        {
    /// HEADER
            Cull Front
            CGPROGRAM
            #include "./../CGIncludes/CoreCG.cginc"
            #pragma vertex base_vert
            #pragma fragment draw_frag
    /// ENDHEADER

    /// PROPERTIES
            fixed4 _MainColor;
    /// ENDPROPERTIES

    /// FRAGMENT
            base_fOUT draw_frag (base_v2f i)
            {
                base_fOUT o;
                o.color = _MainColor;
                return o;
            }
    /// ENDFRAGMENT
            ENDCG
        }
///ENDPASS
    }
}

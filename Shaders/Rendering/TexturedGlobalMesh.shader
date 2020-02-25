/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org



/// <summary> 
/// Renders the global mesh with the given unlit texture, except in points where this texture has alpha=0.
/// </summary>
Shader "COLIBRIVR/Rendering/TexturedGlobalMesh"
{
    Properties
    {
        _MainTex ("Main texture", 2D) = "white" {}
    }

    SubShader
    {

/// PASS
        Pass
        {
    /// HEADER
            CGPROGRAM
            #include "./../CGIncludes/CoreCG.cginc"
            #pragma vertex base_vert
            #pragma fragment draw_frag
    /// ENDHEADER

    /// PROPERTIES
            sampler2D _MainTex;
    /// ENDPROPERTIES

    /// FRAGMENT
            base_fOUT draw_frag (base_v2f i)
            {
                base_fOUT o;
                o.color = tex2D(_MainTex, i.texUV);
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

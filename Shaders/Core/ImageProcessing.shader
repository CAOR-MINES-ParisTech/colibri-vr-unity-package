/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org



/// <summary> 
/// Applies a digital image processing operation on the given texture.
/// </summary>
Shader "COLIBRIVR/Core/ImageProcessing"
{
    Properties
    {
        _MainTex ("Main texture", 2D) = "white" {}
        _PixelResolution ("Texture's pixel resolution", Vector) = (0, 0, 0, 0)
        _OperationType ("Type of the operation to apply", int) = 0
        _KernelType ("Type of the kernel to apply", int) = 0
        _IsZeroMask ("Whether or not to exclude applying the effect on pixels where the value is zero", int) = 1
        _IgnoreAlphaChannel ("Whether or not to ignore the alpha channel", int) = 0
    }

    SubShader
    {

/// PASS
        Pass
        {
    /// HEADER
            CGPROGRAM
            #include "./../CGIncludes/ImageProcessingCG.cginc"
            #pragma vertex base_vert
            #pragma fragment operation_frag
    /// ENDHEADER

    /// FRAGMENT

            float_fOUT operation_frag(base_v2f i)
            {
                float_fOUT o;
                o.color = ApplyImageProcessing(i.texUV);
                return o;
            }

    /// ENDFRAGMENT
            ENDCG
        }
///ENDPASS
    }
}

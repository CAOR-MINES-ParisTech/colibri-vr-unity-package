/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org



/// <summary>
/// Outputs the YCbCR evaluation metric as an RGB color texture, encoded using the plasma colormap.
/// </summary>
Shader "COLIBRIVR/Evaluation/YCbCr"
{
    Properties
    {
        _TextureOne ("Texture one", 2D) = "white" {}
        _TextureTwo ("Texture two", 2D) = "white" {}
        _MultFactor ("Multiplication factor", float) = 10.0
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
            #pragma fragment eval_frag
    /// ENDHEADER

    /// PROPERTIES
            sampler2D _TextureOne;
            sampler2D _TextureTwo;
            float _MultFactor;
    /// ENDPROPERTIES

    /// METHODS
            fixed3 RGBtoYCbCr(fixed3 inputRGB, float midValue)
            {
                const float wR = 0.299;
                const float wG = 0.587;
                const float wB = 0.114;
                const float uMax = 0.436;
                const float vMax = 0.615;
                float y = wR * inputRGB.r + wG * inputRGB.g + wB * inputRGB.b;
                float cb = uMax * (inputRGB.b - y) / (1.0 - wB);
                float cr = vMax * (inputRGB.r - y) / (1.0 - wR);
                cb = cb / (2.0 * uMax) + midValue;
                cr = cr / (2.0 * vMax) + midValue;
                fixed3 outputYCbCr = fixed3(y, cb, cr);
                return outputYCbCr;
            }
    /// ENDMETHODS

    /// FRAGMENT
            base_fOUT eval_frag (base_v2f i)
            {
                base_fOUT o;
                fixed3 oneRGB = tex2D(_TextureOne, i.texUV).rgb;
                fixed3 twoRGB = tex2D(_TextureTwo, i.texUV).rgb;
                if(length(oneRGB) == 0 || length(twoRGB) == 0)
                {
                    o.color = 0;
                }
                else
                {
                    fixed3 oneYCbCr = RGBtoYCbCr(oneRGB, 0.5);
                    fixed3 twoYCbCr = RGBtoYCbCr(twoRGB, 0.5);
                    float diff01 = _MultFactor * (abs(oneYCbCr.y - twoYCbCr.y) + abs(oneYCbCr.z - twoYCbCr.z));
                    o.color = Encode01AsPlasma(diff01);
                }
                return o;
            }
    /// ENDFRAGMENT
            ENDCG
        }
/// ENDPASS
    }
}

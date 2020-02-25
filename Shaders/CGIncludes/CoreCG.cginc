/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org



/// <summary>
/// Contains core structs and methods.
/// </summary>
#ifndef CORE_CG_INCLUDED
#define CORE_CG_INCLUDED

/// HEADER
    #include "UnityCG.cginc"
/// ENDHEADER

/// STRUCTS
    struct base_vIN
    {
        float4 objectXYZW : POSITION;
        float2 texUV : TEXCOORD0;
    };

    struct base_v2f
    {
        float2 texUV : TEXCOORD0;
    };

    struct base_fOUT
    {
        fixed4 color : SV_Target;
    };

    struct depth_fOUT
    {
        fixed4 color : SV_Target;
        float depth : SV_Depth;
    };

    struct float_fOUT
    {
        float4 color : SV_Target;
    };
/// ENDSTRUCTS

/// VERTEX
    base_v2f base_vert(base_vIN i, out float4 clipXYZW : SV_POSITION)
    {
        clipXYZW = UnityObjectToClipPos(i.objectXYZW);
        base_v2f o;
        o.texUV = i.texUV;
        return o;
    }
/// ENDVERTEX

/// FRAGMENT
    base_fOUT base_frag()
    {
        base_fOUT o;
        o.color = 0;
        return o;
    }
/// ENDFRAGMENT

/// METHODS

    /// <summary>
    /// Checks that the given texture UV is within typical UV bounds.
    /// </summary>
    inline bool CheckUVBounds(float2 texUV)
    {
        return (texUV.x >= 0 && texUV.y >= 0 && texUV.x <= 1 && texUV.y <= 1);
    }

    /// <summary>
    /// Checks that the given pixel ID is within the given resolution bounds.
    /// </summary>
    inline bool CheckPixelBounds(uint2 pixelID, uint2 pixelResolution)
    {
        return (pixelID.x >= 0 && pixelID.y >= 0 && pixelID.x < pixelResolution.x && pixelID.y < pixelResolution.y);
    }

    /// <summary>
    /// Returns the pixel ID corresponding to the given texture UV and pixel resolution.
    /// </summary>
    inline uint2 GetPixelID(float2 texUV, uint2 pixelResolution)
    {
        return round((pixelResolution - 1.0) * texUV);
    }

    /// <summary>
    /// Returns the signed angle, in degrees, between two input vectors.
    /// </summary>
    inline float GetDegreeAngleBetweenVectors(float3 vectDirA, float3 vectDirB)
    {
        float dotProduct = dot(vectDirA, vectDirB);
        float degreeAngle = sign(dotProduct) * degrees(acos(dotProduct / (length(vectDirA) * length(vectDirB))));
        return degreeAngle;
    }

    /// <summary>
    /// Normalizes the RGB channels of an inout color by its Alpha channel.
    /// </summary>
    inline void NormalizeByAlpha(inout fixed4 color)
    {
        if(color.a > 0)
            (color = color/color.a);
    }

    /// <summary> 
    /// Encodes an ID (two-dimensional space) to an index (one-dimensional space), given a starting index and a multiplication factor.
    /// </summary>
    inline uint EncodeIDToIndex(uint2 id, uint startIndex, uint indexFactor)
    {
        return startIndex + id.x + indexFactor * id.y;
    }

    /// <summary> 
    /// Decodes an ID from an index, given a starting index and a multiplication factor.
    /// </summary>
    inline uint2 DecodeIDFromIndex(uint index, uint startIndex, uint indexFactor)
    {
        uint temp = index - startIndex;
        return uint2(temp % indexFactor, temp / indexFactor);
    }

    /// <summary>
    /// Encodes a float linearly into a 0-1 range using the given set of limits.
    /// </summary>
    inline float EncodeClampedValueAs01Linear(float clampedValue, float2 clampLimits)
    {
        return (clampedValue - clampLimits.x) / (clampLimits.y - clampLimits.x);
    }

    /// <summary>
    /// Decodes a float from a 0-1 range, linearly encoded, using the given set of limits.
    /// </summary>
    inline float DecodeClampedValueFrom01Linear(float value01, float2 clampLimits)
    {
        return (clampLimits.y - clampLimits.x) * value01 + clampLimits.x;
    }

    /// <summary>
    /// Encodes a float into a 0-1 range using the given set of limits.
    /// The encoding is in 1/x instead of x, so that smaller values are given more precision. This makes it adapted for encoding distance/depth data.
    /// </summary>
    inline float EncodeClampedValueAs01NonLinear(float clampedValue, float2 clampLimits)
    {
        return (1.0/clampedValue - 1.0/clampLimits.x) / (1.0/clampLimits.y - 1.0/clampLimits.x);
    }

    /// <summary>
    /// Decodes a float from a 0-1 range using the given set of limits. The encoding is assumed to be in 1/x.
    /// </summary>
    inline float DecodeClampedValueFrom01NonLinear(float value01, float2 clampLimits)
    {
        return 1.0 / ((1.0/clampLimits.y - 1.0/clampLimits.x) * value01 + 1.0/clampLimits.x);
    }

/// ENDMETHODS

#endif // CORE_CG_INCLUDED

/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org



/// <summary>
/// Contains methods related to digital image processing.
/// </summary>
#ifndef IMAGE_PROCESSING_CG_INCLUDED
#define IMAGE_PROCESSING_CG_INCLUDED

/// HEADER
    #include "./../CGIncludes/CoreCG.cginc"
/// ENDHEADER

/// CONST

static const float3x3 GaussianBlurKernel =
{
    1/16.0, 2/16.0, 1/16.0,
    2/16.0, 4/16.0, 2/16.0,
    1/16.0, 2/16.0, 1/16.0
};

static const float3x3 BoxBlurKernel =
{
    1/9.0, 1/9.0, 1/9.0,
    1/9.0, 1/9.0, 1/9.0,
    1/9.0, 1/9.0, 1/9.0
};

/// ENDCONST

/// PROPERTIES
    sampler2D _MainTex;
    uint2 _PixelResolution;
    uint _OperationType;
    uint _KernelType;
    uint _IsZeroMask;
    uint _IgnoreAlphaChannel;
/// ENDPROPERTIES

/// METHODS

    /// <summary>
    /// Removes the specified channels from a given color.
    /// </summary>
    inline float4 RemoveChannels(float4 color, float4 channels)
    {
        return color * (1 - channels);
    }

    /// <summary>
    /// Resets the specified channels on the given color.
    /// </summary>
    inline float4 ResetChannels(float4 color, float4 channels)
    {
        return RemoveChannels(color, channels) + channels;
    }

    /// <summary>
    /// 
    /// </summary>
    inline float4 AddColor(float4 colorToAdd, float4 nullColor, float weight)
    {
        return weight * ResetChannels(colorToAdd, nullColor);
    }

    /// <summary>
    /// Applies an image processing operation the texture at the given UV, using parameters defined by the shader properties.
    /// </summary>
    inline void ApplyOperation(inout float4 outColor, inout float totalWeight, bool isZeroMask, float4 nullColor, float2 id, float3x3 kernel, float4 centerVal, float4 currentVal)
    {
        // Remove null channels to be able to compute lengths.
        centerVal = RemoveChannels(centerVal, nullColor);
        currentVal = RemoveChannels(currentVal, nullColor);
        // Determine whether to both (1) add the current color, weighted by the kernel, to the color of the center pixel, and (2) update the total weight.
        bool addWeightedColor = false;
        // OPERATIONTYPE 1: BLUR.
        if(_OperationType == 1)
        {
            // Add the weighted color only if either (1) (zeros are not masked) or (2) ((the current value is not null) and (the center value is not null)).
            addWeightedColor = (!isZeroMask || (length(currentVal) > 0 && length(centerVal) > 0));
        }
        // OPERATIONTYPE 2: MORPHOLOGICAL DILATION.
        else if (_OperationType == 2)
        {
            // Add the weighted color only if (1) (the center value is null) and (2) (the current value is not null).
            addWeightedColor = (length(centerVal) == 0 && length(currentVal) > 0);
        }
        // OPERATIONTYPE 3: MORPHOLOGICAL EROSION.
        else if (_OperationType == 3)
        {
            // If the current value is null, set the center pixel to null.
            if(length(currentVal) == 0)
                outColor = nullColor;
            // In any case, do not add any color.
            addWeightedColor = false;
        }
        // If desired, add the weighted color to the value of the center pixel.
        if(addWeightedColor)
        {
            float weight = kernel[id.x + 1][id.y + 1];
            outColor += AddColor(currentVal, nullColor, weight);
            totalWeight += weight;
        }
    }

    /// <summary>
    /// Applies the image processing operator to the image at the given texture UV.
    /// </summary>
    inline float4 ApplyImageProcessing(float2 texUV)
    {
        // Initialize values based on the shader properties.
        float2 texUVIncrement = 1.0 / _PixelResolution;
        bool isZeroMask = (_IsZeroMask == 1);
        float4 nullColor = (_IgnoreAlphaChannel == 1) ? float4(0, 0, 0, 1) : float4(0, 0, 0, 0);
        float3x3 kernel = (_KernelType == 0) ? GaussianBlurKernel : BoxBlurKernel;
        // Set the initial output color to the current value of the pixel, weighted by the specified kernel.
        float4 centerVal = tex2D(_MainTex, texUV);
        bool shouldAddWeight = (!isZeroMask || length(RemoveChannels(centerVal, nullColor)) > 0);
        float totalWeight = shouldAddWeight ? kernel[1][1] : 0;
        float4 outColor = AddColor(centerVal, nullColor, totalWeight);
        // Loop over all adjoining pixels.
        for(int i = -1; i <= 1; i++)
        {
            for(int j = -1; j <= 1; j++)
            {
                float2 id = float2(i, j);
                // Only do something if the selected pixel is not the center one.
                if(length(id) > 0)
                {
                    // Only do something if the selected pixel is within the texture.
                    float2 sampleUV = texUV + id * texUVIncrement;
                    if(CheckUVBounds(sampleUV))
                    {
                        // Add to the value by applying the specified operation.
                        float4 currentVal = tex2D(_MainTex, sampleUV);
                        ApplyOperation(outColor, totalWeight, isZeroMask, nullColor, id, kernel, centerVal, currentVal);
                    }
                }
            }
        }
        // Return the computed color divided by the total weight.
        if(totalWeight == 0)
            outColor = nullColor;
        else
            outColor /= totalWeight;
        return outColor;
    }

/// ENDMETHODS

#endif // IMAGE_PROCESSING_CG_INCLUDED

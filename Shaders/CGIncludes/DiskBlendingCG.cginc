/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org



/// <summary> 
/// Include file for shaders that use disk-based blending.
/// </summary>
#ifndef DISKBLENDING_CG_INCLUDED
#define DISKBLENDING_CG_INCLUDED

/// HEADER
    #include "./../CGIncludes/CoreCG.cginc"
    #include "./../CGIncludes/ColorCG.cginc"
    #include "./../CGIncludes/CameraCG.cginc"
/// ENDHEADER

/// PROPERTIES
    UNITY_DECLARE_TEX2DARRAY(_ColorData);
    UNITY_INSTANCING_BUFFER_START(InstanceProperties)
        UNITY_DEFINE_INSTANCED_PROP(int, _SourceCamIndex)
        UNITY_DEFINE_INSTANCED_PROP(float3, _SourceCamPosXYZ)
        UNITY_DEFINE_INSTANCED_PROP(int, _SourceCamIsOmnidirectional)
    UNITY_INSTANCING_BUFFER_END(InstanceProperties)
    uniform sampler2D _StoredColorTexture;
    uniform sampler2D _StoredDepthTexture;
    uniform float _MaxBlendAngle;
    uniform uint _ClipNullValues;
    uniform uint _SourceCamCount;
    uniform float _FocalLength;
    uniform uint _IsColorSourceCamIndices;
    uniform int _ExcludedSourceView;
    static const float _DepthFactor = 0.1;
    static const float _MinWeight = 0.001;
    static const float _ScalingMargin = 0.01;
/// ENDPROPERTIES

/// METHODS

    /// <summary>
    /// Scales the Z value, as a value between 0.0 (near) and 1.0 (far), so as to assign disjoint subranges of depth to each source camera.
    /// </summary>
    float ScaleZ01(uint renderingIndex, float z01)
    {
        float offset = _SourceCamCount - 1.0 - renderingIndex;
        return (offset + _ScalingMargin + (1.0 - 2.0 * _ScalingMargin) * z01) / _SourceCamCount;
    }

    /// <summary>
    /// Unscales the previously-scaled Z value.
    /// </summary>
    float UnscaleZ01(float scaledZ01)
    {
        float offset = floor(scaledZ01 * _SourceCamCount);
        return ((scaledZ01 * _SourceCamCount) - offset - _ScalingMargin) / (1.0 - 2.0 * _ScalingMargin);
    }

    /// <summary>
    /// Returns the depth value in normalized device coordinates from the scaled depth buffer.
    /// </summary>
    float ScaledDepthBufferToNDC(float scaledZ)
    {
        return Traditional01ToNDC(UnscaleZ01(ReverseDepthIfReversedBuffer(scaledZ)));
    }

    /// <summary>
    /// Returns a simple camera weight for the given point, using a constant angle threshold.
    /// </summary>
    float GetViewpointWeightForFragment(float4 fragmentWorldXYZW, float3 viewCamToSourceCamWorldXYZ, uint isOmnidirectional)
    {
        float3 viewCamToFragmentWorldXYZ = (fragmentWorldXYZW - _WorldSpaceCameraPos).xyz;
        float3 sourceCamToFragmentWorldXYZ = viewCamToFragmentWorldXYZ - viewCamToSourceCamWorldXYZ;
        float degreeAngle = GetDegreeAngleBetweenVectors(viewCamToSourceCamWorldXYZ, viewCamToFragmentWorldXYZ);
        degreeAngle = abs(degreeAngle);
        float oppositeWeight01 = 1.0;
        if(isOmnidirectional)
        {
            if(degreeAngle > 90.0)
                degreeAngle = 180.0 - degreeAngle;
            float maxLength = _FocalLength;
            oppositeWeight01 *= length(viewCamToSourceCamWorldXYZ) / maxLength;
        }
        oppositeWeight01 *= degreeAngle / _MaxBlendAngle;
        float weight01 = clamp(1.0 - oppositeWeight01, _MinWeight, 1.0);
        return weight01;
    }

    /// <summary>
    /// Based on the success of a soft ZTest, blends the color from the frame buffer with the color of the given point.
    /// </summary>
    fixed4 ComputeBlendedColor(float4 viewportXYZW, fixed4 meshColor)
    {
        fixed4 outColor = 0;
        // Get the stored color and depth for the frame.
        float2 screenUV = viewportXYZW.xy / _ScreenParams.xy;
        float4 frameColor = tex2D(_StoredColorTexture, screenUV);
        float frameDepth = tex2D(_StoredDepthTexture, screenUV).r;
        // If nothing has been drawn in this fragment, the stored Z is equal to zero.
        // The corresponding view-space depth is the far clip plane.
        float absFrameViewZ = _ProjectionParams.z;
        bool frameIsBackground = true;
        // If something has been drawn in this fragment, the stored Z is necessarily non-zero (thanks to _ScalingMargin).
        // In this case, the stored view-space depth can be computed by unscaling this stored value.
        if(frameDepth != ReverseDepthIfReversedBuffer(1.0))
        {
            absFrameViewZ = abs(LinearEyeDepth(ScaledDepthBufferToNDC(frameDepth)));
            frameIsBackground = false;
        }
        // Compare the stored value and the one to draw in view space to determine which is closer.
        float absMeshViewZ = abs(LinearEyeDepth(ScaledDepthBufferToNDC(viewportXYZW.z)));
        float furthestZ = max(absMeshViewZ, absFrameViewZ);
        float closestZ = min(absMeshViewZ, absFrameViewZ);
        float distanceFactor = furthestZ / closestZ - 1.0;
        bool meshFragmentCloseToExistingFragment = (distanceFactor < _DepthFactor);
        bool meshFragmentBehindExistingFragment = ((absMeshViewZ == furthestZ) && !meshFragmentCloseToExistingFragment);
        bool meshFragmentInFrontOfExistingFragment = ((absMeshViewZ == closestZ) && !meshFragmentCloseToExistingFragment);
        // Perform a soft Z-test, by blending the colors if the depth values are close together.
        if(meshFragmentBehindExistingFragment)
            clip(-1);
        else if(meshFragmentInFrontOfExistingFragment || frameIsBackground)
            outColor = meshColor;
        else if(meshFragmentCloseToExistingFragment)
            outColor = frameColor + meshColor;
        else
            outColor = fixed4(1, 0, 1, 1);
        return outColor;
    }

/// ENDMETHODS

#endif // DISKBLENDING_CG_INCLUDED

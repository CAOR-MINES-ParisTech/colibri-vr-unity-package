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
    /// Scales the Z value in NDC space to ensure that objects are not discarded because of the ZTest.
    /// Using ZTest Always or Blend One One was problematic for objects with self-occlusions. 
    /// </summary>
    float ScaleNormalizedDeviceZ(uint renderingIndex, float normalizedDeviceZ)
    {
        if(UNITY_NEAR_CLIP_VALUE < 0)   // OpenGL uses z ∈ [-1,1]
            normalizedDeviceZ = (normalizedDeviceZ + 1.0) * 0.5;

        normalizedDeviceZ = saturate(normalizedDeviceZ);

        return (renderingIndex + _ScalingMargin + (1.0 - _ScalingMargin) * normalizedDeviceZ) / _SourceCamCount;
    }

    /// <summary>
    /// Unscales the previously-scaled Z value in NDC space.
    /// </summary>
    float UnscaleNormalizedDeviceZ(float scaledNormalizedDeviceZ)
    {
        uint renderingIndex = floor(scaledNormalizedDeviceZ * _SourceCamCount);
        float unscaledNormalizedDeviceZ = (scaledNormalizedDeviceZ * _SourceCamCount - 1.0 * renderingIndex - _ScalingMargin) / (1.0 - _ScalingMargin);

        if(UNITY_NEAR_CLIP_VALUE < 0)   // OpenGL expects z ∈ [-1,1]
            unscaledNormalizedDeviceZ = 2.0 * unscaledNormalizedDeviceZ - 1.0;

        return unscaledNormalizedDeviceZ;
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
    fixed4 ComputeBlendedColor(float2 screenUV, float meshViewZ, fixed4 meshColor)
    {
        fixed4 outColor = 0;
        float4 frameColor = tex2D(_StoredColorTexture, screenUV);
        float frameStoredZ = tex2D(_StoredDepthTexture, screenUV).r;
        float frameViewZ = _ProjectionParams.z;

    /// Compensate for different depth buffer conventions used by Direct3D and OpenGL for initial depth test
    /// (LinearEyeDepth will then decode correct depth in platform-independent way)
    #if defined(UNITY_REVERSED_Z)
        float frameStoredZreversed = frameViewZ;
    #else
        float frameStoredZreversed = 1.0 - frameViewZ;
    #endif

        if(frameStoredZreversed >= _ScalingMargin)
        {
            float frameNormalizedDeviceZ = UnscaleNormalizedDeviceZ(frameStoredZ);
            frameViewZ = abs(LinearEyeDepth(frameNormalizedDeviceZ));
        }
        float furthestZ = max(meshViewZ, frameViewZ);
        float closestZ = min(meshViewZ, frameViewZ);
        float distanceFactor = furthestZ / closestZ - 1.0;
        bool meshFragmentCloseToExistingFragment = (distanceFactor < _DepthFactor);
        bool meshFragmentBehindExistingFragment = (meshViewZ == furthestZ && !meshFragmentCloseToExistingFragment);
        bool meshFragmentInFrontOfExistingFragment = (meshViewZ == closestZ && !meshFragmentCloseToExistingFragment);
        if(meshFragmentBehindExistingFragment)
            clip(-1);
        else if(meshFragmentInFrontOfExistingFragment)
            outColor = meshColor;
        else if(meshFragmentCloseToExistingFragment)
            outColor = frameColor + meshColor;
        else
            outColor = fixed4(1, 0, 1, 1);
        return outColor;
    }

/// ENDMETHODS

#endif // DISKBLENDING_CG_INCLUDED

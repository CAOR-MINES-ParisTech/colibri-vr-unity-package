/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org



/// <summary> 
/// Include file for the ULR shaders.
/// </summary>
#ifndef ULR_CG_INCLUDED
#define ULR_CG_INCLUDED

/// HEADER
    #include "./../CGIncludes/CoreCG.cginc"
    #include "./../CGIncludes/CameraCG.cginc"
    #include "./../CGIncludes/ColorCG.cginc"
/// ENDHEADER

/// PROPERTIES
    UNITY_DECLARE_TEX2DARRAY(_ColorData);
    UNITY_DECLARE_TEX2DARRAY(_DepthData);
    uniform sampler2D _GlobalTextureMap;
    uniform float3 _LossyScale;
    uniform float2 _DepthDataDimensionsInv;
    uniform uint _BlendCamCount;
    uniform float _MaxBlendAngle;
    uniform float _ResolutionWeight;
    uniform float _DepthCorrectionFactor;
    uniform float _GlobalTextureMapWeight;
    uniform uint _IsSceneViewCamera;
    uniform uint _IsColorSourceCamIndices;
    uniform float2 _BlendFieldComputationParams;
    uniform int _ExcludedSourceView;
    uniform uint _SourceCamCount;
    uniform StructuredBuffer<float3> _SourceCamsWorldXYZBuffer;
    uniform StructuredBuffer<float2> _SourceCamsFieldsOfViewBuffer;
    uniform StructuredBuffer<float4x4> _SourceCamsViewMatricesBuffer;
    uniform StructuredBuffer<float2> _SourceCamsDistanceRangesBuffer;
    uniform RWStructuredBuffer<float4> _VertexCamWeightsBuffer : register(u1);
    uniform RWStructuredBuffer<uint4> _VertexCamIndicesBuffer : register(u2);
    static const uint _MaxBlendCamCount = 4;
    static const float _MinWeight = 0.001;
/// ENDPROPERTIES

/// STRUCTS
    struct SourceCamContribution
    {
        nointerpolation uint index;
        float2 texUV;
        float weight;
    };

    struct SourceCamContributionsForTriangle
    {
        uint indexList[_MaxBlendCamCount];
        float3 weightList[_MaxBlendCamCount];
    };
/// ENDSTRUCTS

/// METHODS

    /// <summary>
    /// Checks whether a given position is clipped or within visible space.
    /// </summary>
    inline bool IsVisible(float4 clipPosXYZW)
    {
        float clipThreshold = 1.5;
        float2 absClipXY = abs(clipPosXYZW.xy / clipPosXYZW.w);
        if(absClipXY.x <= clipThreshold && absClipXY.y <= clipThreshold)
            return true;
        return false;
    }

    /// <summary>
    /// Assigns a weight to a given texture coordinate.
    /// The weight is equal to 0 outside the image, gradually fades in from the image's edges, and is equal to 1 within the rest of the image.
    /// </summary>
    inline float ComputeFOVBlend(float2 texUV, uint selectedCamIndex)
    {
        float fieldOfViewXY = _SourceCamsFieldsOfViewBuffer[selectedCamIndex];
        if(fieldOfViewXY.x > 180)
            return 1;
        float margin = 0.01;
        if(texUV.x < 0 + margin || texUV.x > 1 - margin || texUV.y < 0 + margin || texUV.y > 1 - margin)
            return 0;
        return 1;
        // float val = 1;
        // float fadeLimit = 0.1;
        // if(texUV.x < fadeLimit)
        //     val = texUV.x;
        // else if (texUV.x > 1 - fadeLimit)
        //     val = 1 - texUV.x;
        // if(texUV.y < fadeLimit)
        //     val = min(val, texUV.y);
        // else if (texUV.y > 1 - fadeLimit)
        //     val = min(val, 1 - texUV.y);
        // val = min(1, val / fadeLimit);
        // return val;
    }

    /// <summary>
    /// Computes the texture UV coordinate of a given point for a given source camera.
    /// </summary>
    inline float2 ComputeSourceTexUV(float3 pointWorldXYZ, uint selectedCamIndex)
    {
        float4 pointViewXYZW = mul(_SourceCamsViewMatricesBuffer[selectedCamIndex], float4(pointWorldXYZ, 1));
        float2 texUV = GetTexUV(pointViewXYZW.xyz, _SourceCamsFieldsOfViewBuffer[selectedCamIndex]);
        return texUV;
    }

    /// <summary>
    /// Computes an angle difference metric based on the current viewpoint, a point's world position, and a source camera.
    /// This metric is normalized by the maximum blend angle.
    /// </summary>
    inline float ComputeAngDiff(uint selectedCamIndex, float3 pointWorldXYZ, out float3 sourceCamToPointXYZ, out float3 viewCamToPointXYZ)
    {
        sourceCamToPointXYZ = pointWorldXYZ - _SourceCamsWorldXYZBuffer[selectedCamIndex];
        viewCamToPointXYZ = pointWorldXYZ - _WorldSpaceCameraPos.xyz;
        float angDiff = abs(GetDegreeAngleBetweenVectors(viewCamToPointXYZ, sourceCamToPointXYZ)) / _MaxBlendAngle;
        return angDiff;
    }

    /// <summary>
    /// Enhances the angle difference metric with a resolution metric.
    /// </summary>
    inline float ComputeAngResDiff(float3 sourceCamToPointXYZ, float3 viewCamToPointXYZ, float angDiff)
    {
        float resDiff = max(0, 1 - length(viewCamToPointXYZ) / length(sourceCamToPointXYZ));
        float angResDiff = (1.0 - _ResolutionWeight) * angDiff + _ResolutionWeight * resDiff;
        return angResDiff;
    }

    /// <summary>
    /// Checks whether the computed angle difference .
    /// </summary>
    inline bool CheckAngleDifference(float angDiff)
    {
        bool isWithinMaxAngle = (angDiff <= 1);
        return isWithinMaxAngle;
    }

    /// <summary>
    /// Checks whether the given point normal direction can be seen by the given camera.
    /// </summary>
    inline bool CheckIsFacingCorrectDir(float3 sourceCamToPointXYZ, float3 pointWorldNormalXYZ)
    {
        bool isFacingCorrectDir = (dot(sourceCamToPointXYZ, pointWorldNormalXYZ) < 0);
        return isFacingCorrectDir;
    }

    /// <summary>
    /// Checks whether the given point is within the given camera's field of view.
    /// </summary>
    inline bool CheckIsWithinFOV(uint selectedCamIndex, float3 pointWorldXYZ, out float2 sourceTexUV, out float FOVBlend)
    {
        sourceTexUV = ComputeSourceTexUV(pointWorldXYZ, selectedCamIndex);
        FOVBlend = ComputeFOVBlend(sourceTexUV, selectedCamIndex);
        bool isWithinFOV = (FOVBlend > _MinWeight);
        return isWithinFOV;
    }

    /// <summary>
    /// Checks whether a given point is seen by a given camera or is occluded by another object, based on the point's distance and the camera's depth map.
    /// </summary>
    inline bool CheckPointNotOccluded(float3 sourceCamToPointXYZ, float3 texArrayUVZ, float selectedCamIndex)
    {
        bool isNotOccludedForCamera = true;
        [unroll]
        for(int i = -1; i <= 1; i++)
        {
            [unroll]
            for(int j = -1; j <= 1; j++)
            {
                if(isNotOccludedForCamera)
                {
                    float3 shiftedTexArrayUVZ = float3(texArrayUVZ.xy + float2(i, j) * _DepthDataDimensionsInv, texArrayUVZ.z);
                    float expectedDistance01NonLinear = Decode01FromPreciseColor(UNITY_SAMPLE_TEX2DARRAY_LOD(_DepthData, shiftedTexArrayUVZ, 0));
                    if(expectedDistance01NonLinear < 1)
                    {
                        float expectedDistance = DecodeClampedValueFrom01NonLinear(expectedDistance01NonLinear, _SourceCamsDistanceRangesBuffer[selectedCamIndex]);
                        float realDistance = length(sourceCamToPointXYZ / _LossyScale);
                        bool isNotOccludedInThisPoint = (abs(expectedDistance - realDistance) < _DepthCorrectionFactor * expectedDistance);
                        isNotOccludedForCamera = isNotOccludedForCamera && isNotOccludedInThisPoint;
                    }
                }
            }
        }
        return isNotOccludedForCamera;
    }

    /// <summary>
    /// Computes a global output color for a given point, independent from the current viewpoint.
    /// </summary>
    inline float4 ComputeGlobalBlendedColor(float3 pointWorldXYZ, float3 pointWorldNormalXYZ)
    {
        float4 outColor = 0;
        // Loop over each one of the source cameras.
        for(uint sourceCamIndex = 0; sourceCamIndex < _SourceCamCount; sourceCamIndex++)
        {
            // Check if the source camera is not excluded from rendering.
            if((int)sourceCamIndex == _ExcludedSourceView)
                continue;
            float3 sourceCamToPointXYZ = pointWorldXYZ - _SourceCamsWorldXYZBuffer[sourceCamIndex];
            // Check if the point is facing towards the source camera.
            bool isFacingCorrectDir = CheckIsFacingCorrectDir(sourceCamToPointXYZ, pointWorldNormalXYZ);
            // Check if the point is within the source camera's field of view.
            float2 sourceTexUV;
            float FOVBlend;
            bool isWithinFOV = CheckIsWithinFOV(sourceCamIndex, pointWorldXYZ, sourceTexUV, FOVBlend);
            // Check if the point is not occluded from this camera's view.
            float3 sourceTexArrayUVZ = float3(sourceTexUV, sourceCamIndex);
            bool isNotOccluded = CheckPointNotOccluded(sourceCamToPointXYZ, sourceTexArrayUVZ, sourceCamIndex);
            // If all the checks are valid, add this source camera's color.
            if(isFacingCorrectDir && isWithinFOV && isNotOccluded)
            {
                float weight = 1.0 / length(sourceCamToPointXYZ);
                outColor += weight * float4(UNITY_SAMPLE_TEX2DARRAY(_ColorData, sourceTexArrayUVZ).rgb, 1);
            }
        }
        return outColor;
    }

    inline uint GetBlendCamCount()
    {
        return min(_BlendCamCount, _MaxBlendCamCount);
    }

    inline bool ShouldBlendFieldBeComputedInVertex(uint vertexID)
    {
        uint blendFieldFrontVertexIndex = _BlendFieldComputationParams.x;
        uint nextBlendFieldFrontVertexIndex = _BlendFieldComputationParams.y;
        bool singleRange = (blendFieldFrontVertexIndex < nextBlendFieldFrontVertexIndex);
        bool afterFrontVertex = (vertexID >= blendFieldFrontVertexIndex);
        bool beforeNextFrontVertex = (vertexID <= nextBlendFieldFrontVertexIndex);
        bool vertexShouldComputeBlendingField = singleRange ? (afterFrontVertex && beforeNextFrontVertex) : (afterFrontVertex || beforeNextFrontVertex);     
        return vertexShouldComputeBlendingField;
    }

    inline void ComputeCamWeightsForVertex(float3 worldXYZ, float3 worldNormalXYZ, bool checkOcclusion, out SourceCamContribution sourceCamContributions[_MaxBlendCamCount])
    {
        uint blendCamCount = min(_BlendCamCount, _MaxBlendCamCount);
        // Initialize several arrays.
        float angResDiffArray[_MaxBlendCamCount + 1];
        float3 UVAndSourceIndexArray[_MaxBlendCamCount + 1];
        float FOVBlendArray[_MaxBlendCamCount + 1];
        for(uint blendCamIndex = 0; blendCamIndex < blendCamCount; blendCamIndex++)
        {
            angResDiffArray[blendCamIndex] = 1;
            UVAndSourceIndexArray[blendCamIndex] = 0;
            FOVBlendArray[blendCamIndex] = 0;
        }
        // Loop over each one of the source cameras to select the most relevant ones to use for blending.
        for(uint sourceCamIndex = 0; sourceCamIndex < _SourceCamCount; sourceCamIndex++)
        {
            // Check if the source camera is not excluded from rendering.
            if((int)sourceCamIndex == _ExcludedSourceView)
                continue;
            // Check if the angle difference is below the threshold.
            float3 sourceCamToPointXYZ;
            float3 viewCamToPointXYZ;
            float angDiff = ComputeAngDiff(sourceCamIndex, worldXYZ, sourceCamToPointXYZ, viewCamToPointXYZ);
            bool isWithinMaxAngle = CheckAngleDifference(angDiff);
            // Check if the point is facing towards the source camera.
            bool isFacingCorrectDir = CheckIsFacingCorrectDir(sourceCamToPointXYZ, worldNormalXYZ);
            // Check if the point is within the source camera's field of view.
            float2 sourceTexUV;
            float FOVBlend;
            bool isWithinFOV = CheckIsWithinFOV(sourceCamIndex, worldXYZ, sourceTexUV, FOVBlend);
            // Check if the point is not occluded from this camera's view.
            float3 sourceTexArrayUVZ = float3(sourceTexUV, sourceCamIndex);
            bool isNotOccluded = true;
            if(checkOcclusion)
                isNotOccluded = CheckPointNotOccluded(sourceCamToPointXYZ, sourceTexArrayUVZ, sourceCamIndex);
            // If all the checks are valid, compare the camera's relevance with that of the other most relevant cameras.
            if(isWithinMaxAngle && isFacingCorrectDir && isWithinFOV && isNotOccluded)
            {
                // Calculate the relevance metric for this source camera.
                float angResDiff = ComputeAngResDiff(sourceCamToPointXYZ, viewCamToPointXYZ, angDiff);
                // Loop over the sorted array of most relevant cameras.
                bool placed = false;
                for(blendCamIndex = 0; blendCamIndex < blendCamCount; blendCamIndex++)
                {
                    // Check if the source camera is more relevant than the one in the array.
                    bool isBetterThanCurrentBlendCam = (angResDiff < angResDiffArray[blendCamIndex]);
                    // If so, place the source camera in the array, and move all lesser cameras down by one increment.
                    if(!placed && isBetterThanCurrentBlendCam)
                    {
                        for(uint successiveIndex = 0; successiveIndex < blendCamCount - blendCamIndex; successiveIndex ++)
                        {
                            uint replacedIndex = blendCamCount - successiveIndex;
                            uint replacingIndex = replacedIndex - 1;
                            angResDiffArray[replacedIndex] = angResDiffArray[replacingIndex];
                            UVAndSourceIndexArray[replacedIndex] = UVAndSourceIndexArray[replacingIndex];
                            FOVBlendArray[replacedIndex] = FOVBlendArray[replacingIndex];
                        }
                        angResDiffArray[blendCamIndex] = angResDiff;
                        UVAndSourceIndexArray[blendCamIndex] = sourceTexArrayUVZ;
                        FOVBlendArray[blendCamIndex] = FOVBlend;
                        placed = true;
                    }
                }
            }
        }
        //
        float angResThresh = angResDiffArray[_BlendCamCount];
        [unroll]
        for(blendCamIndex = 0; blendCamIndex < blendCamCount; blendCamIndex++)
        {
            float angResBlend = max(0, 1 - angResDiffArray[blendCamIndex] / angResThresh);
            float weight = angResBlend * FOVBlendArray[blendCamIndex];
            sourceCamContributions[blendCamIndex].index = UVAndSourceIndexArray[blendCamIndex].z;
            sourceCamContributions[blendCamIndex].texUV = UVAndSourceIndexArray[blendCamIndex].xy;
            sourceCamContributions[blendCamIndex].weight = weight;
        }
    }

    inline void TransferArraysToBuffers(uint vertexID, SourceCamContribution sourceCamContributions[_MaxBlendCamCount])
    {
        uint blendCamCount = min(_BlendCamCount, _MaxBlendCamCount);
        [unroll]
        for(uint blendCamIndex = 0; blendCamIndex < blendCamCount; blendCamIndex++)
        {
            SourceCamContribution sourceCamContribution = sourceCamContributions[blendCamIndex];
            _VertexCamIndicesBuffer[vertexID][blendCamIndex] = sourceCamContribution.index;
            _VertexCamWeightsBuffer[vertexID][blendCamIndex] = sourceCamContribution.weight;
        }
    }

    inline void SortIndexWeightsListForTriangle(uint3 vertexIDs, out SourceCamContributionsForTriangle sourceCamContributionsForTriangle)
    {
        uint indexList[3 * _MaxBlendCamCount];
        float3 weightList[3 * _MaxBlendCamCount];
        [unroll]
        for(uint listIter = 0; listIter < 3 * _MaxBlendCamCount; listIter++)
        {
            indexList[listIter] = _SourceCamCount;
            weightList[listIter] = 0;
        }
        uint blendCamCount = GetBlendCamCount();
        uint listCount = 0;
        [unroll]
        for(uint vertexIter = 0; vertexIter < 3; vertexIter++)
        {
            uint vertexID = vertexIDs[vertexIter];
            [unroll]
            for(uint blendCamIndex = 0; blendCamIndex < blendCamCount; blendCamIndex++)
            {
                uint sourceCamIndex = _VertexCamIndicesBuffer[vertexID][blendCamIndex];
                float sourceCamWeight = _VertexCamWeightsBuffer[vertexID][blendCamIndex];
                uint listIndex = 0;
                bool alreadyDone = false;
                for(uint listIter = 0; listIter < listCount; listIter++)
                {
                    if(!alreadyDone && indexList[listIter] == sourceCamIndex)
                    {
                        listIndex = listIter;
                        alreadyDone = true;
                    }
                }
                if(!alreadyDone)
                {
                    listIndex = listCount;
                    indexList[listIndex] = sourceCamIndex;
                    listCount++;
                }
                weightList[listIndex][vertexIter] = sourceCamWeight;
            }
        }
        [unroll]
        for(uint iterA = 0; iterA < listCount; iterA++)
        {
            float triangleWeightA = dot(weightList[iterA], 1);
            bool placed = false;
            for(uint iterB = 0; iterB < iterA; iterB++)
            {
                if(!placed)
                {
                    float3 triangleWeightsB = weightList[iterB];
                    float triangleWeightB = dot(triangleWeightsB, 1);
                    if(triangleWeightB < triangleWeightA)
                    {
                        uint triangleIndexB = indexList[iterB];
                        indexList[iterB] = indexList[iterA];
                        weightList[iterB] = weightList[iterA];
                        indexList[iterA] = triangleIndexB;
                        weightList[iterA] = triangleWeightsB;
                        placed = true;
                    }
                }
            }
        }
        [unroll]
        for(listIter = 0; listIter < _MaxBlendCamCount; listIter++)
        {
            sourceCamContributionsForTriangle.indexList[listIter] = indexList[listIter];
            sourceCamContributionsForTriangle.weightList[listIter] = weightList[listIter];
        }
    }

    inline float4 ComputeColorFromVertexCamWeights(SourceCamContribution sourceCamContributions[_MaxBlendCamCount], bool checkOcclusion, float3 pointWorldXYZ)
    {
        float4 outColor = 0;
        uint blendCamCount = GetBlendCamCount();
        [unroll]
        for(uint blendCamIndex = 0; blendCamIndex < blendCamCount; blendCamIndex++)
        {
            SourceCamContribution sourceCamContribution = sourceCamContributions[blendCamIndex];
            uint sourceCamIndex = sourceCamContribution.index;
            if(sourceCamIndex < _SourceCamCount)
            {
                float sourceCamWeight = saturate(sourceCamContribution.weight);
                float2 sourceCamTexUV = saturate(sourceCamContribution.texUV);
                float3 sourceTexArrayUVZ = float3(sourceCamTexUV, sourceCamIndex);
                bool isNotOccluded = true;
                if(checkOcclusion)
                {
                    float3 sourceCamToPointXYZ = pointWorldXYZ - _SourceCamsWorldXYZBuffer[sourceCamIndex];
                    isNotOccluded = CheckPointNotOccluded(sourceCamToPointXYZ, sourceTexArrayUVZ, sourceCamIndex);
                }
                if(isNotOccluded)
                {
                    float3 colorRGB = 0;
                    if(_IsColorSourceCamIndices == 1 && _IsSceneViewCamera == 1)
                        colorRGB = GetColorForIndex(sourceCamIndex, _SourceCamCount);
                    else
                        colorRGB = UNITY_SAMPLE_TEX2DARRAY_LOD(_ColorData, sourceTexArrayUVZ, 0).rgb;
                    outColor += sourceCamWeight * float4(colorRGB, 1);
                }
            }
        }
        return outColor;
    }

    /// <summary>
    /// Merges the given color with that of the texture map.
    /// </summary>
    inline void MergeColorWithGlobalTextureMap(inout fixed4 color, float2 texUV)
    {
        fixed4 textureMapColor = tex2D(_GlobalTextureMap, texUV);
        color = ((1 - _GlobalTextureMapWeight) * color + _GlobalTextureMapWeight * textureMapColor);
        NormalizeByAlpha(color);
    }

/// ENDMETHODS

#endif // ULR_CG_INCLUDED

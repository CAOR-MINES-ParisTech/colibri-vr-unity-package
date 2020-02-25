/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org



/// <summary>
/// Contains methods related to camera projection.
/// </summary>
#ifndef CAMERA_CG_INCLUDED
#define CAMERA_CG_INCLUDED

/// PERSPECTIVEMETHODS 

    /// <summary>
    /// For a perspective camera: returns the focal length corresponding to the given field of view and sensor size.
    /// </summary>
    inline float2 Perspective_FieldOfViewToFocalLength(float2 fieldOfViewXY, float2 sensorSizeXY)
    {
        return 0.5 * sensorSizeXY / tan(0.5 * radians(fieldOfViewXY));
    }

    /// <summary>
    /// For a perspective camera: returns the field of view corresponding to the given focal length and sensor size.
    /// </summary>
    inline float2 Perspective_FocalLengthToFieldOfView(float2 focalLengthXY, float2 sensorSizeXY)
    {
        return 2.0 * degrees(atan(0.5 * sensorSizeXY / focalLengthXY));
    }

    /// <summary>
    /// For a perspective camera: extracts the camera's field of view from its projection matrix.
    /// </summary>
    inline float2 Perspective_ProjectionMatrixToFOV(float4x4 cameraProjectionMatrix)
    {
        float2 focalLengthXY = float2(cameraProjectionMatrix._m00, cameraProjectionMatrix._m11);
        return Perspective_FocalLengthToFieldOfView(focalLengthXY, 2);
    }

    /// <summary>
    /// For a perspective camera: returns the XYZ position of a point in view space, given its texture UV and distance/depth.
    /// </summary>
    inline float3 Perspective_GetViewXYZ(float distanceOrViewZ, bool isViewZ, float2 texUV, float2 fieldOfViewXY)
    {
        // Compute the XY focal length for an image plane of dimensions 2x2 with the given XY field of view.
        float2 focalLengthXY = Perspective_FieldOfViewToFocalLength(fieldOfViewXY, 2);
        // Get the view-space XYZ for a point on the image plane at viewZ=1.
        float3 viewXYZOnImagePlaneZ1 = float3((2.0 * texUV - 1.0) / focalLengthXY, 1.0);
        // Deduce and return the view-space XYZ of the point with given distance or viewZ.
        if(isViewZ)
            return distanceOrViewZ * viewXYZOnImagePlaneZ1;
        else
            return distanceOrViewZ * normalize(viewXYZOnImagePlaneZ1);
    }

    /// <summary>
    /// For a perspective camera: returns the texture UV corresponding to a given position in local camera space.
    /// </summary>
    inline float2 Perspective_GetTexUV(float3 viewXYZ, float2 fieldOfViewXY)
    {
        // If the position is behind the camera, i.e. on an image plane at viewZ<0, do not compute a texture UV.
        if(viewXYZ.z < 0)
            return -1;
        // Get the view-space XYZ for a point on the image plane at viewZ=1.
        float3 viewXYZOnImagePlaneZ1 = viewXYZ / viewXYZ.z;
        // Compute the XY focal length for an image plane of dimensions 2x2 with the given XY field of view.
        float2 focalLengthXY = Perspective_FieldOfViewToFocalLength(fieldOfViewXY, 2);
        // Deduce and return the texture UV.
        float2 texUV = 0.5 * (focalLengthXY * viewXYZOnImagePlaneZ1.xy + 1.0);
        return texUV;
    }

/// ENDPERSPECTIVEMETHODS

/// OMNIDIRECTIONALMETHODS

    /// <summary>
    /// For an omnidirectional camera: returns the XYZ position of a point in view space, given its texture UV and distance from the camera center.
    /// This is regular cartesian to spherical, adapted to Unity's coordinate system that has X axis rightward, Y axis upward, Z axis forward.
    /// </summary>
    inline float3 Omnidirectional_GetViewXYZ(float distance, float2 texUV)
    {
        // Longitude: Long=0 at U=0, Long=PI at U=0.5, Long=2PI at U=1.
        // Latitude: Lat=0 at V=0, Lat=PI/2 at V=0.5, Lat=PI at V=1.
        float2 longlat = texUV * float2(2.0 * UNITY_PI, UNITY_PI);
        // Compute sin and cos of these values.
        float2 sinLongLat = sin(longlat);
        float2 cosLongLat = cos(longlat);
        // Compute XYZ coordinates in local camera space.
        float normalizedViewX = - sinLongLat.y * sinLongLat.x;
        float normalizedViewY = - cosLongLat.y;
        float normalizedViewZ = - sinLongLat.y * cosLongLat.x;
        // Return viewXYZ.
        float3 viewXYZ = distance * float3(normalizedViewX, normalizedViewY, normalizedViewZ);
        return viewXYZ;
    }

    /// <summary>
    /// For an omnidirectional camera: returns the texture UV corresponding to a given position in local camera space.
    /// </summary>
    inline float2 Omnidirectional_GetTexUV(float3 viewXYZ)
    {
        float3 normalizedViewXYZ = normalize(viewXYZ);
        // Compute longitude and latitude.
        float long = atan2(normalizedViewXYZ.x, normalizedViewXYZ.z) + UNITY_PI;
        float lat = acos(- normalizedViewXYZ.y);
        // Return texUV.
        float2 texUV = float2(long, lat) / float2(2.0 * UNITY_PI, UNITY_PI);
        return texUV;
    }

/// ENDOMNIDIRECTIONALMETHODS

/// GENERALMETHODS

    /// <summary>
    /// Returns the XYZ position of a point in view space, given its texture UV and distance/depth.
    /// </summary>
    inline float3 GetViewXYZ(float distanceOrViewZ, bool isViewZ, float2 texUV, float2 fieldOfViewXY)
    {
        if(fieldOfViewXY.x > 180)
            return Omnidirectional_GetViewXYZ(distanceOrViewZ, texUV);
        return Perspective_GetViewXYZ(distanceOrViewZ, isViewZ, texUV, fieldOfViewXY);
    }

    /// <summary>
    /// Returns the texture UV corresponding to a given position in local camera space.
    /// </summary>
    inline float2 GetTexUV(float3 viewXYZ, float2 fieldOfViewXY)
    {
        if(fieldOfViewXY.x > 180)
            return Omnidirectional_GetTexUV(viewXYZ);
        return Perspective_GetTexUV(viewXYZ, fieldOfViewXY);
    }

/// ENDGENERALMETHODS

#endif // CAMERA_CG_INCLUDED

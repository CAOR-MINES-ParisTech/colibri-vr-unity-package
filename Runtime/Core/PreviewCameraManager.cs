/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

using System.Collections.Generic;
using UnityEngine;

namespace COLIBRIVR
{

    /// <summary>
    /// Class that manages the creation, update and destruction of a preview camera, given a camera model.
    /// </summary>
    [ExecuteInEditMode]
    public class PreviewCameraManager : MonoBehaviour
    {

#region STATIC_METHODS

        /// <summary>
        /// Creates or resets a preview camera manager object as a child of the given transform.
        /// </summary>
        /// <param name="parentTransform"></param> The parent transform.
        /// <returns></returns> The preview camera manager object.
        public static PreviewCameraManager CreateOrResetPreviewCameraManager(Transform parentTransform = null)
        {
            PreviewCameraManager existingManager = GeneralToolkit.GetComponentInChildrenOnly<PreviewCameraManager>(parentTransform);
            if(existingManager == null)
                existingManager = GeneralToolkit.CreateChildComponent<PreviewCameraManager>(parentTransform);
            return existingManager;
        }

#endregion //STATIC_METHODS

#region FIELDS

        public Camera previewCamera;
        public RenderTexture targetTexture;

        private CameraModel _cameraModel;
        private List<GameObject> _otherCameraGOs;

#endregion //FIELDS

#if UNITY_EDITOR

#region METHODS

        /// <summary>
        /// Creates the preview camera given a camera model.
        /// </summary>
        /// <param name="callerGO"></param> The caller gameobject.
        /// <param name="previewCameraTransform"></param> The transform on which to create a preview camera.
        /// <param name="sourceCameraModel"></param> The camera model with which to update the preview camera.
        public void CreatePreviewCamera(GameObject callerGO, Transform previewCameraTransform, CameraModel sourceCameraModel)
        {
            // The preview camera is only created if it is not already so.
            if(previewCamera == null)
            {
                // Set the camera's hide flags.
                previewCameraTransform.gameObject.hideFlags = HideFlags.HideAndDontSave;
                // Indicate that the camera object's parent is the caller object.
                previewCameraTransform.parent = callerGO.transform;
                // Set up the camera component on the given transform object.
                previewCamera = GeneralToolkit.GetOrAddComponent<Camera>(previewCameraTransform.gameObject);
                previewCamera.hideFlags = previewCameraTransform.gameObject.hideFlags;
                // Indicate that this camera is monoscopic, even in VR mode.
                previewCamera.stereoTargetEye = StereoTargetEyeMask.None;
                // Disable the camera component so that it only renders when necessary.
                previewCamera.enabled = false;
                // Update the camera object from the parameters of the camera model.
                UpdateCameraModel(sourceCameraModel, false);
            }
        }
        
        /// <summary>
        /// Updates the preview camera with a given set of camera models.
        /// </summary>
        /// <param name="sourceCameraModel"></param> The camera model with which to update the preview camera.
        /// <param name="useFullResolution"></param> Whether to use the full camera model resolution or limit it by the maximum preview resolution.
        public void UpdateCameraModel(CameraModel sourceCameraModel, bool useFullResolution)
        {
            if(previewCamera != null)
            {
                // Store the camera model for further use.
                _cameraModel = sourceCameraModel;
                // Set up the camera's parameters and target texture from the given camera model.
                previewCamera.targetTexture = null;
                _cameraModel.TransferParametersToCamera(ref previewCamera);
                Vector2Int resolution = useFullResolution ? _cameraModel.pixelResolution : PreviewWindow.GetPreviewResolution(_cameraModel.pixelResolution);
                GeneralToolkit.CreateRenderTexture(ref targetTexture, resolution, targetTexture.depth, targetTexture.format, targetTexture.sRGB==false, targetTexture.filterMode, targetTexture.wrapMode);
                previewCamera.targetTexture = targetTexture;
            }
        }

        /// <summary>
        /// Destroys the preview camera and associated objects.
        /// </summary>
        public void DestroyPreviewCamera()
        {
            if(previewCamera != null)
                GameObject.DestroyImmediate(previewCamera.gameObject);
            if(targetTexture != null)
                RenderTexture.DestroyImmediate(targetTexture);
        }

        /// <summary>
        /// Renders the preview camera (with associated camera model) to a specified target texture.
        /// </summary>
        /// <param name="destTexture"></param> The destination texture in which to render the preview camera.
        /// <param name="isDepth"></param> True if the preview camera should render depth instead of color, false otherwise.
        public void RenderPreviewToTarget(ref RenderTexture destTexture, bool isDepth)
        {
            // Only render the novel view if there is a preview camera.
            if(previewCamera == null)
                return;
            // Activate the preview camera.
            previewCamera.enabled = true;
            CameraClearFlags clearFlags = previewCamera.clearFlags;
            // If the capture is for a depth map, not a color image, set up the corresponding process.
            List<MonoBehaviour> deactivatedComponents = new List<MonoBehaviour>();
            if(isDepth)
            {
                // To do so, deactivate useless components that may interfere with the computation of depth, such as image effects.
                MonoBehaviour[] components = previewCamera.GetComponents<MonoBehaviour>();
                foreach(MonoBehaviour component in components)
                {
                    if(component != previewCamera && component.isActiveAndEnabled)
                    {
                        deactivatedComponents.Add(component);
                        component.enabled = false;
                    }
                }
                // Set a replacement shader that renders distance from the camera.
                Vector3 xyz = previewCamera.transform.position;
                Shader.SetGlobalVector("_CameraWorldXYZ", new Vector4(xyz.x, xyz.y, xyz.z, 0f));
                Shader.SetGlobalVector("_DistanceRange", new Vector4(_cameraModel.distanceRange.x, _cameraModel.distanceRange.y, 0f, 0f));
                previewCamera.clearFlags = CameraClearFlags.Color;
                previewCamera.backgroundColor = Color.white;
                previewCamera.SetReplacementShader(GeneralToolkit.shaderAcquisitionRenderDistance, string.Empty);
            }
            // If the capture is omnidirectional, set up the corresponding process.
            if(_cameraModel.isOmnidirectional)
            {
                // Render the camera to a cubemap.
                RenderTexture cubemap = new RenderTexture(1, 1, 0);
                int cubeWidth = destTexture.width / 4;
                GeneralToolkit.CreateRenderTexture(ref cubemap, new Vector2Int(cubeWidth, cubeWidth), destTexture.depth, destTexture.format, destTexture.sRGB==false, destTexture.filterMode, destTexture.wrapMode);
                cubemap.dimension = UnityEngine.Rendering.TextureDimension.Cube;
                previewCamera.stereoSeparation = 0f;
                previewCamera.RenderToCubemap(cubemap, 63, Camera.MonoOrStereoscopicEye.Left);
                // Convert the cubemap to a single equirectangular texture.
                cubemap.ConvertToEquirect(destTexture);
                RenderTexture.DestroyImmediate(cubemap);
            }
            // Otherwise, simply render the perspective camera.
            else
            {
                previewCamera.targetTexture = destTexture;
                previewCamera.Render();
            }
            // Reset the preview camera, and any deactivated components.
            previewCamera.targetTexture = targetTexture;
            previewCamera.clearFlags = clearFlags;
            previewCamera.ResetReplacementShader();
            foreach(MonoBehaviour component in deactivatedComponents)
                component.enabled = true;
            RenderTexture.active = null;
            // Deactivate the preview camera.
            previewCamera.enabled = false;
        }

#endregion //METHODS

#endif //UNITY_EDITOR

    }

}

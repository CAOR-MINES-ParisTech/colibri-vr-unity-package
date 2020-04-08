/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using COLIBRIVR.Processing;

namespace COLIBRIVR.Rendering
{

    /// <summary>
    /// Class that implements the Disk-Blended Focal Surfaces rendering method.
    /// </summary>
    public class DiskBlendedFocalSurfaces : RenderingMethod
    {

#region FIELDS

        [SerializeField] private Helper_CommandBuffer _helperCommandBuffer;
        [SerializeField] private Helper_DiskBlending _helperDiskBlending;
        [SerializeField] private Helper_FocalSurfaces _helperFocalSurfaces;

        private bool _initialized = false;

#endregion //FIELDS
        
#region INHERITANCE_METHODS

#if UNITY_EDITOR

        /// <inheritdoc/>
        public override GUIContent GetGUIInfo()
        {
            string label = "Disk-blended focal surfaces";
            string tooltip = "Blends the focal surfaces using a disk-based blending field.";
            return new GUIContent(label, tooltip);
        }

        /// <inheritdoc/>
        public override bool HasAdditionalParameters()
        {
            return true;
        }

        /// <inheritdoc/>
        public override void SectionAdditionalParameters()
        {
            // Add an inspector section to enable the user to choose a focal length.
            SerializedObject serializedObject = new SerializedObject(_helperFocalSurfaces);
            _helperFocalSurfaces.SectionFocalLength(serializedObject);
            serializedObject.ApplyModifiedProperties();
            // Add an inspector section to enable the user to choose a max blending angle.
            serializedObject = new SerializedObject(_helperDiskBlending);
            _helperDiskBlending.SectionDiskBlending(serializedObject);
            serializedObject.ApplyModifiedProperties();
        }

#endif //UNITY_EDITOR

        /// <inheritdoc/>
        public override void Reset()
        {
            base.Reset();
            // Initialize the helper methods.
            _helperCommandBuffer = GeneralToolkit.GetOrAddComponent<Helper_CommandBuffer>(gameObject);
            _helperCommandBuffer.Reset();
            _helperDiskBlending = GeneralToolkit.GetOrAddComponent<Helper_DiskBlending>(gameObject);
            _helperDiskBlending.Reset();
            _helperFocalSurfaces = GeneralToolkit.GetOrAddComponent<Helper_FocalSurfaces>(gameObject);
            _helperFocalSurfaces.Reset();
        }

        /// <inheritdoc/>
        public override void InitializeLinks()
        {
            base.InitializeLinks();
            // Initialize links for the helper methods.
            _helperCommandBuffer.InitializeLinks();
            _helperDiskBlending.InitializeLinks();
            _helperFocalSurfaces.InitializeLinks();
            // Define the scene representation methods.
            sceneRepresentationMethods = new ProcessingMethod[] { PMColorTextureArray, PMPerViewMeshesFS };
        }
        
        /// <inheritdoc/>
        public override IEnumerator InitializeRenderingMethodCoroutine()
        {
            // Initialize the command buffer.
            _helperCommandBuffer.InitializeCommandBuffer(CameraEvent.BeforeForwardOpaque);
            // Load the scene representation.
            yield return StartCoroutine(LoadSceneRepresentationCoroutine());
            // Initialize the blending material.
            InitializeMaterial();
            // Indicate that this method has finished initialization.
            _initialized = true;
        }

        /// <inheritdoc/>
        public override void UpdateRenderingMethod()
        {
            if(_initialized)
            {
#if UNITY_EDITOR
                // Update whether the colors represent the camera indices.
                cameraSetup.SetColorIsIndices(ref blendingMaterial);
#endif //UNITY_EDITOR
                // Update the command buffer.
                UpdateCommandBuffer();
                // Update the transforms of the focal surface to match the focal length.
                _helperFocalSurfaces.UpdateFocalSurfaceTransforms(PMPerViewMeshesFS.meshTransforms);
            }
        }

        /// <inheritdoc/>
        public override void ClearRenderingMethod()
        {
            base.ClearRenderingMethod();
            // Clear the command buffer.
            _helperCommandBuffer.ClearCommandBuffer();
        }

#endregion //INHERITANCE_METHODS

#region METHODS

        /// <summary>
        /// Loads a fitting scene representation from the bundled assets.
        /// </summary>
        /// <returns></returns>
        private IEnumerator LoadSceneRepresentationCoroutine()
        {
            // Load the scene representation.
            yield return StartCoroutine(PMColorTextureArray.LoadProcessedTextureArrayCoroutine());
            yield return StartCoroutine(PMPerViewMeshesFS.LoadProcessedFocalSurfacesCoroutine());
            // Deactivate the created geometry.
            _helperCommandBuffer.DeactivateCreatedGeometry(PMPerViewMeshesFS.meshTransforms[0].parent);
            // Store information on the focal surfaces created by the scene representation.
            _helperFocalSurfaces.StoreInformationOnFocalSurfaces(cameraSetup.cameraModels, PMPerViewMeshesFS.meshTransforms);
        }

        /// <summary>
        /// Initializes the blending material.
        /// </summary>
        private void InitializeMaterial()
        {
            // Create the blending material from the corresponding shader.
            blendingMaterial = new Material(GeneralToolkit.shaderRenderingDiskBlendedFocalSurfaces);
            // Because many focal surfaces use the same mesh, enable instancing.
            blendingMaterial.enableInstancing = true;
            // Store the color data.
            blendingMaterial.SetTexture(Processing.ColorTextureArray.shaderNameColorData, PMColorTextureArray.colorData);
        }

        /// <summary>
        /// Updates the command buffer.
        /// </summary>
        private void UpdateCommandBuffer()
        {
            // Determine which cameras are omnidirectional.
            List<float> sourceCamAreOmnidirectional = new List<float>();
            foreach(CameraModel cameraModel in cameraSetup.cameraModels)
                sourceCamAreOmnidirectional.Add(cameraModel.isOmnidirectional ? 1 : 0);
            // Determine camera parameters to pass to the material as properties.
            List<float> sourceCamIndices;
            List<Vector4> sourceCamPositions;
            List<Matrix4x4> meshTransformationMatrices;
            _helperDiskBlending.UpdateBlendingParameters(ref blendingMaterial, cameraSetup.cameraModels, PMPerViewMeshesFS.meshTransforms, out sourceCamIndices, out sourceCamPositions, out meshTransformationMatrices);
            // Update the blending material with the current focal length.
            _helperFocalSurfaces.SendFocalLengthToBlendingMaterial(ref blendingMaterial);
            // Indicate the cameras' indices and positions.
            MaterialPropertyBlock properties = new MaterialPropertyBlock();
            properties.SetFloatArray(_shaderNameSourceCamIndex, sourceCamIndices);
            properties.SetVectorArray(_shaderNameSourceCamPosXYZ, sourceCamPositions);
            properties.SetFloatArray(_shaderNameSourceCamIsOmnidirectional, sourceCamAreOmnidirectional);
            // Clear the instructions in the command buffer.
            _helperCommandBuffer.commandBuffer.Clear();
            // Copy the camera target to a temporary render texture, e.g. to copy the skybox in the scene view.
            int tempID = Shader.PropertyToID("TempCopyColorBuffer");
            _helperCommandBuffer.commandBuffer.GetTemporaryRT(tempID, -1, -1, 0, FilterMode.Bilinear);
            _helperCommandBuffer.commandBuffer.SetRenderTarget(tempID);
            _helperCommandBuffer.commandBuffer.ClearRenderTarget(true, true, Color.clear);
            _helperCommandBuffer.commandBuffer.Blit(BuiltinRenderTextureType.CameraTarget, tempID);
            // Clear the camera target's color and depth buffers.
            _helperCommandBuffer.commandBuffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
            _helperCommandBuffer.commandBuffer.ClearRenderTarget(true, true, Color.clear);
            // Render the focal surfaces to the depth and color buffer using GPU instancing.
            _helperCommandBuffer.commandBuffer.DrawMeshInstanced(PMPerViewMeshesFS.meshTransforms[0].GetComponent<MeshFilter>().sharedMesh, 0, blendingMaterial, 0, meshTransformationMatrices.ToArray(), PMPerViewMeshesFS.meshTransforms.Length, properties);
            // Normalize the RGB channels of the color buffer by the alpha channel, by copying into a temporary render texture.
            // Note: Be sure to use ZWrite Off. Blit renders a quad, and thus - if ZWrite On - provides the target with the quad's depth, not the render texture's depth.
            _helperCommandBuffer.commandBuffer.Blit(BuiltinRenderTextureType.CameraTarget, tempID, blendingMaterial, 1);
            _helperCommandBuffer.commandBuffer.Blit(tempID, BuiltinRenderTextureType.CameraTarget);
            _helperCommandBuffer.commandBuffer.ReleaseTemporaryRT(tempID);
        }

#endregion //METHODS

    }

}

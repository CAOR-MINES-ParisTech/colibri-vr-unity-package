/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using COLIBRIVR.Processing;

namespace COLIBRIVR.Rendering
{

    /// <summary>
    /// Class that implements the Textured Focal Surfaces rendering method.
    /// </summary>
    public class TexturedFocalSurfaces : RenderingMethod
    {

#region FIELDS

        [SerializeField] private Helper_FocalSurfaces _helperFocalSurfaces;

        private Texture2D _colorTex;
        private bool _initialized = false;

#endregion //FIELDS
        
#region INHERITANCE_METHODS

#if UNITY_EDITOR

        /// <inheritdoc/>
        public override GUIContent GetGUIInfo()
        {
            string label = "Textured focal surfaces";
            string tooltip = "Provides each focal surface with the corresponding source image as an unlit texture.";
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
        }

#endif //UNITY_EDITOR

        /// <inheritdoc/>
        public override void Reset()
        {
            base.Reset();
            // Initialize the helper methods.
            _helperFocalSurfaces = GeneralToolkit.GetOrAddComponent<Helper_FocalSurfaces>(gameObject);
            _helperFocalSurfaces.Reset();
        }

        /// <inheritdoc/>
        public override void InitializeLinks()
        {
            base.InitializeLinks();
            // Initialize links for the helper methods.
            _helperFocalSurfaces.InitializeLinks();
            // Define the scene representation methods.
            sceneRepresentationMethods = new ProcessingMethod[] { PMColorTextureArray, PMPerViewMeshesFS };
        }

        /// <inheritdoc/>
        public override IEnumerator InitializeRenderingMethodCoroutine()
        {
            // Load the scene representation.
            yield return StartCoroutine(LoadSceneRepresentationCoroutine());
            // Initialize the blending material.
            InitializeMaterial();
            // Assign the material to the loaded focal surfaces.
            AssignMaterialToGeometricProxy();
            // Indicate that this method has finished initialization.
            _initialized = true;
        }

        /// <inheritdoc/>
        public override void UpdateRenderingMethod()
        {
            if(_initialized)
            {
                // Update the transforms of the focal surface to match the focal length.
                _helperFocalSurfaces.UpdateFocalSurfaceTransforms(PMPerViewMeshesFS.meshTransforms);
            }
        }

        /// <inheritdoc/>
        public override void ClearRenderingMethod()
        {
            base.ClearRenderingMethod();
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
            // Store information on the focal surfaces created by the scene representation.
            _helperFocalSurfaces.StoreInformationOnFocalSurfaces(cameraSetup.cameraModels, PMPerViewMeshesFS.meshTransforms);
        }

        /// <summary>
        /// Initializes the blending material.
        /// </summary>
        private void InitializeMaterial()
        {
            // Create the blending material from the corresponding shader.
            blendingMaterial = new Material(GeneralToolkit.shaderRenderingTexturedProxies);
            // Because many focal surfaces use the same mesh, enable instancing.
            blendingMaterial.enableInstancing = true;
            // Store the color data.
            blendingMaterial.SetTexture(ColorTextureArray.shaderNameColorData, PMColorTextureArray.colorData);
        }

        /// <summary>
        /// Assigns the blending material to each focal surface.
        /// </summary>
        private void AssignMaterialToGeometricProxy()
        {
            MaterialPropertyBlock properties = new MaterialPropertyBlock();
            for(int sourceCamIndex = 0; sourceCamIndex < PMPerViewMeshesFS.meshTransforms.Length; sourceCamIndex++)
            {
                MeshRenderer meshRenderer = PMPerViewMeshesFS.meshTransforms[sourceCamIndex].gameObject.AddComponent<MeshRenderer>();
                meshRenderer.material = blendingMaterial;
                properties.SetFloat(_shaderNameSourceCamIndex, sourceCamIndex);
                meshRenderer.SetPropertyBlock(properties);
            }
        }

#endregion //METHODS

    }

}

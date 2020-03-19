/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

using System.Collections;
using UnityEngine;
using COLIBRIVR.Processing;

namespace COLIBRIVR.Rendering
{

    /// <summary>
    /// Class that implements the Textured Per-View Meshes rendering method.
    /// </summary>
    public class TexturedPerViewMeshes : RenderingMethod
    {

#region FIELDS

        protected Texture2D _colorTex;

#endregion //FIELDS
        
#region INHERITANCE_METHODS

#if UNITY_EDITOR

        /// <inheritdoc/>
        public override GUIContent GetGUIInfo()
        {
            string label = "Textured per-view meshes";
            string tooltip = "Provides each per-view mesh with the corresponding source image as an unlit texture.";
            return new GUIContent(label, tooltip);
        }

        /// <inheritdoc/>
        public override bool HasAdditionalParameters()
        {
            return false;
        }

        /// <inheritdoc/>
        public override void SectionAdditionalParameters()
        {
            
        }

#endif //UNITY_EDITOR

        /// <inheritdoc/>
        public override void InitializeLinks()
        {
            base.InitializeLinks();
            // Define the scene representation methods.
            sceneRepresentationMethods = new ProcessingMethod[] { PMColorTextureArray, PMPerViewMeshesQSTR };
        }

        /// <inheritdoc/>
        public override IEnumerator InitializeRenderingMethodCoroutine()
        {
            // Load the scene representation.
            yield return StartCoroutine(LoadSceneRepresentationCoroutine());
            // Initialize the blending material.
            InitializeMaterial();
            // Send the texture array to the material.
            UpdateMaterialWithColorData();
            // Assign the material to the loaded focal surfaces.
            AssignMaterialToGeometricProxy();
        }

        /// <inheritdoc/>
        public override void UpdateRenderingMethod()
        {

        }

        /// <inheritdoc/>
        public override void ClearRenderingMethod()
        {
            base.ClearRenderingMethod();
        }

        /// <summary>
        /// Initializes the blending material.
        /// </summary>
        public virtual void InitializeMaterial()
        {
            blendingMaterial = new Material(GeneralToolkit.shaderRenderingTexturedProxies);
        }

#endregion //INHERITANCE_METHODS

#region METHODS

        /// <summary>
        /// Loads a fitting scene representation from the bundled assets.
        /// </summary>
        /// <returns></returns>
        private IEnumerator LoadSceneRepresentationCoroutine()
        {
            yield return StartCoroutine(PMColorTextureArray.LoadProcessedTextureArrayCoroutine());
            yield return StartCoroutine(PMPerViewMeshesQSTR.LoadProcessedPerViewMeshesCoroutine());
        }

        /// <summary>
        /// Updates the blending material with the color texture array.
        /// </summary>
        private void UpdateMaterialWithColorData()
        {
            blendingMaterial.SetTexture(ColorTextureArray.shaderNameColorData, PMColorTextureArray.colorData);
        }

        /// <summary>
        /// Assigns the blending material to each focal surface.
        /// </summary>
        private void AssignMaterialToGeometricProxy()
        {
            MaterialPropertyBlock properties = new MaterialPropertyBlock();
            for(int sourceCamIndex = 0; sourceCamIndex < PMPerViewMeshesQSTR.perViewMeshTransforms.Length; sourceCamIndex++)
            {
                MeshRenderer meshRenderer = PMPerViewMeshesQSTR.perViewMeshTransforms[sourceCamIndex].gameObject.AddComponent<MeshRenderer>();
                meshRenderer.material = blendingMaterial;
                properties.SetFloat(_shaderNameSourceCamIndex, sourceCamIndex);
                meshRenderer.SetPropertyBlock(properties);
            }
        }

#endregion //METHODS

    }

}

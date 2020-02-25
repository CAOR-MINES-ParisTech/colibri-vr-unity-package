/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using COLIBRIVR.Processing;

namespace COLIBRIVR.Rendering
{

    /// <summary>
    /// Class that implements the Textured Global Mesh rendering method.
    /// </summary>
    public class TexturedGlobalMesh : RenderingMethod
    {

#region FIELDS

        private Material[] _submeshMaterials;

#endregion //FIELDS
        
#region INHERITANCE_METHODS

#if UNITY_EDITOR

        /// <inheritdoc/>
        public override GUIContent GetGUIInfo()
        {
            string label = "Textured global mesh";
            string tooltip = "Provides the global mesh with the corresponding texture map.";
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
            sceneRepresentationMethods = new ProcessingMethod[] { PMGlobalMeshEF, PMGlobalTextureMap };
        }

        /// <inheritdoc/>
        public override IEnumerator InitializeRenderingMethodCoroutine()
        {
            // Load the scene representation.
            yield return StartCoroutine(LoadSceneRepresentationCoroutine());
            // Initialize the materials.
            InitializeMaterials();
            // Assign the materials to the loaded submeshes.
            AssignMaterialsToGeometricProxy();
            // Add a mesh collider.
            AddMeshCollider();
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

#endregion //INHERITANCE_METHODS

#region METHODS

        /// <summary>
        /// Loads a fitting scene representation from the bundled assets.
        /// </summary>
        /// <returns></returns>
        private IEnumerator LoadSceneRepresentationCoroutine()
        {
            // Load the scene representation.
            yield return StartCoroutine(PMGlobalTextureMap.LoadProcessedTextureMapsCoroutine());
            yield return StartCoroutine(PMGlobalMeshEF.LoadProcessedGlobalMeshCoroutine());
        }

        /// <summary>
        /// Initializes a material for each submesh.
        /// </summary>
        private void InitializeMaterials()
        {
            _submeshMaterials = new Material[PMGlobalMeshEF.globalMesh.subMeshCount];
            for(int i = 0; i < _submeshMaterials.Length; i++)
            {
                _submeshMaterials[i] = new Material(GeneralToolkit.shaderRenderingTexturedGlobalMesh);
                _submeshMaterials[i].SetTexture("_MainTex", PMGlobalTextureMap.textureMaps[i]);
            }
        }

        /// <summary>
        /// Assigns the materials to the submeshes.
        /// </summary>
        private void AssignMaterialsToGeometricProxy()
        {
            MeshRenderer meshRenderer = PMGlobalMeshEF.globalMeshTransform.gameObject.AddComponent<MeshRenderer>();
            meshRenderer.materials = _submeshMaterials;
        }

        /// <summary>
        /// Adds a mesh collider to the global mesh.
        /// </summary>
        private void AddMeshCollider()
        {
            MeshCollider meshCollider = PMGlobalMeshEF.globalMeshTransform.gameObject.AddComponent<MeshCollider>();
            PMGlobalMeshEF.globalMesh.UploadMeshData(true);
        }

#endregion //METHODS

    }

}

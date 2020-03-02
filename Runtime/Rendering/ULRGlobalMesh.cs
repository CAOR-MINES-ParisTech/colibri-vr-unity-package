/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace COLIBRIVR.Rendering
{

    /// <summary>
    /// Class that implements the Unstructured Lumigraph Rendering (on Global Mesh) rendering method.
    /// </summary>
    public class ULRGlobalMesh : RenderingMethod
    {

#region FIELDS

        [SerializeField] private Helper_ULR _helperULR;

#endregion //FIELDS
        
#region INHERITANCE_METHODS

#if UNITY_EDITOR

        /// <inheritdoc/>
        public override GUIContent GetGUIInfo()
        {
            string label = "ULR on global mesh";
            string tooltip = "Performs view-dependent rendering on the global mesh using the Unstructured Lumigraph Rendering algorithm.";
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
            // Add an inspector section to enable the user to choose ULR blending parameters.
            SerializedObject serializedObject = new SerializedObject(_helperULR);
            _helperULR.SectionULRBlendingParameters(serializedObject);
            serializedObject.ApplyModifiedProperties();
        }

#endif //UNITY_EDITOR

        /// <inheritdoc/>
        public override void Reset()
        {
            base.Reset();
            // Initialize the helper methods.
            _helperULR = GeneralToolkit.GetOrAddComponent<Helper_ULR>(gameObject);
            _helperULR.Reset();
        }

        /// <inheritdoc/>
        public override void InitializeLinks()
        {
            base.InitializeLinks();
            _helperULR.InitializeLinks();
            sceneRepresentationMethods = _helperULR.GetULRSceneRepresentationMethods();
        }

        /// <inheritdoc/>
        public override IEnumerator InitializeRenderingMethodCoroutine()
        {
            // Load the scene representation, and thereby instantiate a geometric proxy.
            yield return StartCoroutine(_helperULR.LoadSceneRepresentationCoroutine());
            // Create the buffers used for blending.
            _helperULR.CreateULRBuffersAndArrays();
            // Create the blending material and assign it to the geometric proxy.
            _helperULR.ResetBlendingMaterial(ref blendingMaterial);
            // Indicate that this method has finished initialization.
            _helperULR.initialized = true;
        }

        /// <inheritdoc/>
        public override void UpdateRenderingMethod()
        {

        }

        /// <inheritdoc/>
        public override void ClearRenderingMethod()
        {
            // Clear the blending material.
            base.ClearRenderingMethod();
            // Clear other created objects.
            _helperULR.ClearAll();
        }

#endregion //INHERITANCE_METHODS

    }

}

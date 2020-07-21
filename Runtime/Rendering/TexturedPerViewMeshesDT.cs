/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

using System.Collections;
using UnityEngine;
using UnityEditor;
using COLIBRIVR.Processing;

namespace COLIBRIVR.Rendering
{

    /// <summary>
    /// Class that implements the Textured Per-View Meshes with Disocclusion Triangles rendering method.
    /// </summary>
    public class TexturedPerViewMeshesDT : TexturedPerViewMeshes
    {

#region CONST_FIELDS

        private const string _propertyNameUseDebugColor = "_useDebugColor";
        private const string _shaderNameUseDebugColor = "_UseDebugColor";
        private const string _propertyNameMipMapLevel = "_mipMapLevel";
        private const string _shaderNameMipMapLevel = "_MipMapLevel";

#endregion //CONST_FIELDS

#region FIELDS

        [SerializeField] private bool _useDebugColor;
        [SerializeField] private int _mipMapLevel;
        [SerializeField] private Helper_DisocclusionTriangles _helperDisocclusionTriangles;

#endregion //FIELDS
        
#region INHERITANCE_METHODS

#if UNITY_EDITOR

        /// <inheritdoc/>
        public override GUIContent GetGUIInfo()
        {
            string label = "Textured per-view meshes with disocclusion triangles";
            string tooltip = "Provides each per-view mesh with the corresponding source image as an unlit texture, and enables visualizing/blurring disocclusion triangles.";
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
            SerializedObject serializedObject = new SerializedObject(this);
            // Enable the user to assign a debug color to the disocclusion triangles.
            string label = "Debug color:";
            string tooltip = "Whether to use a debug color to clearly identify the disocclusion triangles.";
            SerializedProperty propertyUseDebugColor = serializedObject.FindProperty(_propertyNameUseDebugColor);
            propertyUseDebugColor.boolValue = EditorGUILayout.Toggle(new GUIContent(label, tooltip), propertyUseDebugColor.boolValue);
            // Enable the user to select the mip map level for blurring the color of disocclusion triangles.
            if(!propertyUseDebugColor.boolValue)
            {
                label = "Mip level:";
                tooltip = "The mip map level to use for blurring the color of the disocclusion triangles.";
                SerializedProperty propertyMipMapLevel = serializedObject.FindProperty(_propertyNameMipMapLevel);
                propertyMipMapLevel.intValue = EditorGUILayout.IntSlider(new GUIContent(label, tooltip), propertyMipMapLevel.intValue, 0, 4);
            }
            // Enable the user to select parameters for detecting disocclusion triangles.
            SerializedObject serializedObjectHelper = new SerializedObject(_helperDisocclusionTriangles);
            _helperDisocclusionTriangles.SectionDisocclusionTriangles(serializedObjectHelper);
            serializedObjectHelper.ApplyModifiedProperties();
            // Apply the modified properties.
            serializedObject.ApplyModifiedProperties();
        }

#endif //UNITY_EDITOR

        /// <inheritdoc/>
        public override void Reset()
        {
            base.Reset();
            _useDebugColor = false;
            _mipMapLevel = 2;
            // Initialize the helper methods.
            _helperDisocclusionTriangles = GeneralToolkit.GetOrAddComponent<Helper_DisocclusionTriangles>(gameObject);
            _helperDisocclusionTriangles.Reset();
        }

        /// <inheritdoc/>
        public override void InitializeLinks()
        {
            base.InitializeLinks();
            // Initialize links for the helper methods.
            _helperDisocclusionTriangles.InitializeLinks();
        }

        /// <inheritdoc/>
        public override void UpdateRenderingMethod()
        {
            base.UpdateRenderingMethod();
            UpdateMaterialParameters();
        }

        /// <inheritdoc/>
        public override void InitializeMaterial()
        {
            blendingMaterial = new Material(GeneralToolkit.shaderRenderingTexturedPerViewMeshesDT);
        }

        /// <inheritdoc/>
        private protected override void AssignMaterialToGeometricProxy()
        {
            MaterialPropertyBlock properties = new MaterialPropertyBlock();
            for(int sourceCamIndex = 0; sourceCamIndex < PMPerViewMeshesQSTR.perViewMeshTransforms.Length; sourceCamIndex++)
            {
                MeshRenderer meshRenderer = PMPerViewMeshesQSTR.perViewMeshTransforms[sourceCamIndex].gameObject.AddComponent<MeshRenderer>();
                meshRenderer.material = blendingMaterial;
                properties.SetFloat(_shaderNameSourceCamIndex, sourceCamIndex);
                properties.SetVector(_shaderNameSourceCamPosXYZ, PMPerViewMeshesQSTR.perViewMeshTransforms[sourceCamIndex].position);
                meshRenderer.SetPropertyBlock(properties);
            }
        }


#endregion //INHERITANCE_METHODS

#region METHODS

        /// <summary>
        /// Updates the parameters of the blending material.
        /// </summary>
        private void UpdateMaterialParameters()
        {
            blendingMaterial.SetInt(_shaderNameUseDebugColor, _useDebugColor ? 1 : 0);
            blendingMaterial.SetInt(_shaderNameMipMapLevel, _mipMapLevel);
            _helperDisocclusionTriangles.UpdateMaterialParameters(ref blendingMaterial);
        }

#endregion //METHODS

    }

}

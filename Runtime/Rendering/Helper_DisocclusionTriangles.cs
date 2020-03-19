/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

using UnityEngine;
using UnityEditor;

namespace COLIBRIVR.Rendering
{

    /// <summary>
    /// Helper class to be called by methods that require handling disocclusion triangles.
    /// </summary>
    public class Helper_DisocclusionTriangles : Method
    {

#region CONST_FIELDS

        private const string _propertyNameOrthogonalityParameter = "_orthogonalityParameter";
        private const string _shaderNameOrthogonalityParameter = "_OrthogonalityParameter";
        private const string _propertyNameTriangleSizeParameter = "_triangleSizeParameter";
        private const string _shaderNameTriangleSizeParameter = "_TriangleSizeParameter";

#endregion //CONST_FIELDS

#region FIELDS

        [SerializeField] private float _orthogonalityParameter;
        [SerializeField] private float _triangleSizeParameter;

#endregion //FIELDS

#region INHERITANCE_METHODS

        /// <inheritdoc/>
        public override void Reset()
        {
            base.Reset();
            _orthogonalityParameter = 0.1f;
            _triangleSizeParameter = 0.1f;
        }

#endregion //INHERITANCE_METHODS
          
#region METHODS

#if UNITY_EDITOR

        /// <summary>
        /// Enables the user to choose base parameters for disocclusion triangle handling.
        /// </summary>
        /// <param name="serializedObject"></param> The serialized object on which to find the properties to modify.
        public void SectionDisocclusionTriangles(SerializedObject serializedObject)
        {
            // Enable the user to choose the value of the orthogonality parameter for the triangle removal step.
            string label = "Orthog. param.: ";
            string tooltip = "Orthogonality parameter, that prevents the display of triangles that face away from the acquisition camera.";
            SerializedProperty propertyOrthogonalityParameter = serializedObject.FindProperty(_propertyNameOrthogonalityParameter);
            propertyOrthogonalityParameter.floatValue = EditorGUILayout.Slider(new GUIContent(label, tooltip), propertyOrthogonalityParameter.floatValue, 0f, 1f);
            // Enable the user to choose the value of the triangle size parameter for the triangle removal step.
            label = "Size param.: ";
            tooltip = "Triangle size parameter, that excludes triangles from being discarded if they are small enough.";
            SerializedProperty propertyTriangleSizeParameter = serializedObject.FindProperty(_propertyNameTriangleSizeParameter);
            propertyTriangleSizeParameter.floatValue = EditorGUILayout.Slider(new GUIContent(label, tooltip), propertyTriangleSizeParameter.floatValue, 0f, 1f);
        }

#endif //UNITY_EDITOR

        /// <summary>
        /// Updates the given material with the parameters for disocclusion triangle handling.
        /// </summary>
        /// <param name="material"></param> The material to update.
        public void UpdateMaterialParameters(ref Material material)
        {
            material.SetFloat(_shaderNameOrthogonalityParameter, _orthogonalityParameter);
            material.SetFloat(_shaderNameTriangleSizeParameter, _triangleSizeParameter);
        }

        /// <summary>
        /// Updates the given compute shader with the parameters for disocclusion triangle handling.
        /// </summary>
        /// <param name="computeShader"></param> The compute shader to update.
        public void UpdateComputeShaderParameters(ref ComputeShader computeShader)
        {
            computeShader.SetFloat(_shaderNameOrthogonalityParameter, _orthogonalityParameter);
            computeShader.SetFloat(_shaderNameTriangleSizeParameter, _triangleSizeParameter);
        }

#endregion //METHODS

    }

}
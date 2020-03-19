/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace COLIBRIVR.Rendering
{

    /// <summary>
    /// Helper class to be called by blending methods that rely on disk-based blending.
    /// </summary>
    public class Helper_DiskBlending : Method
    {

#region CONST_FIELDS

        private const string _propertyNameClipNullValues = "_clipNullValues";
        private const string _shaderNameClipNullValues = "_ClipNullValues";

#endregion //CONST_FIELDS

#region FIELDS

        [SerializeField] private float _maxBlendAngle;
        [SerializeField] private bool _clipNullValues;

#endregion //FIELDS

#region INHERITANCE_METHODS

        /// <inheritdoc/>
        public override void Reset()
        {
            base.Reset();
            _maxBlendAngle = 20f;
            _clipNullValues = false;
        }

#endregion //INHERITANCE_METHODS
          
#region METHODS

#if UNITY_EDITOR

        /// <summary>
        /// Enables the user to choose base parameters for disk blending.
        /// </summary>
        /// <param name="serializedObject"></param> The serialized object on which to find the properties to modify.
        public void SectionDiskBlending(SerializedObject serializedObject)
        {
            // Enable the user to choose the maximum blending angle.
            string label = "Max. blend angle:";
            string tooltip = "Maximum angle difference (degrees) between source ray and view ray for the color value to be blended.";
            SerializedProperty propertyMaxBlendAngle= serializedObject.FindProperty(_propertyNameMaxBlendAngle);
            propertyMaxBlendAngle.floatValue = EditorGUILayout.Slider(new GUIContent(label, tooltip), propertyMaxBlendAngle.floatValue, 1f, 180f);
            // Enable the user to choose whether null values should be clipped or displayed as black.
            label = "Clip null values:";
            tooltip = "Whether to clip null values or display them as black.";
            SerializedProperty propertyClipNullValues = serializedObject.FindProperty(_propertyNameClipNullValues);
            propertyClipNullValues.boolValue = EditorGUILayout.Toggle(new GUIContent(label, tooltip), propertyClipNullValues.boolValue);
        }

#endif //UNITY_EDITOR

        /// <summary>
        /// Updates several disk-based blending parameters, and updates the blending material.
        /// </summary>
        /// <param name="blendingMaterial"></param> The blending material to update.
        /// <param name="cameraModels"></param> The source camera models.
        /// <param name="sourceCamIndices"></param> Outputs the list of source camera indices.
        /// <param name="sourceCamPositions"></param> Outputs the list of source camera positions.
        /// <param name="transformationMatrices"></param> Outputs the list of source camera matrices.
        public void UpdateBlendingParameters(ref Material blendingMaterial, CameraModel[] cameraModels, out List<float> sourceCamIndices, out List<Vector4> sourceCamPositions, out List<Matrix4x4> transformationMatrices)
        {
            // Set parameters for the blending material.
            blendingMaterial.SetInt(_shaderNameSourceCamCount, cameraModels.Length);
            blendingMaterial.SetFloat(_shaderNameMaxBlendAngle, _maxBlendAngle);
            blendingMaterial.SetInt(_shaderNameClipNullValues, _clipNullValues ? 1 : 0);
            // Determine additional camera parameters to pass to the material as properties.
            sourceCamIndices = new List<float>();
            sourceCamPositions = new List<Vector4>();
            transformationMatrices = new List<Matrix4x4>();
            for(int iter = 0; iter < cameraModels.Length; iter ++)
            {
                CameraModel cameraModel = cameraModels[iter];
                sourceCamIndices.Add(iter);
                sourceCamPositions.Add(cameraModel.transform.position);
                transformationMatrices.Add(cameraModel.meshRenderer.localToWorldMatrix);
            }
        }

#endregion //METHODS

    }

}
/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

using UnityEngine;
using UnityEditor;

namespace COLIBRIVR.Rendering
{

    /// <summary>
    /// Helper class to be called by blending methods that instantiate focal surfaces in the scene.
    /// </summary>
    public class Helper_FocalSurfaces : Method
    {

#region FIELDS

        [SerializeField] private float _focalLength;
        [SerializeField] private Vector2 _focalBounds;
        
        private bool[] _areCamerasOmnidirectional;
        private float[] _initialFocals;
        private Vector3[] _initialPositions;
        private Vector3[] _initialScales;

#endregion //FIELDS

#region INHERITANCE_METHODS

        /// <inheritdoc/>
        public override void Reset()
        {
            _focalLength = 1f;
            _focalBounds = new Vector2(0.1f, 20f);
        }

#endregion //INHERITANCE_METHODS
          
#region METHODS

#if UNITY_EDITOR

        /// <summary>
        /// Enables the user to choose a focal length.
        /// </summary>
        /// <param name="serializedObject"></param> The serialized object on which to find the properties to modify.
        public void SectionFocalLength(SerializedObject serializedObject)
        {
            // Enable the user to choose boundaries for the focal length.
            string label = "Focal bounds:";
            string tooltip = "Boundaries for the choice of the focal distance (meters).";
            SerializedProperty propertyFocalBounds = serializedObject.FindProperty("_focalBounds");
            propertyFocalBounds.vector2Value = EditorGUILayout.Vector2Field(new GUIContent(label, tooltip), propertyFocalBounds.vector2Value);
            // Enable the user to choose the focal length.
            label = "Focal length:";
            tooltip = "Focal length of the rendering system (meters). Objects will seem in focus if their distance to the source cameras is close to the focal length.";
            SerializedProperty propertyFocalLength = serializedObject.FindProperty("_focalLength");
            propertyFocalLength.floatValue = EditorGUILayout.Slider(new GUIContent(label, tooltip), propertyFocalLength.floatValue, propertyFocalBounds.vector2Value.x, propertyFocalBounds.vector2Value.y);
        }

#endif //UNITY_EDITOR

        /// <summary>
        /// Stores information on the instantiated focal surfaces.
        /// </summary>
        /// <param name="cameraModels"></param> The camera models used to instantiate the focal surfaces.
        /// <param name="focalSurfaceTransforms"></param> The focal surface transforms.
        public void StoreInformationOnFocalSurfaces(CameraModel[] cameraModels, Transform[] focalSurfaceTransforms)
        {
            _areCamerasOmnidirectional = new bool[cameraModels.Length];
            _initialFocals = new float[cameraModels.Length];
            _initialPositions = new Vector3[cameraModels.Length];
            _initialScales = new Vector3[cameraModels.Length];
            for(int sourceCamIndex = 0; sourceCamIndex < cameraModels.Length; sourceCamIndex++)
            {
                CameraModel cameraModel = cameraModels[sourceCamIndex];
                _areCamerasOmnidirectional[sourceCamIndex] = cameraModel.isOmnidirectional;
                _initialFocals[sourceCamIndex] = cameraModel.isOmnidirectional ? 1f : Camera.FieldOfViewToFocalLength(cameraModel.fieldOfView.x, 1f);
                _initialPositions[sourceCamIndex] = focalSurfaceTransforms[sourceCamIndex].position;
                _initialScales[sourceCamIndex] = focalSurfaceTransforms[sourceCamIndex].localScale;
            }
        }

        /// <summary>
        /// Updates the focal surface positions and scales based on the current focal length.
        /// </summary>
        /// <param name="focalSurfaceTransforms"></param> The focal surface transforms.
        public void UpdateFocalSurfaceTransforms(Transform[] focalSurfaceTransforms)
        {
            for(int i = 0; i < focalSurfaceTransforms.Length; i++)
            {
                float focalRatio = _focalLength / _initialFocals[i];
                Vector3 position = _initialPositions[i];
                if(!_areCamerasOmnidirectional[i])
                    position += _focalLength * focalSurfaceTransforms[i].forward;
                Quaternion rotation = focalSurfaceTransforms[i].rotation;
                Vector3 scale = focalRatio * _initialScales[i];
                GeneralToolkit.SetTransformValues(focalSurfaceTransforms[i], false, position, rotation, scale);
            }
        }

        /// <summary>
        /// Sends the current focal length to the blending material.
        /// </summary>
        /// <param name="blendingMaterial"></param> The blending material to modify.
        public void SendFocalLengthToBlendingMaterial(ref Material blendingMaterial)
        {
            blendingMaterial.SetFloat("_FocalLength", _focalLength);
        }

#endregion //METHODS

    }

}
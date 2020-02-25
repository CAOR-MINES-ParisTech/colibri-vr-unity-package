/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

using UnityEngine;
using UnityEditor;

namespace COLIBRIVR.Debugging
{

#if UNITY_EDITOR

    /// <summary>
    /// Editor class for Debug_QuadtreeMesh.
    /// </summary>
    [CustomEditor(typeof(PerViewMeshesQSTR))]
    public class PerViewMeshQSTREditor : Editor
    {

#region FIELDS

        private PerViewMeshesQSTR _targetObject;
        private SerializedObject _objectCameraParams;
        private SerializedProperty _propertyGeometryProcessingMethod;

#endregion //FIELDS
 
#region INHERITANCE_METHODS

        /// <summary>
        /// On selection, sets up the target properties.
        /// </summary>
        void OnEnable()
        {
            _targetObject = (PerViewMeshesQSTR)serializedObject.targetObject;
            _targetObject.Selected();
            _objectCameraParams = new SerializedObject(_targetObject.cameraParams);
            _propertyGeometryProcessingMethod = serializedObject.FindProperty("_geometryProcessingMethod");
        }

        /// <summary>
        /// On deselection, notifies the gameobject.
        /// </summary>
        void OnDisable()
        {
            if(_targetObject != null)
                _targetObject.Deselected();
        }
        
        /// <summary>
        /// Displays a GUI enabling the user to create a mesh based on the camera's Z-buffer.
        /// </summary>
        public override void OnInspectorGUI()
        {
            // Start the GUI.
            GeneralToolkit.EditorStart(serializedObject, _targetObject);

            // Start a change check.
            EditorGUI.BeginChangeCheck();

            // Enable the user to choose the camera model.
            GeneralToolkit.EditorNewSection("Camera model");
            CameraModelEditor.SectionCamera(_objectCameraParams);
            
            // End the change check.
            bool shouldUpdatePreview = EditorGUI.EndChangeCheck();

            // Enable the user to choose parameters for depth processing.
            GeneralToolkit.EditorNewSection("Depth processing parameters");
            SectionDepthProcessing();

            // Enable the user to generate the mesh.
            GeneralToolkit.EditorNewSection("Generate");
            SectionGenerateButton();
            
            // End the GUI.
            GeneralToolkit.EditorEnd(serializedObject);

            // If the preview window should be updated, notify the target object.
            if(shouldUpdatePreview)
                _targetObject.UpdateCameraModel();
        }

#endregion //INHERITANCE_METHODS

#region METHODS

        /// <summary>
        /// Enables the user to choose the depth processing method's parameters.
        /// </summary>
        private void SectionDepthProcessing()
        {
            // Find the quadtree mesh processing method.
            SerializedObject objectGeometryProcessingMethod = new SerializedObject(_propertyGeometryProcessingMethod.objectReferenceValue);
            objectGeometryProcessingMethod.Update();
            // Display a popup to choose whether to display the generated mesh as a 3D projection or as a 2D plane.
            SerializedProperty propertyProject3D = objectGeometryProcessingMethod.FindProperty("_project3D");
            int popupIndex = propertyProject3D.boolValue ? 1 : 0;
            string label = "Projection type:";
            string tooltip = "Whether to project the created mesh as a 2D plane or a 3D mesh.";
            popupIndex = EditorGUILayout.Popup(new GUIContent(label, tooltip), popupIndex, new string[] {"2D", "3D"});
            propertyProject3D.boolValue = (popupIndex == 1);
            // Apply the modified properties.
            objectGeometryProcessingMethod.ApplyModifiedProperties();
            // Display the additional editor parameters of the quadtree mesh processing method.
            _targetObject.SectionAdditionalParameters();
        }

        /// <summary>
        /// Enables the user to generate a mesh from the depth information.
        /// </summary>
        private void SectionGenerateButton()
        {
            // Display a button to enable the user to generate the mesh.
            EditorGUILayout.HelpBox("Note: generated mesh will be destroyed on deselection.", MessageType.Info);
            string label = "Create mesh from Z-buffer";
            string tooltip = "Creates a mesh from the game engine's Z-buffer, using the given depth processing parameters.";
            if(GUILayout.Button(new GUIContent(label, tooltip)))
                _targetObject.CreateMeshFromZBuffer();
            EditorGUILayout.Space();
            // Display the compression information generated during the process.
            _targetObject.DisplayCompressionInfo();
        }

#endregion //METHODS

    }

#endif //UNITY_EDITOR

}

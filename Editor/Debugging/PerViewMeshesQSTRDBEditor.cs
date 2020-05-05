/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

using UnityEditor;

namespace COLIBRIVR.Debugging
{

#if UNITY_EDITOR

    /// <summary>
    /// Editor class for PerViewMeshesQSTRDB.
    /// </summary>
    [CustomEditor(typeof(PerViewMeshesQSTRDB))]
    public class PerViewMeshQSTRDBEditor : PerViewMeshesQSTRDebugEditor
    {

#region FIELDS

        private SerializedObject _objectCameraModel;

#endregion //FIELDS
 
#region INHERITANCE_METHODS

        /// <summary>
        /// On selection, sets up the target properties.
        /// </summary>
        public override void OnEnable()
        {
            base.OnEnable();
            _objectCameraModel = new SerializedObject(_targetObject.GetCameraModel());
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
            CameraModelEditor.SectionCamera(_objectCameraModel);
            
            // End the change check.
            bool shouldUpdatePreview = EditorGUI.EndChangeCheck();

            // Enable the user to choose parameters for depth processing.
            GeneralToolkit.EditorNewSection("Depth processing parameters");
            SectionDepthProcessing(_targetObject.GetCameraModel().isOmnidirectional);

            // Enable the user to generate the mesh.
            GeneralToolkit.EditorNewSection("Generate");
            SectionGenerateButton();
            
            // End the GUI.
            GeneralToolkit.EditorEnd(serializedObject);

            // If the preview window should be updated, notify the target object.
            if(shouldUpdatePreview)
                ((PerViewMeshesQSTRDB)_targetObject).UpdateCameraModel(false);
        }

#endregion //INHERITANCE_METHODS

    }

#endif //UNITY_EDITOR

}

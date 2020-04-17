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
    /// Editor class for PerViewMeshesQSTREF.
    /// </summary>
    [CustomEditor(typeof(PerViewMeshesQSTREF))]
    public class PerViewMeshQSTREFEditor : PerViewMeshesQSTRDebugEditor
    {

#region FIELDS

        private Processing.Processing _targetProcessing;

#endregion //FIELDS
 
#region INHERITANCE_METHODS

        /// <inheritdoc/>
        public override void OnEnable()
        {
            base.OnEnable();
            _targetProcessing = ((PerViewMeshesQSTREF)_targetObject).processing;
        }

        /// <summary>
        /// Indicates that the object has frame bounds.
        /// </summary>
        /// <returns></returns> True.
        private bool HasFrameBounds()
        {
            return true;
        }

        /// <summary>
        /// On being asked for them, returns the object's frame bounds.
        /// </summary>
        /// <returns></returns>
        private Bounds OnGetFrameBounds()
        {
            return _targetProcessing.cameraSetup.GetFrameBounds();
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

            // Enable the user to select the source data directory.
            GeneralToolkit.EditorNewSection("Source data");
            COLIBRIVR.Processing.ProcessingEditor.SectionDataDirectory(_targetProcessing);
            // Enable the user to select the source camera index.
            SubsectionSourceCameraIndex();

            // End the change check.
            bool shouldDestroyMesh = EditorGUI.EndChangeCheck();

            // Enable the user to choose parameters for depth processing.
            CameraModel cameraModel = _targetObject.GetCameraModel();
            if(cameraModel != null)
            {
                GeneralToolkit.EditorNewSection("Depth processing parameters");
                SectionDepthProcessing(cameraModel.isOmnidirectional);

                // Enable the user to generate the mesh.
                GeneralToolkit.EditorNewSection("Generate");
                bool isGUIEnabled = GUI.enabled;
                GUI.enabled = (_targetProcessing.dataHandler.sourcePerViewCount > 0);
                SectionGenerateButton();
                GUI.enabled = isGUIEnabled;
            }
            
            // End the GUI.
            GeneralToolkit.EditorEnd(serializedObject);

            // If the mesh should be destroyed, do so.
            if(shouldDestroyMesh)
                _targetObject.DestroyMesh();
        }

#endregion //INHERITANCE_METHODS

#region METHODS

        /// <summary>
        /// Enables the user to select the source camera index.
        /// </summary>
        protected void SubsectionSourceCameraIndex()
        {
            CameraSetup cameraSetup = _targetProcessing.cameraSetup;
            int sliderMaxInt = _targetProcessing.dataHandler.sourcePerViewCount;
            int newPreviewIndex = sliderMaxInt;
            if(sliderMaxInt > 0)
            {
                string label = "Source index:";
                string tooltip = "The index, in the setup, of the source depth map to use as input for per-view mesh creation.";
                newPreviewIndex = EditorGUILayout.IntSlider(new GUIContent(label, tooltip), cameraSetup.previewIndex, 0, sliderMaxInt);
            }
            if(newPreviewIndex != cameraSetup.previewIndex)
            {
                cameraSetup.previewIndex = newPreviewIndex;
                cameraSetup.OnPreviewIndexChange();
                SceneView.RepaintAll();
            }
        }

#endregion //METHODS

    }

#endif //UNITY_EDITOR

}

/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

using UnityEngine;
using UnityEditor;

namespace COLIBRIVR.Acquisition
{

#if UNITY_EDITOR

    /// <summary>
    /// Editor class for Acquisition.
    /// </summary>
    [CustomEditor(typeof(Acquisition))]
    public class AcquisitionEditor : Editor
    {

#region FIELDS

        private Acquisition _targetObject;
        private SerializedObject _objectCameraSetup;
        private SerializedProperty _propertyLockSetup;
        private SerializedProperty _propertyCameraCount;
        private SerializedProperty _propertyCameraPrefab;
        private SerializedProperty _propertyAcquireDepth;
        private SerializedProperty _propertyCopyGlobalMesh;
        private SerializedProperty _propertySetupType;
        private SerializedProperty _propertySetupDirection;

#endregion //FIELDS

#region INHERITANCE_METHODS

        /// <summary>
        /// On selection, sets up the target properties.
        /// </summary>
        void OnEnable()
        {
            _targetObject = (Acquisition)serializedObject.targetObject;
            // Notify the target object that it has been selected.
            _targetObject.Selected();
            // Get the target properties.
            _objectCameraSetup = new SerializedObject(_targetObject.cameraSetup);
            _propertyLockSetup = serializedObject.FindProperty(Acquisition.propertyNameLockSetup);
            _propertyCameraCount = serializedObject.FindProperty(Acquisition.propertyNameCameraCount);
            _propertyCameraPrefab = serializedObject.FindProperty(Acquisition.propertyNameCameraPrefab);
            _propertyAcquireDepth = serializedObject.FindProperty(Acquisition.propertyNameAcquireDepthData);
            _propertyCopyGlobalMesh = serializedObject.FindProperty(Acquisition.propertyNameCopyGlobalMesh);
            _propertySetupType = serializedObject.FindProperty(Acquisition.propertyNameSetupType);
            _propertySetupDirection = serializedObject.FindProperty(Acquisition.propertyNameSetupDirection);
        }

        /// <summary>
        /// On deselection, destroys created objects.
        /// </summary>
        void OnDisable()
        {
            if(_targetObject != null)
                _targetObject.Deselected();
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
            return _targetObject.cameraSetup.GetFrameBounds();
        }

        /// <summary>
        /// Displays a GUI enabling the user to modify various acquisition parameters.
        /// </summary>
        public override void OnInspectorGUI()
        {
            // Start the GUI.
            GeneralToolkit.EditorStart(serializedObject, _targetObject);

            // Start a change check.
            EditorGUI.BeginChangeCheck();

            // Enable the user to define the acquisition setup.
            GeneralToolkit.EditorNewSection("Acquisition setup");
            SectionSetup();
            // Enable the user to define the source camera parameters.
            GeneralToolkit.EditorNewSection("Camera parameters");
            SectionCameraParameters();
            // Enable the user to define whether geometric data is to be captured.
            GeneralToolkit.EditorNewSection("Geometry acquisition");
            SectionGeometry();

            // End the change check. If any of the previous parameters have changed, the preview window should be updated.
            bool shouldUpdatePreview = EditorGUI.EndChangeCheck();

            // Enable the user to define where to store the captured data.
            GeneralToolkit.EditorNewSection("Output data directory");
            SectionDataDirectory();
            // Provide the user with a button to launch the acquisition process.
            GeneralToolkit.EditorNewSection("Acquire data");
            SectionAcquire();

            // End the GUI.
            GeneralToolkit.EditorEnd(serializedObject);

            // Inform the target object to update the displayed preview information if necessary.
            if(shouldUpdatePreview)
            {
                _targetObject.ComputeAcquisitionCameraPoses();
                _targetObject.UpdatePreviewCameraModel();
                SceneView.RepaintAll();
            }
        }

#endregion //INHERITANCE_METHODS

#region METHODS

        /// <summary>
        /// Enables the user to define the acquisition setup.
        /// </summary>
        private void SectionSetup()
        {
            string label = "Lock: ";
            string tooltip = "You should lock the setup once you have finished setting the overall parameters.";
            tooltip += " This will enable modifying individual cameras, e.g. to move them to specific positions.";
            tooltip += " Unlocking will reset all cameras in the setup with the values displayed below.";
            _propertyLockSetup.boolValue = EditorGUILayout.Toggle(new GUIContent(label, tooltip), _propertyLockSetup.boolValue);
            bool isGUIEnabled = GUI.enabled;
            GUI.enabled = isGUIEnabled && !_propertyLockSetup.boolValue;
            Vector2Int oldCameraCount = _propertyCameraCount.vector2IntValue;
            int newHorizontalCameraCount = oldCameraCount.x;
            int newVerticalCameraCount = oldCameraCount.y;
            label = "Type: ";
            tooltip = "Shape of the acquisition setup.";
            EditorGUILayout.PropertyField(_propertySetupType, new GUIContent(label, tooltip));
            if((SetupType)_propertySetupType.intValue == SetupType.Grid)
            {
                label = "Per row: ";
                tooltip = "Number of cameras per row.";
                newHorizontalCameraCount = EditorGUILayout.IntSlider(new GUIContent(label, tooltip), oldCameraCount.x, 1, 64);
                label = "Per column: ";
                tooltip = "Number of cameras per column.";
                newVerticalCameraCount = EditorGUILayout.IntSlider(new GUIContent(label, tooltip), oldCameraCount.y, 1, 64);
            }
            else
            {
                label = "Direction: ";
                tooltip = "Whether the cameras face inward or outward.";
                EditorGUILayout.PropertyField(_propertySetupDirection, new GUIContent(label, tooltip));
                label = "Per H arc: ";
                tooltip = "Number of cameras per horizontal arc.";
                newHorizontalCameraCount = EditorGUILayout.IntSlider(new GUIContent(label, tooltip), oldCameraCount.x, 1, 128);
                label = "Per V arc: ";
                tooltip = "Number of cameras per vertical arc.";
                newVerticalCameraCount = EditorGUILayout.IntSlider(new GUIContent(label, tooltip), oldCameraCount.y, 1, 64);
            }
            _propertyCameraCount.vector2IntValue = new Vector2Int(newHorizontalCameraCount, newVerticalCameraCount);
            GUI.enabled = isGUIEnabled;
        }

        /// <summary>
        /// Enables the user to choose the camera parameters.
        /// </summary>
        private void SectionCameraParameters()
        {
            bool isGUIEnabled = GUI.enabled;
            GUI.enabled = isGUIEnabled && !_propertyLockSetup.boolValue;
            string label = "Prefab: ";
            string tooltip = "Camera object used for acquisition.";
            tooltip += " If this value is left to null, the script will use a default camera.";
            tooltip += " Otherwise, a specific prefab can be used to apply post-processing effects to the captured image.";
            tooltip += " Modifying the acquisition camera parameters will modify the prefab.";
            EditorGUILayout.PropertyField(_propertyCameraPrefab, new GUIContent(label, tooltip));
            _objectCameraSetup.Update();
            CameraModelEditor.SectionCamera(new SerializedObject(_targetObject.cameraSetup.cameraModels[0]));
            _objectCameraSetup.ApplyModifiedProperties();
            GUI.enabled = isGUIEnabled;
        }

        /// <summary>
        /// Enables the user to define whether geometric data is to be captured.
        /// </summary>
        private void SectionGeometry()
        {
            string label = "Per-view depth: ";
            string tooltip = "Whether depth maps are to be acquired.";
            EditorGUILayout.PropertyField(_propertyAcquireDepth, new GUIContent(label, tooltip));
            label = "Global mesh: ";
            tooltip = "Whether the objects in the scene are to be copied as a global mesh.";
            EditorGUILayout.PropertyField(_propertyCopyGlobalMesh, new GUIContent(label, tooltip));
        }

        /// <summary>
        /// Enables the user to choose the data handler's data directory.
        /// </summary>
        public void SectionDataDirectory()
        {
            string searchTitle = "Select output root directory for the captured data";
            string tooltip = "Path must be within the Unity project folder (but can be outside of Assets). Existing data in this folder will be overwritten.";
            bool clicked;
            string outPath;
            GeneralToolkit.EditorPathSearch(out clicked, out outPath, PathType.Directory, _targetObject.dataHandler.dataDirectory, searchTitle, tooltip, Color.grey);
            _targetObject.dataHandler.ChangeDataDirectory(outPath, clicked);
        }

        /// <summary>
        /// Provides the user with a button to launch the acquisition process.
        /// </summary>
        private void SectionAcquire()
        {
            string label = "Acquire geometric and color data";
            string tooltip = "This will acquire";
            // Indicate the number of photographs.
            Vector2Int cameraCount = _propertyCameraCount.vector2IntValue;
            int totalCameraCount = cameraCount.x * cameraCount.y;
            string photographPlural = (totalCameraCount > 1) ? "s" : string.Empty;
            tooltip += " " + totalCameraCount + " photograph" + photographPlural;
            // Indicate the number of depth maps.
            bool acquireDepth = _propertyAcquireDepth.boolValue;
            if(acquireDepth)
                tooltip += " and " + totalCameraCount + " depth map" + photographPlural;
            // Indicate whether the global mesh is copied.
            bool copyGlobalMesh = _propertyCopyGlobalMesh.boolValue;
            if(copyGlobalMesh)
                tooltip += ", and copy the global mesh as an asset";
            // Indicate the output data directory.
            tooltip += ", stored at: " + _targetObject.dataHandler.dataDirectory + ".";
            // Display the button, and, if clicked, display a confirmation dialog.
            bool isGUIEnabled = GUI.enabled;
            GUI.enabled = isGUIEnabled && Application.isPlaying;
            GeneralToolkit.EditorRequirePlayMode(ref tooltip);
            if(GUILayout.Button(new GUIContent(label, tooltip)))
            {
                label = "Existing data will be erased. Are you ready to proceed?";
                tooltip = "Launching acquisition will erase any existing data in the folder: " + _targetObject.dataHandler.dataDirectory + ". Are you ready to proceed?";
                if(EditorUtility.DisplayDialog(label, tooltip, "Yes", "No"))
                {
                    _targetObject.CaptureScene();
                }
            }
            GUI.enabled = isGUIEnabled;
        }

#endregion //METHODS

    }

#endif //UNITY_EDITOR

}

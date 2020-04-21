/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

using System.IO;
using UnityEngine;
using UnityEditor;

namespace COLIBRIVR.ExternalConnectors
{

    /// <summary>
    /// Helper class that provides GUI methods to enable users to access methods from the COLMAP toolkit.
    /// </summary>
    [ExecuteInEditMode]
    public class COLMAPHelper : ExternalHelper
    {

#region CONST_FIELDS

        private const string _propertyNameCOLMAPCameraIndex = "_COLMAPCameraIndex";
        private const string _propertyNameIsSingleCamera = "_isSingleCamera";
        private const string _propertyNameMaxImageSize = "_maxImageSize";

#endregion //CONST_FIELDS

#region PROPERTIES

        private string _updateDirectoryIndicatorPathEnd { get { return gameObject.name + gameObject.GetInstanceID() + "UpdateDirectory_TEMP.prefab"; } }

#endregion //PROPERTIES

#region FIELDS

        [SerializeField] private int _COLMAPCameraIndex;
        [SerializeField] private bool _isSingleCamera;
        [SerializeField] private int _maxImageSize;

        private bool hasPerformedSparseReconstruction;

#endregion //FIELDS

#if UNITY_EDITOR

#region INHERITANCE_METHODS

        /// <inheritdoc/>
        public override void Reset()
        {
            base.Reset();
            _COLMAPCameraIndex = 2;
            _isSingleCamera = true;
            _maxImageSize = 2000;
            hasPerformedSparseReconstruction = false;
        }

        /// <inheritdoc/>
        public override void DisplayEditorFoldout()
        {
            EditorFoldout(COLIBRIVRSettings.packageSettings.COLMAPSettings);
        }

        /// <inheritdoc/>
        public override void DisplaySubsections()
        {
            SerializedObject serializedObject = new SerializedObject(this);
            serializedObject.Update();
            SubsectionSparseReconstruction(serializedObject);
            SubsectionDenseReconstruction();

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// On enable, changes the workspace if entering edit mode after having performed reconstruction in play mode.
        /// </summary>
        void OnEnable()
        {
            if(!EditorApplication.isPlaying && GeneralToolkit.IsStartingNewScene())
            {
                string indicatorPathAbsolute = Path.Combine(Application.dataPath, _updateDirectoryIndicatorPathEnd);
                if(File.Exists(indicatorPathAbsolute))
                {
                    COLMAPConnector.ChangeWorkspaceAfterSparseReconstruction(dataHandler);
                    GeneralToolkit.Delete(indicatorPathAbsolute);
                    AssetDatabase.Refresh();
                }
            }
        }

        /// <summary>
        /// On destroy, saves an update indicator if exiting play mode after reconstruction.
        /// </summary>
        public override void OnDestroy()
        {
            base.OnDestroy();
            if(hasPerformedSparseReconstruction && EditorApplication.isPlaying && GeneralToolkit.IsStartingNewScene())
            {
                PrefabUtility.SaveAsPrefabAsset(gameObject, Path.Combine("Assets", _updateDirectoryIndicatorPathEnd));
            }
        }

#endregion //INHERITANCE_METHODS

#region METHODS

        /// <summary>
        /// Enables the user to launch sparse 3D reconstruction via COLMAP.
        /// </summary>
        /// <param name="serializedObject"></param> The serialized object to modify.
        private void SubsectionSparseReconstruction(SerializedObject serializedObject)
        {
            EditorGUILayout.Space();
            string workspace = dataHandler.dataDirectory;
            string label = "Recover sparse camera setup from images.";
            string tooltip = "Images should be stored in the \"" + COLMAPConnector.GetImagesDir(workspace) + "\" folder.\n";
            tooltip += "Camera setup information will be stored at: \"" + COLMAPConnector.GetCamerasFile(workspace) + "\".";
            // Check if this option is available.
            bool isGUIEnabled = GUI.enabled;
            GUI.enabled = isGUIEnabled && (processingCaller.dataHandler.sourceColorCount > 1) && !(workspace.Contains(COLMAPConnector.dense0DirName)) && Application.isPlaying;
            GeneralToolkit.EditorRequirePlayMode(ref tooltip);
            // Display a button to launch the helper method.
            bool hasPressed = GeneralToolkit.EditorWordWrapLeftButton(new GUIContent("Run", tooltip), new GUIContent(label, tooltip));
            // Display additional parameters.
            EditorGUILayout.Space();
            if(GUI.enabled)
            {
                label = "Camera type";
                tooltip = "COLMAP camera type of the camera(s) that acquired the source images.";
                SerializedProperty propertyCOLMAPCameraIndex = serializedObject.FindProperty(_propertyNameCOLMAPCameraIndex);
                propertyCOLMAPCameraIndex.intValue = EditorGUILayout.Popup(new GUIContent(label, tooltip), propertyCOLMAPCameraIndex.intValue, COLMAPConnector.COLMAPCameraTypes.ToArray());
                label = "Is single camera";
                tooltip = "This value should be set to true if the source images were acquired by the same camera, false otherwise.";
                SerializedProperty propertyIsSingleCamera = serializedObject.FindProperty(_propertyNameIsSingleCamera);
                propertyIsSingleCamera.boolValue = EditorGUILayout.Toggle(new GUIContent(label, tooltip), propertyIsSingleCamera.boolValue);
                label = "Max. image size: ";
                tooltip = "Maximum image size for the undistortion step. The resized images will be the ones used for rendering.";
                SerializedProperty propertyMaxImageSize = serializedObject.FindProperty(_propertyNameMaxImageSize);
                propertyMaxImageSize.intValue = EditorGUILayout.IntSlider(new GUIContent(label, tooltip), propertyMaxImageSize.intValue, 1, 8192);
            }
            // If the button is pressed, display a dialog to confirm.
            if(hasPressed)
            {
                label = "Existing data will be erased. Are you ready to proceed?";
                tooltip = "Launching this process will erase data in the folder: \"" + workspace + "\". Are you ready to proceed?";
                // If the user confirms, launch the method.
                if(EditorUtility.DisplayDialog(label, tooltip, "Yes", "No"))
                {
                    StartCoroutine(COLMAPConnector.RunSparseReconstructionCoroutine(processingCaller, workspace, _COLMAPCameraIndex, _isSingleCamera, _maxImageSize));
                    hasPerformedSparseReconstruction = true;
                }
            }
            // Reset the GUI.
            GUI.enabled = isGUIEnabled;
            EditorGUILayout.Space();
        }

        /// <summary>
        /// Enables the user to launch dense 3D reconstruction and meshing via COLMAP.
        /// </summary>
        /// <param name="workspace"></param> The workspace from which to perform this method.
        private void SubsectionDenseReconstruction()
        {
            EditorGUILayout.Space();
            string workspace = dataHandler.dataDirectory;
            string label = "Reconstruct 3D mesh (.PLY) from sparse camera setup.";
            string tooltip = "Processed geometry will be stored at: \"" + COLMAPConnector.GetDelaunayFile(workspace) + "\".";
            // Check if this option is available.
            bool isGUIEnabled = GUI.enabled;
            GUI.enabled = isGUIEnabled && (cameraSetup != null && cameraSetup.cameraModels != null) && workspace.Contains(COLMAPConnector.dense0DirName) && Application.isPlaying;
            GeneralToolkit.EditorRequirePlayMode(ref tooltip);
            // Display a button to launch the helper method.
            bool hasPressed = GeneralToolkit.EditorWordWrapLeftButton(new GUIContent("Run", tooltip), new GUIContent(label, tooltip));
            // If the button is pressed, display a dialog to confirm.
            if(hasPressed)
            {
                label = "Existing data will be erased. Are you ready to proceed?";
                tooltip = "Launching this process will erase data in the folder: \"" + workspace + "\". Are you ready to proceed?";
                // If the user confirms, update the workspace directory and launch the method.
                if(EditorUtility.DisplayDialog(label, tooltip, "Yes", "No"))
                {
                    StartCoroutine(COLMAPConnector.RunDenseReconstructionCoroutine(this, workspace));
                }
            }
            // Reset the GUI.
            GUI.enabled = isGUIEnabled;
            EditorGUILayout.Space();
        }

#endregion //METHODS

#endif //UNITY_EDITOR
        
    }

}


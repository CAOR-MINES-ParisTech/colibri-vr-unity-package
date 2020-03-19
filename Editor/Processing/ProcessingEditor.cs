/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using COLIBRIVR.ExternalConnectors;

namespace COLIBRIVR.Processing
{

#if UNITY_EDITOR

    /// <summary>
    /// Editor class for Processing.
    /// </summary>
    [CustomEditor(typeof(Processing))]
    public class ProcessingEditor : Editor
    {

#region STATIC_METHODS        

        /// <summary>
        /// Enables the user to select the source data directory.
        /// </summary>
        /// <param name="targetObject"></param> The target processing object.
        public static void SectionDataDirectory(Processing targetObject)
        {
            bool isGUIEnabled = GUI.enabled;
            GUI.enabled = isGUIEnabled && !Application.isPlaying;
            EditorGUILayout.LabelField(targetObject.sourceDataInfo, GeneralToolkit.wordWrapStyle, GUILayout.MaxWidth(GeneralToolkit.EditorGetCurrentScopeWidth()));
            using(var verticalScope = new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                string searchTitle = "Select directory that contains color/depth data";
                string tooltip = "Select the base directory where the acquisition data was stored.";
                bool clicked;
                string outPath;
                GeneralToolkit.EditorPathSearch(out clicked, out outPath, PathType.Directory, targetObject.dataHandler.dataDirectory, searchTitle, tooltip, Color.grey);
                targetObject.dataHandler.ChangeDataDirectory(outPath, clicked);
            }
            GUI.enabled = isGUIEnabled;
        }

#endregion //STATIC_METHODS

#region FIELDS

        private Processing _targetObject;

#endregion //FIELDS

#region INHERITANCE_METHODS

        /// <summary>
        /// On selection, sets up the target properties.
        /// </summary>
        void OnEnable()
        {
            // Inform the target object that it has been selected.
            _targetObject = (Processing)serializedObject.targetObject;
            _targetObject.Selected();
        }

        /// <summary>
        /// On deselection, inform the target object that it has been deselected.
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
        /// Displays a GUI enabling the user to perform various processing tasks.
        /// </summary>
        public override void OnInspectorGUI()
        {
            // Start the GUI.
            GeneralToolkit.EditorStart(serializedObject, _targetObject);
            
            // Enable the user to select the source data directory.
            GeneralToolkit.EditorNewSection("Source data");
            SectionDataDirectory(_targetObject);

            // Enable the user to perform 3D reconstruction and simplification using external tools.
            GeneralToolkit.EditorNewSection("External processing helpers");
            SectionExternalTools();
            
            // Enable the user to process the source data to create Unity assets and compress these assets into an asset bundle.
            GeneralToolkit.EditorNewSection("Data processing");
            SectionProcessingMethods();
            SectionProcessAndBundle();

            // End the GUI.
            GeneralToolkit.EditorEnd(serializedObject);
        }

#endregion //INHERITANCE_METHODS

#region METHODS

        /// <summary>
        /// Enables the user to enhance the source data (e.g. with reconstructed geometry) using external tools.
        /// </summary>
        private void SectionExternalTools()
        {
            if(ExternalSettings.areExternalToolsLinked)
            {
                using(var indentScope = new EditorGUI.IndentLevelScope())
                    for(int iter = 0; iter < _targetObject.externalHelpers.Length; iter++)
                        _targetObject.externalHelpers[iter].DisplayEditorFoldout();
            }
            else
            {
                EditorGUILayout.LabelField("No external tool was linked. Add executable paths in the project settings to enable new functionalities!", GeneralToolkit.wordWrapStyle);
                EditorGUILayout.Space();
            }
        }

        /// <summary>
        /// Enables the user to choose color and geometry processing methods.
        /// </summary>
        private void SectionProcessingMethods()
        {
            EditorGUILayout.Space();
            string label = "Color and geometry processing methods:";
            string tooltip = "Choice of color and geometry processing methods.";
            // Get methods that have a GUI that should be displayed at the topmost level.
            ProcessingMethod[] allMethods = _targetObject.processingMethods;
            List<ProcessingMethod> topmostMethods = new List<ProcessingMethod>();
            for(int iter = 1; iter < allMethods.Length; iter++)
                if(!allMethods[iter].IsGUINested())
                    topmostMethods.Add(allMethods[iter]);
            // Check whether one of these methods is compatible.
            bool[] arrayIsMethodCompatible = new bool[topmostMethods.Count];
            bool isThereACompatibleMethod = false;
            for(int iter = 0; iter < topmostMethods.Count; iter++)
            {
                arrayIsMethodCompatible[iter] = topmostMethods[iter].IsCompatible(_targetObject.sourceColorCount, _targetObject.sourcePerViewGeometryCount, _targetObject.sourceGlobalGeometryCount);
                if(arrayIsMethodCompatible[iter])
                    isThereACompatibleMethod = true;
            }
            // If there is a compatible method, display the section enabling to choose a set of processing methods.
            bool isGUIEnabled = GUI.enabled;
            GUI.enabled = isGUIEnabled && isThereACompatibleMethod;
            EditorGUILayout.LabelField(new GUIContent(label, tooltip));
            using(var indentScopeOne = new EditorGUI.IndentLevelScope())
            {
                for(int iter = 0; iter < topmostMethods.Count; iter++)
                {
                    bool isCompatible = arrayIsMethodCompatible[iter];
                    GUI.enabled = isGUIEnabled && isCompatible;
                    topmostMethods[iter].SectionProcessingMethod(isCompatible);
                }
            }
            GUI.enabled = isGUIEnabled;            
            EditorGUILayout.Space();
        }
        
        /// <summary>
        /// Enables the user to process the source data.
        /// </summary>
        private void SectionProcessAndBundle()
        {
            bool isGUIEnabled = GUI.enabled;
            GUI.enabled = isGUIEnabled && (_targetObject.sourceColorCount > 0 || _targetObject.sourcePerViewGeometryCount > 0 || _targetObject.sourceGlobalGeometryCount > 0);
            EditorGUILayout.Space();
            string label = "Process source data";
            string tooltip = "Processed assets will be stored at \"" + _targetObject.dataHandler.processedDataDirectory + "\".";
            GeneralToolkit.EditorRequirePlayMode(ref tooltip);
            if(GUILayout.Button(new GUIContent(label, tooltip)))
            {
                label = "Data will be processed. Are you ready to proceed?";
                tooltip = "Launching this process will save data in the folder: \"" + _targetObject.dataHandler.processedDataDirectory + "\". Are you ready to proceed?";
                if(EditorUtility.DisplayDialog(label, tooltip, "Yes", "No"))
                {
                    _targetObject.ProcessData();
                }
            }
            EditorGUILayout.Space();
            GUI.enabled = isGUIEnabled && !Application.isPlaying;
            label = "Bundle processed data";
            tooltip = "The Unity asset bundle will be stored at \"" + _targetObject.dataHandler.bundleDirectory + "\".";
            if(GUILayout.Button(new GUIContent(label, tooltip)))
            {
                GUI.enabled = false;
                label = "Existing asset bundle will be overwritten. Are you ready to proceed?";
                tooltip = "Launching this process will overwrite any existing data in the folder: \"" + _targetObject.dataHandler.bundleDirectory + "\". Are you ready to proceed?";
                if(EditorUtility.DisplayDialog(label, tooltip, "Yes", "No"))
                {
                    _targetObject.BundleButton();
                }
                GUI.enabled = isGUIEnabled;
            }
            EditorGUILayout.Space();
            GUI.enabled = isGUIEnabled;
        }

#endregion //METHODS

    }

#endif //UNITY_EDITOR

}

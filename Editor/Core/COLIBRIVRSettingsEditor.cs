/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

using UnityEngine;
using UnityEditor;

namespace COLIBRIVR
{

#if UNITY_EDITOR

    /// <summary>
    /// Editor class for COLIBRIVRSettings.
    /// </summary>
    [CustomEditor(typeof(COLIBRIVRSettings))]
    public class COLIBRIVRSettingsEditor : Editor
    {

#region FIELDS

        private COLIBRIVRSettings _targetObject;

#endregion //FIELDS

#region INHERITANCE_METHODS

        /// <summary>
        /// On selection, sets up the target properties.
        /// </summary>
        void OnEnable()
        {
            // Get the target object.
            _targetObject = (COLIBRIVRSettings)serializedObject.targetObject;
        }

        /// <summary>
        /// Displays a GUI enabling the user to see the current package settings.
        /// Intentionally, this interface cannot be used to modify these settings.
        /// </summary>
        public override void OnInspectorGUI()
        {
            // Start the GUI.
            GeneralToolkit.EditorStart(serializedObject, _targetObject);

            // Indicate how to modify these settings.
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("The settings below can be modified from the Project Settings window.", GeneralToolkit.wordWrapStyle);
            EditorGUILayout.Space();

            // Disable the GUI.
            bool isGUIEnabled = GUI.enabled;
            GUI.enabled = false;

            // Enable the user to see the package's settings.
            COLIBRIVRSettings.SectionPackageSettings(_targetObject);

            // End the GUI.
            GUI.enabled = isGUIEnabled;
            GeneralToolkit.EditorEnd(serializedObject);
        }

#endregion //INHERITANCE_METHODS
        
    }

#endif //UNITY_EDITOR

}
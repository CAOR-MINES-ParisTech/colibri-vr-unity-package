/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

using UnityEngine;
using UnityEditor;

namespace COLIBRIVR.ExternalConnectors
{

    /// <summary>
    /// Abstract class that is used as a parent for classes that enable specifying settings for external tools.
    /// </summary>
    public abstract class ExternalSettings : ScriptableObject
    {

#region CONST_FIELDS

        public const string defaultExePath = "No path specified.";

#endregion //CONST_FIELDS

#region STATIC_PROPERTIES

#if UNITY_EDITOR

        public static bool areExternalToolsLinked
        {
            get
            {
                COLIBRIVRSettings packageSettings = COLIBRIVRSettings.packageSettings;
                return (packageSettings.COLMAPSettings.isLinked || packageSettings.BlenderSettings.isLinked || packageSettings.InstantMeshesSettings.isLinked);
            }
        }

#endif //UNITY_EDITOR

#endregion //STATIC_PROPERTIES

#region FIELDS

        [HideInInspector] public bool foldout;
        [HideInInspector] public string exePath;

        private string _initialized;

#endregion //FIELDS

#if UNITY_EDITOR

#region INHERITANCE_PROPERTIES

        public abstract string toolkitName { get; }

#endregion //INHERITANCE_PROPERTIES

#region PROPERTIES

        public bool isLinked { get { return (exePath != defaultExePath); } }

#endregion //PROPERTIES

#region INHERITANCE_METHODS

        /// <summary>
        /// Initializes the external settings.
        /// </summary>
        /// <returns></returns> The initialized settings.
        public virtual ExternalSettings Initialize()
        { 
            foldout = true;
            exePath = defaultExePath;
            name = toolkitName + " - Settings";
            return this;
        }

        /// <summary>
        /// Enables the user to specify the path to the external tool's executable file.
        /// </summary>
        public virtual void EditorSettingsFoldout()
        {
            SerializedObject serializedObject = new SerializedObject(this);
            serializedObject.Update();
            SerializedProperty propertyFoldout = serializedObject.FindProperty("foldout");
            SerializedProperty propertyExePath = serializedObject.FindProperty("exePath");
            using (var verticalScope = new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                string label = toolkitName;
                string tooltip = "Select the path to the " + toolkitName + " toolkit.";
                propertyFoldout.boolValue = EditorGUILayout.Foldout(propertyFoldout.boolValue, new GUIContent(label, tooltip));
                if(propertyFoldout.boolValue)
                {
                    label = "Executable path";
                    tooltip = "Path to the " + toolkitName + " executable file.";
                    string extensions = "EXE,exe,BAT,bat";
                    bool clicked;
                    string newExePath;
                    GeneralToolkit.EditorPathSearch(out clicked, out newExePath, PathType.File, propertyExePath.stringValue, label, tooltip, Color.grey, false, extensions);
                    if(clicked)
                        propertyExePath.stringValue = newExePath;
                }
            }
            serializedObject.ApplyModifiedProperties();
        }

#endregion //INHERITANCE_METHODS

#endif //UNITY_EDITOR

    }

}

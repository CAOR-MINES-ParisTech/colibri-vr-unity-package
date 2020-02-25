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
    /// Helper class that provides GUI methods to enable users to access methods from the Instant Meshes toolkit.
    /// </summary>
    public class InstantMeshesHelper : ExternalHelper
    {

#if UNITY_EDITOR

#region INHERITANCE_METHODS

        /// <inheritdoc/>
        public override void DisplayEditorFoldout()
        {
            EditorFoldout(COLIBRIVRSettings.packageSettings.InstantMeshesSettings);
        }

        /// <inheritdoc/>
        public override void DisplaySubsections()
        {
            SerializedObject serializedObject = new SerializedObject(this);
            serializedObject.Update();
            SubsectionRunInstantMeshesOBJ(serializedObject);
            serializedObject.ApplyModifiedProperties();
        }

#endregion //INHERITANCE_METHODS

#region METHODS

        /// <summary>
        /// Enables the user to perform automatic retopology on the .OBJ mesh via Instant Meshes.
        /// </summary>
        /// <param name="serializedObject"></param> The serialized object to modify.
        public void SubsectionRunInstantMeshesOBJ(SerializedObject serializedObject)
        {
            EditorGUILayout.Space();
            string workspace = dataHandler.dataDirectory;
            string inputFilePath = Path.Combine(workspace, BlenderConnector.convertPLYtoOBJOutputFileName);
            string outputFilePath = inputFilePath;
            string label = "Perform automatic retopology.";
            string tooltip = "This will re-mesh the .OBJ file at \"" + GeneralToolkit.FormatPathForCommand(inputFilePath) + "\".";
            // Check if this option is available.
            bool isGUIEnabled = GUI.enabled;
            GUI.enabled = isGUIEnabled && File.Exists(inputFilePath) && Application.isPlaying;
            GeneralToolkit.EditorRequirePlayMode(ref tooltip);
            // Display a button to launch the helper method.
            bool hasPressed = GeneralToolkit.EditorWordWrapLeftButton(new GUIContent("Run", tooltip), new GUIContent(label, tooltip));
            // If the button is pressed, launch the method.
            if(hasPressed)
            {
                BlenderHelper blenderHelper = GetComponent<BlenderHelper>();
                StartCoroutine(InstantMeshesConnector.RunInstantMeshesCoroutine(this, workspace, inputFilePath, outputFilePath, blenderHelper));
            }
            // Reset the GUI.
            GUI.enabled = isGUIEnabled;
            EditorGUILayout.Space();
        }

#endregion //METHODS

#endif //UNITY_EDITOR
        
    }

}


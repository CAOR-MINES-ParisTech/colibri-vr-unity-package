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
    /// Helper class that provides GUI methods to enable users to access methods from the Blender toolkit.
    /// </summary>
    public class BlenderHelper : ExternalHelper
    {

#region FIELDS

        public int meshFaceCount;

        [SerializeField] private string _meshWorkspace;

#endregion //FIELDS

#if UNITY_EDITOR

#region INHERITANCE_METHODS

        /// <inheritdoc/>
        public override void Reset()
        {
            base.Reset();
            meshFaceCount = -1;
            _meshWorkspace = string.Empty;
        }

        /// <inheritdoc/>
        public override void DisplayEditorFoldout()
        {
            EditorFoldout(COLIBRIVRSettings.packageSettings.BlenderSettings);
        }

        /// <inheritdoc/>
        public override void DisplaySubsections()
        {
            string workspace = dataHandler.dataDirectory;
            if(workspace != _meshWorkspace)
                meshFaceCount = -1;
            _meshWorkspace = workspace;
            SerializedObject serializedObject = new SerializedObject(this);
            serializedObject.Update();
            SubsectionConvertPLYtoOBJ();
            // SubsectionCheckOBJMeshInfo();
            SubsectionSimplifyOBJ(serializedObject);
            SubsectionSmartUVProjectOBJ();
            serializedObject.ApplyModifiedProperties();
        }

#endregion //INHERITANCE_METHODS

#region METHODS

        /// <summary>
        /// Reads the mesh's face count from a dedicated debug log.
        /// </summary>
        /// <param name="sendingProcess"></param> The process sending the log.
        /// <param name="outLine"></param> The log's output line.
        private void ReadMeshFaceCount(object sendingProcess, System.Diagnostics.DataReceivedEventArgs outLine)
        {
            string line = outLine.Data;
            if(!string.IsNullOrEmpty(line) && line.Contains("FACE_COUNT_OUTPUT:"))
            {
                meshFaceCount = GeneralToolkit.ParseInt(line.Split(':')[1]);
            }
        }

        /// <summary>
        /// Enables the user to convert the .PLY mesh file to .OBJ via Blender.
        /// </summary>
        public void SubsectionConvertPLYtoOBJ()
        {
            EditorGUILayout.Space();
            string inputFilePath = Path.Combine(_meshWorkspace, COLMAPConnector.delaunayFileName);
            string outputFilePath = Path.Combine(_meshWorkspace, BlenderConnector.convertPLYtoOBJOutputFileName);
            string label = "Convert .PLY mesh to .OBJ file.";
            string tooltip = "File " + GeneralToolkit.FormatPathForCommand(inputFilePath) + " will be converted to file "  + GeneralToolkit.FormatPathForCommand(outputFilePath) + ".";
            // Check if this option is available.
            bool isGUIEnabled = GUI.enabled;
            GUI.enabled = isGUIEnabled && File.Exists(inputFilePath) && Application.isPlaying;
            GeneralToolkit.EditorRequirePlayMode(ref tooltip);
            // Display a button to launch the helper method.
            bool hasPressed = GeneralToolkit.EditorWordWrapLeftButton(new GUIContent("Run", tooltip), new GUIContent(label, tooltip));
            if(hasPressed)
                StartCoroutine(BlenderConnector.RunConvertPLYtoOBJCoroutine(this, inputFilePath, outputFilePath, ReadMeshFaceCount));
            // Reset the GUI.
            GUI.enabled = isGUIEnabled;
            EditorGUILayout.Space();
        }

        // /// <summary>
        // /// Checks the number of faces on the current .OBJ mesh.
        // /// </summary>
        // /// <param name="workspace"></param> The workspace from which to perform this method.
        // public void CheckOBJMeshInfo(string workspace)
        // {
        //     string inputFilePath = Path.Combine(workspace, BlenderConnector.convertPLYtoOBJOutputFileName);
        //     StartCoroutine(BlenderConnector.RunCheckOBJMeshInfoCoroutine(this, inputFilePath, ReadMeshFaceCount));
        // }

        // /// <summary>
        // /// Enables the user to check the mesh information contained in the .OBJ file via Blender.
        // /// </summary>
        // public void SubsectionCheckOBJMeshInfo()
        // {
        //     EditorGUILayout.Space();
        //     string inputFilePath = Path.Combine(_meshWorkspace, BlenderConnector.convertPLYtoOBJOutputFileName);
        //     string label = "Check .OBJ mesh information.";
        //     string tooltip = "File " + GeneralToolkit.FormatPathForCommand(inputFilePath) + " will be read from, to check its number of triangle faces.";
        //     // Check if this option is available.
        //     bool isGUIEnabled = GUI.enabled;
        //     GUI.enabled = isGUIEnabled && File.Exists(inputFilePath) && Application.isPlaying;
        //     GeneralToolkit.EditorRequirePlayMode(ref tooltip);
        //     // Display a button to launch the helper method.
        //     bool hasPressed = GeneralToolkit.EditorWordWrapLeftButton(new GUIContent("Run", tooltip), new GUIContent(label, tooltip));
        //     if(hasPressed)
        //         CheckOBJMeshInfo(_meshWorkspace);
        //     // Display the current face count.
        //     GUIStyle grey = GeneralToolkit.wordWrapStyle;
        //     grey.normal.textColor = Color.grey;
        //     label = "Mesh face count: ";
        //     if(meshFaceCount == -1)
        //         label += "not determined.";
        //     else
        //         label += GeneralToolkit.ToString(meshFaceCount) + ".";
        //     tooltip = "Number of faces on the mesh.";
        //     EditorGUILayout.LabelField(new GUIContent(label, tooltip), grey);
        //     // Reset the GUI.
        //     GUI.enabled = isGUIEnabled;
        //     EditorGUILayout.Space();
        // }

        /// <summary>
        /// Enables the user to simplify the .OBJ mesh via Blender.
        /// </summary>
        /// <param name="serializedObject"></param> The serialized object to modify.
        public void SubsectionSimplifyOBJ(SerializedObject serializedObject)
        {
            EditorGUILayout.Space();
            string inputFilePath = Path.Combine(_meshWorkspace, BlenderConnector.convertPLYtoOBJOutputFileName);
            string outputFilePath = inputFilePath;
            string label = "Simplify .OBJ mesh.";
            string tooltip = "Mesh in file " + GeneralToolkit.FormatPathForCommand(inputFilePath) + " will be simplified, reducing its face count.";
            // Check if this option is available.
            bool isGUIEnabled = GUI.enabled;
            GUI.enabled = isGUIEnabled && File.Exists(inputFilePath) && Application.isPlaying;
            GeneralToolkit.EditorRequirePlayMode(ref tooltip);
            // Display a button to launch the helper method.
            bool hasPressed = GeneralToolkit.EditorWordWrapLeftButton(new GUIContent("Run", tooltip), new GUIContent(label, tooltip));
            // If the button is pressed, launch the method.
            if(hasPressed)
                StartCoroutine(BlenderConnector.RunSimplifyOBJCoroutine(this, inputFilePath, outputFilePath, ReadMeshFaceCount));
            // Reset the GUI.
            GUI.enabled = isGUIEnabled;
            EditorGUILayout.Space();
        }

        /// <summary>
        /// Enables the user to UV-map the given .OBJ mesh using Blender's Smart UV Project algorithm.
        /// </summary>
        public void SubsectionSmartUVProjectOBJ()
        {
            EditorGUILayout.Space();
            string inputFilePath = Path.Combine(_meshWorkspace, BlenderConnector.convertPLYtoOBJOutputFileName);
            string outputFilePath = inputFilePath;
            string label = "UV-map .OBJ file.";
            string tooltip = "Mesh in file " + GeneralToolkit.FormatPathForCommand(inputFilePath) + " will be UV-mapped using Blender's Smart UV Project algorithm.";
            // Check if this option is available.
            bool isGUIEnabled = GUI.enabled;
            GUI.enabled = isGUIEnabled && File.Exists(inputFilePath) && Application.isPlaying;
            GeneralToolkit.EditorRequirePlayMode(ref tooltip);
            // Display a button to launch the helper method.
            bool hasPressed = GeneralToolkit.EditorWordWrapLeftButton(new GUIContent("Run", tooltip), new GUIContent(label, tooltip));
            if(hasPressed)
                StartCoroutine(BlenderConnector.RunSmartUVProjectOBJCoroutine(this, inputFilePath, outputFilePath));
            // Reset the GUI.
            GUI.enabled = isGUIEnabled;
            EditorGUILayout.Space();
        }

#endregion //METHODS

#endif //UNITY_EDITOR
        
    }

}


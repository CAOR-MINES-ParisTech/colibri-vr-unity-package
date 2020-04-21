/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

using System.Collections;
using UnityEngine;

namespace COLIBRIVR.ExternalConnectors
{

    /// <summary>
    /// Class handling the connection between COLIBRI VR and Instant Meshes.
    /// For more information on Instant Meshes, see: https://github.com/wjakob/instant-meshes
    /// </summary>
    public static class InstantMeshesConnector
    {

#if UNITY_EDITOR

#region STATIC_METHODS

        /// <summary>
        /// Coroutine that performs automatic retopology on a given mesh using the Instant Meshes implementation.
        /// </summary>
        /// <param name="caller"></param> The Instant Meshes helper calling this method.
        /// <param name="workspace"></param> The workspace from which to launch the command.
        /// <param name="inputFilePath"></param> The full path to the input .PLY or .OBJ file.
        /// <param name="outputFilePath"></param> The full path to the output .PLY or .OBJ file.
        /// <param name="reduceVertexCount"></param> Whether to reduce the vertex count to the recommended value.
        /// <returns></returns>
        public static IEnumerator RunInstantMeshesCoroutine(InstantMeshesHelper caller, string workspace, string inputFilePath, string outputFilePath, bool reduceVertexCount)
        {
            // Indicate to the user that the process has started.
            GeneralToolkit.ResetCancelableProgressBar(true, true);
            // If the initial face count is needed, display the input mesh to get it.
            if(!reduceVertexCount)
                caller.DisplayMeshInSceneView(inputFilePath);
            // Initialize the command parameters.
            bool displayProgressBar = true;
            bool stopOnError = true;
            string[] progressBarParams = new string[3];
            progressBarParams[0] = "2";
            progressBarParams[1] = "Automatic retopology";
            progressBarParams[2] = "Processing canceled by user.";
            // Prepare the command.
            string formattedExePath = InstantMeshesSettings.formattedInstantMeshesExePath;
            string command = "CALL " + formattedExePath;
            command += " --output " + GeneralToolkit.FormatPathForCommand(outputFilePath);
            command += " --deterministic --boundaries --rosy 6 --posy 6";
            // If desired, use the determined mesh face count to define the desired face count.
            if(!reduceVertexCount)
                command += " --faces " + GeneralToolkit.ToString(caller.loadedMeshFaceCount);
            // Launch the command.
            command += " " + GeneralToolkit.FormatPathForCommand(inputFilePath);
            yield return caller.StartCoroutine(GeneralToolkit.RunCommandCoroutine(typeof(InstantMeshesConnector), command, workspace, displayProgressBar, null, null, stopOnError, progressBarParams));
            // Display the transformed mesh in the Scene view.
            caller.DisplayMeshInSceneView(outputFilePath);
            // Indicate to the user that the process has ended.
            GeneralToolkit.ResetCancelableProgressBar(false, false);
        }

#endregion //STATIC_METHODS

#endif //UNITY_EDITOR

    }

}
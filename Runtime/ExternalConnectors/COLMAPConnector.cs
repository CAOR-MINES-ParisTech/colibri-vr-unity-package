/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace COLIBRIVR.ExternalConnectors
{

    /// <summary>
    /// Static class handling the connection between COLIBRI VR and COLMAP.
    /// For more information on COLMAP, see: https://colmap.github.io/
    /// </summary>
    public static class COLMAPConnector
    {

#region CONST_FIELDS

        public const string imagesDirName = "images";
        public const string sparseDirName = "sparse";
        public const string denseDirName = "dense";
        public const string databaseFileName = "database.db";
        public const string camerasFileName = "cameras.txt";
        public const string imagesFileName = "images.txt";
        public const string pointsFileName = "points3D.txt";
        public const string fusedFileName = "fused.ply";
        public const string delaunayFileName = "meshed-delaunay.ply";
        public const string stereoDirName = "stereo";
        public const string depthMapsDirName = "depth_maps";

#endregion //CONST_FIELDS

#region STATIC_PROPERTIES

        public static List<string> COLMAPCameraTypes { get { return new List<string>() { "SIMPLE_PINHOLE", "PINHOLE", "SIMPLE_RADIAL", "SIMPLE_RADIAL_FISHEYE", "RADIAL", "RADIAL_FISHEYE", "OPENCV", "OPENCV_FISHEYE", "FULL_OPENCV", "FOV", "THIN_PRISM_FISHEYE"} ;} }
        public static string sparse0DirName { get { return Path.Combine(COLMAPConnector.sparseDirName, "0"); } }
        public static string dense0DirName { get { return Path.Combine(COLMAPConnector.denseDirName, "0"); } }

#endregion //STATIC_PROPERTIES

#region STATIC_METHODS

#if UNITY_EDITOR

        /// <summary>
        /// Creates a directory structure that can directly be read by COLMAP. Erases the existing workspace if it exists.
        /// </summary>
        /// <param name="workspace"></param> The path to the COLMAP workspace.
        public static void CreateDirectoryStructureForAcquisition(string workspace)
        {
            GeneralToolkit.CreateOrClear(PathType.Directory, workspace);
            GeneralToolkit.CreateOrClear(PathType.Directory, GetImagesDir(workspace));
            GeneralToolkit.CreateOrClear(PathType.Directory, GetSparseDir(workspace));
            GeneralToolkit.CreateOrClear(PathType.File, GetCamerasFile(workspace));
            GeneralToolkit.CreateOrClear(PathType.File, GetImagesFile(workspace));
            GeneralToolkit.CreateOrClear(PathType.File, GetPointsFile(workspace));
            GeneralToolkit.CreateOrClear(PathType.Directory, GetStereoDir(workspace));
            GeneralToolkit.CreateOrClear(PathType.Directory, GetDepthMapsDir(workspace));
        }

        /// <summary>
        /// Coroutine that runs the feature extraction command.
        /// </summary>
        /// <param name="caller"></param> The object calling this method.
        /// <param name="workspace"></param> The workspace from which to perform this step.
        /// <param name="displayProgressBar"></param> True if the progress bar should be displayed, false otherwise.
        /// <param name="stopOnError"></param> True if the process should stop on error, false otherwise.
        /// <param name="progressBarParams"></param> Parameters for the progress bar.
        /// <param name="COLMAPCameraIndex"></param> The index of the type of source camera (in the list of COLMAP cameras).
        /// <param name="isSingleCamera"></param> True if the source images were acquired by the same camera, false otherwise.
        /// <param name="focalLengthFactor"></param> Parameter for feature extraction.
        /// <returns></returns>
        public static IEnumerator RunFeatureExtractionCommand(MonoBehaviour caller, string workspace, bool displayProgressBar, bool stopOnError, string[] progressBarParams, int COLMAPCameraIndex, bool isSingleCamera, float focalLengthFactor)
        {
            string command = "CALL " + COLMAPSettings.formattedCOLMAPExePath + " feature_extractor";
            command += " --database_path ./" +  databaseFileName;
            command += " --image_path ./" + imagesDirName;
            command += " --ImageReader.camera_model " + COLMAPCameraTypes[COLMAPCameraIndex];
            if(isSingleCamera)
                command += " --ImageReader.single_camera 1";
            if(focalLengthFactor > 0)
                command += " --ImageReader.default_focal_length_factor " + GeneralToolkit.ToString(focalLengthFactor);
            yield return caller.StartCoroutine(GeneralToolkit.RunCommandCoroutine(typeof(COLMAPConnector), command, workspace, displayProgressBar, null, null, stopOnError, progressBarParams));
        }

        /// <summary>
        /// Coroutine that runs the feature matching command.
        /// </summary>
        /// <param name="caller"></param> The object calling this method.
        /// <param name="workspace"></param> The workspace from which to perform this step.
        /// <param name="displayProgressBar"></param> True if the progress bar should be displayed, false otherwise.
        /// <param name="stopOnError"></param> True if the process should stop on error, false otherwise.
        /// <param name="progressBarParams"></param> Parameters for the progress bar.
        /// <returns></returns>
        public static IEnumerator RunFeatureMatchingCommand(MonoBehaviour caller, string workspace, bool displayProgressBar, bool stopOnError, string[] progressBarParams)
        {
            string command = "CALL " + COLMAPSettings.formattedCOLMAPExePath + " exhaustive_matcher";
            command += " --database_path ./" + databaseFileName;
            yield return caller.StartCoroutine(GeneralToolkit.RunCommandCoroutine(typeof(COLMAPConnector), command, workspace, displayProgressBar, null, null, stopOnError, progressBarParams));
        }

        /// <summary>
        /// Coroutine that runs the mapping command.
        /// </summary>
        /// <param name="caller"></param> The object calling this method.
        /// <param name="workspace"></param> The workspace from which to perform this step.
        /// <param name="displayProgressBar"></param> True if the progress bar should be displayed, false otherwise.
        /// <param name="stopOnError"></param> True if the process should stop on error, false otherwise.
        /// <param name="progressBarParams"></param> Parameters for the progress bar.
        /// <param name="isCameraModelKnown"></param> True if the camera model is already known, false otherwise.
        /// <returns></returns>
        public static IEnumerator RunMappingCommand(MonoBehaviour caller, string workspace, bool displayProgressBar, bool stopOnError, string[] progressBarParams, bool isCameraModelKnown)
        {
            string command = "CALL " + COLMAPSettings.formattedCOLMAPExePath + " mapper";
            command += " --database_path ./" + databaseFileName;
            command += " --image_path ./" + imagesDirName;
            command += " --output_path ./" + sparseDirName;
            if(isCameraModelKnown)
                command += " --Mapper.ba_refine_focal_length 0 --Mapper.ba_refine_extra_params 0";
            yield return caller.StartCoroutine(GeneralToolkit.RunCommandCoroutine(typeof(COLMAPConnector), command, workspace, displayProgressBar, null, null, stopOnError, progressBarParams));
        }

        /// <summary>
        /// Coroutine that runs the exporting model as text command.
        /// </summary>
        /// <param name="caller"></param> The object calling this method.
        /// <param name="workspace"></param> The workspace from which to perform this step.
        /// <param name="displayProgressBar"></param> True if the progress bar should be displayed, false otherwise.
        /// <param name="stopOnError"></param> True if the process should stop on error, false otherwise.
        /// <param name="progressBarParams"></param> Parameters for the progress bar.
        /// <returns></returns>
        public static IEnumerator RunExportModelAsTextCommand(MonoBehaviour caller, string workspace, bool displayProgressBar, bool stopOnError, string[] progressBarParams)
        {
            string inoutName = Directory.Exists(GetSparse0Dir(workspace)) ? sparse0DirName : sparseDirName;
            string command = "CALL " + COLMAPSettings.formattedCOLMAPExePath + " model_converter";
            command += " --input_path ./" + inoutName;
            command += " --output_path ./" + inoutName;
            command += " --output_type TXT";
            yield return caller.StartCoroutine(GeneralToolkit.RunCommandCoroutine(typeof(COLMAPConnector), command, workspace, displayProgressBar, null, null, stopOnError, progressBarParams));
        }

        /// <summary>
        /// Coroutine that runs the undistortion command.
        /// </summary>
        /// <param name="caller"></param> The object calling this method.
        /// <param name="workspace"></param> The workspace from which to perform this step.
        /// <param name="displayProgressBar"></param> True if the progress bar should be displayed, false otherwise.
        /// <param name="stopOnError"></param> True if the process should stop on error, false otherwise.
        /// <param name="progressBarParams"></param> Parameters for the progress bar.
        /// <param name="maxImageSize"></param> Parameter specifying the maximum image size for this step.
        /// <returns></returns>
        public static IEnumerator RunUndistortionCommand(MonoBehaviour caller, string workspace, bool displayProgressBar, bool stopOnError, string[] progressBarParams, int maxImageSize)
        {
            string command = "CALL " + COLMAPSettings.formattedCOLMAPExePath + " image_undistorter";
            command += " --image_path ./" + imagesDirName;
            command += " --input_path ./" + sparse0DirName;
            command += " --output_path ./" + dense0DirName;
            command += " --max_image_size " + GeneralToolkit.ToString(maxImageSize);
            yield return caller.StartCoroutine(GeneralToolkit.RunCommandCoroutine(typeof(COLMAPConnector), command, workspace, displayProgressBar, null, null, stopOnError, progressBarParams));
        }

        /// <summary>
        /// Coroutine that runs the stereo command.
        /// </summary>
        /// <param name="caller"></param> The object calling this method.
        /// <param name="workspace"></param> The workspace from which to perform this step.
        /// <param name="displayProgressBar"></param> True if the progress bar should be displayed, false otherwise.
        /// <param name="stopOnError"></param> True if the process should stop on error, false otherwise.
        /// <param name="progressBarParams"></param> Parameters for the progress bar.
        /// <returns></returns>
        public static IEnumerator RunStereoCommand(MonoBehaviour caller, string workspace, bool displayProgressBar, bool stopOnError, string[] progressBarParams)
        {
            string command = "CALL " + COLMAPSettings.formattedCOLMAPExePath + " patch_match_stereo";
            command += " --workspace_path ./";
            yield return caller.StartCoroutine(GeneralToolkit.RunCommandCoroutine(typeof(COLMAPConnector), command, workspace, displayProgressBar, null, null, stopOnError, progressBarParams));
        }

        /// <summary>
        /// Coroutine that runs the fusion command.
        /// </summary>
        /// <param name="caller"></param> The object calling this method.
        /// <param name="workspace"></param> The workspace from which to perform this step.
        /// <param name="displayProgressBar"></param> True if the progress bar should be displayed, false otherwise.
        /// <param name="stopOnError"></param> True if the process should stop on error, false otherwise.
        /// <param name="progressBarParams"></param> Parameters for the progress bar.
        /// <returns></returns>
        public static IEnumerator RunFusionCommand(MonoBehaviour caller, string workspace, bool displayProgressBar, bool stopOnError, string[] progressBarParams)
        {
            string command = "CALL " + COLMAPSettings.formattedCOLMAPExePath + " stereo_fusion";
            command += " --workspace_path ./";
            command += " --output_path ./" + fusedFileName;
            yield return caller.StartCoroutine(GeneralToolkit.RunCommandCoroutine(typeof(COLMAPConnector), command, workspace, displayProgressBar, null, null, stopOnError, progressBarParams));
        }

        /// <summary>
        /// Coroutine that runs the Delaunay meshing command.
        /// </summary>
        /// <param name="caller"></param> The object calling this method.
        /// <param name="workspace"></param> The workspace from which to perform this step.
        /// <param name="displayProgressBar"></param> True if the progress bar should be displayed, false otherwise.
        /// <param name="stopOnError"></param> True if the process should stop on error, false otherwise.
        /// <param name="progressBarParams"></param> Parameters for the progress bar.
        /// <returns></returns>
        public static IEnumerator RunDelaunayMeshingCommand(MonoBehaviour caller, string workspace, bool displayProgressBar, bool stopOnError, string[] progressBarParams)
        {
            string command = "CALL " + COLMAPSettings.formattedCOLMAPExePath + " delaunay_mesher";
            command += " --input_path ./";
            command += " --output_path ./" + delaunayFileName;
            yield return caller.StartCoroutine(GeneralToolkit.RunCommandCoroutine(typeof(COLMAPConnector), command, workspace, displayProgressBar, null, null, stopOnError, progressBarParams));
        }

        /// <summary>
        /// Determines the progress bar's parameter at index one for the given inputs.
        /// </summary>
        /// <param name="prefix"></param> The information's prefix.
        /// <param name="isSparse"></param> True if this is sparse reconstruction, false otherwise.
        /// <param name="step"></param> The step of the process.
        /// <param name="maxStep"></param> The max step of the process.
        /// <returns></returns> The progress bar parameter.
        public static string GetProgressBarParamsOne(string prefix, bool isSparse, int step, int maxStep)
        {
            string typeName = isSparse ? "Sparse" : "Dense";
            return prefix + " - " + typeName + " reconstruction step " + GeneralToolkit.ToString(step) + "/" + GeneralToolkit.ToString(maxStep) + ".";
        }

        /// <summary>
        /// Changes the workspace after having performed sparse reconstruction.
        /// </summary>
        /// <param name="dataHandler"></param> The data handler of which to change the data directory.
        public static void ChangeWorkspaceAfterSparseReconstruction(DataHandler dataHandler)
        {
            string newDirectory = GetDense0Dir(dataHandler.dataDirectory);
            dataHandler.ChangeDataDirectory(newDirectory);
        }

        /// <summary>
        /// Coroutine that runs the sparse reconstruction process.
        /// </summary>
        /// <param name="caller"></param> The processing object calling this method.
        /// <param name="workspace"></param> The workspace from which to perform this method.
        /// <param name="COLMAPCameraIndex"></param> The index of the type of source camera (in the list of COLMAP cameras).
        /// <param name="isSingleCamera"></param> True if the source images were acquired by the same camera, false otherwise.
        /// <param name="maxImageSize"></param> The maximum image size for the undistortion step.
        /// <returns></returns>
        public static IEnumerator RunSparseReconstructionCoroutine(Processing.Processing caller, string workspace, int COLMAPCameraIndex, bool isSingleCamera, int maxImageSize)
        {
            // Indicate to the user that the process has started.
            GeneralToolkit.ResetCancelableProgressBar(true, true);
            // Create or clear the folders needed for the reconstruction.
            GeneralToolkit.Delete(GetDatabaseFile(workspace));
            GeneralToolkit.CreateOrClear(PathType.Directory, GetSparseDir(workspace));
            GeneralToolkit.CreateOrClear(PathType.Directory, GetSparse0Dir(workspace));
            GeneralToolkit.CreateOrClear(PathType.Directory, GetDenseDir(workspace));
            GeneralToolkit.CreateOrClear(PathType.Directory, GetDense0Dir(workspace));
            // Initialize the command parameters.
            bool displayProgressBar = true;
            bool stopOnError = true;
            string[] progressBarParams = new string[3];
            int maxStep = 6;
            progressBarParams[0] = GeneralToolkit.ToString(maxStep);
            progressBarParams[2] = "Processing canceled by user.";
            // Launch the different steps of the sparse reconstruction process.
            float focalLengthFactor = 0;
            for(int step = 1; step <= maxStep; step++)
            {
                // Step one: launch feature extraction.
                if(step == 1)
                {
                    progressBarParams[1] = GetProgressBarParamsOne("Feature extraction", true, step, maxStep);
                    CameraModel[] cameraModels = caller.cameraSetup.cameraModels;
                    if(cameraModels != null && cameraModels.Length > 0)
                    {
                        CameraModel cameraParams = cameraModels[0];
                        float focalLength = Camera.FieldOfViewToFocalLength(cameraParams.fieldOfView.x, cameraParams.pixelResolution.x);
                        focalLengthFactor = focalLength / Mathf.Max(cameraParams.pixelResolution.x, cameraParams.pixelResolution.y);
                    }
                    yield return caller.StartCoroutine(RunFeatureExtractionCommand(caller, workspace, displayProgressBar, stopOnError, progressBarParams, COLMAPCameraIndex, isSingleCamera, focalLengthFactor));
                }
                // Step two: launch feature matching.
                else if(step == 2)
                {
                    progressBarParams[1] = GetProgressBarParamsOne("Feature matching", true, step, maxStep);
                    yield return caller.StartCoroutine(RunFeatureMatchingCommand(caller, workspace, displayProgressBar, stopOnError, progressBarParams));
                }
                // Step three: launch mapping.
                else if(step == 3)
                {
                    progressBarParams[1] = GetProgressBarParamsOne("Mapping", true, step, maxStep);
                    yield return caller.StartCoroutine(RunMappingCommand(caller, workspace, displayProgressBar, stopOnError, progressBarParams, (focalLengthFactor > 0)));
                }
                // Step four: launch exporting original camera setup as text.
                else if(step == 4)
                {
                    progressBarParams[1] = GetProgressBarParamsOne("Exporting camera setup (original) as text", true, step, maxStep);
                    yield return caller.StartCoroutine(RunExportModelAsTextCommand(caller, workspace, displayProgressBar, stopOnError, progressBarParams));
                }
                // Step five: launch image undistortion.
                else if(step == 5)
                {
                    // Launch undistortion.
                    progressBarParams[1] = GetProgressBarParamsOne("Undistortion", true, step, maxStep);
                    yield return caller.StartCoroutine(RunUndistortionCommand(caller, workspace, displayProgressBar, stopOnError, progressBarParams, maxImageSize));
                    // Change the workspace and the data directory to the one created in the dense folder.
                    ChangeWorkspaceAfterSparseReconstruction(caller.dataHandler);
                    Debug.Log(GeneralToolkit.FormatScriptMessage(typeof(COLMAPConnector), "Changed data directory to: " + workspace + "."));
                }
                // Step six: launch exporting undistorted camera setup as text.
                else if(step == 6)
                {
                    // Launch export process.
                    progressBarParams[1] = GetProgressBarParamsOne("Exporting camera setup (undistorted) as text", true, step, maxStep);
                    yield return caller.StartCoroutine(RunExportModelAsTextCommand(caller, GetDense0Dir(workspace), displayProgressBar, stopOnError, progressBarParams));
                    // Display the parsed camera setup in the Scene view.
                    caller.Deselected();
                    caller.Selected();
                    yield return null;
                }
                // For each step, continue only if the user does not cancel the process.
                if(GeneralToolkit.progressBarCanceled)
                    break;
            }
            // Change the data directory to the one created in the dense folder.
            if(!GeneralToolkit.progressBarCanceled)
                Debug.Log(GeneralToolkit.FormatScriptMessage(typeof(COLMAPConnector), "Sparse reconstruction was a success."));
            // Indicate to the user that the process has ended.
            GeneralToolkit.ResetCancelableProgressBar(false, false);
        }

        /// <summary>
        /// Coroutine that runs the dense reconstruction process.
        /// </summary>
        /// <param name="caller"></param> The object calling this method.
        /// <param name="workspace"></param> The workspace from which to perform this method.
        /// <returns></returns>
        public static IEnumerator RunDenseReconstructionCoroutine(MonoBehaviour caller, string workspace)
        {
            // Indicate to the user that the process has started.
            GeneralToolkit.ResetCancelableProgressBar(true, true);
            // Initialize the command parameters.
            bool displayProgressBar = true;
            bool stopOnError = true;
            string[] progressBarParams = new string[3];
            int maxStep = 3;
            progressBarParams[0] = GeneralToolkit.ToString(maxStep);
            progressBarParams[2] = "Processing canceled by user.";
            // Launch the different steps of the reconstruction process.
            for(int step = 1; step <= maxStep; step++)
            {
                // Step one: launch stereo.
                if(step == 1)
                {
                    progressBarParams[1] = GetProgressBarParamsOne("Stereo", false, step, maxStep);
                    yield return caller.StartCoroutine(RunStereoCommand(caller, workspace, displayProgressBar, stopOnError, progressBarParams));
                }
                // Step two: launch fusion.
                else if(step == 2)
                {
                    progressBarParams[1] = GetProgressBarParamsOne("Fusion", false, step, maxStep);
                    yield return caller.StartCoroutine(RunFusionCommand(caller, workspace, displayProgressBar, stopOnError, progressBarParams));
                }
                // Step three: launch Delaunay meshing, which exports the mesh as a .PLY file.
                else if(step == 3)
                {
                    progressBarParams[1] = GetProgressBarParamsOne("Delaunay meshing", false, step, maxStep);
                    yield return caller.StartCoroutine(RunDelaunayMeshingCommand(caller, workspace, displayProgressBar, stopOnError, progressBarParams));
                }
                // For each step, continue only if the user does not cancel the process.
                if(GeneralToolkit.progressBarCanceled)
                    break;
            }
            // Change the data directory to the one created in the dense folder.
            if(!GeneralToolkit.progressBarCanceled)
                Debug.Log(GeneralToolkit.FormatScriptMessage(typeof(COLMAPConnector), "Dense reconstruction was a success."));
            // Indicate to the user that the process has ended.
            GeneralToolkit.ResetCancelableProgressBar(false, false);
        }

#endif //UNITY_EDITOR

        /// <summary>
        /// Gets the images directory path for the given workspace.
        /// </summary>
        /// <param name="workspace"></param> The given workspace.
        /// <returns></returns> The images directory path.
        public static string GetImagesDir(string workspace)
        {
            return Path.Combine(workspace, imagesDirName);
        }

        /// <summary>
        /// Gets the sparse directory path for the given workspace.
        /// </summary>
        /// <param name="workspace"></param> The given workspace.
        /// <returns></returns> The sparse directory path.
        public static string GetSparseDir(string workspace)
        {
            return Path.Combine(workspace, sparseDirName);
        }

        /// <summary>
        /// Gets the sparse0 directory path for the given workspace.
        /// </summary>
        /// <param name="workspace"></param> The given workspace.
        /// <returns></returns> The sparse0 directory path.
        public static string GetSparse0Dir(string workspace)
        {
            return Path.Combine(workspace, sparse0DirName);
        }

        /// <summary>
        /// Gets the dense directory path for the given workspace.
        /// </summary>
        /// <param name="workspace"></param> The given workspace.
        /// <returns></returns> The dense directory path.
        public static string GetDenseDir(string workspace)
        {
            return Path.Combine(workspace, denseDirName);
        }

        /// <summary>
        /// Gets the dense0 directory path for the given workspace.
        /// </summary>
        /// <param name="workspace"></param> The given workspace.
        /// <returns></returns> The dense0 directory path.
        public static string GetDense0Dir(string workspace)
        {
            return Path.Combine(workspace, dense0DirName);
        }

        /// <summary>
        /// Gets the database file path for the given workspace.
        /// </summary>
        /// <param name="workspace"></param> The given workspace.
        /// <returns></returns> The database file path.
        public static string GetDatabaseFile(string workspace)
        {
            return Path.Combine(workspace, databaseFileName);
        }

        /// <summary>
        /// Gets the camera setup directory path for the given workspace.
        /// </summary>
        /// <param name="workspace"></param> The given workspace.
        /// <returns></returns> The camera setup directory path.
        private static string GetCameraSetupDir(string workspace)
        {
            return Directory.Exists(GetSparse0Dir(workspace)) ? GetSparse0Dir(workspace) : GetSparseDir(workspace);
        }

        /// <summary>
        /// Gets the cameras file path for the given workspace.
        /// </summary>
        /// <param name="workspace"></param> The given workspace.
        /// <returns></returns> The cameras file path.
        public static string GetCamerasFile(string workspace)
        {
            return Path.Combine(GetCameraSetupDir(workspace), camerasFileName);
        }

        /// <summary>
        /// Gets the images file path for the given workspace.
        /// </summary>
        /// <param name="workspace"></param> The given workspace.
        /// <returns></returns> The images file path.
        public static string GetImagesFile(string workspace)
        {
            return Path.Combine(GetCameraSetupDir(workspace), imagesFileName);
        }

        /// <summary>
        /// Gets the points file path for the given workspace.
        /// </summary>
        /// <param name="workspace"></param> The given workspace.
        /// <returns></returns> The points file path.
        public static string GetPointsFile(string workspace)
        {
            return Path.Combine(GetCameraSetupDir(workspace), pointsFileName);
        }

        /// <summary>
        /// Gets the Delaunay mesh file path for the given workspace.
        /// </summary>
        /// <param name="workspace"></param> The given workspace.
        /// <returns></returns> The Delaunay mesh file path.
        public static string GetDelaunayFile(string workspace)
        {
            return Path.Combine(workspace, delaunayFileName);
        }

        /// <summary>
        /// Gets the stereo directory path for the given workspace.
        /// </summary>
        /// <param name="workspace"></param> The given workspace.
        /// <returns></returns> The stereo directory path.
        public static string GetStereoDir(string workspace)
        {
            return Path.Combine(workspace, stereoDirName);
        }

        /// <summary>
        /// Gets the depth maps directory path for the given workspace.
        /// </summary>
        /// <param name="workspace"></param> The given workspace.
        /// <returns></returns> The depth maps directory path.
        public static string GetDepthMapsDir(string workspace)
        {
            return Path.Combine(GetStereoDir(workspace), depthMapsDirName);
        }
        
        /// <summary>
        /// Parses camera parameters from a string.
        /// The string must be formated as a line from COLMAP's "cameras.txt" file.
        /// The supported camera types from COLMAP are: SIMPLE_PINHOLE, PINHOLE.
        /// </summary>
        /// <param name="line"></param> The line to be parsed.
        /// <returns></returns> A camera model containing the parsed parameters.
        public static CameraModel TryParseCameraModel(string line)
        {
            CameraModel cameraModel = null;
            string[] split = line.Split(' ');
            int numberOfParameters = split.Length - 2;
            // COLMAP cameras have at least one parameter.
            if(numberOfParameters < 1)
            {
                Debug.LogError(line + " is not in the right format to be a COLMAP camera.");
            }
            // Parse the camera based on the camera type and number of parameters.
            else
            {
                string cameraType = split[1];
                if(cameraType == "SIMPLE_PINHOLE" || cameraType == "PINHOLE" || cameraType == "OMNIDIRECTIONAL")
                {
                    if(cameraType == "SIMPLE_PINHOLE" && numberOfParameters == 5)
                        cameraModel = ParsePinhole(split, true);
                    else if(cameraType == "PINHOLE" && numberOfParameters == 6)
                        cameraModel = ParsePinhole(split, false);
                    else if(cameraType == "OMNIDIRECTIONAL" && numberOfParameters == 5)
                        cameraModel = ParseOmnidirectional(split);
                    else
                        Debug.LogWarning("Provided number of parameters (" + numberOfParameters + ") is not the expected number for camera type " + cameraType + ".");
                }
                else if (COLMAPCameraTypes.Contains(cameraType))
                {
                    Debug.LogWarning("COLMAP camera type " + cameraType + " is not currently supported by COLIBRI VR.");
                }
                else
                {
                    Debug.LogWarning("Camera type " + cameraType + " is not valid.");
                }
            }
            return cameraModel;
        }

        /// <summary>
        /// Parses the basic parameters of any COLMAP camera model.
        /// </summary>
        /// <param name="split"></param> The split string from which to parse information.
        /// <param name="isOmnidirectional"></param> True if the camera model is omnidirectional, false otherwise.
        /// <returns></returns> A camera model containing the parsed parameters.
        private static CameraModel BasicParse(string[] split, bool isOmnidirectional)
        {
            CameraModel cameraModel = CameraModel.CreateCameraModel();
            // The camera's projection type is not handled by COLMAP, and has to be specified.
            cameraModel.isOmnidirectional = isOmnidirectional;
            // The camera's index is given by the first parameter in the .txt file.
            cameraModel.SetCameraReferenceIndexAndImageName(GeneralToolkit.ParseInt(split[0]), cameraModel.imageName);
            // The camera's pixel resolution is given by the second and third parameters in the .txt file.
            cameraModel.pixelResolution = new Vector2Int(GeneralToolkit.ParseInt(split[2]), GeneralToolkit.ParseInt(split[3]));
            // Return the camera model.
            return cameraModel;
        }

        /// <summary>
        /// Parses camera parameters for a COLMAP camera of the SIMPLE_PINHOLE or PINHOLE type.
        /// </summary>
        /// <param name="split"></param> The split string from which to parse information.
        /// <returns></returns> A camera model containing the parsed parameters.
        private static CameraModel ParsePinhole(string[] split, bool isSimple)
        {
            // Parse basic parameters for a perspective camera.
            CameraModel cameraModel = BasicParse(split, false);
            // Parse focal length based on whether this is a SIMPLE_PINHOLE or PINHOLE camera model.
            Vector2 focalLength = Vector2.zero;
            if(isSimple)
                focalLength = GeneralToolkit.ParseFloat(split[4]) * Vector2.one;
            else
                focalLength = new Vector2(GeneralToolkit.ParseFloat(split[4]), GeneralToolkit.ParseFloat(split[5]));
            // Compute field of view based on the focal length.
            float fieldOfViewX = Camera.FocalLengthToFieldOfView(focalLength.x, cameraModel.pixelResolution.x);
            float fieldOfViewY = Camera.FocalLengthToFieldOfView(focalLength.y, cameraModel.pixelResolution.y);
            cameraModel.fieldOfView = new Vector2(fieldOfViewX, fieldOfViewY);
            // Return the camera model.
            return cameraModel;
        }

        /// <summary>
        /// Parses camera parameters for a camera of the OMNIDIRECTIONAL type. Note that this type is not actually handled by COLMAP.
        /// </summary>
        /// <param name="split"></param> The split string from which to parse information.
        /// <returns></returns> A camera model containing the parsed parameters.
        private static CameraModel ParseOmnidirectional(string[] split)
        {
            // Parse basic parameters for an omnidirectional camera.
            CameraModel cameraModel = BasicParse(split, true);
            // Return the camera model.
            return cameraModel;
        }

        /// <summary>
        /// Converts position and rotation from Unity's left-handed coordinate system to COLMAP's right-handed coordinate system.
        /// </summary>
        /// <param name="position"></param> The position: input in Unity space, output in COLMAP space.
        /// <param name="rotation"></param> The rotation: input in Unity space, output in COLMAP space.
        private static void ConvertCoordinatesUnityToCOLMAP(ref Vector3 position, ref Quaternion rotation)
        {
            rotation = new Quaternion(-rotation.x, rotation.y, -rotation.z, rotation.w);
            rotation = Quaternion.Inverse(rotation);
            position = Vector3.Scale(position, new Vector3(1, -1, 1));
            position = rotation * - position;
        }

        /// <summary>
        /// Converts position and rotation from COLMAP's right-handed coordinate system to Unity's left-handed coordinate system.
        /// </summary>
        /// <param name="position"></param> The position: input in COLMAP space, output in Unity space.
        /// <param name="rotation"></param> The rotation: input in COLMAP space, output in Unity space.
        private static void ConvertCoordinatesCOLMAPToUnity(ref Vector3 position, ref Quaternion rotation)
        {
            position = Quaternion.Inverse(rotation) * - position;
            position = Vector3.Scale(position, new Vector3(1, -1, 1));
            rotation = Quaternion.Inverse(rotation);
            rotation = new Quaternion(-rotation.x, rotation.y, -rotation.z, rotation.w);
        }

        /// <summary>
        /// Writes the parameters from the given camera models into a text file.
        /// The format is that of COLMAP's "cameras.txt" file, and can be read directly. 
        /// </summary>
        /// <param name="cameraModels"></param> The camera models to be written to file.
        /// <param name="workspace"></param> The workspace from which to work.
        public static void SaveCamerasInformation(CameraModel[] cameraModels, string workspace)
        {
            StringBuilder stringBuilder = new StringBuilder();
            // Set up the file's header.
            stringBuilder.AppendLine("# Camera list with one line of data per camera:");
            stringBuilder.AppendLine("#   CAMERA_ID, MODEL, WIDTH, HEIGHT, PARAMS[]");
            stringBuilder.AppendLine("# Number of cameras: " + GeneralToolkit.ToString(cameraModels.Length));
            // Compute strings for each parameter.
            for(int iter = 0; iter < cameraModels.Length; iter++)
            {
                CameraModel cameraModel = cameraModels[iter];
                string CAMERA_ID = GeneralToolkit.ToString(cameraModel.cameraReferenceIndex);
                string MODEL = cameraModel.modelName;
                string WIDTH = GeneralToolkit.ToString(cameraModel.pixelResolution.x);
                string HEIGHT = GeneralToolkit.ToString(cameraModel.pixelResolution.y);
                string PARAMS_FOCALLENGTH = GeneralToolkit.ToString(Camera.FieldOfViewToFocalLength(cameraModel.fieldOfView.x, cameraModel.pixelResolution.x));
                string PARAMS_CENTERWIDTH = GeneralToolkit.ToString(cameraModel.pixelResolution.x / 2);
                string PARAMS_CENTERHEIGHT = GeneralToolkit.ToString(cameraModel.pixelResolution.y / 2);
                string line = CAMERA_ID + " " + MODEL + " " + WIDTH + " " + HEIGHT + " " + PARAMS_FOCALLENGTH + " " + PARAMS_CENTERWIDTH + " " + PARAMS_CENTERHEIGHT;
                stringBuilder.AppendLine(line);
            }
            // Write the header and parameters into a .txt file.
            File.WriteAllText(GetCamerasFile(workspace), stringBuilder.ToString());
        }

        /// <summary>
        /// Reads camera information from a COLMAP "cameras.txt" file, and saves it into the referenced array.
        /// Note that the array contains a camera element for each image. Array elements should already contain the cameras' pose information.
        /// </summary>
        /// <param name="cameraSetup"></param> The camera setup containing the camera models, one for each image.
        /// <param name="workspace"></param> The workspace from which to work.
        public static void ReadCamerasInformation(CameraSetup cameraSetup, string workspace)
        {
            // Only continue if there is information to work with.
            if(cameraSetup.cameraModels == null)
                return;
            // Parse the set of COLMAP cameras from the file.
            List<CameraModel> cameraModelList = new List<CameraModel>();
            string[] lines = File.ReadAllLines(GetCamerasFile(workspace));
            foreach(string line in lines)
            {
                if(!line.StartsWith("#"))
                {
                    CameraModel cameraModel = TryParseCameraModel(line);
                    if(cameraModel == null)
                    {
                        Debug.LogWarning("One or more of the camera models could not be parsed. Camera setup cannot be computed.");
                        cameraSetup.ResetCameraModels();
                        return;
                    }
                    else
                    {
                        cameraModelList.Add(cameraModel);
                    }
                }
            }
            CameraModel[] camerasInFile = cameraModelList.ToArray();
            // Update the camera models from this information.
            CameraModel[] cameraModels = cameraSetup.cameraModels;
            for(int i = 0; i < cameraModels.Length; i++)
            {
                CameraModel cameraModel = cameraModels[i];
                // The camera models should already know the COLMAP camera they are related to.
                int desiredCameraIndex = cameraModel.cameraReferenceIndex;
                // Find the corresponding COLMAP camera, and provide the camera model with its parameters.
                foreach(CameraModel cameraInFile in camerasInFile)
                {
                    if(cameraInFile.cameraReferenceIndex == desiredCameraIndex)
                    {
                        cameraModel.isOmnidirectional = cameraInFile.isOmnidirectional;
                        cameraModel.pixelResolution = cameraInFile.pixelResolution;
                        cameraModel.fieldOfView = cameraInFile.fieldOfView;
                    }
                }
            }
            // Destroy the temporary camera models.
            for(int iter = 0; iter < camerasInFile.Length; iter++)
                CameraModel.DestroyImmediate(camerasInFile[iter].gameObject);
        }

        /// <summary>
        /// Writes the parameters from the given array of camera models into a text file.
        /// The format is that of COLMAP's "images.txt" file, and can be read directly. 
        /// </summary>
        /// <param name="cameraModels"></param> The camera models to parse to obtain image information.
        /// <param name="workspace"></param> The workspace from which to work.
        public static void SaveImagesInformation(CameraModel[] cameraModels, string workspace)
        {
            StringBuilder stringBuilder = new StringBuilder();
            // Set up the file's header.
            stringBuilder.AppendLine("# Image list with two lines of data per image:");
            stringBuilder.AppendLine("#   IMAGE_ID, QW, QX, QY, QZ, TX, TY, TZ, CAMERA_ID, NAME");
            stringBuilder.AppendLine("#   POINTS2D[] as (X, Y, POINT3D_ID)");
            stringBuilder.AppendLine("# Number of images: " + cameraModels.Length + ", mean observations per image: 0");
            // For each camera model, extract the parameters and write them as strings.
            for(int i = 0; i < cameraModels.Length; i++)
            {
                // Convert position and rotation to COLMAP's coordinate system.
                Quaternion rotation = cameraModels[i].transform.rotation;
                Vector3 position = cameraModels[i].transform.position;
                ConvertCoordinatesUnityToCOLMAP(ref position, ref rotation);
                // Compute strings for each parameter. Note that we only use this for acquisition, where there is only one camera.
                string IMAGE_ID = GeneralToolkit.ToString(i + 1);
                string QW = GeneralToolkit.ToString(rotation.w);
                string QX = GeneralToolkit.ToString(rotation.x);
                string QY = GeneralToolkit.ToString(rotation.y);
                string QZ = GeneralToolkit.ToString(rotation.z);
                string TX = GeneralToolkit.ToString(position.x);
                string TY = GeneralToolkit.ToString(position.y);
                string TZ = GeneralToolkit.ToString(position.z);
                string CAMERA_ID = "1";
                string NAME = cameraModels[i].imageName;
                string line = IMAGE_ID + " " + QW + " " + QX + " " + QY + " " + QZ + " " + TX + " " + TY + " " + TZ + " " + CAMERA_ID + " " + NAME;
                // For each image, append one line with the parameters and one empty line.
                stringBuilder.AppendLine(line);
                stringBuilder.AppendLine(string.Empty);
            }
            // Write the header and parameters into a .txt file.
            File.WriteAllText(GetImagesFile(workspace), stringBuilder.ToString());
        }

        /// <summary>
        /// Reads images information from a COLMAP "images.txt" file, and saves it into the referenced array.
        /// Note that the array contains a camera element for each image. Array elements will be initialized here.
        /// </summary>
        /// <param name="cameraSetup"></param> The camera setup to which to output the list of parsed camera models.
        /// <param name="workspace"></param> The workspace from which to work.
        public static void ReadImagesInformation(CameraSetup cameraSetup, string workspace)
        {
            List<Vector3> positionList = new List<Vector3>();
            List<Quaternion> rotationList = new List<Quaternion>();
            List<string> fileNameList = new List<string>();
            List<int> cameraIDList = new List<int>();
            // Read COLMAP's images file.
            using(StreamReader reader = File.OpenText(GetImagesFile(workspace)))
            {
                bool isOdd = false;
                string line;
                // Read the file line-by-line to the end.
                while((line = reader.ReadLine()) != null)
                {
                    // Skip the lines from the header, that start with #.
                    if(!line.StartsWith("#"))
                    {
                        // If the line is odd, skip it, and indicate that the next line will be even.
                        if(isOdd)
                        {
                            isOdd = false;
                        }
                        // If the line is even, parse it, and indicate that the next line will be odd.
                        else
                        {
                            string[] split = line.Split(' ');
                            // COLMAP's images should have 10 parameters.
                            if(split.Length > 9)
                            {
                                // Parse position and rotation, and convert them to Unity's coordinate system.
                                Quaternion rotation = new Quaternion(GeneralToolkit.ParseFloat(split[2]), GeneralToolkit.ParseFloat(split[3]), GeneralToolkit.ParseFloat(split[4]), GeneralToolkit.ParseFloat(split[1]));
                                Vector3 position = new Vector3(GeneralToolkit.ParseFloat(split[5]), GeneralToolkit.ParseFloat(split[6]), GeneralToolkit.ParseFloat(split[7]));
                                ConvertCoordinatesCOLMAPToUnity(ref position, ref rotation);
                                // Add all the parameters to the dedicated lists.
                                positionList.Add(position);
                                rotationList.Add(rotation);
                                fileNameList.Add(split[9]);
                                cameraIDList.Add(GeneralToolkit.ParseInt(split[8]));
                                // Indicate that the next line will be odd.
                                isOdd = true;
                            }
                        }
                    }
                }
                reader.Close();
            }
            // Use these lists to create and fill the output array of camera models.
            cameraSetup.cameraModels = new CameraModel[positionList.Count];
            for(int iter = 0; iter < positionList.Count; iter++)
            {
                CameraModel cameraModel = cameraSetup.AddCameraModel(iter);
                cameraModel.SetCameraReferenceIndexAndImageName(cameraIDList[iter], fileNameList[iter]);
                cameraModel.transform.localPosition = positionList[iter];
                cameraModel.transform.localRotation = rotationList[iter];
            }
        }

        /// <summary>
        /// Returns whether a given workspace path contains data organized in a way that can be read by COLMAP.
        /// </summary>
        /// <param name="workspace"></param> The path to the COLMAP workspace.
        /// <returns></returns> True if the workspace is valid, false otherwise.
        public static bool DirectoryIsValidForReading(string workspace)
        {
            if(File.Exists(GetCamerasFile(workspace)) && File.Exists(GetImagesFile(workspace)))
                return true;
            return false;
        }

        /// <summary>
        /// Reads the given depth map stored in COLMAP's binary format, and outputs the corresponding two-dimensional array of floats.
        /// </summary>
        /// <param name="workspace"></param> The path to the COLMAP workspace.
        /// <param name="depthMapIndex"></param> The index of the depth map to read.
        /// <returns></returns> The array of floats.
        public static float[,] ReadDepthToArray(string workspace, int depthMapIndex)
        {
            float[,] floatArray = null;
            if(Directory.Exists(GetDepthMapsDir(workspace)))
            {
                FileInfo[] depthMaps = GeneralToolkit.GetFilesByExtension(GetDepthMapsDir(workspace), ".bin");
                using(FileStream fileStream = new FileStream(depthMaps[depthMapIndex].FullName, FileMode.Open))
                {
                    using(BinaryReader binaryReader = new BinaryReader(fileStream))
                    {
                        int delimiterCount = 0;
                        int width = 0;
                        int height = 0;
                        int channels = 0;
                        string readString = string.Empty;
                        for(int iter = 0; iter < 100; iter++)
                        {
                            char readChar = binaryReader.ReadChar();
                            if(readChar == '&')
                            {
                                int parsedInt = GeneralToolkit.ParseInt(readString);
                                if(delimiterCount == 0)
                                    width = parsedInt;
                                else if(delimiterCount == 1)
                                    height = parsedInt;
                                else if(delimiterCount == 2)
                                    channels = parsedInt;
                                delimiterCount ++;
                                readString = string.Empty;
                            }
                            else
                            {
                                readString += readChar;
                            }
                            if(delimiterCount > 2)
                                break;
                        }
                        
                        int expectedFloatCount = width * height * channels;
                        floatArray = new float[width, height];
                        for(int iterY = 0; iterY < height; iterY++)
                        {
                            for(int iterX = 0; iterX < width; iterX++)
                            {
                                floatArray[iterX, iterY] = binaryReader.ReadSingle();
                            }
                        }
                        for(int iter = 0; iter < 10; iter++)
                        {
                            Debug.Log(floatArray[iter, height-1]);
                        }
                    }
                }
            }
            return floatArray;
        }

#endregion //STATIC_METHODS

    }

}
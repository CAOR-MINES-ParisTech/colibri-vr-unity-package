/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

using System.Collections;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using COLIBRIVR.ExternalConnectors;
using COLIBRIVR.Evaluation;
using COLIBRIVR.Rendering;

namespace COLIBRIVR.Processing
{

    /// <summary>
    /// Class that handles the processing of source data into asset bundles ready for rendering.
    /// </summary>
    [ExecuteInEditMode]
    public class Processing : MonoBehaviour
    {

#region CONST_FIELDS

        private const string _sourceCallerName = "Source image";

#endregion //CONST_FIELDS

#region PROPERTIES

        public CameraSetup cameraSetup { get { return _cameraSetup; } }
        public DataHandler dataHandler { get { return _dataHandler; } }
        public ExternalHelper[] externalHelpers { get { return _externalHelpers; } }
        public ProcessingMethod[] processingMethods { get { return _processingMethods; } }

#endregion //PROPERTIES

#region FIELDS

        public Rendering.Rendering renderingCaller;
        public string sourceDataInfo;
        public string processedDataInfo;
        public bool isDataReadyForBundling;
        public bool isDataBundled;
        public bool processingCanceled;
        public RenderTexture previewSourceTexture;

        [SerializeField] private CameraSetup _cameraSetup;
        [SerializeField] private DataHandler _dataHandler;
        [SerializeField] private ExternalHelper[] _externalHelpers;
        [SerializeField] private ProcessingMethod[] _processingMethods;
        [SerializeField] private ColorTextureArray _previewSourceImagesLoader;

        private int _lastLoadedPreviewIndex;

#endregion //FIELDS

#region INHERITANCE_METHODS

        /// <summary>
        /// Resets the object's properties.
        /// </summary>
        public void Reset()
        {
            // Check if there is a rendering caller.
            renderingCaller = GetComponent<Rendering.Rendering>();
            // Reset the key child components.
            _cameraSetup = CameraSetup.CreateOrResetCameraSetup(transform);
            _dataHandler = DataHandler.CreateOrResetDataHandler(transform);
            // Initialize the processing methods.
            _processingMethods = ProcessingMethod.CreateOrResetProcessingMethods(transform);
            for(int iter = 0; iter < _processingMethods.Length; iter++)
                _processingMethods[iter].InitializeLinks();
            // Initialize the external tools.
            _externalHelpers = ExternalHelper.CreateOrResetExternalHelpers(transform);
            for(int iter = 0; iter < _externalHelpers.Length; iter++)
                _externalHelpers[iter].InitializeLinks();
            // Get the method defining the container for the input images.
            _previewSourceImagesLoader = _processingMethods[0].PMColorTextureArray;
            // Reads the acquisition information from the source data directory.
            ReadAcquisitionInformation();
            // Destroys all created method components.
            foreach(MonoBehaviour component in GetComponents<MonoBehaviour>())
                if(component is IMethodGUI)
                    DestroyImmediate(component);
        }

        /// <summary>
        /// Clears all created objects.
        /// </summary>
        void OnDestroy()
        {
            if(!GeneralToolkit.IsStartingNewScene())
            {
#if UNITY_EDITOR
                Deselected();
#endif //UNITY_EDITOR
                GeneralToolkit.RemoveChildComponents(transform, typeof(CameraSetup), typeof(DataHandler));
                GeneralToolkit.RemoveChildren(transform, ExternalHelper.EHTransformName, ProcessingMethod.PMTransformName,
                    RenderingMethod.RMTransformName, EvaluationMethod.EMTransformName);
            }
        }

#endregion //INHERITANCE_METHODS

#region METHODS

#if UNITY_EDITOR

        /// <summary>
        /// On preview index change, load the preview images and update the Scene window.
        /// </summary>
        private void OnPreviewIndexChange()
        {
            LoadSourceImageAsPreview();
            SceneView.RepaintAll();
        }

        /// <summary>
        /// On selection, reads source information and displays preview images.
        /// </summary>
        public void Selected()
        {
            // Notify the camera setup.
            cameraSetup.Selected();
            // Read the source data if necessary.
            if(cameraSetup.cameraModels == null)
                cameraSetup.FindCameraModels();
            // Check whether data has already been processed or bundled.
            dataHandler.CheckStatusOfDataProcessingAndBundling(out isDataReadyForBundling, out isDataBundled, out processedDataInfo);
            // Add a preview window listener.
            cameraSetup.onPreviewIndexChangeEvent.AddListener(OnPreviewIndexChange);
            // Inform the preview window that this object will send preview data to be displayed.
            PreviewWindow.AddCaller(cameraSetup, _sourceCallerName);
            // Load the preview images.
            LoadSourceImageAsPreview();
            // If it exists, inform the rendering component that it has been selected.
            if(renderingCaller != null)
                renderingCaller.Selected();
        }

        /// <summary>
        /// On deselection, informs the preview window that this object no longer sends preview data to be displayed.
        /// </summary>
        public void Deselected()
        {
            // Notify the camera setup.
            cameraSetup.Deselected();
            // Remove the preview window listener.
            cameraSetup.onPreviewIndexChangeEvent.RemoveListener(OnPreviewIndexChange);
            // Inform the preview window that this object no longer sends preview data to be displayed.
            PreviewWindow.RemoveCaller(_sourceCallerName);
        }

        /// <summary>
        /// Coroutine that prepares all that is necessary to load the source images as preview.
        /// </summary>
        /// <returns></returns>
        public IEnumerator PrepareLoadingSourceImagesAsPreviewCoroutine()
        {
            yield return StartCoroutine(_previewSourceImagesLoader.LoadProcessedTextureArrayCoroutine());
            _lastLoadedPreviewIndex = -1;
            LoadSourceImageAsPreview();
        }

        /// <summary>
        /// Loads the selected source image as preview.
        /// </summary>
        public void LoadSourceImageAsPreview()
        {
            if(!Application.isPlaying || _previewSourceImagesLoader == null || _previewSourceImagesLoader.colorData == null ||
                cameraSetup.previewIndex == _lastLoadedPreviewIndex || cameraSetup.cameraModels == null || cameraSetup.cameraModels.Length < 1)
                return;
            _lastLoadedPreviewIndex = cameraSetup.previewIndex;
            if(previewSourceTexture != null)
                DestroyImmediate(previewSourceTexture);
            Vector2Int previewResolution = PreviewWindow.GetPreviewResolution(cameraSetup.cameraModels[_lastLoadedPreviewIndex].pixelResolution);
            GeneralToolkit.CreateRenderTexture(ref previewSourceTexture, previewResolution, 0, RenderTextureFormat.ARGB32, false, FilterMode.Point);
            Graphics.Blit(_previewSourceImagesLoader.colorData, previewSourceTexture, _lastLoadedPreviewIndex, 0);
            int previewMaxIndex = cameraSetup.cameraModels.Length - 1;
            PreviewWindow.DisplayImage(_sourceCallerName, previewSourceTexture, previewMaxIndex);
        }

        /// <summary>
        /// Launches the coroutine that processes data.
        /// </summary>
        public void ProcessData()
        {
            StartCoroutine(ProcessDataCoroutine());
        }
        
        /// <summary>
        /// Processes color and/or depth data.
        /// </summary>
        /// <returns></returns>
        private IEnumerator ProcessDataCoroutine()
        {
            // Inform the user that the process has started.
            GeneralToolkit.ResetCancelableProgressBar(true, true);
            Debug.Log(GeneralToolkit.FormatScriptMessage(typeof(Processing), "Started processing data."));
            processingCanceled = false;
            // Save the current position, rotation, and scale for later.
            Vector3 position = transform.position;
            Quaternion rotation = transform.rotation;
            Vector3 scale = transform.localScale;
            // Reset the transform, and wait for the camera models to be updated accordingly.
            GeneralToolkit.SetTransformValues(transform, false, Vector3.zero, Quaternion.identity, Vector3.one);
            yield return null;
            // Create the processed data directory and initialize the processing information file.
            dataHandler.CreateProcessingInfoFile();
            // Move the processed data directory to the temporary directory.
            GeneralToolkit.Move(PathType.Directory, dataHandler.processedDataDirectory, GeneralToolkit.tempDirectoryAbsolutePath, false);
            AssetDatabase.Refresh();
            if(!Directory.Exists(GeneralToolkit.tempDirectoryAbsolutePath))
                processingCanceled = true;
            // Execute the processing methods.
            for(int iter = 0; iter < processingMethods.Length; iter++)
            {
                ProcessingMethod processingMethod = processingMethods[iter];
                if(!processingMethod.IsGUINested() && processingMethod.shouldExecute)
                    yield return StartCoroutine(processingMethod.ExecuteAndDisplayLog());
                if(processingCanceled)
                    break;
            }
            // Unload loaded assets.
            Resources.UnloadUnusedAssets();
            EditorUtility.UnloadUnusedAssetsImmediate();
            yield return null;
            // Move the processed data directory back to its original position.
            GeneralToolkit.Move(PathType.Directory, GeneralToolkit.tempDirectoryAbsolutePath, dataHandler.processedDataDirectory, false);
            AssetDatabase.Refresh();
            // Update the processed asset information.
            dataHandler.UpdateProcessedAssets();
            // Inform the user of the end of the process.
            if(!processingCanceled)
                Debug.Log(GeneralToolkit.FormatScriptMessage(typeof(Processing), "Finished processing data."));
            else
                Debug.Log(GeneralToolkit.FormatScriptMessage(typeof(Processing), "Processing was canceled."));
            // Perform a check to verify whether data was processed.
            dataHandler.CheckStatusOfDataProcessingAndBundling(out isDataReadyForBundling, out isDataBundled, out processedDataInfo);
            // Return the transform's values to their previous ones.
            GeneralToolkit.SetTransformValues(transform, false, position, rotation, scale);
            // Reset the progress bar.
            GeneralToolkit.ResetCancelableProgressBar(false, false);
        }

        /// <summary>
        /// Displays and updates a cancelable progress bar informing on the processing process.
        /// </summary>
        /// <param name="progressBarTitle"></param> The title to be displayed by the progress bar.
        /// <param name="progressBarInfo"></param> The information to be displayed by the progress bar.
        public void DisplayAndUpdateCancelableProgressBar(string progressBarTitle, string progressBarInfo)
        {
            int progressBarMaxIter = cameraSetup.cameraModels.Length;
            string exitMessage = "Processing canceled by user.";
            GeneralToolkit.UpdateCancelableProgressBar(typeof(Processing), true, true, true, progressBarMaxIter, progressBarTitle, progressBarInfo, exitMessage);
        }

        /// <summary>
        /// Bundles the generated assets, and updates the status of processed data.
        /// </summary>
        /// <returns></returns>
        public bool BundleButton()
        {
            bool success = dataHandler.CreateAssetBundleFromCreatedAssets();
            // Perform a check to verify whether data was bundled.
            dataHandler.CheckStatusOfDataProcessingAndBundling(out isDataReadyForBundling, out isDataBundled, out processedDataInfo);
            return success;
        }

#endif //UNITY_EDITOR

        /// <summary>
        /// Reads the acquisition information to store camera parameters and poses.
        /// </summary>
        public void ReadAcquisitionInformation()
        {
            // Reset the setup's camera models.
            cameraSetup.ResetCameraModels();
            // Check the data directory for source data.
            dataHandler.CheckStatusOfSourceData();
            // Check that the source data directory is configured based on the COLMAP file structure.
            if(COLMAPConnector.DirectoryIsValidForReading(dataHandler.dataDirectory))
            {
                // Read the pose data and camera parameters, and store it into the initial camera models.
                COLMAPConnector.ReadImagesInformation(cameraSetup, dataHandler.dataDirectory, out dataHandler.imagePointCorrespondencesExist);
                COLMAPConnector.ReadCamerasInformation(cameraSetup, dataHandler.dataDirectory);
                // Only continue if camera models were successfully parsed.
                if(cameraSetup.cameraModels != null && cameraSetup.cameraModels.Length > 0)
                {
#if UNITY_EDITOR
                    // If needed, provide default parameters for the additional source data information.
                    if(!File.Exists(dataHandler.additionalInfoFile))
                        dataHandler.SaveCOLIBRIVRAdditionalInformation(cameraSetup);
#endif //UNITY_EDITOR
                    // Read the additional information, and store it in the camera setup.
                    if(File.Exists(dataHandler.additionalInfoFile))
                        dataHandler.ReadCOLIBRIVRAdditionalInformation(cameraSetup);
#if UNITY_EDITOR
                    // Update the gizmo size.
                    cameraSetup.gizmoSize = CameraSetup.ComputeGizmoSize(cameraSetup.cameraModels);
                    cameraSetup.UpdateGizmosSize();
#endif //UNITY_EDITOR
                }
            }
            // Check whether data has already been processed or bundled.
            dataHandler.CheckStatusOfDataProcessingAndBundling(out isDataReadyForBundling, out isDataBundled, out processedDataInfo);
        }

#endregion //METHODS

    }

}
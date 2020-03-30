/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

using System.IO;
using System.Collections;
using UnityEngine;
using UnityEditor;
using COLIBRIVR.ExternalConnectors;

namespace COLIBRIVR.Acquisition
{

    /// <summary>
    /// Class used to acquire color/depth data from a synthetic Unity scene.
    /// </summary>
    [ExecuteInEditMode]
    public class Acquisition : MonoBehaviour
    {

#region CONST_FIELDS

        public const string propertyNameLockSetup = "_lockSetup";
        public const string propertyNameCameraCount = "_cameraCount";
        public const string propertyNameCameraPrefab = "_cameraPrefab";
        public const string propertyNameAcquireDepthData = "_acquireDepthData";
        public const string propertyNameCopyGlobalMesh = "_copyGlobalMesh";
        public const string propertyNameSetupType = "_setupType";
        public const string propertyNameSetupDirection = "_setupDirection";
        public const string shaderNameIsPrecise = "_IsPrecise";

        private const string _colorCallerName = "Source color";
        private const string _depthCallerName = "Source depth";

#endregion //CONST_FIELDS

#region PROPERTIES

        public DataHandler dataHandler { get { return _dataHandler; } }
        public CameraSetup cameraSetup { get { return _cameraSetup; } }

#endregion //PROPERTIES

#region FIELDS

        [SerializeField] private DataHandler _dataHandler;
        [SerializeField] private CameraSetup _cameraSetup;
        [SerializeField] private PreviewCameraManager _previewCameraManager;
        [SerializeField] private Vector2Int _cameraCount;
        [SerializeField] private bool _acquireDepthData;
        [SerializeField] private bool _copyGlobalMesh;
        [SerializeField] private SetupType _setupType;
        [SerializeField] private SetupDirection _setupDirection;
        [SerializeField] private GameObject _cameraPrefab;
        [SerializeField] private bool _lockSetup;

        private RenderTexture _targetDepthTexture;
        private RenderTexture _distanceAsColorTexture;
        private Material _distanceToColorMat;
        private Vector3 _previousPosition;
        private Quaternion _previousRotation;
        private Vector3 _previousLossyScale;

#endregion //FIELDS

#region INHERITANCE_METHODS

#if UNITY_EDITOR
        
        /// <summary>
        /// On update, check for transform changes.
        /// </summary>
        void Update()
        {
            if(cameraSetup != null)
                CheckTransformChanged();
        }

#endif //UNITY_EDITOR

        /// <summary>
        /// Resets the object's properties.
        /// </summary>
        void Reset()
        {
            // Reset the key child components.
            _cameraSetup = CameraSetup.CreateOrResetCameraSetup(transform);
            _dataHandler = DataHandler.CreateOrResetDataHandler(transform);
            _previewCameraManager = PreviewCameraManager.CreateOrResetPreviewCameraManager(transform);
#if UNITY_EDITOR
            // Reset parameters to their default values.
            _cameraPrefab = null;
            _cameraCount = new Vector2Int(4, 4);
            _acquireDepthData = false;
            _copyGlobalMesh = false;
            _setupType = SetupType.Grid;
            _setupDirection = SetupDirection.Outwards;
            _lockSetup = false;
            // Update the acquisition camera poses to reflect the updated setup.
            ComputeAcquisitionCameraPoses();
            // Create a preview camera from the prefab.
            CreatePreviewCameraFromPrefab();
#endif //UNITY_EDITOR
        }

        /// <summary>
        /// On destroy, destroys the created objects.
        /// </summary>
        void OnDestroy()
        {
            if (!GeneralToolkit.IsStartingNewScene())
                GeneralToolkit.RemoveChildComponents(transform, typeof(CameraSetup), typeof(DataHandler), typeof(PreviewCameraManager));
        }

#endregion //INHERITANCE_METHODS

#if UNITY_EDITOR

#region METHODS

        /// <summary>
        /// Checks whether the transform has changed. If so, updates the camera models.
        /// </summary>
        private void CheckTransformChanged()
        {
            if(GeneralToolkit.HasTransformChanged(transform, ref _previousPosition, ref _previousRotation, ref _previousLossyScale))
            {
                ComputeAcquisitionCameraPoses();
                UpdatePreviewCameraModel();
            }
        }

        /// <summary>
        /// On selection, initialize certain properties.
        /// </summary>
        public void Selected()
        {
            // Notify the camera setup.
            cameraSetup.Selected();
            // Add a preview window listener.
            cameraSetup.onPreviewIndexChangeEvent.AddListener(OnPreviewIndexChange);
            // Inform the preview window that this object will send preview data to be displayed.
            PreviewWindow.AddCaller(cameraSetup, _colorCallerName);
            PreviewWindow.AddCaller(cameraSetup, _depthCallerName);
            // Create a preview camera.
            CreatePreviewCameraFromPrefab();
        }

        /// <summary>
        /// On deselection, reset certain properties.
        /// </summary>
        public void Deselected()
        {
            // Notify the camera setup.
            cameraSetup.Deselected();
            // Destroy any objects created for preview.
            DestroyPreviewObjects();
            // Remove the preview window listener.
            cameraSetup.onPreviewIndexChangeEvent.RemoveListener(OnPreviewIndexChange);
            // Inform the preview window that this object no longer sends preview data to be displayed.
            PreviewWindow.RemoveCaller(_colorCallerName);
            PreviewWindow.RemoveCaller(_depthCallerName);
        }

        /// <summary>
        /// On preview index change, updates the Preview and Scene windows.
        /// </summary>
        private void OnPreviewIndexChange()
        {
            // Render a new preview image.
            UpdatePreviewCameraModel();
            // Update the scene view.
            SceneView.RepaintAll();
        }

        /// <summary>
        /// Computes the acquisition camera poses, based on the specified setup type.
        /// </summary>
        public void ComputeAcquisitionCameraPoses()
        {
            if(!_lockSetup)
            {
                cameraSetup.ChangeCameraCount(_cameraCount.x * _cameraCount.y);
                if(_setupType == SetupType.Grid)
                    cameraSetup.ComputeGridPoses(transform, _cameraCount);
                else
                    cameraSetup.ComputeSpherePoses(transform, _cameraCount, _setupDirection);
            }
        }

        /// <summary>
        /// Creates a preview camera from the camera prefab.
        /// </summary>
        public void CreatePreviewCameraFromPrefab()
        {
            // If it is not already done, add a preview camera to the preview camera manager, initialize its target texture, and update its camera model.
            if(_previewCameraManager.previewCamera == null)
            {
                Transform previewCameraTransform;
                if(_cameraPrefab != null)
                {
                    previewCameraTransform = GameObject.Instantiate(_cameraPrefab).transform;
                }
                else
                {
                    previewCameraTransform = new GameObject("PreviewCamera").transform;
                    Camera previewCamera = previewCameraTransform.gameObject.AddComponent<Camera>();
                    previewCamera.stereoTargetEye = StereoTargetEyeMask.None;
                }
                previewCameraTransform.name = "Preview_" + previewCameraTransform.name;
                GeneralToolkit.CreateRenderTexture(ref _previewCameraManager.targetTexture, Vector2Int.one, 0, RenderTextureFormat.ARGB32, false, FilterMode.Point, TextureWrapMode.Clamp);
                CameraModel previewCameraModel = cameraSetup.cameraModels[cameraSetup.previewIndex];
                _previewCameraManager.CreatePreviewCamera(gameObject, previewCameraTransform, previewCameraModel);
                UpdatePreviewCameraModel();
            }
        }

        /// <summary>
        /// Updates the preview camera with the camera model, and displays the rendered view in the preview window.
        /// </summary>
        public void UpdatePreviewCameraModel()
        {
            // The preview camera manager, and its camera, need to have been initialized in a previous step.
            if(_previewCameraManager != null && _previewCameraManager.previewCamera != null)
            {
                // Update the preview camera's camera model, and render the preview image.
                CameraModel cameraParams = cameraSetup.cameraModels[cameraSetup.previewIndex];
                _previewCameraManager.UpdateCameraModel(cameraParams);
                _previewCameraManager.RenderPreviewToTarget(ref _previewCameraManager.targetTexture, false);
                int previewMaxIndex = cameraSetup.cameraModels.Length - 1;
                PreviewWindow.DisplayImage(_colorCallerName, _previewCameraManager.targetTexture, previewMaxIndex);
                // If depth data, or mesh data, is to be acquired, display a depth preview.
                if(_acquireDepthData || _copyGlobalMesh)
                {
                    // Render actual depth into a precise depth texture.
                    GeneralToolkit.CreateRenderTexture(ref _targetDepthTexture, cameraParams.pixelResolution, 24, RenderTextureFormat.RFloat, true, FilterMode.Point, TextureWrapMode.Clamp);
                    _previewCameraManager.RenderPreviewToTarget(ref _targetDepthTexture, true);
                    // Encode the depth texture into a color texture, using a colormap suited for visualization.
                    if(_distanceToColorMat == null)
                        _distanceToColorMat = new Material(GeneralToolkit.shaderAcquisitionConvert01ToColor);
                    _distanceToColorMat.SetInt(shaderNameIsPrecise, 0);
                    GeneralToolkit.CreateRenderTexture(ref _distanceAsColorTexture, cameraParams.pixelResolution, 0, RenderTextureFormat.ARGB32, true, FilterMode.Point, TextureWrapMode.Clamp);
                    Graphics.Blit(_targetDepthTexture, _distanceAsColorTexture, _distanceToColorMat);
                    // Display the texture in the preview window.
                    PreviewWindow.DisplayImage(_depthCallerName, _distanceAsColorTexture, previewMaxIndex);
                    // Reset the active render texture.
                    RenderTexture.active = null;
                }
            }
        }

        /// <summary>
        /// Destroys preview objects.
        /// </summary>
        public void DestroyPreviewObjects()
        {
            // Destroy the preview camera manager.
            if(_previewCameraManager != null)
                _previewCameraManager.DestroyPreviewCamera();
            // Destroy all created textures.
            if(_targetDepthTexture != null)
                DestroyImmediate(_targetDepthTexture);
            if(_distanceAsColorTexture != null)
                DestroyImmediate(_distanceAsColorTexture);
            if(_distanceToColorMat != null)
                DestroyImmediate(_distanceToColorMat);
        }

        /// <summary>
        /// Displays and updates a cancelable progress bar informing on the acquisition process.
        /// </summary>
        private void DisplayAndUpdateCancelableProgressBar()
        {
            int progressBarMaxIter = cameraSetup.cameraModels.Length;
            string progressBarTitle = "COLIBRI VR - Acquire Scene Data";
            string progressBarInfo = "Acquiring data from the synthetic Unity scene";
            string exitMessage = "Acquisition canceled by user.";
            GeneralToolkit.UpdateCancelableProgressBar(typeof(Acquisition), true, true, true, progressBarMaxIter, progressBarTitle, progressBarInfo, exitMessage);
        }

        /// <summary>
        /// Saves the acquisition information in specific files.
        /// The data is stored based on the COLMAP file system. 
        /// </summary>
        public void SaveAcquisitionInformation()
        {
            COLMAPConnector.CreateDirectoryStructureForAcquisition(dataHandler.dataDirectory);
            COLMAPConnector.SaveCamerasInformation(cameraSetup.cameraModels, dataHandler.dataDirectory);
            COLMAPConnector.SaveImagesInformation(cameraSetup.cameraModels, dataHandler.dataDirectory);
            dataHandler.SaveCOLIBRIVRAdditionalInformation(cameraSetup);
        }

        /// <summary>
        /// Saves the meshes in the scene as a global mesh asset in the data directory.
        /// </summary>
        public void SaveGlobalMesh()
        {
            // Combines the different meshes in the scene into a single asset.
            Mesh outputMesh = new Mesh();
            MeshFilter[] meshFilters = FindObjectsOfType<MeshFilter>();
            CombineInstance[] combine = new CombineInstance[meshFilters.Length];
            for(int i = 0; i < meshFilters.Length; i++)
            {
                combine[i].mesh = meshFilters[i].sharedMesh;
                combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
            }
            outputMesh.CombineMeshes(combine, false);
            // Saves the asset in the data directory.
            string assetName = "GlobalMesh.asset";
            string relativeAssetPath = Path.Combine("Assets", assetName);
            string globalAssetPath = Path.Combine(Path.GetFullPath(Application.dataPath), assetName);
            string newGlobalAssetPath = Path.Combine(dataHandler.dataDirectory, assetName);
            GeneralToolkit.CreateAndUnloadAsset(outputMesh, relativeAssetPath);
            GeneralToolkit.Replace(PathType.File, globalAssetPath, newGlobalAssetPath);
            GeneralToolkit.Delete(globalAssetPath);
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Starts the coroutine that captures the scene.
        /// </summary>
        public void CaptureScene()
        {
            StartCoroutine(CaptureSceneCoroutine());
        }

        /// <summary>
        /// Moves the acquisition camera into the desired poses, and acquires the corresponding color and depth data.
        /// </summary>
        /// <returns></returns>
        private IEnumerator CaptureSceneCoroutine()
        {
            // Inform the user that the process has started.
            GeneralToolkit.ResetCancelableProgressBar(true, true);
            Debug.Log(GeneralToolkit.FormatScriptMessage(typeof(Acquisition), "Started acquiring scene data."));
            // Find the camera models if needed.
            if(cameraSetup.cameraModels == null)
                cameraSetup.FindCameraModels();
            if(cameraSetup.cameraModels == null)
                yield break;
            // Store the initial preview index.
            int initialPreviewIndex = cameraSetup.previewIndex;
            // Store the pose data and camera parameters into a file.
            SaveAcquisitionInformation();
            // If desired, save the scene's meshes as a global asset.
            if(_copyGlobalMesh)
                SaveGlobalMesh();
            // Acquire data for each source pose.
            for(int i = 0; i < cameraSetup.cameraModels.Length; i++)
            {
                // Display and update the progress bar.
                DisplayAndUpdateCancelableProgressBar();
                if(GeneralToolkit.progressBarCanceled)
                    break;
                // Change the preview index to the current camera.
                cameraSetup.previewIndex = i;
                string imageName = cameraSetup.cameraModels[i].imageName;
                // Update the camera model for the preview camera, thereby rendering to the target color and depth textures.
                UpdatePreviewCameraModel();
                // Save the color texture as a file.
                GeneralToolkit.SaveRenderTextureToPNG(_previewCameraManager.targetTexture, Path.Combine(dataHandler.colorDirectory, imageName));
                // If depth data is to be acquired, save the scene's depth (encoded as a 3-channel RGB texture) as a file.
                if(_acquireDepthData)
                {
                    _distanceToColorMat.SetInt(shaderNameIsPrecise, 1);
                    Graphics.Blit(_targetDepthTexture, _distanceAsColorTexture, _distanceToColorMat);
                    GeneralToolkit.SaveRenderTextureToPNG(_distanceAsColorTexture, Path.Combine(dataHandler.depthDirectory, imageName));
                }
                yield return null;
            }
            // If the process completes without being canceled, inform the user.
            if(!GeneralToolkit.progressBarCanceled)
            {
                Debug.Log(GeneralToolkit.FormatScriptMessage(typeof(Acquisition), "Successfully acquired scene data."));
                Debug.Log(GeneralToolkit.FormatScriptMessage(typeof(Acquisition), "Data can be found in directory: " + dataHandler.dataDirectory + "."));
            }
            // Reset displayed information.
            GeneralToolkit.ResetCancelableProgressBar(false, false);
            // Reset the preview camera's pose.
            cameraSetup.previewIndex = initialPreviewIndex;
            UpdatePreviewCameraModel();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

#endregion //METHODS

#endif //UNITY_EDITOR

    }

}

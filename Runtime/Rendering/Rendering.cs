/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using COLIBRIVR.Evaluation;

namespace COLIBRIVR.Rendering
{

    /// <summary>
    /// Class that handles the rendering of photographic data in Unity.
    /// </summary>
    [RequireComponent(typeof(Processing.Processing))]
    public class Rendering : MonoBehaviour
    {

#region CONST_FIELDS

        public const string propertyNameLaunchOrderIndex = "launchOrderIndex";
        public const string propertyNameLaunchOnAwake = "_launchOnAwake";
        public const string propertyNameRenderingMethodIndex = "_renderingMethodIndex";
        public const string propertyNameEvaluationMethodIndex = "_evaluationMethodIndex";

        private const string _renderCallerName = "Rendered image";
        private const string _evalCallerName = "Evaluation";

#endregion //CONST_FIELDS$

#region STATIC_PROPERTIES

        public static int renderingObjectCount { get { return _renderingObjectList.Count; } }

#endregion //STATIC_PROPERTIES

#region STATIC_FIELDS

        private static List<Rendering> _renderingObjectList;

#endregion //STATIC_FIELDS

#region STATIC_METHODS

        /// <summary>
        /// Changes the index of a rendering object in the list.
        /// </summary>
        /// <param name="renderingObj"></param> The rendering object.
        /// <param name="newIndex"></param> The new index for the object.
        public static void ChangeIndexInQueue(Rendering renderingObj, int newIndex)
        {
            if(_renderingObjectList.Contains(renderingObj))
                _renderingObjectList.Remove(renderingObj);
            if(newIndex < _renderingObjectList.Count)
                _renderingObjectList.Insert(newIndex, renderingObj);
            else
                _renderingObjectList.Add(renderingObj);
            for(int iter = newIndex; iter < renderingObjectCount; iter++)
                _renderingObjectList[iter].launchOrderIndex = iter;
        }

        /// <summary>
        /// Updates the list of rendering objects.
        /// </summary>
        private static void UpdateRenderingObjectsList()
        {
            if(_renderingObjectList == null)
                _renderingObjectList = new List<Rendering>();
            Rendering[] renderingObjArray = FindObjectsOfType<Rendering>();
            for(int iter = 0; iter < renderingObjArray.Length; iter++)
            {
                ChangeIndexInQueue(renderingObjArray[iter], renderingObjArray[iter].launchOrderIndex);
            }
        }

        /// <summary>
        /// Launches the rendering objects in the list that are ready to be launched.
        /// </summary>
        private static void LaunchRenderingObjects()
        {
            for(int iter = 0; iter < renderingObjectCount; iter++)
            {
                Rendering renderingObj = _renderingObjectList[iter];
                if(renderingObj.readyToBeLaunched)
                {
                    renderingObj.readyToBeLaunched = false;
                    renderingObj.LaunchRendering();
                }
            }
        }

#endregion //STATIC_METHODS

#region PROPERTIES

        public Processing.Processing processing { get { return _processing; } }
        public RenderingMethod[] renderingMethods { get { return _renderingMethods; } }
        public EvaluationMethod[] evaluationMethods { get { return _evaluationMethods; } }

#endregion //PROPERTIES

#region FIELDS

        public RenderingMethod selectedBlendingMethod;
        public EvaluationMethod selectedEvaluationMethod;
        public Camera mainCamera;
        public int launchOrderIndex;
        public bool readyToBeLaunched;

        [SerializeField] private bool _launchOnAwake;
        [SerializeField] private Processing.Processing _processing;
        [SerializeField] private int _renderingMethodIndex;
        [SerializeField] private int _evaluationMethodIndex;
        [SerializeField] private RenderingMethod[] _renderingMethods;
        [SerializeField] private EvaluationMethod[] _evaluationMethods;

        private RenderTexture _previewRenderTexture;
        private RenderTexture _previewEvalTexture;
        private bool _launched;

#endregion //FIELDS

#region EVENTS

        public event IndicateLoadingFinished OnLoadingFinished;

        /// <summary>
        /// Indicates that the scene has finished loading.
        /// </summary>
        public delegate void IndicateLoadingFinished();

#endregion //EVENTS

#region INHERITANCE_METHODS

        /// <summary>
        /// Resets the object's properties.
        /// </summary>
        void Reset()
        {
            // Set the default values.
            UpdateRenderingObjectsList();
            launchOrderIndex = renderingObjectCount;
            readyToBeLaunched = false;
            _launchOnAwake = true;
            _launched = false;
            _previewRenderTexture = null;
            _previewEvalTexture = null;
            // Get the processing component. If this was not called on AddComponent, reset it as well.
            _processing = GeneralToolkit.GetOrAddComponent<Processing.Processing>(gameObject);
            if(_processing.renderingCaller != null)
                _processing.Reset();
            else
                _processing.renderingCaller = this;
            // Initialize the rendering methods and helpers. This has to be done after the processing component has been reset.
            _renderingMethods = RenderingMethod.CreateOrResetRenderingMethods(transform);
            Method[] methods = GetComponentsInChildren<Method>();
            for(int iter = 0; iter < methods.Length; iter++)
                methods[iter].InitializeLinks();
            // Initialize the evaluation methods.
            _evaluationMethods = EvaluationMethod.GetOrCreateEvaluationMethods(transform);
#if UNITY_EDITOR
            UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(processing, false);
#endif //UNITY_EDITOR
        }

        /// <summary>
        /// Notifies the rendering manager that this object should be launched.
        /// </summary>
        void Awake()
        {
            if(_renderingObjectList == null)
                UpdateRenderingObjectsList();
            if(_launchOnAwake)
                readyToBeLaunched = true;
        }

        /// <summary>
        /// Clears all objects created for rendering.
        /// </summary>
        void OnDestroy()
        {
#if UNITY_EDITOR
            // Deselect the object.
            Deselected();
            // Clear the evaluation method.
            selectedEvaluationMethod.ClearEvaluationMethod();
#endif //UNITY_EDITOR
            // Clear the blending method.
            selectedBlendingMethod.ClearRenderingMethod();
            // Destroy created textures.
            if(_previewRenderTexture != null)
                DestroyImmediate(_previewRenderTexture);
            if(_previewEvalTexture != null)
                DestroyImmediate(_previewEvalTexture);
            // Unload loaded asset bundles.
            GeneralToolkit.UnloadAssetBundleInMemory(true);
        }

        /// <summary>
        /// Executes the rendering method every frame.
        /// </summary>
        void Update()
		{
            if(readyToBeLaunched)
            {
                LaunchRenderingObjects();
            }
            if(_launched)
            {
                // Update the blending method.
                selectedBlendingMethod.UpdateRenderingMethod();
#if UNITY_EDITOR
                // Update the evaluation method.
                selectedEvaluationMethod.UpdateEvaluationMethod();
#endif //UNITY_EDITOR
            }
		}

#endregion //INHERITANCE_METHODS

#region METHODS

#if UNITY_EDITOR

        /// <summary>
        /// On preview index change in play mode, load the preview images and update the Scene window.
        /// </summary>
        private void OnPreviewIndexChange()
        {
            if(Application.isPlaying)
            {
                processing.LoadSourceImageAsPreview();
                LoadRenderedViewAsPreview();
                SceneView.RepaintAll();
            }
        }

        /// <summary>
        /// On selection, adds listeners and callers for preview.
        /// </summary>
        public void Selected()
        {
            // Update the rendering queue.
            UpdateRenderingObjectsList();
            // Add a preview window listener.
            processing.cameraSetup.onPreviewIndexChangeEvent.AddListener(OnPreviewIndexChange);
            // Inform the preview window that this object will send preview data to be displayed.
            PreviewWindow.AddCaller(processing.cameraSetup, _renderCallerName);
            PreviewWindow.AddCaller(processing.cameraSetup, _evalCallerName);
            // Load the preview images.
            LoadRenderedViewAsPreview();
        }

        /// <summary>
        /// On deselection, informs the preview window that this object no longer sends preview data to be displayed.
        /// </summary>
        public void Deselected()
        {
            // Remove the preview window listener.
            processing.cameraSetup.onPreviewIndexChangeEvent.RemoveListener(OnPreviewIndexChange);
            // Inform the preview window that this object no longer sends preview data to be displayed.
            PreviewWindow.RemoveCaller(_renderCallerName);
            PreviewWindow.RemoveCaller(_evalCallerName);
        }

        /// <summary>
        /// In play mode, loads the rendered view and the evaluation metric for preview.
        /// </summary>
        public void LoadRenderedViewAsPreview()
        {
            RenderTexture.active = null;
            if(_previewEvalTexture != null)
                DestroyImmediate(_previewEvalTexture);
            if(_previewRenderTexture != null)
                DestroyImmediate(_previewRenderTexture);
            // Check that there are camera models, that the application is playing, and that the object is active.
            if(processing.cameraSetup.cameraModels == null || !Application.isPlaying || !gameObject.activeInHierarchy || !_launched)
                return;
            int previewMaxIndex = processing.cameraSetup.cameraModels.Length - 1;
            // Get the camera model for this index.
            CameraModel tempCameraModel = CameraModel.CreateCameraModel();
            tempCameraModel.ParametersFromCameraModel(processing.cameraSetup.cameraModels[processing.cameraSetup.previewIndex]);
            tempCameraModel.pixelResolution = PreviewWindow.GetPreviewResolution(tempCameraModel.pixelResolution);
            // Display a preview of the rendered view.
            if(selectedBlendingMethod != null && selectedEvaluationMethod != null)
            {
                // Inform the blending method to exclude the source view if desired.
                if(selectedEvaluationMethod.excludeSourceView)
                    selectedBlendingMethod.ExcludeSourceView(processing.cameraSetup.previewIndex);
                // Create a preview camera manager and initialize it with the camera model's pose and parameters.
                PreviewCameraManager previewCameraManager = new GameObject("Preview Camera Manager").AddComponent<PreviewCameraManager>();
                Transform previewCameraTransform = new GameObject("Preview Camera").transform;
                GeneralToolkit.CreateRenderTexture(ref previewCameraManager.targetTexture, Vector2Int.one, 0, RenderTextureFormat.ARGB32, false, FilterMode.Point, TextureWrapMode.Clamp);
                previewCameraManager.CreatePreviewCamera(previewCameraManager.gameObject, previewCameraTransform, tempCameraModel);
                previewCameraManager.previewCamera.clearFlags = CameraClearFlags.Color;
                previewCameraManager.previewCamera.backgroundColor = Color.black;
                // Render the preview camera to a target texture, and display it in the preview window.
                previewCameraManager.RenderPreviewToTarget(ref previewCameraManager.targetTexture, false);
                GeneralToolkit.CreateRenderTexture(ref _previewRenderTexture, tempCameraModel.pixelResolution, 0, RenderTextureFormat.ARGB32, false, FilterMode.Point, TextureWrapMode.Clamp);
                Graphics.Blit(previewCameraManager.targetTexture, _previewRenderTexture);
                PreviewWindow.DisplayImage(_renderCallerName, _previewRenderTexture, previewMaxIndex);
                // Destroy the preview camera manager.
                previewCameraManager.DestroyPreviewCamera();
                DestroyImmediate(previewCameraManager.gameObject);
                // Inform the blending method that it should no longer exclude the source view.
                selectedBlendingMethod.ExcludeSourceView(-1);
            }
            DestroyImmediate(tempCameraModel.gameObject);
            // Display the evaluation metric as an RGB color texture.
            if(selectedEvaluationMethod != null && selectedEvaluationMethod.evaluationMaterial != null)
            {
                // Use a shader to compute the evaluation metric for each pixel and display it as a color value.
                GeneralToolkit.CreateRenderTexture(ref _previewEvalTexture, tempCameraModel.pixelResolution, 0, RenderTextureFormat.ARGB32, true, FilterMode.Point, TextureWrapMode.Clamp);
                selectedEvaluationMethod.evaluationMaterial.SetTexture(EvaluationMethod.shaderNameTextureOne, processing.previewSourceTexture);
                selectedEvaluationMethod.evaluationMaterial.SetTexture(EvaluationMethod.shaderNameTextureTwo, _previewRenderTexture);
                Graphics.Blit(null, _previewEvalTexture, selectedEvaluationMethod.evaluationMaterial);
                // Display the created texture in the preview window.
                PreviewWindow.DisplayImage(_evalCallerName, _previewEvalTexture, previewMaxIndex);
            }
        }

#endif //UNITY_EDITOR

        /// <summary>
        /// Launches the rendering initialization process.
        /// </summary>
        public void LaunchRendering()
        {
            StartCoroutine(LaunchRenderingCoroutine());
        }

        /// <summary>
        /// Coroutine that launches the rendering initialization process.
        /// </summary>
        /// <returns></returns>
        private IEnumerator LaunchRenderingCoroutine()
        {
#if UNITY_EDITOR
            // Check if the object is selected.
            Deselected();
            processing.Deselected();
            if(gameObject == Selection.activeGameObject)
                processing.Selected();
#endif //UNITY_EDITOR
            // Check the camera models.
            if(processing.cameraSetup.cameraModels == null)
                processing.cameraSetup.FindCameraModels();
            // Initialize the main camera.
            InitializeCamera();
            // Initialize the blending method.
            yield return StartCoroutine(selectedBlendingMethod.InitializeRenderingMethodCoroutine());
#if UNITY_EDITOR
            // Initialize the evaluation method.
            selectedEvaluationMethod.InitializeEvaluationMethod();
            // Load the preview images.
            yield return StartCoroutine(processing.PrepareLoadingSourceImagesAsPreviewCoroutine());
            if(gameObject == Selection.activeGameObject)
                LoadRenderedViewAsPreview();
#endif //UNITY_EDITOR
            // Unload the asset bundle.
            yield return null;
            GeneralToolkit.UnloadAssetBundleInMemory(false);
            // Indicate that loading has finished.
            _launched = true;
            Debug.Log(GeneralToolkit.FormatScriptMessage(this.GetType(), "Finished initialization for object " + gameObject.name + "."));
            // Send an event on loading finished.
            if (OnLoadingFinished != null)
                OnLoadingFinished();
        }

        /// <summary>
        /// Initializes the camera object.
        /// </summary>
        private void InitializeCamera()
        {
            mainCamera = Camera.main;
            // If there is no main camera, initialize it.
            if(mainCamera == null)
            {
                GameObject mainCameraGO = new GameObject("Main Camera");
                mainCameraGO.tag = "MainCamera";
                mainCamera = mainCameraGO.AddComponent<Camera>();
                // Set the camera's clear flags and background color.
                if(RenderSettings.skybox == null)
                {
                    mainCamera.clearFlags = CameraClearFlags.SolidColor;
                    mainCamera.backgroundColor = Color.clear;
                }
            }
        }

#endregion //METHODS

    }

}


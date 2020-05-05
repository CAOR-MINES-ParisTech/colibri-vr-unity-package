/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace COLIBRIVR.Processing
{

    /// <summary>
    /// Class that implements the Global Texture Map color processing method.
    /// This method generates texture maps for the global mesh's submeshes using the color data.
    /// </summary>
    [ExecuteInEditMode]
    public class GlobalTextureMap : ProcessingMethod
    {

#region CONST_FIELDS

        public const string textureMapAssetPrefix = "GlobalTextureMap";
        public const string propertyNameTextureMapResolutionMinMax = "_textureMapResolutionMinMax";
        public const string shaderNameGlobalTextureMap = "_GlobalTextureMap";

#endregion //CONST_FIELDS

#region FIELDS

        public Texture2D[] textureMaps;

        [SerializeField] private Vector2Int _textureMapResolutionMinMax;

        private List<GameObject> _deactivatedRendererGOs;
        private PreviewCameraManager _previewCameraManager;
        private CameraModel _previewCameraModel;
        private Material _renderToTextureMapMat;
        private Material _normalizeByAlphaMat;
        private Vector2Int _textureMapResolution;
        private int _submeshIndex;
        private Rendering.Helper_ULR _helperULR;

#endregion //FIELDS

#if UNITY_EDITOR

#region INHERITANCE_METHODS

        /// <inheritdoc/>
        public override void Reset()
        {
            base.Reset();
            _textureMapResolutionMinMax = new Vector2Int(1024, 2048);
        }

        /// <inheritdoc/>
        public override bool IsCompatible(int colorDataCount, int depthDataCount, int meshDataCount)
        {
            return true;
        }

        /// <inheritdoc/>
        public override bool IsGUINested()
        {
            return true;
        }

        /// <inheritdoc/>
        public override bool IsNestedGUIEnabled()
        {
            // Indicate that the GUI for this nested method should be enabled only if the color and depth data is processed previously as texture arrays.
            return PMColorTextureArray.shouldExecute && PMDepthTextureArray.shouldExecute;;
        }

        /// <inheritdoc/>
        public override GUIContent GetGUIInfo()
        {
            string label = "Texture maps from existing global mesh and color and depth texture arrays";
            string tooltip = "Color data samples will be merged into a texture map for each of the global mesh's submeshes.";
            if(!GUI.enabled)
                tooltip = "Prerequisites are not satisfied.\nPrerequisites: global mesh, color texture array, depth texture array.";
            return new GUIContent(label, tooltip);
        }

        /// <inheritdoc/>
        public override GUIContent GetProcessedDataName()
        {
            string label = "Color texture map";
            string tooltip = GetGUIInfo().text;
            return new GUIContent(label, tooltip);
        }

        /// <inheritdoc/>
        protected override IEnumerator ExecuteMethodCoroutine()
        {
            yield return StartCoroutine(StoreGlobalTextureMapCoroutine());
        }

        /// <inheritdoc/>
        public override void DeactivateIncompatibleProcessingMethods()
        {
            
        }
        
        /// <inheritdoc/>
        public override bool HasAdditionalParameters()
        {
            return true;
        }

        /// <inheritdoc/>
        public override void SectionAdditionalParameters()
        {
            SerializedObject serializedObject = new SerializedObject(this);
            serializedObject.Update();
            SerializedProperty propertyTextureMapResolutionMinMax = serializedObject.FindProperty(propertyNameTextureMapResolutionMinMax);
            Vector2Int textureMapResolutionMinMaxVal = propertyTextureMapResolutionMinMax.vector2IntValue;
            float minVal = textureMapResolutionMinMaxVal.x;
            float maxVal = textureMapResolutionMinMaxVal.y;
            EditorGUILayout.LabelField(new GUIContent("Resolution min:" + minVal.ToString(), "Minimum bound for the texture map resolution."));
            EditorGUILayout.LabelField(new GUIContent("Resolution max:" + maxVal.ToString(), "Maximum bound for the texture map resolution."));
            EditorGUILayout.MinMaxSlider(ref minVal, ref maxVal, 4, 4096);
            propertyTextureMapResolutionMinMax.vector2IntValue = new Vector2Int(Mathf.ClosestPowerOfTwo(Mathf.RoundToInt(minVal)), Mathf.ClosestPowerOfTwo(Mathf.RoundToInt(maxVal)));
            serializedObject.ApplyModifiedProperties();
        }

#endregion //INHERITANCE_METHODS

#endif //UNITY_EDITOR

#region METHODS

#if UNITY_EDITOR

        /// <summary>
        /// Coroutine that merges the color data into a single texture map per submesh, and stores this information as a texture asset.
        /// </summary>
        /// <returns></returns>
        private IEnumerator StoreGlobalTextureMapCoroutine()
        {
            // Reset the progress bar.
            GeneralToolkit.ResetCancelableProgressBar(true, false);
            // Per call, fetch the assets and initialize a camera and materials.
            InitializePerCall();
            yield return null;
            // Create and save a texture map for each submesh of the global mesh.
            int textureMapCount = PMGlobalMeshEF.globalMesh.subMeshCount;
            textureMaps = new Texture2D[textureMapCount];
            for(_submeshIndex = 0; _submeshIndex < textureMapCount; _submeshIndex++)
            {
                // Check if the progress bar has been canceled.
                if(GeneralToolkit.progressBarCanceled)
                {
                    processingCaller.processingCanceled = true;
                    break;
                }
                // Update the message on the progress bar.
                DisplayAndUpdateCancelableProgressBar();
                // Per submesh, determine the texture map resolution, compute the texture map, and save it as an asset.
                ComputeTextureMapResolution();
                ComputeAndStoreTextureMap();
                yield return null;
            }
            // Destroy all objects created per call.
            ClearPerCall();
            // Reset the progress bar.
            GeneralToolkit.ResetCancelableProgressBar(true, false);
        }

        /// <summary>
        /// Displays and updates a cancelable progress bar informing on the texture map generation process.
        /// </summary>
        private void DisplayAndUpdateCancelableProgressBar()
        {
            int subMeshCount = PMGlobalMeshEF.globalMesh.subMeshCount;
            int progressBarMaxIter = subMeshCount + 1;
            string progressBarTitle = "COLIBRI VR - " + GetProcessedDataName().text;
            string exitMessage = "Processing canceled by user.";
            string progressBarInfo = "Creating texture map for submesh " + (_submeshIndex + 1) + "/" + subMeshCount + ".";
            GeneralToolkit.UpdateCancelableProgressBar(typeof(Processing), true, false, false, progressBarMaxIter, progressBarTitle, progressBarInfo, exitMessage);
        }

        /// <summary>
        /// Fetches the global mesh and initializes the camera and materials.
        /// </summary>
        private void InitializePerCall()
        {
            // Deactivate any other renderer in the scene.
            _deactivatedRendererGOs = GeneralToolkit.DeactivateOtherActiveComponents<Renderer>(gameObject);
            // Create a preview camera manager and initialize it with the camera model's pose and parameters.
            _previewCameraModel = CameraModel.CreateCameraModel();
            _previewCameraModel.transform.position = Vector3.zero;
            _previewCameraModel.transform.rotation = Quaternion.identity;
            _previewCameraModel.fieldOfView = 60f * Vector2.one;
            float focalLength = Camera.FieldOfViewToFocalLength(_previewCameraModel.fieldOfView.x, 1f);
            _previewCameraManager = new GameObject("Preview Camera Manager").AddComponent<PreviewCameraManager>();
            Transform previewCameraTransform = new GameObject("Preview Camera").transform;
            GeneralToolkit.CreateRenderTexture(ref _previewCameraManager.targetTexture, Vector2Int.one, 0, RenderTextureFormat.ARGB32, false, FilterMode.Point, TextureWrapMode.Clamp);
            _previewCameraManager.CreatePreviewCamera(_previewCameraManager.gameObject, previewCameraTransform, _previewCameraModel);
            _previewCameraManager.previewCamera.clearFlags = CameraClearFlags.Color;
            _previewCameraManager.previewCamera.backgroundColor = Color.clear;
            // Create the materials.
            _renderToTextureMapMat = new Material(GeneralToolkit.shaderProcessingGlobalTextureMap);
            _renderToTextureMapMat.SetFloat(_shaderNameFocalLength, focalLength);
            _normalizeByAlphaMat = new Material(GeneralToolkit.shaderNormalizeByAlpha);
            // Initialize the helper object for ULR.
            _helperULR = gameObject.AddComponent<Rendering.Helper_ULR>();
            _helperULR.Reset();
            _helperULR.InitializeLinks();
            _helperULR.blendCamCount = Rendering.Helper_ULR.maxBlendCamCount;
            _helperULR.numberOfSourceCameras = PMColorTextureArray.colorData.depth;
            _helperULR.CreateULRBuffersAndArrays();
            _helperULR.InitializeBlendingMaterialParameters(ref _renderToTextureMapMat);
            _helperULR.currentBlendingMaterial = _renderToTextureMapMat;
            _helperULR.initialized = true;
        }

        /// <summary>
        /// Destroys all objects created per call.
        /// </summary>
        private void ClearPerCall()
        {
            // Reactivate deactivated renderers.
            GeneralToolkit.ReactivateOtherActiveComponents(_deactivatedRendererGOs);
            // Destroy created objects.
            if(_previewCameraModel != null)
                DestroyImmediate(_previewCameraModel.gameObject);
            if(_previewCameraManager != null)
            {
                _previewCameraManager.DestroyPreviewCamera();   
                DestroyImmediate(_previewCameraManager.gameObject);
            }
            if(_renderToTextureMapMat != null)
                DestroyImmediate(_renderToTextureMapMat);
            if(_normalizeByAlphaMat != null)
                DestroyImmediate(_normalizeByAlphaMat);
            if(_helperULR != null)
            {
                _helperULR.ClearAll();
                DestroyImmediate(_helperULR);
            }
        }

        /// <summary>
        /// Computes the texture map resolution for the current submesh.
        /// </summary>
        private void ComputeTextureMapResolution()
        {
            int textureMapResolutionInt = _textureMapResolutionMinMax.x;
            if(textureMapResolutionInt != _textureMapResolutionMinMax.y)
            {
                // Compute the submesh's bounds (quick approximation).
                float boundsRelativeMagnitude = 1f;
                Mesh globalMesh = PMGlobalMeshEF.globalMesh;
                if(globalMesh.subMeshCount > 1)
                {
                    Bounds bounds = new Bounds();
                    int maxTriangleCount = 100;
                    int[] submeshTriangles = globalMesh.GetTriangles(_submeshIndex);
                    int submeshTriangleCount = submeshTriangles.Length / 3;
                    int skipFactor = Mathf.CeilToInt(submeshTriangleCount * 1f / maxTriangleCount);
                    for(int triangleIndex = 0; triangleIndex < submeshTriangleCount; triangleIndex+=3*skipFactor)
                    {
                        bounds.Encapsulate(globalMesh.vertices[submeshTriangles[3 * triangleIndex]]);
                        bounds.Encapsulate(globalMesh.vertices[submeshTriangles[3 * triangleIndex + 1]]);
                        bounds.Encapsulate(globalMesh.vertices[submeshTriangles[3 * triangleIndex + 2]]);
                    }
                    boundsRelativeMagnitude = bounds.size.magnitude / globalMesh.bounds.size.magnitude;
                }
                // Determine the texture map's resolution.
                _textureMapResolution = ColorTextureArray.GetCeilPowerOfTwoForImages(cameraSetup.cameraModels) * 4;
                float textureMapResolutionFloat = Mathf.Max(_textureMapResolution.x, _textureMapResolution.y);
                textureMapResolutionFloat *=  Mathf.Sqrt(boundsRelativeMagnitude);
                textureMapResolutionInt = Mathf.NextPowerOfTwo(Mathf.RoundToInt(textureMapResolutionFloat));
                // Submit to the min and max bounds.
                textureMapResolutionInt = Mathf.Max(Mathf.Min(textureMapResolutionInt, _textureMapResolutionMinMax.y), _textureMapResolutionMinMax.x);
            }
            _textureMapResolution = new Vector2Int(textureMapResolutionInt, textureMapResolutionInt);
        }

        /// <summary>
        /// Draws the current submesh onto the preview camera with the appropriate material.
        /// </summary>
        /// <param name="camera"></param> The render camera.
        private void DrawSubmeshAsTextureMapWithCamera(Camera camera)
        {
            if (camera == _previewCameraManager.previewCamera && camera != null && PMGlobalMeshEF.globalMesh != null && _renderToTextureMapMat != null)
                Graphics.DrawMesh(PMGlobalMeshEF.globalMesh, Vector3.zero, Quaternion.identity, _renderToTextureMapMat, gameObject.layer, camera, _submeshIndex);
        }

        /// <summary>
        /// Computes and stores a texture map for the current submesh.
        /// </summary>
        private void ComputeAndStoreTextureMap()
        {
            // Check if the asset has already been processed.
            string assetName = textureMapAssetPrefix + GeneralToolkit.ToString(_submeshIndex);
            string bundledAssetName = dataHandler.GetBundledAssetName(this, assetName);
            string textureMapRelativePath = Path.Combine(GeneralToolkit.tempDirectoryRelativePath, bundledAssetName + ".asset");
            if(dataHandler.IsAssetAlreadyProcessed(textureMapRelativePath))
                return;
            // Render to the preview camera a first time to initialize all buffers correctly.
            _previewCameraModel.pixelResolution = _textureMapResolution;
            _previewCameraManager.UpdateCameraModel(_previewCameraModel, true);
            _previewCameraManager.RenderPreviewToTarget(ref _previewCameraManager.targetTexture, false);
            // Render the mesh to the preview camera's target texture.
            Camera.onPreCull += DrawSubmeshAsTextureMapWithCamera;
            _previewCameraManager.RenderPreviewToTarget(ref _previewCameraManager.targetTexture, false);
            Camera.onPreCull -= DrawSubmeshAsTextureMapWithCamera;
            // Normalize the RGB channels by the alpha channel.
            RenderTexture tempTex = new RenderTexture(1, 1, 0);
            GeneralToolkit.CreateRenderTexture(ref tempTex, _textureMapResolution, 0, RenderTextureFormat.ARGB32, false, FilterMode.Point, TextureWrapMode.Clamp);
            Graphics.Blit(_previewCameraManager.targetTexture, tempTex, _normalizeByAlphaMat);
            // Apply a morphological dilation to better handle seams in the texture map..
            GeneralToolkit.RenderTextureApplyMorphologicalDilation(ref tempTex, _textureMapResolution.x / 200, ImageProcessingKernelType.Box, false);
            // Copy the render texture to an output texture.
            Texture2D outTex = new Texture2D(1, 1);
            GeneralToolkit.CreateTexture2D(ref outTex, _textureMapResolution, TextureFormat.RGB24, false, FilterMode.Bilinear, TextureWrapMode.Clamp, true);
            GeneralToolkit.CopyRenderTextureToTexture2D(tempTex, ref outTex);
            outTex.filterMode = FilterMode.Bilinear;
            outTex.anisoLevel = 3;
            // Destroy created objects.
            DestroyImmediate(tempTex);
            // Save a copy as a png file for visualization.
            string copyName = DataHandler.GetBundledAssetPrefixFromType(this.GetType()) + assetName + ".png";
            GeneralToolkit.SaveTexture2DToPNG(outTex, Path.Combine(GeneralToolkit.tempDirectoryAbsolutePath, copyName));
            // Create an asset from the created texture map.
            AssetDatabase.CreateAsset(outTex, textureMapRelativePath);
            AssetDatabase.Refresh();
            // Set the created texture in the final array.
            Texture2D texAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(textureMapRelativePath);
            textureMaps[_submeshIndex] = (Texture2D)Instantiate(texAsset);
        }

#endif //UNITY_EDITOR

        /// <summary>
        /// Loads the processed texture map for play.
        /// </summary>
        /// <returns></returns>
        public IEnumerator LoadProcessedTextureMapsCoroutine()
        {
            // Check that the application is playing.
            if(!Application.isPlaying)
                yield break;
            // Count the number of texture maps to load by checking each bundled asset name for the prefix corresponding to this class.
            string bundledAssetsPrefix = DataHandler.GetBundledAssetPrefixFromType(typeof(GlobalTextureMap));
            int textureMapCount = 0;
            foreach(string bundledAssetName in dataHandler.bundledAssetsNames)
            {
                if(bundledAssetName.Contains(bundledAssetsPrefix))
                    textureMapCount++;
            }
            // If there are texture maps to load, load them.
            if(textureMapCount > 0)
            {
                textureMaps = new Texture2D[textureMapCount];
                foreach(string bundledAssetName in dataHandler.bundledAssetsNames)
                {
                    if(bundledAssetName.Contains(bundledAssetsPrefix))
                    {
                        // Determine the texture map index.
                        string assetName = bundledAssetName.Replace(bundledAssetsPrefix, string.Empty);
                        string textureMapIndexString = assetName.Replace(textureMapAssetPrefix, string.Empty);
                        int textureMapIndex = GeneralToolkit.ParseInt(textureMapIndexString);
                        // Load the texture map.
                        yield return dataHandler.StartCoroutine(dataHandler.LoadAssetsFromBundleCoroutine<Texture2D>((result => textureMaps[textureMapIndex] = result[0]), bundledAssetName));
                    }
                }
            }
        }

#endregion //METHODS

    }

}

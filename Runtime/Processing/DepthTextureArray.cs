/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

using System.IO;
using System.Collections;
using UnityEngine;
using UnityEditor;

namespace COLIBRIVR.Processing
{

    /// <summary>
    /// Class that implements the Depth Texture Array geometry processing method.
    /// This method converts depth samples acquired from a global mesh into a texture array asset, that will appear as a single object on the GPU.
    /// </summary>
    public class DepthTextureArray : ProcessingMethod
    {

#region CONST_FIELDS

        public const string depthMapsAssetName = "DepthMapsTextureArray";
        public const string shaderNameDepthData = "_DepthData";

#endregion //CONST_FIELDS

#region FIELDS

        public Texture2DArray depthData;

#endregion //FIELDS

#if UNITY_EDITOR

#region INHERITANCE_METHODS

        /// <inheritdoc/>
        public override void InitializeLinks()
        {
            base.InitializeLinks();
            // Enable the user to choose whether or not to generate per-view meshes from the created depth texture array.
            _nestedMethodsToDisplay = new ProcessingMethod[] { PMPerViewMeshesQSTRDTA };
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
        public override GUIContent GetGUIInfo()
        {
            string label = "Per-view depth as texture array";
            string tooltip = "A texture array of depth maps will be computed from the specified global mesh.";
            if(!GUI.enabled)
                tooltip = "Prerequisites are not satisfied.\nPrerequisites: global mesh.";
            return new GUIContent(label, tooltip);
        }

        /// <inheritdoc/>
        public override GUIContent GetProcessedDataName()
        {
            string label = "Depth texture array";
            string tooltip = GetGUIInfo().text;
            return new GUIContent(label, tooltip);
        }

        /// <inheritdoc/>
        protected override IEnumerator ExecuteMethodCoroutine()
        {
            // Execute the corresponding method.
            yield return StartCoroutine(StoreDepthMapTextureArrayCoroutine());
            // If activated, compute per-view meshes for the depth maps in the texture array.
            if(PMPerViewMeshesQSTRDTA.shouldExecute)
                yield return StartCoroutine(PMPerViewMeshesQSTRDTA.ExecuteAndDisplayLog());
        }
        
        /// <inheritdoc/>
        public override void DeactivateIncompatibleProcessingMethods()
        {
            
        }

        /// <inheritdoc/>
        public override bool HasAdditionalParameters()
        {
            return false;
        }

        /// <inheritdoc/>
        public override void SectionAdditionalParameters()
        {

        }

#endregion //INHERITANCE_METHODS

#endif //UNITY_EDITOR
          
#region METHODS

#if UNITY_EDITOR

        /// <summary>
        /// Displays and updates a cancelable progress bar informing on the depth texture array generation process.
        /// </summary>
        private void DisplayAndUpdateCancelableProgressBar()
        {
            string progressBarTitle = "COLIBRI VR - " + GetProcessedDataName().text;
            string progressBarInfo = "Generating depth data";
            processingCaller.DisplayAndUpdateCancelableProgressBar(progressBarTitle, progressBarInfo);
        }

        /// <summary>
        /// Coroutine that renders depth maps for each view using a given global mesh, and stores this information as a depth texture array.
        /// </summary>
        /// <returns></returns>
        private IEnumerator StoreDepthMapTextureArrayCoroutine()
        {
            // Get the processed asset's name and path in the bundle.
            string bundledAssetName = GetBundledAssetName(depthMapsAssetName);
            string depthDataPathRelative = GetAssetPathRelative(bundledAssetName);
            // Check if the asset has already been processed.
            if(!dataHandler.IsAssetAlreadyProcessed(depthDataPathRelative))
            {
                // Reset the progress bar.
                GeneralToolkit.ResetCancelableProgressBar(true, false);
                // Create and initialize a temporary preview camera manager aimed at storing depth data.
                PreviewCameraManager previewCameraManager = new GameObject("TempPreviewCameraManager").AddComponent<PreviewCameraManager>();
                Transform previewCameraTransform = new GameObject("TempPreviewCamera").transform;
                GeneralToolkit.CreateRenderTexture(ref previewCameraManager.targetTexture, Vector2Int.one, 24, RenderTextureFormat.RFloat, true, FilterMode.Point, TextureWrapMode.Clamp);
                previewCameraManager.CreatePreviewCamera(previewCameraManager.gameObject, previewCameraTransform, cameraSetup.cameraModels[0]);
                // Instantiate the mesh as a set of submeshes, provided with the default material.
                Material defaultMat = new Material(GeneralToolkit.shaderStandard);
                GameObject[] submeshGOs = new GameObject[PMGlobalMeshEF.globalMesh.subMeshCount];
                for(int i = 0; i < submeshGOs.Length; i++)
                {
                    submeshGOs[i] = new GameObject("TempMesh_" + i);
                    submeshGOs[i].transform.parent = previewCameraManager.transform;
                    submeshGOs[i].AddComponent<MeshFilter>().sharedMesh = PMGlobalMeshEF.globalMesh;
                    Material[] materials = new Material[submeshGOs.Length];
                    materials[i] = defaultMat;
                    submeshGOs[i].AddComponent<MeshRenderer>().materials = materials;
                }
                // Create an empty texture array in which to store the depth data.
                Vector2Int arrayResolution; int arrayDepth;
                ColorTextureArray.GetCorrectedPowerOfTwoForImages(cameraSetup.cameraModels, out arrayResolution, out arrayDepth);
                depthData = new Texture2DArray(1, 1, 1, TextureFormat.RGB24, false);
                GeneralToolkit.CreateTexture2DArray(ref depthData, arrayResolution, arrayDepth, TextureFormat.RGB24, false, FilterMode.Point, TextureWrapMode.Clamp, false);
                // Create a render texture in which to store RFloat depth data, with the array's resolution.
                RenderTexture arraySliceRFloatRenderTex = new RenderTexture(1, 1, 0);
                GeneralToolkit.CreateRenderTexture(ref arraySliceRFloatRenderTex, arrayResolution, 24, RenderTextureFormat.RFloat, true, FilterMode.Point, TextureWrapMode.Clamp);
                // Create a material and render texture to encode the RFloat distance as a RGB color.
                Material distanceToColorMat = new Material(GeneralToolkit.shaderAcquisitionConvert01ToColor);
                RenderTexture distanceAsColorTexture = new RenderTexture(1, 1, 0);
                GeneralToolkit.CreateRenderTexture(ref distanceAsColorTexture, arrayResolution, 0, RenderTextureFormat.ARGB32, true, FilterMode.Point, TextureWrapMode.Clamp);
                // Create a texture in which to store the RGB-encoded distance.
                Texture2D arraySliceRGBTex = new Texture2D(1, 1);
                GeneralToolkit.CreateTexture2D(ref arraySliceRGBTex, arrayResolution, TextureFormat.RGB24, true, FilterMode.Point, TextureWrapMode.Clamp, false);
                // Create a depth map in each layer of the texture array, corresponding to each source camera.
                for(int i = 0; i < arrayDepth; i++)
                {
                    // Update the progress bar, and enable the user to cancel the process.
                    DisplayAndUpdateCancelableProgressBar();
                    if(GeneralToolkit.progressBarCanceled)
                    {
                        processingCaller.processingCanceled = true;
                        break;
                    }
                    // Set the preview camera manager's camera model to the current source camera.
                    previewCameraManager.UpdateCameraModel(cameraSetup.cameraModels[i]);
                    // Render the depth data seen by this camera as an RFloat texture.
                    previewCameraManager.RenderPreviewToTarget(ref previewCameraManager.targetTexture, true);
                    // Resize the rendered texture to the output array's resolution.
                    Graphics.Blit(previewCameraManager.targetTexture, arraySliceRFloatRenderTex);
                    // Convert the resized RFloat texture to an RGB encoding.
                    Graphics.Blit(arraySliceRFloatRenderTex, distanceAsColorTexture, distanceToColorMat);
                    // Store the RGB color texture into the texture array.
                    GeneralToolkit.CopyRenderTextureToTexture2D(distanceAsColorTexture, ref arraySliceRGBTex);
                    depthData.SetPixels(arraySliceRGBTex.GetPixels(), i);
                    depthData.Apply ();
                    yield return null;
                }
                // If the user has not canceled the process, continue.
                if(!GeneralToolkit.progressBarCanceled)
                {
                    // Create an asset from this texture array.
                    AssetDatabase.CreateAsset(depthData, depthDataPathRelative);
                    AssetDatabase.Refresh();
                }
                // Destroy the created textures and conversion material.
                DestroyImmediate(arraySliceRGBTex);
                DestroyImmediate(distanceAsColorTexture);
                DestroyImmediate(distanceToColorMat);
                DestroyImmediate(arraySliceRFloatRenderTex);
                // Destroy the created meshes and default material.
                DestroyImmediate(defaultMat);
                foreach(GameObject submeshGO in submeshGOs)
                    DestroyImmediate(submeshGO);
                // Destroy the preview camera manager.
                previewCameraManager.DestroyPreviewCamera();
                DestroyImmediate(previewCameraManager.gameObject);
                // Reset the progress bar.
                GeneralToolkit.ResetCancelableProgressBar(true, false);
            }
            Texture2DArray depthDataAsset = AssetDatabase.LoadAssetAtPath<Texture2DArray>(depthDataPathRelative);
            depthData = (Texture2DArray)Instantiate(depthDataAsset);
        }

#endif //UNITY_EDITOR

        /// <summary>
        /// Loads the processed depth texture array for play.
        /// </summary>
        /// <returns></returns>
        public IEnumerator LoadDepthTextureArrayCoroutine()
        {
            // Check that the application is playing.
            if(!Application.isPlaying)
                yield break;
            // Check each bundled asset name for the prefix corresponding to this class.
            string bundledAssetsPrefix = DataHandler.GetBundledAssetPrefixFromType(typeof(DepthTextureArray));
            foreach(string bundledAssetName in dataHandler.bundledAssetsNames)
            {
                string assetName = bundledAssetName.Replace(bundledAssetsPrefix, string.Empty);
                // If the correct asset name is found, load the depth texture array.
                if (assetName == depthMapsAssetName)
                {
                    yield return dataHandler.StartCoroutine(dataHandler.LoadAssetsFromBundleCoroutine<Texture2DArray>((result => depthData = result[0]), bundledAssetName));
                }
            }
        }

#endregion //METHODS

    }

}
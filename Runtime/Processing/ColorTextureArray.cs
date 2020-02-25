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
    /// Class that implements the Color Texture Array color processing method.
    /// This method converts the color samples into a texture array asset, that will appear as a single object on the GPU.
    /// </summary>
    public class ColorTextureArray : ProcessingMethod
    {

#region CONST_FIELDS

        public const string colorDataAssetName = "ColorTextureArray";

#endregion //CONST_FIELDS

#region STATIC_METHODS

        /// <summary>
        /// Returns the ceiling power-of-two resolution for a given set of pixel resolutions.
        /// </summary>
        /// <param name="cameraModels"></param> The array of camera models containing the pixel resolutions.
        /// <returns></returns> The ceiling power-of-two resolution.
        public static Vector2Int GetCeilPowerOfTwoForImages(CameraModel[] cameraModels)
        {
            int x = 0;
            int y = 0;
            // If there are no camera models, return (0, 0).
            if(cameraModels != null)
            {
                // Otherwise, choose the ceiling power-of-two resolution for each axis.
                foreach(CameraModel cameraModel in cameraModels)
                {
                    x = Mathf.Max(x, Mathf.ClosestPowerOfTwo(cameraModel.pixelResolution.x));
                    y = Mathf.Max(y, Mathf.ClosestPowerOfTwo(cameraModel.pixelResolution.y));
                }
            }
            return new Vector2Int(x, y);
        }

        /// <summary>
        /// Corrects the desired array resolution and depth to account for practical memory limitations.
        /// </summary>
        /// <param name="desiredArrayResolution"></param> The desired array resolution in pixels.
        /// <param name="desiredArrayDepth"></param> The desired array depth, i.e. number of textures in the array.
        private static void CorrectForMemorySize(ref Vector2Int desiredArrayResolution, ref int desiredArrayDepth)
        {
            // Correct the desired array depth to account for the maximum depth of texture arrays.
            int maxArrayDepth = 2048;
            desiredArrayDepth = Mathf.Min(desiredArrayDepth, maxArrayDepth);
            // Correct the desired array resolution to account for the maximum pixel resolution of texture arrays.
            int maxResolution = 16384;
            desiredArrayResolution.x = Mathf.Min(desiredArrayResolution.x, maxResolution);
            desiredArrayResolution.y = Mathf.Min(desiredArrayResolution.y, maxResolution);
            // Correct the desired array resolution to account for the maximum size (in gigabytes) of texture arrays.
            float maxArraySizeGB = 2f;
            // Note: we also apply further downscaling based on the performances of our setup.
            maxArraySizeGB = 0.5f;
            for(int i = 0; i < 6; i++)
            {
                float arraySizeGB = (0.001f * desiredArrayResolution.x) * (0.001f * desiredArrayResolution.y) * desiredArrayDepth * 3 * 0.001f;
                if(arraySizeGB < maxArraySizeGB)
                {
                    break;
                }
                else
                {
                    desiredArrayResolution.x /= 2;
                    desiredArrayResolution.y /= 2;
                }
            }
        }

        /// <summary>
        /// Gets the texture array's resolution and depth for the given set of camera models.
        /// </summary>
        /// <param name="cameraModels"></param> The set of source camera models.
        /// <param name="arrayResolution"></param> The output texture array resolution, in pixels.
        /// <param name="arrayDepth"></param> The output texture array depth, i.e. number of images in the array.
        public static void GetCorrectedPowerOfTwoForImages(CameraModel[] cameraModels, out Vector2Int arrayResolution, out int arrayDepth)
        {
            arrayResolution = GetCeilPowerOfTwoForImages(cameraModels);
            arrayDepth = (cameraModels == null) ? 0 : cameraModels.Length;
            CorrectForMemorySize(ref arrayResolution, ref arrayDepth);
        }

#endregion //STATIC_METHODS

#region FIELDS

        public Texture2DArray colorData;

#endregion //FIELDS

#if UNITY_EDITOR

#region INHERITANCE_METHODS

        /// <inheritdoc/>
        public override bool IsCompatible(int colorDataCount, int depthDataCount, int meshDataCount)
        {
            // Indicate that this method is available only if there are more than one color data samples.
            return (colorDataCount >= 1);
        }

        /// <inheritdoc/>
        public override bool IsGUINested()
        {
            return false;
        }

        /// <inheritdoc/>
        public override GUIContent GetGUIInfo()
        {
            string label = "Texture array from color data";
            string tooltip = string.Empty;
            if(GUI.enabled && cameraSetup != null)
            {
                tooltip = "Color data samples will be converted into a texture array asset.\n";
                Vector2Int arrayResolution; int arrayDepth;
                GetCorrectedPowerOfTwoForImages(cameraSetup.cameraModels, out arrayResolution, out arrayDepth);
                tooltip += "Resolution: " + arrayResolution.x + "x" + arrayResolution.y + "x" + arrayDepth;
            }
            else
            {
                tooltip = "Prerequisites are not satisfied.\nPrerequisites: multiple color data files (.JPG, .PNG).";
            }
            return new GUIContent(label, tooltip);
        }

        /// <inheritdoc/>
        public override GUIContent GetProcessedDataName()
        {
            string label = "Color texture array";
            string tooltip = GetGUIInfo().text;
            return new GUIContent(label, tooltip);
        }

        /// <inheritdoc/>
        protected override IEnumerator ExecuteMethodCoroutine()
        {
            // Get the processed asset's name and path in the bundle.
            string bundledAssetName = GetBundledAssetName(colorDataAssetName);
            string colorDataPathRelative = GetAssetPathRelative(bundledAssetName);
            // Check if the asset has already been processed.
            if(!dataHandler.IsAssetAlreadyProcessed(colorDataPathRelative))
            {
                // Reset the progress bar.
                GeneralToolkit.ResetCancelableProgressBar(true, false);
                // Determine the resolution and depth that should be given to the texture array.
                Vector2Int arrayResolution; int arrayDepth;
                GetCorrectedPowerOfTwoForImages(cameraSetup.cameraModels, out arrayResolution, out arrayDepth);
                // Create an empty texture array.
                colorData = new Texture2DArray (1, 1, 1, TextureFormat.RGBA32, false);
                GeneralToolkit.CreateTexture2DArray(ref colorData, arrayResolution, arrayDepth, TextureFormat.RGB24, false, FilterMode.Point, TextureWrapMode.Clamp, false);
                // Create an empty texture, with the array's resolution.
                Texture2D arraySlice = new Texture2D(1, 1);
                GeneralToolkit.CreateTexture2D(ref arraySlice, arrayResolution, TextureFormat.RGB24, false, FilterMode.Point, TextureWrapMode.Clamp, false);
                // Create an empty texture, in which we will load the set of source images one-by-one.
                Texture2D loadTex = new Texture2D(1, 1);
                // Process as many images as possible from the set of source images.
                for(int i = 0; i < arrayDepth; i++)
                {
                    // Update the progress bar, and enable the user to cancel the process.
                    DisplayAndUpdateCancelableProgressBar();
                    if(GeneralToolkit.progressBarCanceled)
                    {
                        processingCaller.processingCanceled = true;
                        break;
                    }
                    // Load the camera model.
                    CameraModel cameraModel = cameraSetup.cameraModels[i];
                    // Load the image into a texture object.
                    string imagePath = Path.Combine(dataHandler.colorDirectory, cameraModel.imageName);
                    GeneralToolkit.CreateTexture2D(ref loadTex, cameraModel.pixelResolution, TextureFormat.RGB24, false, FilterMode.Point, TextureWrapMode.Clamp, false);
                    GeneralToolkit.LoadTexture(imagePath, ref loadTex);
                    // Resize the texture so that it fits the array's resolution.
                    GeneralToolkit.ResizeTexture2D(loadTex, ref arraySlice);
                    // Add the texture to the texture array.
                    colorData.SetPixels(arraySlice.GetPixels(), i);
                    colorData.Apply ();
                    yield return null;
                }
                // If the user has not canceled the process, continue.
                if(!GeneralToolkit.progressBarCanceled)
                {
                    // Create an asset from this texture array.            
                    AssetDatabase.CreateAsset(colorData, colorDataPathRelative);
                    AssetDatabase.Refresh();
                }
                // Destroy created objects.
                DestroyImmediate(loadTex);
                DestroyImmediate(arraySlice);
                // Reset the progress bar.
                GeneralToolkit.ResetCancelableProgressBar(true, false);
            }
            Texture2DArray colorDataAsset = AssetDatabase.LoadAssetAtPath<Texture2DArray>(colorDataPathRelative);
            colorData = (Texture2DArray)Instantiate(colorDataAsset);
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
        /// Displays and updates a cancelable progress bar informing on the texture array generation process.
        /// </summary>
        private void DisplayAndUpdateCancelableProgressBar()
        {
            string progressBarTitle = "COLIBRI VR - " + GetProcessedDataName().text;
            string progressBarInfo = "Processing color data";
            processingCaller.DisplayAndUpdateCancelableProgressBar(progressBarTitle, progressBarInfo);
        }

#endif //UNITY_EDITOR

        /// <summary>
        /// Loads the processed texture array for play.
        /// </summary>
        /// <returns></returns>
        public IEnumerator LoadProcessedTextureArrayCoroutine()
        {
            // Check that the application is playing.
            if(!Application.isPlaying)
                yield break;
            // Check each bundled asset name for the prefix corresponding to this class.
            string bundledAssetsPrefix = DataHandler.GetBundledAssetPrefixFromType(typeof(ColorTextureArray));
            foreach(string bundledAssetName in dataHandler.bundledAssetsNames)
            {
                string assetName = bundledAssetName.Replace(bundledAssetsPrefix, string.Empty);
                // If the correct asset name is found, load the texture array.
                if(assetName == colorDataAssetName)
                {
                    yield return dataHandler.StartCoroutine(dataHandler.LoadAssetsFromBundleCoroutine<Texture2DArray>((result => colorData = result[0]), bundledAssetName));
                }
            }
        }

#endregion //METHODS

    }

}

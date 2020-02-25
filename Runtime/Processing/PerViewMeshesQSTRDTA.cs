/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

using System.Collections;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace COLIBRIVR.Processing
{

    /// <summary>
    /// Class that implements the Per-View Meshes by Quadtree Simplification and Triangle Removal from a Depth Texture Array geometry processing method.
    /// This method applies the Per-View Meshes QSTR method to an input set of depth maps stored as a depth texture array.
    /// </summary>
    public class PerViewMeshesQSTRDTA : ProcessingMethod
    {

#if UNITY_EDITOR

#region INHERITANCE_METHODS

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
            return PMPerViewMeshesQSTR.GetGUIInfo();
        }

        /// <inheritdoc/>
        public override GUIContent GetProcessedDataName()
        {
            return PMPerViewMeshesQSTR.GetProcessedDataName();
        }

        /// <inheritdoc/>
        protected override IEnumerator ExecuteMethodCoroutine()
        {
            yield return StartCoroutine(StorePerViewMeshesCoroutine());
        }

        /// <inheritdoc/>
        public override void DeactivateIncompatibleProcessingMethods()
        {
            PMPerViewMeshesQSTR.shouldExecute = false;
        }
        
        /// <inheritdoc/>
        public override bool HasAdditionalParameters()
        {
            return true;
        }

        /// <inheritdoc/>
        public override void SectionAdditionalParameters()
        {
            // Enable the user to choose the parameters for per-view meshing.
            if(shouldExecute)
                using(var indentScope = new EditorGUI.IndentLevelScope())
                    PMPerViewMeshesQSTR.SectionAdditionalParameters();
        }

#endregion //INHERITANCE_METHODS
          
#region METHODS

        /// <summary>
        /// Coroutine that renders per-view meshes from the given depth texture array.
        /// </summary>
        /// <returns></returns>
        private IEnumerator StorePerViewMeshesCoroutine()
        {
            // Reset the progress bar.
            GeneralToolkit.ResetCancelableProgressBar(true, false);
            // Initialize the compute shader's properties.
            PMPerViewMeshesQSTR.InitializePerCall();
            // Create a mesh for each source depth map.
            for(int sourceCamIndex = 0; sourceCamIndex < cameraSetup.cameraModels.Length; sourceCamIndex++)
            {
                // Check if the asset has already been processed.
                string bundledAssetName = dataHandler.GetBundledAssetName(PMPerViewMeshesQSTR, PerViewMeshesQSTR.perViewMeshAssetPrefix + sourceCamIndex);
                string meshRelativePath = Path.Combine(GeneralToolkit.tempDirectoryRelativePath, bundledAssetName + ".asset");
                if(dataHandler.IsAssetAlreadyProcessed(meshRelativePath))
                    continue;
                // Update the progress bar, and enable the user to cancel the process.
                PMPerViewMeshesQSTR.DisplayAndUpdateCancelableProgressBar();
                if(GeneralToolkit.progressBarCanceled)
                {
                    processingCaller.processingCanceled = true;
                    break;
                }
                // Update the camera model.
                PMPerViewMeshesQSTR.cameraModel = cameraSetup.cameraModels[sourceCamIndex];
                // Initialize the distance map texture, and load the depth data into it.
                PMPerViewMeshesQSTR.InitializeDistanceMap();
                Vector2Int distanceMapResolution = new Vector2Int(PMPerViewMeshesQSTR.distanceMap.width, PMPerViewMeshesQSTR.distanceMap.height);
                RenderTexture depthTextureArraySlice = new RenderTexture(1, 1, 0);
                GeneralToolkit.CreateRenderTexture(ref depthTextureArraySlice, distanceMapResolution, 0, RenderTextureFormat.ARGB32, true, FilterMode.Point);
                Graphics.Blit(PMDepthTextureArray.depthData, depthTextureArraySlice, sourceCamIndex, 0);
                GeneralToolkit.CopyRenderTextureToTexture2D(depthTextureArraySlice, ref PMPerViewMeshesQSTR.distanceMap);
                DestroyImmediate(depthTextureArraySlice);
                // Compute a mesh from the distance map.
                Mesh meshAsset;
                PMPerViewMeshesQSTR.ComputeMesh(out meshAsset);
                // Save this mesh as an asset.
                GeneralToolkit.CreateAndUnloadAsset(meshAsset, meshRelativePath);
                yield return null;
            }
            // Reset the progress bar.
            GeneralToolkit.ResetCancelableProgressBar(true, false);
        }

#endregion //METHODS

#endif //UNITY_EDITOR

    }

}
/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

using UnityEngine;
using System.IO;
using COLIBRIVR.Processing;

namespace COLIBRIVR.Debugging
{

    /// <summary>
    /// Debug class that applies the Per-View Meshes by Quadtree Simplification and Triangle Removal geometry processing method to create a mesh from an Existing File (depth map).
    /// </summary>
    [ExecuteInEditMode]
    public class PerViewMeshesQSTREF : PerViewMeshesQSTRDebug
    {

#region CONST_FIELDS

        private const string _shaderNameMinMax01 = "_MinMax01";

#endregion //CONST_FIELDS

#region FIELDS

        public Processing.Processing processing;

#endregion //FIELDS

#if UNITY_EDITOR

#region INHERITANCE_METHODS

        /// <summary>
        /// Resets the camera parameters and geometry processing parameters.
        /// </summary>
        void Reset()
        {
            // Reset the processing object.
            processing = GeneralToolkit.GetOrCreateChildComponent<Processing.Processing>(transform);
            processing.Reset();
            // Reset the geometry processing parameters.
            _geometryProcessingMethod = (Processing.PerViewMeshesQSTR)processing.processingMethods[ProcessingMethod.indexPerViewMeshesQSTR];
        }

        /// <inheritdoc/>
        public override void OnDestroy()
        {
            base.OnDestroy();
            GeneralToolkit.RemoveChildComponents(transform, typeof(Processing.Processing));
        }
        
        /// <inheritdoc/>
        protected override void ProvideDepthTextureToGeometryProcessingMethod()
        {
            // Load the depth map and provide it to the geometry processing method.
            string imagePath = Path.Combine(processing.dataHandler.depthDirectory, GetCameraModel().imageName);
            GeneralToolkit.LoadTexture(imagePath, ref _geometryProcessingMethod.distanceMap);
            // Convert the precise depth map encoding to one more fitted for visualization.
            Material preciseToVizMat = new Material(GeneralToolkit.shaderDebuggingConvertPreciseToVisualization);
            Vector2 minMax01 = GetMinMax01FromDepthMap();
            preciseToVizMat.SetVector(_shaderNameMinMax01, minMax01);
            Graphics.Blit(_geometryProcessingMethod.distanceMap, _visualizationTexture, preciseToVizMat);
            DestroyImmediate(preciseToVizMat);
        }

        /// <inheritdoc/>
        public override CameraModel GetCameraModel()
        {
            if(processing.cameraSetup.cameraModels != null && processing.cameraSetup.cameraModels.Length > 0)
                return processing.cameraSetup.cameraModels[processing.cameraSetup.previewIndex];
            else
                return null;
        }

#endregion //INHERITANCE_METHODS

#region METHODS

        /// <summary>
        /// Gets the min and max values from the depth map's precise encoding.
        /// </summary>
        /// <returns></returns> The min and max values.
        private Vector2 GetMinMax01FromDepthMap()
        {
            Vector2 minMax01 = new Vector2(1, 0);
            Color[] pixels = _geometryProcessingMethod.distanceMap.GetPixels();
            for(int iter = 0; iter < pixels.Length; iter++)
            {
                float distanceNonlinear01 = GeneralToolkit.Decode01FromPreciseColor(pixels[iter]);
                if(distanceNonlinear01 < minMax01.x)
                    minMax01.x = distanceNonlinear01;
                if(distanceNonlinear01 > minMax01.y)
                    minMax01.y = distanceNonlinear01;
            }
            return minMax01;
        }

#endregion //METHODS

#endif //UNITY_EDITOR

    }

}

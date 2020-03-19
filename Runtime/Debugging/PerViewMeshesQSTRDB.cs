/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

using UnityEngine;
using UnityEngine.Events;

namespace COLIBRIVR.Debugging
{

    /// <summary>
    /// Debug class that applies the Per-View Meshes by Quadtree Simplification and Triangle Removal geometry processing method to create a mesh from the camera's Depth Buffer.
    /// </summary>
    [ExecuteInEditMode]
    public class PerViewMeshesQSTRDB : PerViewMeshesQSTRDebug, IPreviewCaller
    {

#region CONST_FIELDS

        private const string _previewCallerName = "Debugging_PerViewMeshesQSTR";

#endregion //CONST_FIELDS

#region INHERITANCE_PROPERTIES

        public int previewIndex { get; set; }
        public UnityEvent onPreviewIndexChangeEvent { get; }

#endregion //INHERITANCE_PROPERTIES

#region FIELDS

        public CameraModel cameraModel;

        [SerializeField] private PreviewCameraManager _previewCameraManager;

        private RenderTexture _distanceAsColorTexture;
        private Vector3 _previousPosition;
        private Quaternion _previousRotation;

#endregion //FIELDS

#if UNITY_EDITOR

#region INHERITANCE_METHODS

        /// <summary>
        /// Resets the camera parameters and geometry processing parameters.
        /// </summary>
        void Reset()
        {
            // Reset the camera parameters.
            if(cameraModel == null)
                cameraModel = CameraModel.CreateCameraModel(transform);
            cameraModel.Reset();
            // Reset the geometry processing parameters.
            _geometryProcessingMethod = GeneralToolkit.GetOrCreateChildComponent<COLIBRIVR.Processing.PerViewMeshesQSTR>(transform);
            _geometryProcessingMethod.Reset();
            // Get the preview camera manager.
            _previewCameraManager = GeneralToolkit.GetOrCreateChildComponent<PreviewCameraManager>(transform);
            // Update the displayed preview.
            previewIndex = -1;
            Selected();
        }

        /// <summary>
        /// On update, check if the transform has changed.
        /// </summary>
        void Update()
        {
            CheckTransformChanged();
        }

        /// <summary>
        /// On destroy, destroys all created objects.
        /// </summary>
        public override void OnDestroy()
        {
            Deselected();
            DestroyAll();
        }

        /// <inheritdoc/>
        public override void Selected()
        {
            base.Selected();
            // If needed, initialize the preview camera.
            if(_previewCameraManager.previewCamera == null)
            {
                Transform previewCameraTransform = new GameObject("PreviewCamera").transform;
                GeneralToolkit.CreateRenderTexture(ref _previewCameraManager.targetTexture, Vector2Int.one, 0, RenderTextureFormat.ARGB32, false, FilterMode.Point, TextureWrapMode.Clamp);
                _previewCameraManager.CreatePreviewCamera(gameObject, previewCameraTransform, cameraModel);
            }
            // Notify the preview window that this object will send images for preview.
            PreviewWindow.AddCaller(this, _previewCallerName);
            // Update the camera model and display the rendered preview.
            UpdateCameraModel();
        }

        /// <inheritdoc/>
        public override void Deselected()
        {
            base.Deselected();
            // Notify the preview window that this object will no longer send images for preview.
            PreviewWindow.RemoveCaller(_previewCallerName);
        }

        /// <inheritdoc/>
        public override void DestroyMesh()
        {
            base.DestroyMesh();
            if(_distanceAsColorTexture != null)
                DestroyImmediate(_distanceAsColorTexture);
        }

        /// <inheritdoc/>
        public override void CreateMeshFromZBuffer()
        {
            base.CreateMeshFromZBuffer();
            // Render the preview.
            UpdateCameraModel();
        }

        /// <inheritdoc/>
        protected override void ProvideDepthTextureToGeometryProcessingMethod()
        {
            // Convert the camera's z-buffer to a color texture, and provide it to the processing method.
            ConvertZBufferToColorTexture();
            GeneralToolkit.CopyRenderTextureToTexture2D(_distanceAsColorTexture, ref _geometryProcessingMethod.distanceMap);
        }

        /// <inheritdoc/>
        public override CameraModel GetCameraModel()
        {
            return cameraModel;
        }

#endregion //INHERITANCE_METHODS

#region METHODS

        /// <summary>
        /// Checks whether the transform has changed. If so, destroys the mesh, and updates the camera models.
        /// </summary>
        private void CheckTransformChanged()
        {
            if(GeneralToolkit.HasTransformChanged(transform, ref _previousPosition, ref _previousRotation))
            {
                DestroyMesh();
                UpdateCameraModel();
            }
        }

        /// <summary>
        /// Destroys all created objects.
        /// </summary>
        public void DestroyAll()
        {
            DestroyMesh();
            if(_previewCameraManager != null)
                _previewCameraManager.DestroyPreviewCamera();
        }

        /// <summary>
        /// Updates the preview camera's pose and parameters.
        /// </summary>
        public void UpdateCameraModel()
        {
            // Render the image and display it in the preview window.
            if(_previewCameraManager != null && _previewCameraManager.previewCamera != null)
            {
                // Set the preview camera to the current model parameters.
                _previewCameraManager.UpdateCameraModel(cameraModel);
                // Render the preview and display it.
                _previewCameraManager.RenderPreviewToTarget(ref _previewCameraManager.targetTexture, false);
                PreviewWindow.DisplayImage(_previewCallerName, _previewCameraManager.targetTexture, 0);
            }
        }

        /// <summary>
        /// Converts the camera's Z-buffer to a color texture.
        /// </summary>
        private void ConvertZBufferToColorTexture()
        {
            // Initialize a RFloat texture to store the camera's depth information.
            RenderTexture depthTexture = new RenderTexture (1, 1, 0);
            GeneralToolkit.CreateRenderTexture(ref depthTexture, cameraModel.pixelResolution, 24, RenderTextureFormat.RFloat, true, FilterMode.Point, TextureWrapMode.Clamp);
            // Render the preview camera's depth to this texture.
            _previewCameraManager.RenderPreviewToTarget(ref depthTexture, true);
            // Initialize the output RGB color texture.
            GeneralToolkit.CreateRenderTexture(ref _distanceAsColorTexture, cameraModel.pixelResolution, 0, RenderTextureFormat.ARGB32, true, FilterMode.Point, TextureWrapMode.Clamp);
            // Convert the depth information as color into this texture.
            Material distanceToColorMat = new Material(GeneralToolkit.shaderAcquisitionConvert01ToColor);
            Graphics.Blit(depthTexture, _distanceAsColorTexture, distanceToColorMat);
            // Create a visualization texture, showing the depth information with a visual color map.
            distanceToColorMat.SetInt(Acquisition.Acquisition.shaderNameIsPrecise, 0);
            Graphics.Blit(depthTexture, _visualizationTexture, distanceToColorMat);
            // Destroy the created temporary objects.
            DestroyImmediate(depthTexture);
            DestroyImmediate(distanceToColorMat);
        }

#endregion //METHODS

#endif //UNITY_EDITOR

    }

}

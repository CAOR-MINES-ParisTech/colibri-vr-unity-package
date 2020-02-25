/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

using UnityEngine;
using UnityEngine.Events;
using UnityEditor;

namespace COLIBRIVR.Debugging
{

    /// <summary>
    /// Debug class that displays a mesh from the camera's depth buffer.
    /// </summary>
    [ExecuteInEditMode]
    public class PerViewMeshesQSTR : MonoBehaviour, IPreviewCaller
    {

#region CONST_FIELDS

        private const string _previewCallerName = "Debugging_PerViewMeshesQSTR";

#endregion //CONST_FIELDS

#region INHERITANCE_PROPERTIES

        public int previewIndex { get; set; }
        public UnityEvent onPreviewIndexChangeEvent { get; }

#endregion //INHERITANCE_PROPERTIES

#region FIELDS

        public CameraModel cameraParams;

        [SerializeField] private COLIBRIVR.Processing.PerViewMeshesQSTR _geometryProcessingMethod;
        [SerializeField] private PreviewCameraManager _previewCameraManager;

        private Material _displayMaterial;
        private Mesh _mesh;
        private GameObject _meshGO;
        private RenderTexture _distanceAsColorTexture;
        private RenderTexture _visualizationTexture;
        private string[] _compressionInfo;
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
            if(cameraParams == null)
                cameraParams = CameraModel.CreateCameraModel(transform);
            cameraParams.Reset();
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
        void OnDestroy()
        {
            Deselected();
            DestroyAll();
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
        /// On selection, initializes the object for preview.
        /// </summary>
        public void Selected()
        {
            // If needed, initialize the preview camera.
            if(_previewCameraManager.previewCamera == null)
            {
                Transform previewCameraTransform = new GameObject("PreviewCamera").transform;
                GeneralToolkit.CreateRenderTexture(ref _previewCameraManager.targetTexture, Vector2Int.one, 0, RenderTextureFormat.ARGB32, false, FilterMode.Point, TextureWrapMode.Clamp);
                _previewCameraManager.CreatePreviewCamera(gameObject, previewCameraTransform, cameraParams);
            }
            // Notify the preview window that this object will send images for preview.
            PreviewWindow.AddCaller(this, _previewCallerName);
            // Update the camera model and display the rendered preview.
            UpdateCameraModel();
        }

        /// <summary>
        /// On deselection, destroys any created objects.
        /// </summary>
        public void Deselected()
        {
            // Check if the new selection is the gameobject or one of its children.
            bool newSelectionIsObjectOrChildren = (Selection.activeGameObject == gameObject);
            foreach(Transform child in transform)
                if(Selection.activeGameObject == child.gameObject)
                    newSelectionIsObjectOrChildren = true;
            // If it is not, destroy all created objects.
            if(!newSelectionIsObjectOrChildren)
                DestroyAll();
            // Notify the preview window that this object will no longer send images for preview.
            PreviewWindow.RemoveCaller(_previewCallerName);
        }

        /// <summary>
        /// Destroys the created mesh.
        /// </summary>
        public void DestroyMesh()
        {
            _compressionInfo = null;
            if(_meshGO != null)
                DestroyImmediate(_meshGO);
            if(_mesh != null)
                DestroyImmediate(_mesh);
            if(_distanceAsColorTexture != null)
                DestroyImmediate(_distanceAsColorTexture);
            if(_visualizationTexture != null)
                DestroyImmediate(_visualizationTexture);
            if(_displayMaterial != null)
                DestroyImmediate(_displayMaterial);
            if(_geometryProcessingMethod != null)
                _geometryProcessingMethod.ReleaseDistanceMap();
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
                _previewCameraManager.UpdateCameraModel(cameraParams);
                // Render the preview and display it.
                _previewCameraManager.RenderPreviewToTarget(ref _previewCameraManager.targetTexture, false);
                PreviewWindow.DisplayImage(_previewCallerName, _previewCameraManager.targetTexture, 0);
            }
        }

        /// <summary>
        /// Creates a mesh based on the camera's Z-buffer. Overwrites previously created mesh.
        /// </summary>
        public void CreateMeshFromZBuffer()
        {
            // Destroy the previous mesh.
            DestroyMesh();
            // Initialize the quadtree mesh processing method with the camera parameters.
            _geometryProcessingMethod.InitializePerCall();
            _geometryProcessingMethod.cameraModel = cameraParams;
            _geometryProcessingMethod.InitializeDistanceMap();
            // Convert the camera's z-buffer to a color texture, and provide it to the processing method.
            ConvertZBufferToColorTexture();
            GeneralToolkit.CopyRenderTextureToTexture2D(_distanceAsColorTexture, ref _geometryProcessingMethod.distanceMap);
            // Use the processing method to generate a mesh from the provided depth texture.
            GenerateDepthMapMesh();
            // Render the preview.
            UpdateCameraModel();
        }

        /// <summary>
        /// Converts the camera's Z-buffer to a color texture.
        /// </summary>
        private void ConvertZBufferToColorTexture()
        {
            // Initialize a RFloat texture to store the camera's depth information.
            RenderTexture depthTexture = new RenderTexture (1, 1, 0);
            GeneralToolkit.CreateRenderTexture(ref depthTexture, cameraParams.pixelResolution, 24, RenderTextureFormat.RFloat, true, FilterMode.Point, TextureWrapMode.Clamp);
            // Render the preview camera's depth to this texture.
            _previewCameraManager.RenderPreviewToTarget(ref depthTexture, true);
            // Initialize the output RGB color texture.
            GeneralToolkit.CreateRenderTexture(ref _distanceAsColorTexture, cameraParams.pixelResolution, 0, RenderTextureFormat.ARGB32, true, FilterMode.Point, TextureWrapMode.Clamp);
            // Convert the depth information as color into this texture.
            Material distanceToColorMat = new Material(GeneralToolkit.shaderAcquisitionConvert01ToColor);
            Graphics.Blit(depthTexture, _distanceAsColorTexture, distanceToColorMat);
            // Create a visualization texture, showing the depth information with a visual color map.
            GeneralToolkit.CreateRenderTexture(ref _visualizationTexture, cameraParams.pixelResolution, 0, RenderTextureFormat.ARGB32, true, FilterMode.Point, TextureWrapMode.Clamp);
            distanceToColorMat.SetInt("_IsPrecise", 0);
            Graphics.Blit(depthTexture, _visualizationTexture, distanceToColorMat);
            // Initialize the material used to display the mesh in the Scene view.
            _displayMaterial = new Material(GeneralToolkit.shaderUnlitTexture);
            _displayMaterial.SetTexture("_MainTex", _visualizationTexture);
            // Destroy the created temporary objects.
            DestroyImmediate(depthTexture);
            DestroyImmediate(distanceToColorMat);
        }

        /// <summary>
        /// Generates a mesh from the depth information passed to the quadtree mesh processing method.
        /// </summary>
        private void GenerateDepthMapMesh()
        {
            // Initialize a mesh gameobject.
            _meshGO = new GameObject("Mesh");
            _meshGO.transform.parent = transform;
            _meshGO.transform.localEulerAngles = Vector3.zero;
            _meshGO.transform.localPosition = Vector3.zero;
            _meshGO.AddComponent<MeshRenderer>().material = _displayMaterial;
            // Compute a mesh using the quadtree mesh processing method, and add it to the gameobject.
            _mesh = new Mesh();
            _compressionInfo = _geometryProcessingMethod.ComputeMesh(out _mesh);
            _meshGO.AddComponent<MeshFilter>().mesh = _mesh;
            // Release the distance map.
            _geometryProcessingMethod.ReleaseDistanceMap();
        }

        /// <summary>
        /// Displays the processing method's additional parameters in the inspector.
        /// </summary>
        public void SectionAdditionalParameters()
        {
            _geometryProcessingMethod.SectionAdditionalParameters();
        }

        /// <summary>
        /// Diplays the compression info in the inspector.
        /// </summary>
        public void DisplayCompressionInfo()
        {
            if(_compressionInfo != null)
                for(int i = 0; i < _compressionInfo.Length; i++)
                    EditorGUILayout.LabelField(_compressionInfo[i]);
        }

#endregion //METHODS

#endif //UNITY_EDITOR

    }

}

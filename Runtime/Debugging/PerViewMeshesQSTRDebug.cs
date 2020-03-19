/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

using UnityEngine;
using UnityEditor;

namespace COLIBRIVR.Debugging
{

    /// <summary>
    /// Abstract class to be used as parent by classes debugging the Per-View Meshes by Quadtree Simplification and Triangle Removal geometry processing method.
    /// </summary>
    public abstract class PerViewMeshesQSTRDebug : MonoBehaviour
    {

#region CONST_FIELDS

        public const string propertyNameGeometryProcessingMethod = "_geometryProcessingMethod";

#endregion //CONST_FIELDS

#region FIELDS

        [SerializeField] protected COLIBRIVR.Processing.PerViewMeshesQSTR _geometryProcessingMethod;

        protected Material _displayMaterial;
        protected Mesh _mesh;
        protected GameObject _meshGO;
        protected RenderTexture _visualizationTexture;
        protected string[] _compressionInfo;

#endregion //FIELDS

#if UNITY_EDITOR

#region INHERITANCE_METHODS

        /// <summary>
        /// On destroy, destroys all created objects.
        /// </summary>
        public virtual void OnDestroy()
        {
            DestroyMesh();
        }

        /// <summary>
        /// On selection, do something.
        /// </summary>
        public virtual void Selected()
        {
            
        }

        /// <summary>
        /// On deselection, do something.
        /// </summary>
        public virtual void Deselected()
        {
            // Check if the new selection is the gameobject or one of its children.
            bool newSelectionIsObjectOrChildren = (Selection.activeGameObject == gameObject);
            foreach(Transform child in transform)
                if(Selection.activeGameObject == child.gameObject)
                    newSelectionIsObjectOrChildren = true;
            // If it is not, destroy all created objects.
            if(!newSelectionIsObjectOrChildren)
                DestroyMesh();
        }

        /// <summary>
        /// Destroys the created mesh.
        /// </summary>
        public virtual void DestroyMesh()
        {
            _compressionInfo = null;
            if(_meshGO != null)
                DestroyImmediate(_meshGO);
            if(_mesh != null)
                DestroyImmediate(_mesh);
            if(_visualizationTexture != null)
                DestroyImmediate(_visualizationTexture);
            if(_displayMaterial != null)
                DestroyImmediate(_displayMaterial);
            if(_geometryProcessingMethod != null)
                _geometryProcessingMethod.ReleaseDistanceMap();
        }

        /// <summary>
        /// Creates a mesh based on the camera's Z-buffer. Overwrites previously created mesh.
        /// </summary>
        public virtual void CreateMeshFromZBuffer()
        {
            // Destroy the previous mesh.
            DestroyMesh();
            // Initialize the quadtree mesh processing method with the camera parameters.
            CameraModel cameraModel = GetCameraModel();
            _geometryProcessingMethod.InitializePerCall();
            _geometryProcessingMethod.cameraModel = cameraModel;
            _geometryProcessingMethod.InitializeDistanceMap();
            // Initialize the visualization texture.
            GeneralToolkit.CreateRenderTexture(ref _visualizationTexture, cameraModel.pixelResolution, 0, RenderTextureFormat.ARGB32, true, FilterMode.Point, TextureWrapMode.Clamp);
            // Provides the depth texture to use as input to the geometry processing method.
            ProvideDepthTextureToGeometryProcessingMethod();
            // Initialize the material used to display the mesh in the Scene view.
            _displayMaterial = new Material(GeneralToolkit.shaderUnlitTexture);
            _displayMaterial.SetTexture("_MainTex", _visualizationTexture);
            // Use the processing method to generate a mesh from the provided depth texture.
            GenerateDepthMapMesh();
        }

        /// <summary>
        /// Provides the depth texture to use as input to the geometry processing method.
        /// </summary>
        protected abstract void ProvideDepthTextureToGeometryProcessingMethod();

        /// <summary>
        /// Gets the camera model.
        /// </summary>
        /// <returns></returns> The camera model.
        public abstract CameraModel GetCameraModel();

#endregion //INHERITANCE_METHODS

#region METHODS

        /// <summary>
        /// Generates a mesh from the depth information passed to the quadtree mesh processing method.
        /// </summary>
        private void GenerateDepthMapMesh()
        {
            // Initialize a mesh gameobject.
            _meshGO = new GameObject("Mesh");
            _meshGO.transform.parent = transform;
            Transform cameraModelTransform = GetCameraModel().transform;
            _meshGO.transform.eulerAngles = cameraModelTransform.eulerAngles;
            _meshGO.transform.position = cameraModelTransform.position;
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

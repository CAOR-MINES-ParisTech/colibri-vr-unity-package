/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using COLIBRIVR.Processing;

namespace COLIBRIVR.Rendering
{

    /// <summary>
    /// Helper class to be called by blending methods based on the Unstructured Lumigraph Rendering method.
    /// </summary>
    public class Helper_ULR : Method
    {

#region CONST_FIELDS

        public const int maxBlendCamCount = 4;

#endregion //CONST_FIELDS

#region FIELDS

        public int blendCamCount;
        public Material currentBlendingMaterial;
        public int targetFramerate;
        public int maxFrameCountForProcessing;
        public bool initialized;
        public int numberOfSourceCameras;

        [SerializeField] private bool _optimizeForHighFramerates;
        [SerializeField] private float _maxBlendAngle;
        [SerializeField] private float _resolutionWeight;
        [SerializeField] private float _depthCorrectionFactor;
        [SerializeField] private float _globalTextureMapWeight;

        private Vector3[] _sourceCamsWorldXYZArray;
        private Vector2[] _sourceCamsFieldsOfViewArray;
        private Matrix4x4[] _sourceCamsViewMatricesArray;
        private Vector2[] _sourceCamsDistanceRangesArray;
        private ComputeBuffer _sourceCamsWorldXYZBuffer;
        private ComputeBuffer _sourceCamsFieldsOfViewBuffer;
        private ComputeBuffer _sourceCamsViewMatricesBuffer;
        private ComputeBuffer _sourceCamsDistanceRangesBuffer;
        private bool _shouldResetMaterial;
        private List<Camera> _helperULRCameras;

#endregion //FIELDS

#region INHERITANCE_METHODS

        /// <summary>
        /// Resets this object's properties.
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            blendCamCount = 2;
            targetFramerate = 60;
            maxFrameCountForProcessing = 10;
            initialized = false;
            _maxBlendAngle = 180f;
            _resolutionWeight = 0.2f;
            _depthCorrectionFactor = 0.1f;
            _globalTextureMapWeight = 0.1f;
            _optimizeForHighFramerates = true;
        }

        /// <summary>
        /// On any camera being rendered to, add the camera to the list of cameras to which transfer data by way of a helper class.
        /// </summary>
        void OnRenderObject()
        {
            Camera currentCamera = Camera.current;
            if(_helperULRCameras == null)
                _helperULRCameras = new List<Camera>();
            if(!_helperULRCameras.Contains(currentCamera))
            {
                if(PMGlobalMeshEF.globalMesh != null)
                {
                    Helper_ULRCamera helperULRCamera = GeneralToolkit.GetOrAddComponent<Helper_ULRCamera>(currentCamera.gameObject);
                    int totalVertexCount = PMGlobalMeshEF.globalMesh.vertexCount;
                    helperULRCamera.AddRenderedObject(this, totalVertexCount);
                    _helperULRCameras.Add(currentCamera);
                }
            }
            if(initialized)
            {
                // Update the buffers with the parameters of the source cameras.
                FillULRArrays();
                UpdateULRBuffers();
                // Update the blending material with the buffers.
                UpdateBlendingMaterialParameters(ref currentBlendingMaterial);
#if UNITY_EDITOR
                cameraSetup.SetColorIsIndices(ref currentBlendingMaterial);
#endif //UNITY_EDITOR
            }
        }

#endregion //INHERITANCE_METHODS
          
#region METHODS

#if UNITY_EDITOR

        /// <summary>
        /// Enables the user to choose parameters for the blending method.
        /// </summary>
        /// <param name="serializedObject"></param> The serialized object on which to find the properties to modify.
        public void SectionULRBlendingParameters(SerializedObject serializedObject)
        {
            // Enable the user to choose whether to optimize for high framerates.
            string label = "Optimized version:";
            string tooltip = "Method will be optimized for high framerates, slightly reducing visual quality.";
            SerializedProperty propertyOptimize = serializedObject.FindProperty("_optimizeForHighFramerates");
            bool optimizedVersion = EditorGUILayout.Toggle(new GUIContent(label, tooltip), propertyOptimize.boolValue);
            if(optimizedVersion != propertyOptimize.boolValue)
                _shouldResetMaterial = true;
            propertyOptimize.boolValue = optimizedVersion;
            if(optimizedVersion)
            {
                label = "Target framerate:";
                tooltip = "Target framerate (in frames per second) that the optimized ULR method will attempt to reach.";
                SerializedProperty propertyTargetFramerate = serializedObject.FindProperty("targetFramerate");
                propertyTargetFramerate.intValue = EditorGUILayout.IntSlider(new GUIContent(label, tooltip), targetFramerate, 1, 120);
                label = "Max. frame count:";
                tooltip = "Maximum frame count after which all of the mesh's vertices have to be processed.";
                SerializedProperty propertyMaxFrameCount = serializedObject.FindProperty("maxFrameCountForProcessing");
                propertyMaxFrameCount.intValue = EditorGUILayout.IntSlider(new GUIContent(label, tooltip), maxFrameCountForProcessing, 1, 10);
            }
            // Enable the user to choose the number of blended cameras.
            label = "Blend cam. count:";
            tooltip = "Number of source cameras that will have a non-zero blending weight for a given fragment.";
            SerializedProperty propertyBlendCamCount = serializedObject.FindProperty("blendCamCount");
            propertyBlendCamCount.intValue = EditorGUILayout.IntSlider(new GUIContent(label, tooltip), propertyBlendCamCount.intValue, 1, maxBlendCamCount);
            // Enable the user to choose the maximum blending angle.
            label = "Max. blend angle:";
            tooltip = "Maximum angle difference (degrees) between source ray and view ray for the color value to be blended.\n";
            tooltip += "(Note: this value also accounts for resolution difference when relevant.)";
            SerializedProperty propertyMaxBlendAngle = serializedObject.FindProperty("_maxBlendAngle");
            propertyMaxBlendAngle.floatValue = EditorGUILayout.Slider(new GUIContent(label, tooltip), propertyMaxBlendAngle.floatValue, 1f, 180f);
            // Enable the user to choose the relative weight of the resolution factor.
            label = "Res. weight:";
            tooltip = "Relative impact of the {resolution} factor compared to the {angle difference} factor in the ULR algorithm.";
            SerializedProperty propertyResolutionWeight = serializedObject.FindProperty("_resolutionWeight");
            propertyResolutionWeight.floatValue = EditorGUILayout.Slider(new GUIContent(label, tooltip), propertyResolutionWeight.floatValue, 0f, 0.9f);
            // Enable the user to choose the value of the depth correction factor.
            label = "Depth corr.:";
            tooltip = "Depth correction factor to check visibility in each view based on the corresponding depth map.";
            SerializedProperty propertyDepthCorrectionFactor = serializedObject.FindProperty("_depthCorrectionFactor");
            propertyDepthCorrectionFactor.floatValue = EditorGUILayout.Slider(new GUIContent(label, tooltip), propertyDepthCorrectionFactor.floatValue, 0f, 10f);
            // Enable the user to choose the value of the global texture map weight.
            label = "Tex. map weight:";
            tooltip = "Relative weight of the global texture map, compared to the estimated color.";
            SerializedProperty propertyGlobalTextureMapWeight = serializedObject.FindProperty("_globalTextureMapWeight");
            propertyGlobalTextureMapWeight.floatValue = EditorGUILayout.Slider(new GUIContent(label, tooltip), propertyGlobalTextureMapWeight.floatValue, 0f, 1f);
        }

#endif //UNITY_EDITOR

        /// <summary>
        /// Gets the scene representation methods that this blending method is compatible with.
        /// </summary>
        /// <returns></returns> The array of such methods.
        public ProcessingMethod[] GetULRSceneRepresentationMethods()
        {
            return new ProcessingMethod[] { PMColorTextureArray, PMGlobalTextureMap, PMGlobalMeshEF, PMDepthTextureArray };
        }

        /// <summary>
        /// Loads a fitting scene representation from the bundled assets.
        /// </summary>
        /// <returns></returns>
        public IEnumerator LoadSceneRepresentationCoroutine()
        {
            yield return StartCoroutine(PMColorTextureArray.LoadProcessedTextureArrayCoroutine());
            yield return StartCoroutine(PMGlobalTextureMap.LoadProcessedTextureMapsCoroutine());
            yield return StartCoroutine(PMGlobalMeshEF.LoadProcessedGlobalMeshCoroutine());
            yield return StartCoroutine(PMDepthTextureArray.LoadDepthTextureArrayCoroutine());
            numberOfSourceCameras = PMColorTextureArray.colorData.depth;
        }

        /// <summary>
        /// Creates the buffers and arrays used by ULR methods.
        /// </summary>
        public void CreateULRBuffersAndArrays()
        {
            // Create buffers.
            _sourceCamsWorldXYZBuffer = new ComputeBuffer(numberOfSourceCameras, 3 * sizeof(float));
            _sourceCamsFieldsOfViewBuffer = new ComputeBuffer(numberOfSourceCameras, 2 * sizeof(float));
            _sourceCamsDistanceRangesBuffer = new ComputeBuffer(numberOfSourceCameras, 2 * sizeof(float));
            _sourceCamsViewMatricesBuffer = new ComputeBuffer(numberOfSourceCameras, 16 * sizeof(float));
            // Create arrays.
            _sourceCamsWorldXYZArray = new Vector3[numberOfSourceCameras];
            _sourceCamsFieldsOfViewArray = new Vector2[numberOfSourceCameras];
            _sourceCamsDistanceRangesArray = new Vector2[numberOfSourceCameras];
            _sourceCamsViewMatricesArray = new Matrix4x4[numberOfSourceCameras];
        }

        /// <summary>
        /// Resets the blending material.
        /// </summary>
        /// <param name="blendingMaterial"></param> The blending material to modify.
        public void ResetBlendingMaterial(ref Material blendingMaterial)
        {
            if(blendingMaterial != null)
                DestroyImmediate(blendingMaterial);
            if(_optimizeForHighFramerates)
                blendingMaterial = new Material(GeneralToolkit.shaderRenderingULR);
            else
                blendingMaterial = new Material(GeneralToolkit.shaderRenderingULRPerFragment);
            InitializeBlendingMaterialParameters(ref blendingMaterial);
            AssignMaterialToGeometricProxy(ref blendingMaterial);
            currentBlendingMaterial = blendingMaterial;
            _shouldResetMaterial = false;
        }

        /// <summary>
        /// Initializes the parameters of the blending material.
        /// </summary>
        /// <param name="blendingMaterial"></param> The blending material to modify.
        public void InitializeBlendingMaterialParameters(ref Material blendingMaterial)
        {
            blendingMaterial.SetTexture("_ColorData", PMColorTextureArray.colorData); 
            blendingMaterial.SetInt("_SourceCamCount", numberOfSourceCameras);
            blendingMaterial.SetTexture("_DepthData", PMDepthTextureArray.depthData);
            blendingMaterial.SetVector("_DepthDataDimensionsInv", new Vector2(1f/PMDepthTextureArray.depthData.width, 1f/PMDepthTextureArray.depthData.height));
        }
        
        /// <summary>
        /// Assigns the blending material to the instantiated global geometry.
        /// </summary>
        /// <param name="blendingMaterial"></param> The blending material to assign.
        public void AssignMaterialToGeometricProxy(ref Material blendingMaterial)
        {
            MeshRenderer renderer = GeneralToolkit.GetOrAddComponent<MeshRenderer>(PMGlobalMeshEF.globalMeshTransform.gameObject);
            Material[] materials = new Material[PMGlobalMeshEF.globalMesh.subMeshCount];
            for(int i = 0; i < materials.Length; i++)
                materials[i] = blendingMaterial;
            renderer.materials = materials;
            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            for(int i = 0; i < materials.Length; i++)
            {
                propertyBlock.SetTexture("_GlobalTextureMap", PMGlobalTextureMap.textureMaps[i]);
                renderer.SetPropertyBlock(propertyBlock, i);
            }
        }

        /// <summary>
        /// Fills the arrays with the camera setup information.
        /// </summary>
        public void FillULRArrays()
        {
            CameraModel[] cameraModels = cameraSetup.cameraModels;
            for(int sourceCamIndex = 0; sourceCamIndex < numberOfSourceCameras; sourceCamIndex++)
            {
                CameraModel cameraParams = cameraModels[sourceCamIndex];
                _sourceCamsWorldXYZArray[sourceCamIndex] = cameraParams.transform.position;
                _sourceCamsViewMatricesArray[sourceCamIndex] = cameraParams.meshRenderer.worldToLocalMatrix;
                _sourceCamsFieldsOfViewArray[sourceCamIndex] = cameraParams.fieldOfView;
                _sourceCamsDistanceRangesArray[sourceCamIndex] = cameraParams.distanceRange;
            }
        }
        
        /// <summary>
        /// Updates the buffers with the contents of the arrays.
        /// </summary>
        public void UpdateULRBuffers()
        {
            _sourceCamsWorldXYZBuffer.SetData(_sourceCamsWorldXYZArray);
            _sourceCamsFieldsOfViewBuffer.SetData(_sourceCamsFieldsOfViewArray);
            _sourceCamsDistanceRangesBuffer.SetData(_sourceCamsDistanceRangesArray);
            _sourceCamsViewMatricesBuffer.SetData(_sourceCamsViewMatricesArray);
        }

        /// <summary>
        /// Updates the parameters of the blending material.
        /// </summary>
        /// <param name="blendingMaterial"></param> The blending material to modify.
        public void UpdateBlendingMaterialParameters(ref Material blendingMaterial)
        {
            if(_shouldResetMaterial)
                ResetBlendingMaterial(ref blendingMaterial);
            blendingMaterial.SetVector("_LossyScale", transform.lossyScale);
            blendingMaterial.SetInt("_BlendCamCount", blendCamCount);
            blendingMaterial.SetFloat("_MaxBlendAngle", _maxBlendAngle);
            blendingMaterial.SetFloat("_ResolutionWeight", _resolutionWeight);
            blendingMaterial.SetFloat("_DepthCorrectionFactor", _depthCorrectionFactor);
            blendingMaterial.SetFloat("_GlobalTextureMapWeight", _globalTextureMapWeight);
            blendingMaterial.SetBuffer("_SourceCamsWorldXYZBuffer", _sourceCamsWorldXYZBuffer);
            blendingMaterial.SetBuffer("_SourceCamsFieldsOfViewBuffer", _sourceCamsFieldsOfViewBuffer);
            blendingMaterial.SetBuffer("_SourceCamsViewMatricesBuffer", _sourceCamsViewMatricesBuffer);
            blendingMaterial.SetBuffer("_SourceCamsDistanceRangesBuffer", _sourceCamsDistanceRangesBuffer);
        }

        /// <summary>
        /// Clears all created objects.
        /// </summary>
        public void ClearAll()
        {
            if(_sourceCamsWorldXYZBuffer != null)
                _sourceCamsWorldXYZBuffer.Release();
            if(_sourceCamsFieldsOfViewBuffer != null)
                _sourceCamsFieldsOfViewBuffer.Release();
            if(_sourceCamsDistanceRangesBuffer != null)
                _sourceCamsDistanceRangesBuffer.Release();
            if(_sourceCamsViewMatricesBuffer != null)
                _sourceCamsViewMatricesBuffer.Release();
            if(_helperULRCameras != null && _helperULRCameras.Count > 0)
            {
                for(int iter = 0; iter < _helperULRCameras.Count; iter++)
                {
                    Camera currentCamera = _helperULRCameras[iter];
                    if(currentCamera != null)
                    {
                        Helper_ULRCamera helperULRCamera = currentCamera.GetComponent<Helper_ULRCamera>();
                        if(helperULRCamera != null)
                            DestroyImmediate(helperULRCamera);
                    }
                }
                _helperULRCameras.Clear();
            }
        }

#endregion //METHODS

    }

}
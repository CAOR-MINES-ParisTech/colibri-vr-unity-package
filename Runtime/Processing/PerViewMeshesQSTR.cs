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
    /// Class that implements the Per-View Meshes by Quadtree Simplification and Triangle Removal geometry processing method.
    /// This method converts per-view depth map data (stored as images in a specific folder) to meshes, based on quadtree simplification and triangle removal at depth edges.
    /// </summary>
    [System.Serializable]
    public class PerViewMeshesQSTR : ProcessingMethod
    {

#region CONST_FIELDS

        public const string perViewMeshAssetPrefix = "PerViewMeshAsset";

#endregion //CONST_FIELDS

#region STATIC_METHODS

        /// <summary>
        /// Corrects a given pixel resolution to account for memory limitations.
        /// </summary>
        /// <param name="depthMapPixelResolution"></param> The pixel resolution to modify.
        public static void CorrectForMemorySize(ref Vector2Int depthMapPixelResolution)
        {
            int maxPixelResolution = 2048;
            depthMapPixelResolution = new Vector2Int(Mathf.Min(maxPixelResolution, depthMapPixelResolution.x), Mathf.Min(maxPixelResolution, depthMapPixelResolution.y));
        }

#endregion //STATIC_METHODS

#region FIELDS

        public Texture2D distanceMap;
        public CameraModel cameraModel;
        public Mesh[] perViewMeshes;
        public Transform[] perViewMeshTransforms;

        [SerializeField] private bool _project3D;
        [SerializeField] private float _triangleErrorThreshold;
        [SerializeField] private bool _removeBackground;
        [SerializeField] private int _disocclusionHandlingIndex;
        [SerializeField] private float _orthogonalityParameter;
        [SerializeField] private float _triangleSizeParameter;

        private Vector2Int _correctedPixelResolution;
        private int _equivalentPoTResolution;
        private static ComputeShader _computeShader;
        private static int _quadtreeDepth;
        private static int _quadtreeBufferSize;
        private static int _viewXYZBufferSize;
        private static int _vertexBufferSize;
        private static int _triangleBufferSize;
        private static ComputeBuffer _quadtreeBuffer;
        private static ComputeBuffer _viewXYZBuffer;
        private static ComputeBuffer _vertexBuffer;
        private static ComputeBuffer _uvBuffer;
        private static ComputeBuffer _triangleBuffer;
        private static ComputeBuffer _accumulatedErrorBuffer;
        private static Vector4[] _quadtreeData;
        private static Vector3[] _viewXYZData;
        private static Vector3[] _vertexData;
        private static Vector2[] _uvData;
        private static Vector3[] _triangleData;
        private static float[] _accumulatedErrorData;

#endregion //FIELDS

#if UNITY_EDITOR

#region INHERITANCE_METHODS

        /// <inheritdoc/>
        public override void Reset()
        {
            base.Reset();
            _project3D = true;
            _triangleErrorThreshold = 0.1f;
            _removeBackground = false;
            _disocclusionHandlingIndex = 1;
            _orthogonalityParameter = 0.1f;
            _triangleSizeParameter = 0.1f;
        }

        /// <inheritdoc/>
        public override bool IsCompatible(int colorDataCount, int depthDataCount, int meshDataCount)
        {
            // Indicate that this method is available only for processing one or more depth maps.
            return (depthDataCount > 0);
        }

        /// <inheritdoc/>
        public override bool IsGUINested()
        {
            return false;
        }

        /// <inheritdoc/>
        public override GUIContent GetGUIInfo()
        {
            string label = "Per-view meshes from depth maps by quadtree simplification and triangle removal";
            string tooltip = "Each depth map will be converted into a per-view mesh asset, using a method based on quadtree mesh simplification and triangle removal at depth edges.";
            if(!GUI.enabled)
                tooltip = "Prerequisites are not satisfied.\nPrerequisites: per-view depth maps, as individual files or texture array.";
            return new GUIContent(label, tooltip);
        }

        /// <inheritdoc/>
        public override GUIContent GetProcessedDataName()
        {
            string label = "Per-view depth map meshes";
            string tooltip = GetGUIInfo().text;
            return new GUIContent(label, tooltip);
        }

        /// <inheritdoc/>
        protected override IEnumerator ExecuteMethodCoroutine()
        {
            // Reset the progress bar.
            GeneralToolkit.ResetCancelableProgressBar(true, false);
            // Initialize the compute shader's properties.
            InitializePerCall();
            // Create a mesh for each source depth map.
            perViewMeshes = new Mesh[cameraSetup.cameraModels.Length];
            for(int sourceIndex = 0; sourceIndex < perViewMeshes.Length; sourceIndex++)
            {
                // Update the progress bar, and enable the user to cancel the process.
                DisplayAndUpdateCancelableProgressBar();
                if(GeneralToolkit.progressBarCanceled)
                {
                    processingCaller.processingCanceled = true;
                    break;
                }
                // Process the depth map.
                ProcessDepthImage(sourceIndex);
                yield return null;
            }
            //Releases the compute shader.
            ReleasePerCall();
            // Reset the progress bar.
            GeneralToolkit.ResetCancelableProgressBar(true, false);
        }

        /// <inheritdoc/>
        public override void DeactivateIncompatibleProcessingMethods()
        {
            PMPerViewMeshesQSTRDTA.shouldExecute = false;
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
            // Enable the user to choose a triangle error threshold.
            string label = "Error threshold: ";
            string tooltip = "Triangle error threshold (meters).";
            SerializedProperty propertyTriangleError = serializedObject.FindProperty("_triangleErrorThreshold");
            propertyTriangleError.floatValue = EditorGUILayout.Slider(new GUIContent(label, tooltip), propertyTriangleError.floatValue, 0f, 1f);
            // Enable the user to choose how to handle disocclusion edges.
            label = "Disocc. edges: ";
            tooltip = "How to handle disocclusion edges (i.e. prevent the display of \"rubber sheet\" triangles linking foreground and background).";
            SerializedProperty propertyDisocclusionHandlingIndex = serializedObject.FindProperty("_disocclusionHandlingIndex");
            GUIContent[] options = new GUIContent[2];
            options[0] = new GUIContent("No handling", "Disocclusion edges will be left untouched.");
            options[1] = new GUIContent("Remove edge triangles", "Disocclusion edge triangles will be removed.");
            propertyDisocclusionHandlingIndex.intValue = EditorGUILayout.Popup(new GUIContent(label, tooltip), propertyDisocclusionHandlingIndex.intValue, options);
            // If the depth edges are to be removed, enable the user to modify additional parameters.
            if(propertyDisocclusionHandlingIndex.intValue == 1)
            {
                // Enable the user to choose the value of the orthogonality parameter for the triangle removal step.
                label = "Orthog. param.: ";
                tooltip = "Orthogonality parameter, that prevents the display of triangles that face away from the acquisition camera.";
                SerializedProperty propertyOrthogonalityParameter = serializedObject.FindProperty("_orthogonalityParameter");
                propertyOrthogonalityParameter.floatValue = EditorGUILayout.Slider(new GUIContent(label, tooltip), propertyOrthogonalityParameter.floatValue, 0f, 1f);
                // Enable the user to choose the value of the triangle size parameter for the triangle removal step.
                label = "Size param.: ";
                tooltip = "Triangle size parameter, that excludes triangles from being discarded if they are small enough.";
                SerializedProperty propertyTriangleSizeParameter = serializedObject.FindProperty("_triangleSizeParameter");
                propertyTriangleSizeParameter.floatValue = EditorGUILayout.Slider(new GUIContent(label, tooltip), propertyTriangleSizeParameter.floatValue, 0f, 1f);
            }
            // Enable the user to choose whether to remove the depth map's background or integrate it in the mesh.
            label = "Hide background: ";
            tooltip = "Whether to remove the background from the generated mesh.";
            SerializedProperty propertyRemoveBackground = serializedObject.FindProperty("_removeBackground");
            propertyRemoveBackground.boolValue = EditorGUILayout.Toggle(new GUIContent(label, tooltip), propertyRemoveBackground.boolValue);
            serializedObject.ApplyModifiedProperties();
        }

#endregion //INHERITANCE_METHODS

#endif //UNITY_EDITOR

#region METHODS

#if UNITY_EDITOR

        /// <summary>
        /// Displays and updates a cancelable progress bar informing on the per-view geometry generation process.
        /// </summary>
        public void DisplayAndUpdateCancelableProgressBar()
        {
            string progressBarTitle = "COLIBRI VR - " + GetProcessedDataName().text;
            string progressBarInfo = "Generating per-view mesh";
            processingCaller.DisplayAndUpdateCancelableProgressBar(progressBarTitle, progressBarInfo);
        }

        /// <summary>
        /// Initializes properties (to be used once per call to the processing method).
        /// </summary>
        public void InitializePerCall()
        {
            _computeShader = GeneralToolkit.computeShaderPerViewMeshesQSTR;
            _computeShader.SetFloat("_TriangleErrorThreshold", _triangleErrorThreshold);
            _computeShader.SetInt("_MeshProjectionType", _project3D ? 1 : 0);
            _computeShader.SetInt("_DisocclusionHandlingType", _disocclusionHandlingIndex==1 ? 1 : 0);
            _computeShader.SetInt("_RemoveBackground", _removeBackground ? 1 : 0);
            _computeShader.SetFloat("_OrthogonalityParameter", _orthogonalityParameter);
            _computeShader.SetFloat("_TriangleSizeParameter", _triangleSizeParameter);
        }

        /// <summary>
        /// Releases properties (to be used once per call to the processing method.)
        /// </summary>
        public void ReleasePerCall()
        {
            GeneralToolkit.UnloadAsset(_computeShader);
        }

        /// <summary>
        /// Initializes the distance map as a Texture2D.
        /// </summary>
        public void InitializeDistanceMap()
        {
            distanceMap = new Texture2D(1,1);
            _correctedPixelResolution = cameraModel.pixelResolution;
            CorrectForMemorySize(ref _correctedPixelResolution);
            GeneralToolkit.CreateTexture2D(ref distanceMap, _correctedPixelResolution, TextureFormat.ARGB32, true, FilterMode.Point, TextureWrapMode.Clamp, false);
        }

        /// <summary>
        /// Releases the distance map.
        /// </summary>
        public void ReleaseDistanceMap()
        {
            if(distanceMap != null)
                DestroyImmediate(distanceMap);
        }

        /// <summary>
        /// Initializes properties (to be used once for each mesh that is generated).
        /// </summary>
        private void InitializePerMesh()
        {
            // Send the current camera model's parameters to the compute shader.
            _equivalentPoTResolution = Mathf.NextPowerOfTwo(Mathf.Max(_correctedPixelResolution.x, _correctedPixelResolution.y));
            _computeShader.SetInt("_EquivalentPowerofTwoResolution", _equivalentPoTResolution);
            _computeShader.SetInts("_PixelResolution", _correctedPixelResolution.x, _correctedPixelResolution.y);
            _computeShader.SetFloats("_FieldOfView", cameraModel.fieldOfView.x, cameraModel.fieldOfView.y);
            _computeShader.SetFloats("_DistanceRange", cameraModel.distanceRange.x, cameraModel.distanceRange.y);
            // Compute buffer sizes.
            _quadtreeDepth = (int)Mathf.Log(_equivalentPoTResolution, 2) + 1;
            _quadtreeBufferSize = (int)((4 * _equivalentPoTResolution * _equivalentPoTResolution - 1) / 3f);
            _viewXYZBufferSize = (int)Mathf.Pow(2 * _equivalentPoTResolution + 1, 2);
            _vertexBufferSize = (int)(Mathf.Pow(_equivalentPoTResolution + 1, 2) + Mathf.Pow(_equivalentPoTResolution, 2));
            _triangleBufferSize = 4 * _quadtreeBufferSize;
            // Initialize the Quadtree buffer and array.
            _quadtreeData = new Vector4[_quadtreeBufferSize];
            _quadtreeBuffer = new ComputeBuffer(_quadtreeData.Length, 4 * sizeof(float));
            _quadtreeBuffer.SetData(_quadtreeData);
            // Initialize the ViewXYZ buffer and array.
            _viewXYZData = new Vector3[_viewXYZBufferSize];
            _viewXYZBuffer = new ComputeBuffer(_viewXYZData.Length, 3 * sizeof(float));
            _viewXYZBuffer.SetData(_viewXYZData);
            // Initialize the Vertex buffer and array.
            _vertexData = new Vector3[_vertexBufferSize];
            for(int i = 0; i < _vertexData.Length; i++)
                _vertexData[i] = -1f * Vector3.one;
            _vertexBuffer = new ComputeBuffer(_vertexData.Length, 3 * sizeof(float));
            _vertexBuffer.SetData(_vertexData);
            // Initialize the UV buffer and array.
            _uvData = new Vector2[_vertexBufferSize];
            for(int i = 0; i < _uvData.Length; i++)
                _uvData[i] = -1f * Vector2.one;
            _uvBuffer = new ComputeBuffer(_uvData.Length, 2 * sizeof(float));
            _uvBuffer.SetData(_uvData);
            // Initialize the Triangle buffer and array.
            _triangleData = new Vector3[_triangleBufferSize];
            for(int i = 0; i < _triangleData.Length; i++)
                _triangleData[i] = -1f * Vector3.one;
            _triangleBuffer = new ComputeBuffer(_triangleData.Length, 3 * sizeof(float), ComputeBufferType.Append);
            _triangleBuffer.SetCounterValue(0);
            _triangleBuffer.SetData(_triangleData);
            // Initialize the AccumulatedError buffer and array.
            _accumulatedErrorData = new float[_quadtreeBufferSize];
            _accumulatedErrorBuffer = new ComputeBuffer(_accumulatedErrorData.Length, 1 * sizeof(float));
            _accumulatedErrorBuffer.SetCounterValue(0);
            _accumulatedErrorBuffer.SetData(_accumulatedErrorData);
        }

        /// <summary>
        /// Computes the number of blocks per row and per column for a given LOD level.
        /// </summary>
        /// <param name="lod"></param> The current LOD level.
        /// <returns></returns> The number of blocks for this LOD level.
        private Vector2 GetNumberOfBlocksPerRowAndColumn(int lod)
        {
            return new Vector2(_correctedPixelResolution.x, _correctedPixelResolution.y) / Mathf.Pow(2, lod);
        }

        /// <summary>
        /// Computes the values of the Quadtree and ViewXYZ buffers, by iterating on the level of detail from fine to coarse.
        /// </summary>
        private void ComputeQuadtreeValues()
        {
            // Send the buffers and texture to the compute shader at the appropriate kernel index.
            int kernelIndex = _computeShader.FindKernel("ComputeQuadtreeValues");
            _computeShader.SetBuffer(kernelIndex, "_QuadtreeBuffer", _quadtreeBuffer);
            _computeShader.SetBuffer(kernelIndex, "_ViewXYZBuffer", _viewXYZBuffer);
            _computeShader.SetTexture(kernelIndex, "_DistanceMap", distanceMap);
            // Iterate on the level of detail, from fine to coarse.
            for(int lod = 0; lod < _quadtreeDepth; lod++)
            {
                _computeShader.SetInt("_CurrentLOD", lod);
                Vector2 threadGroupXY = GetNumberOfBlocksPerRowAndColumn(lod) / 8f;
                _computeShader.Dispatch(kernelIndex, Mathf.CeilToInt(threadGroupXY.x), Mathf.CeilToInt(threadGroupXY.y), 1);
            }
        }

        /// <summary>
        /// Computes, for each block, whether and how the block should split, based on the previously computed error values.
        /// This process fills the Vertex, UV, and Triangle buffers, by iterating on the level of detail from coarse to fine.
        /// </summary>
        private void ComputeBlockSplits()
        {
            // Send the buffers and texture to the compute shader at the appropriate kernel index.
            int kernelIndex = _computeShader.FindKernel("ComputeBlockSplits");
            _computeShader.SetBuffer(kernelIndex, "_QuadtreeBuffer", _quadtreeBuffer);
            _computeShader.SetBuffer(kernelIndex, "_ViewXYZBuffer", _viewXYZBuffer);
            _computeShader.SetBuffer(kernelIndex, "_VertexBuffer", _vertexBuffer);
            _computeShader.SetBuffer(kernelIndex, "_UVBuffer", _uvBuffer);
            _computeShader.SetBuffer(kernelIndex, "_TriangleAppendBuffer", _triangleBuffer);
            _computeShader.SetBuffer(kernelIndex, "_AccumulatedErrorBuffer", _accumulatedErrorBuffer);
            _computeShader.SetTexture(kernelIndex, "_DistanceMap", distanceMap);
            // Iterate on the level of detail, from coarse to fine.
            for(int lod = _quadtreeDepth - 1; lod >= 0; lod--)
            {
                _computeShader.SetInt("_CurrentLOD", lod);
                Vector2 threadGroupXY = GetNumberOfBlocksPerRowAndColumn(lod) / 8f;
                _computeShader.Dispatch(kernelIndex, Mathf.CeilToInt(threadGroupXY.x), Mathf.CeilToInt(threadGroupXY.y), 1);
            }
            // Transfer the output data from buffers to arrays.
            _vertexBuffer.GetData(_vertexData);
            _uvBuffer.GetData(_uvData);
            _triangleBuffer.GetData(_triangleData);
            _quadtreeBuffer.GetData(_quadtreeData);
            _accumulatedErrorBuffer.GetData(_accumulatedErrorData);
        }

        /// <summary>
        /// Creates the mesh from the computed Vertex, UV and Triangle arrays.
        /// </summary>
        /// <param name="outputMesh"></param> The output mesh generated at the end of the process.
        private void CreateMesh(out Mesh outputMesh)
        {
            // Prepare lists for the mesh's vertices, UVs, and triangles.
            List<Vector3> meshVertexList = new List<Vector3>();
            List<Vector2> meshUVList = new List<Vector2>();
            List<int> meshTriangleList = new List<int>();
            // Add vertices and UVs to the lists, and record the order in which they are added.
            int[] vertexOrderArray = new int[_vertexData.Length];
            int count = 0;
            for(int i = 0; i < _vertexData.Length; i++)
            {
                if(_vertexData[i].x != -1)
                {
                    meshVertexList.Add(_vertexData[i]);
                    meshUVList.Add(_uvData[i]);
                    vertexOrderArray[i] = count;
                    count++;
                }
            }
            // Add triangles to the list. The vertex order array provides the right indices for each vertex.
            for(int i = 0; i < _triangleData.Length; i++)
            {
                if(_triangleData[i].x != -1)
                {
                    meshTriangleList.Add(vertexOrderArray[(int)_triangleData[i].x]);
                    meshTriangleList.Add(vertexOrderArray[(int)_triangleData[i].y]);
                    meshTriangleList.Add(vertexOrderArray[(int)_triangleData[i].z]);
                }
            }
            // Create a mesh from the lists' contents.
            outputMesh = new Mesh();
            outputMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            outputMesh.vertices = meshVertexList.ToArray();
            outputMesh.uv = meshUVList.ToArray();
            outputMesh.triangles = meshTriangleList.ToArray();
            outputMesh.RecalculateNormals();
            outputMesh.RecalculateBounds();
        }

        /// <summary>
        /// Releases the different buffers that were created during the mesh generation process (to be used once per generated mesh).
        /// </summary>
        private void ReleasePerMesh()
        {
            _quadtreeBuffer.Release();
            _viewXYZBuffer.Release();
            _vertexBuffer.Release();
            _uvBuffer.Release();
            _triangleBuffer.Release();
            _accumulatedErrorBuffer.Release();
            _quadtreeData = null;
            _viewXYZData = null;
            _vertexData = null;
            _uvData = null;
            _triangleData = null;
            _accumulatedErrorData = null;
        }

        /// <summary>
        /// Computes the compression achieved during mesh creation.
        /// </summary>
        /// <param name="compressedTriangleCount"></param> The triangle count of the output mesh.
        /// <returns></returns> Mesh compression info (triangle counts, compression ratio, accumulated error).
        private string[] ComputeCompression(int compressedTriangleCount)
        {
            int originalTriangleCount = 4 * _correctedPixelResolution.x * _correctedPixelResolution.y;
            // Compute the compression ratio.
            float compressionRatioFloat = originalTriangleCount * 1f / compressedTriangleCount;
            int compressionRatio = Mathf.RoundToInt(compressionRatioFloat);
            // Compute the space savings.
            float spaceSavings = 100f * (1 - 1f / compressionRatioFloat);
            // Get the accumulated error from the array.
            float accumulatedError = 0f;
            for(int i = 0; i < _accumulatedErrorData.Length; i++)
                accumulatedError += _accumulatedErrorData[i];
            accumulatedError /= compressedTriangleCount;
            // Return the compression information as a text string.
            string[] compressionInfo = new string[3];
            compressionInfo[0] = "Triangle count: " + compressedTriangleCount.ToString() + " instead of " + originalTriangleCount.ToString();
            compressionInfo[1] = "Compression: " + compressionRatio.ToString() + ":1 compression ratio (" + spaceSavings.ToString("F3") + "%)";
            compressionInfo[2] = "Average error per triangle: " + accumulatedError.ToString("F3");
            return compressionInfo;
        }

        /// <summary>
        /// Main method, that computes a mesh from a given depth map.
        /// </summary>
        /// <param name="outputMesh"></param> The output mesh generated at the end of the process.
        /// <returns></returns> The mesh compression data, as a text string.
        public string[] ComputeMesh(out Mesh outputMesh)
        {
            // Initialize the properties, buffers and arrays.
            InitializePerMesh();
            // Compute the values of the Quadtree buffer.
            ComputeQuadtreeValues();
            // Compute the values of the Vertex, UV, and Triangles buffers.
            ComputeBlockSplits();
            // Create a mesh from the computed values.
            CreateMesh(out outputMesh);
            // Compute the compression information.
            string[] compressionInfo = ComputeCompression(outputMesh.triangles.Length / 3);
            // Destroy all objects created for this step.
            ReleasePerMesh();
            ReleaseDistanceMap();
            // Return the compression information.
            return compressionInfo;
        }

        /// <summary>
        /// Creates mesh assets for the depth image specified by the given index.
        /// </summary>
        /// <param name="acquisitionIndex"></param> The index of the depth image.
        /// <returns></returns>
        private void ProcessDepthImage(int acquisitionIndex)
        {
            // Check if the asset has already been processed.
            string bundledAssetName = dataHandler.GetBundledAssetName(this, perViewMeshAssetPrefix + GeneralToolkit.ToString(acquisitionIndex));
            string meshRelativePath = Path.Combine(GeneralToolkit.tempDirectoryRelativePath, bundledAssetName + ".asset");
            if(dataHandler.IsAssetAlreadyProcessed(meshRelativePath))
                return;
            // Update the camera model.
            cameraModel = cameraSetup.cameraModels[acquisitionIndex];
            // Initialize the distance map texture, and load the depth data into it.
            InitializeDistanceMap();
            string imageName = cameraModel.imageName;
            string imagePath = Path.Combine(dataHandler.depthDirectory, imageName);
            GeneralToolkit.LoadTexture(imagePath, ref distanceMap);
            // Compute a mesh from the distance map.
            Mesh outMesh;
            ComputeMesh(out outMesh);
            // Save this mesh as an asset.
            AssetDatabase.CreateAsset(outMesh, meshRelativePath);
            AssetDatabase.Refresh();
            // Store the per-view mesh into the final array.
            Mesh meshAsset = AssetDatabase.LoadAssetAtPath<Mesh>(meshRelativePath);
            perViewMeshes[acquisitionIndex] = (Mesh)Instantiate(meshAsset);
        }

#endif //UNITY_EDITOR

        /// <summary>
        /// Loads the processed per-view meshes for play.
        /// </summary>
        /// <returns></returns>
        public IEnumerator LoadProcessedPerViewMeshesCoroutine()
        {
            CameraModel[] cameraModels = cameraSetup.cameraModels;
            perViewMeshes = new Mesh[cameraModels.Length];
            perViewMeshTransforms = new Transform[cameraModels.Length];
            // Check that the application is playing.
            if(!Application.isPlaying)
                yield break;
            // Only continue if there are source images.
            if(cameraModels.Length > 0)
            {
                // Create a gameobject for the geometric data, and set it as a child of this transform.
                Transform geometricDataTransform = new GameObject("Geometric data").transform;
                geometricDataTransform.parent = dataHandler.transform;
                // Reset the geometric data's local position, rotation, and scale, to fit that of the parent object.
                GeneralToolkit.SetTransformValues(geometricDataTransform.transform, true, Vector3.zero, Quaternion.identity, Vector3.one);
                // Check each bundled asset name for the prefix corresponding to this class.
                string bundledAssetsPrefix = DataHandler.GetBundledAssetPrefixFromType(typeof(PerViewMeshesQSTR));
                foreach(string bundledAssetName in dataHandler.bundledAssetsNames)
                {
                    // If the correct asset name is found, load the per-view mesh.
                    if(bundledAssetName.Contains(bundledAssetsPrefix))
                    {
                        Mesh processedPerViewMesh = new Mesh();
                        yield return dataHandler.StartCoroutine(dataHandler.LoadAssetsFromBundleCoroutine<Mesh>((result => processedPerViewMesh = result[0]), bundledAssetName));
                        // Create a gameobject for the mesh, and set it as a child of the geometric data.
                        Transform meshTransform = new GameObject(processedPerViewMesh.name).transform;
                        meshTransform.parent = geometricDataTransform;
                        // Link the per-view mesh to the gameobject.
                        meshTransform.gameObject.AddComponent<MeshFilter>().sharedMesh = processedPerViewMesh;
                        // Determine the source camera index.
                        string assetName = bundledAssetName.Replace(bundledAssetsPrefix, string.Empty);
                        string sourceCamIndexString = assetName.Replace(perViewMeshAssetPrefix, string.Empty);
                        int sourceCamIndex = GeneralToolkit.ParseInt(sourceCamIndexString);
                        // Determine the mesh's transform values from the camera model.
                        CameraModel cameraModel = cameraModels[sourceCamIndex];
                        Vector3 meshPosition = cameraModel.transform.position;
                        Quaternion meshRotation = cameraModel.transform.rotation;
                        Vector3 meshScale = Vector3.one;
                        // Set the mesh's transform to the computed values.
                        GeneralToolkit.SetTransformValues(meshTransform, false, meshPosition, meshRotation, meshScale);
                        // Assign to the output arrays.
                        perViewMeshes[sourceCamIndex] = processedPerViewMesh;
                        perViewMeshTransforms[sourceCamIndex] = meshTransform;
                    }
                }
            }
        }

#endregion //METHODS

    }

}

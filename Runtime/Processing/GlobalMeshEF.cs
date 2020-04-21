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
    /// Class that implements the Global Mesh from Existing File geometry processing method.
    /// This method converts an existing mesh file into an asset, and stores it into the asset bundle.
    /// </summary>
    public class GlobalMeshEF : ProcessingMethod
    {

#region CONST_FIELDS

        public const string globalMeshAssetName = "GlobalMesh";

        private const string _propertyNameGlobalMeshPathAbsolute = "_globalMeshPathAbsolute";

#endregion //CONST_FIELDS

#region FIELDS

        public Mesh globalMesh;
        public Transform globalMeshTransform;

        [SerializeField] private string _globalMeshPathAbsolute;

#endregion //FIELDS

#if UNITY_EDITOR

#region INHERITANCE_METHODS

        /// <inheritdoc/>
        public override void InitializeLinks()
        {
            base.InitializeLinks();
            // Enable the user to choose whether or not to generate a depth texture array from the created global mesh, and to choose whether or not to generate texture maps for the global mesh.
            _nestedMethodsToDisplay = new ProcessingMethod[] { PMDepthTextureArray, PMGlobalTextureMap };
        }

        /// <inheritdoc/>
        public override bool IsCompatible(int colorDataCount, int depthDataCount, int meshDataCount)
        {
            // Indicate that this method is available only for processing one or more meshes.
            return (meshDataCount > 0);
        }

        /// <inheritdoc/>
        public override bool IsGUINested()
        {
            return false;
        }

        /// <inheritdoc/>
        public override GUIContent GetGUIInfo()
        {
            string label = "Global mesh from existing file";
            string tooltip = "An existing global mesh file will be exported as an asset.";
            if(!GUI.enabled)
                tooltip = "Prerequisites are not satisfied.\nPrerequisites: global mesh file (.OBJ, .FBX).";
            return new GUIContent(label, tooltip);
        }

        /// <inheritdoc/>
        public override GUIContent GetProcessedDataName()
        {
            string label = "Global mesh";
            string tooltip = GetGUIInfo().text;
            return new GUIContent(label, tooltip);
        }

        /// <inheritdoc/>
        protected override IEnumerator ExecuteMethodCoroutine()
        {
            // If the mesh to store does not exist, exit.
            if(_globalMeshPathAbsolute == string.Empty || !File.Exists(_globalMeshPathAbsolute))
            {
                Debug.LogError(GeneralToolkit.FormatScriptMessage(typeof(GlobalMeshEF), "Global mesh was not found at path: " + _globalMeshPathAbsolute + "."));
                yield break;
            }
            // Save the mesh as an asset.
            yield return StartCoroutine(SaveGlobalMeshAsAssetCoroutine());
            // If activated, compute a texture array of depth maps, corresponding to each source camera viewing the mesh.
            if(PMDepthTextureArray.shouldExecute)
                yield return StartCoroutine(PMDepthTextureArray.ExecuteAndDisplayLog());
            // If activated, compute a global texture map for the mesh based on the source color date.
            if(PMGlobalTextureMap.shouldExecute)
                yield return StartCoroutine(PMGlobalTextureMap.ExecuteAndDisplayLog());
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
            SerializedProperty propertyGlobalMeshPath = serializedObject.FindProperty(_propertyNameGlobalMeshPathAbsolute);
            // Enable the user to select another mesh by modifying the path.
            if(string.IsNullOrEmpty(_globalMeshPathAbsolute))
            {
                string[] extensionsArray = new string[] {".asset", ".obj",".fbx"};
                FileInfo[] files = GeneralToolkit.GetFilesByExtension(dataHandler.dataDirectory, extensionsArray);
                if(files.Length > 0)
                {
                    _globalMeshPathAbsolute = files[0].FullName;
                }
            }
            string searchTitle = "Select global mesh";
            string tooltip = "Select the global mesh to use for rendering.";
            string extensions = "FBX,fbx,OBJ,obj,ASSET,asset";
            string outPath;
            bool clicked;
            GeneralToolkit.EditorPathSearch(out clicked, out outPath, PathType.File, _globalMeshPathAbsolute, searchTitle, tooltip, Color.grey, true, extensions);
            propertyGlobalMeshPath.stringValue = outPath;
            serializedObject.ApplyModifiedProperties();
        }

#endregion //INHERITANCE_METHODS

#endif //UNITY_EDITOR

#region METHODS

#if UNITY_EDITOR

        /// <summary>
        /// Coroutine that saves the specified global mesh asset into the asset bundle.
        /// </summary>
        /// <returns></returns>
        private IEnumerator SaveGlobalMeshAsAssetCoroutine()
        {
            // Set the destination paths to a temporary asset folder.
            string bundledAssetName = GetBundledAssetName(globalMeshAssetName);
            string assetPathAbsolute = GetAssetPathAbsolute(bundledAssetName);
            string assetPathRelative = GetAssetPathRelative(bundledAssetName);
            // Check if the asset has already been processed.
            if(!dataHandler.IsAssetAlreadyProcessed(assetPathRelative))
            {
                // If the mesh to store is already an asset, move it to the asset bundle path.
                string dstExtension = Path.GetExtension(_globalMeshPathAbsolute);
                if(dstExtension == ".asset")
                {
                    // Copy the asset to the destination path.
                    GeneralToolkit.Replace(PathType.File, _globalMeshPathAbsolute, assetPathAbsolute);
                    // Refresh the asset database.
                    AssetDatabase.Refresh();
                }
                // Otherwise, the mesh first has to be converted into an asset.
                else
                {
                    // Copy the mesh to the resources folder.
                    string dstFullPath = GeneralToolkit.CopyObjectFromPathIntoResources(_globalMeshPathAbsolute);
                    yield return null;
                    // Make the mesh readable so that colliders can be added.
                    GeneralToolkit.MakeMeshReadable(dstFullPath);
                    // Load the mesh from resources.
                    Mesh loadedMesh = Resources.Load<Mesh>(Path.GetFileNameWithoutExtension(dstFullPath));
                    // Copy the mesh by instantiating it.
                    globalMesh = (Mesh)Instantiate(loadedMesh);
                    // Recalculate the mesh's normals and bounds.
                    globalMesh.RecalculateNormals();
                    globalMesh.RecalculateBounds();
                    // Create an asset from the copied mesh.
                    AssetDatabase.CreateAsset(globalMesh, assetPathRelative);
                    AssetDatabase.Refresh();
                    // Delete the mesh that was copied into the resources folder. 
                    GeneralToolkit.Delete(dstFullPath);
                }
            }
            Mesh meshAsset = AssetDatabase.LoadAssetAtPath<Mesh>(assetPathRelative);
            globalMesh = (Mesh)Instantiate(meshAsset);
        }

#endif //UNITY_EDITOR

        /// <summary>
        /// Loads the processed global mesh with per-view depth for play.
        /// </summary>
        /// <returns></returns>
        public IEnumerator LoadProcessedGlobalMeshCoroutine()
        {
            globalMesh = null;
            globalMeshTransform = null;
            // Check that the application is playing.
            if(!Application.isPlaying)
                yield break;
            // Check each bundled asset name for the prefix corresponding to this class.
            string bundledAssetsPrefix = DataHandler.GetBundledAssetPrefixFromType(typeof(GlobalMeshEF));
            foreach(string bundledAssetName in dataHandler.bundledAssetsNames)
            {
                string assetName = bundledAssetName.Replace(bundledAssetsPrefix, string.Empty);
                // If the correct asset name is found, load the global mesh.
                if(assetName == globalMeshAssetName)
                {
                    yield return dataHandler.StartCoroutine(dataHandler.LoadAssetsFromBundleCoroutine<Mesh>((result => globalMesh = result[0]), bundledAssetName));
                }
            }
            // If the mesh data was found, initialize a transform in the scene with a mesh filter to contain it.
            if(globalMesh != null)
            {
                // Create a gameobject for the geometric data, and set it as a child of this transform.
                Transform geometricDataTransform = new GameObject("Geometric data").transform;
                geometricDataTransform.parent = dataHandler.transform;
                // Create a gameobject for the mesh, and set it as a child of the geometric data.
                globalMeshTransform = new GameObject(globalMesh.name).transform;
                globalMeshTransform.parent = geometricDataTransform;
                // Link the global mesh to the gameobject.
                globalMeshTransform.gameObject.AddComponent<MeshFilter>().sharedMesh = globalMesh;
                // Add a mesh collider.
                if(dataHandler.generateColliders)
                    globalMeshTransform.gameObject.AddComponent<MeshCollider>();
                // Reset the geometric data's local position, rotation, and scale, to fit that of the parent object.
                GeneralToolkit.SetTransformValues(geometricDataTransform.transform, true, Vector3.zero, Quaternion.identity, Vector3.one);
            }
        }

#endregion //METHODS

    }

}

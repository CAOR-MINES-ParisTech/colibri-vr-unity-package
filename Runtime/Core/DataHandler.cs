/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using UnityEngine;
using UnityEditor;
using COLIBRIVR.ExternalConnectors;

namespace COLIBRIVR
{
    /// <summary>
    /// Class that defines the file and folder structure, and enables objects to access data using this structure.
    /// </summary>
    public class DataHandler : MonoBehaviour
    {

#region CONST_FIELDS

        public const string bundleName = "BundleName";
        public const string processingInfoHeader = "# Processing information file";
        public const string processingInfoSuccessfulBundle = "# These assets were successfully bundled.";
        public const string assemblyName = "mines-paristech.colibri-vr";

        private const string _propertyNameDataDirectory = "_dataDirectory";
        private const string _propertyNameGenerateColliders = "generateColliders";

#endregion //CONST_FIELDS

#if UNITY_EDITOR

#region STATIC_PROPERTIES

        public static string pathToDataFolder { get { return Path.Combine(GeneralToolkit.GetProjectDirectoryPath(), "Data"); } }

#endregion //STATIC_PROPERTIES

#endif //UNITY_EDITOR

#region STATIC_METHODS

        /// <summary>
        /// Creates or resets a data handler object as child of the given transform.
        /// </summary>
        /// <param name="parentTransform"></param> The parent transform.
        /// <returns></returns> The data handler object.
        public static DataHandler CreateOrResetDataHandler(Transform parentTransform = null)
        {
            DataHandler existingDataHandler = GeneralToolkit.GetOrCreateChildComponent<DataHandler>(parentTransform);
            existingDataHandler.Reset();
            return existingDataHandler;
        }

        /// <summary>
        /// Gets the bundled asset prefix from the given type.
        /// </summary>
        /// <param name="callerType"></param> The type from which to get the prefix.
        /// <returns></returns> The prefix.
        public static string GetBundledAssetPrefixFromType(System.Type callerType)
        {
            return callerType.ToString() + '-';
        }
        
        /// <summary>
        /// Gets the type from te given bundled asset name.
        /// </summary>
        /// <param name="bundledAssetName"></param> The name from which to get the type.
        /// <returns></returns> The type.
        public static System.Type GetTypeFromBundledAssetName(string bundledAssetName)
        {
            string[] split = bundledAssetName.Split('-');
            return System.Type.GetType(split[0] + ", " + assemblyName);
        }

#endregion //STATIC_METHODS
        
#region FIELDS

        public List<string> bundledAssetsNames;
        public List<System.Type> bundledAssetsMethodTypes;
        public bool generateColliders;
        public int sourceColorCount;
        public int sourcePerViewCount;
        public int sourceGlobalCount;
        public bool imagePointCorrespondencesExist;

        [SerializeField] private Processing.Processing _processingCaller;
        [SerializeField] private string _dataDirectory;

        private bool _isLoadingBundle;

#endregion //FIELDS

#region PROPERTIES

        public string dataDirectory { get { return _dataDirectory; } }
        public string colorDirectory { get { return COLMAPConnector.GetImagesDir(dataDirectory); } }
        public string depthDirectory { get { return COLMAPConnector.GetDepthMapsDir(dataDirectory); } }
        public string processedDataDirectory { get { return Path.Combine(dataDirectory, "processed_data"); } }
        public string processingInfoFileName { get { return "processing_information.txt"; } }
        public string processingInfoFilePath { get { return Path.Combine(processedDataDirectory, processingInfoFileName); } }
        public string bundleDirectory { get { return Path.Combine(dataDirectory, "bundled_data"); } }
        public string additionalInfoFile { get { return Path.Combine(dataDirectory, "additional_information.txt"); } }

#endregion //PROPERTIES

#region INHERITANCE_METHODS

        /// <summary>
        /// Resets the object's properties.
        /// </summary>
        public void Reset()
        {
            _processingCaller = null;
            _dataDirectory = null;
            sourceColorCount = 0;
            sourcePerViewCount = 0;
            sourceGlobalCount = 0;
#if UNITY_EDITOR
            ChangeDataDirectory(pathToDataFolder);
#endif //UNITY_EDITOR
            _processingCaller = GeneralToolkit.GetParentOfType<Processing.Processing>(transform);
        }

#endregion //INHERITANCE_METHODS

#region METHODS

#if UNITY_EDITOR

        /// <summary>
        /// Changes the data directory to the given value.
        /// </summary>
        /// <param name="newValue"></param> New value for the data directory.
        /// <param name="force"></param> False if the method should be applied only if the directory points to a new path, true otherwise.
        public void ChangeDataDirectory(string newValue, bool force = false)
        {
            if(newValue == dataDirectory && !force)
                return;
            SerializedObject serializedObject = new SerializedObject(this);
            serializedObject.Update();
            _dataDirectory = newValue;
            SerializedProperty propertyDataDirectory = serializedObject.FindProperty(_propertyNameDataDirectory);
            propertyDataDirectory.stringValue = newValue;
            serializedObject.ApplyModifiedProperties();
            if(_processingCaller != null)
            {
                _processingCaller.ReadAcquisitionInformation();
                _processingCaller.cameraSetup.onPreviewIndexChangeEvent.Invoke();
            }
        }

        /// <summary>
        /// Saves additional information related to acquisition, with the given values.
        /// </summary>
        /// <param name="cameraSetup"></param> The camera setup containing the acquisition information.
        public void SaveAdditionalSetupInformation(CameraSetup cameraSetup)
        {
            // Determine the camera model, or initialize a new one if there is none.
            CameraModel cameraParams;
            if(cameraSetup.cameraModels != null && cameraSetup.cameraModels.Length > 0)
                cameraParams = cameraSetup.cameraModels[0];
            else
                cameraParams = CameraModel.CreateCameraModel();
            // Store the initial viewing position.
            Vector3 initialViewingPos = cameraSetup.initialViewingPosition;
            GeneralToolkit.CreateOrClear(PathType.File, additionalInfoFile);
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("# Additional setup information:");
            stringBuilder.AppendLine("#   INITIAL_VIEWING_POSITION");
            string line = initialViewingPos.x + " " + initialViewingPos.y + " " + initialViewingPos.z;
            stringBuilder.AppendLine(line);
            // Store the minimum/maximum distance range
            stringBuilder.AppendLine("#   Distance ranges with one line of data per camera:");
            stringBuilder.AppendLine("#   CAMERA_ID DISTANCE_RANGE_MIN DISTANCE_RANGE_MAX");
            foreach(CameraModel camera in cameraSetup.cameraModels)
            {
                Vector2 distanceRange = camera.distanceRange;
                stringBuilder.AppendLine(camera.cameraReferenceIndex + " " + distanceRange.x + " " + distanceRange.y);
            }
            // Save the file.
            File.WriteAllText(additionalInfoFile, stringBuilder.ToString());
            // Delete any temporary camera model.
            if(cameraSetup.cameraModels == null)
                DestroyImmediate(cameraParams.gameObject);
        }

        /// <summary>
        /// Creates the processed data directory and initializes the processing information file.
        /// </summary>
        public void CreateProcessingInfoFile()
        {
            if(!Directory.Exists(processedDataDirectory))
                Directory.CreateDirectory(processedDataDirectory);
            if(!File.Exists(processingInfoFilePath))
                using(File.Create(processingInfoFilePath)){}
            
        }

        /// <summary>
        /// Creates an asset bundle from the assets located in the processed data directory.
        /// </summary>
        /// <returns></returns> Returns true if the asset bundle was created successfully, false otherwise.
        public bool CreateAssetBundleFromCreatedAssets()
        {
            // Delete previous asset bundle.
            GeneralToolkit.Delete(bundleDirectory);
            // Copy assets to the temporary directory.
            GeneralToolkit.Replace(PathType.Directory, processedDataDirectory, GeneralToolkit.tempDirectoryAbsolutePath);
            AssetDatabase.Refresh();
            // Get the assets' relative path locations.
            string[] assetFullPaths = Directory.GetFiles(GeneralToolkit.tempDirectoryAbsolutePath);
            string[] assetRelativePaths = new string[assetFullPaths.Length];
            for(int i = 0; i < assetFullPaths.Length; i++)
                assetRelativePaths[i] = GeneralToolkit.ToRelativePath(assetFullPaths[i]);
            // Create an asset bundle from these assets.
            bool success = GeneralToolkit.CreateAssetBundle(bundleDirectory, bundleName, assetRelativePaths);
            // If successful, indicate to the processing information file that the asset bundle was created.
            if(success && File.Exists(processingInfoFilePath))
                File.AppendAllLines(processingInfoFilePath, new string[]{ processingInfoSuccessfulBundle });
            // Delete the temporary directory.
            GeneralToolkit.Delete(GeneralToolkit.tempDirectoryAbsolutePath);
            // Refresh the database.
            AssetDatabase.Refresh();
            // Return whether the operation was successful.
            return success;
        }
        
        /// <summary>
        /// Enables the user to choose whether the displayed geometry should have colliders.
        /// </summary>
        public void SectionGenerateColliders()
        {
            SerializedObject serializedObject = new SerializedObject(this);
            serializedObject.Update();
            string label = "Generate colliders:";
            string tooltip = "Whether the displayed geometry should have colliders.";
            SerializedProperty propertyGenerateColliders = serializedObject.FindProperty(_propertyNameGenerateColliders);
            propertyGenerateColliders.boolValue = EditorGUILayout.Toggle(new GUIContent(label, tooltip), propertyGenerateColliders.boolValue);
            serializedObject.ApplyModifiedProperties();
        }

#endif //UNITY_EDITOR

        /// <summary>
        /// Sets the image URLs for the given camera setup.
        /// </summary>
        /// <param name="cameraSetup"></param> The camera setup.
        public void SetImageURLs(CameraSetup cameraSetup)
        {
            for(int iter = 0; iter < cameraSetup.cameraModels.Length; iter++)
            {
                cameraSetup.cameraModels[iter].imageURL = Path.Combine(colorDirectory, cameraSetup.cameraModels[iter].imageName);
            }
        }

        /// <summary>
        /// Reads the stored additional information.
        /// </summary>
        /// <param name="cameraSetup"></param> The camera setup to modify with the parsed information.
        public void ReadCOLIBRIVRAdditionalInformation(CameraSetup cameraSetup)
        {
            string[] lines = File.ReadAllLines(additionalInfoFile);
            for(int i=0; i<lines.Length; ++i)
            {
                if(lines[i].Contains("INITIAL_VIEWING_POSITION"))
                {
                    string[] split = lines[++i].Split(' ');
                    if(split.Length == 3)
                    {
                        Vector3 newInitialViewingPosition = new Vector3(GeneralToolkit.ParseFloat(split[0]), GeneralToolkit.ParseFloat(split[1]), GeneralToolkit.ParseFloat(split[2]));
                        cameraSetup.SetAdditionalParameters(newInitialViewingPosition);
                    }
                    continue;
                }

                if(lines[i].Contains("DISTANCE_RANGE"))
                {
                    while(i+1 < lines.Length && !lines[i+1].StartsWith("#"))
                    {
                        string[] split = lines[++i].Split(' ');
                        if(split.Length == 3)
                        {
                            int cameraReferenceIndex = GeneralToolkit.ParseInt(split[0]);
                            Vector2 newDistanceRange = new Vector2(GeneralToolkit.ParseFloat(split[1]), GeneralToolkit.ParseFloat(split[2]));
                            // Find the referenced camera and set its distance range
                            foreach(CameraModel camera in cameraSetup.cameraModels)
                            {
                                if(camera.cameraReferenceIndex == cameraReferenceIndex)
                                    camera.distanceRange = newDistanceRange;
                            } 
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Checks the source data directory for color images, depth maps, and meshes.
        /// </summary>
        public void CheckStatusOfSourceData()
        {
            string[] extensions = new string[] {".png",".jpg"};
            // Check the color directory for color images.
            if(Directory.Exists(colorDirectory))
                sourceColorCount = GeneralToolkit.GetFilesByExtension(colorDirectory, extensions).Length;
            else
                sourceColorCount = 0;
            // Check the depth directory for depth maps.
            if(Directory.Exists(depthDirectory))
                sourcePerViewCount = GeneralToolkit.GetFilesByExtension(depthDirectory, extensions).Length;
            else
                sourcePerViewCount = 0;
            extensions = new string[] {".asset", ".obj",".fbx"};
            // Check the root directory for meshes.
            sourceGlobalCount = GeneralToolkit.GetFilesByExtension(dataDirectory, extensions).Length;
            // Compile all of this information into an output string for the processing caller.
            if(_processingCaller != null)
            {
                string colorInfo = sourceColorCount + " color image" + ((sourceColorCount == 1) ? string.Empty : "s") + ", ";
                string depthInfo = sourcePerViewCount + " depth map" + ((sourcePerViewCount == 1) ? string.Empty : "s") + ", ";
                string meshInfo = sourceGlobalCount + " mesh" + ((sourceGlobalCount == 1) ? string.Empty : "es") + ".";
                _processingCaller.sourceDataInfo = "This directory contains: " + colorInfo + depthInfo + meshInfo;
            } 
        }

        /// <summary>
        /// Checks the processing and bundling status of the data.
        /// </summary>
        /// <param name="isReadyForBundling"></param> Outputs true if there is processed data to bundle, false otherwise.
        /// <param name="isBundled"></param> Outputs true if the current processed data is already bundled, false otherwise.
        /// <param name="summaryInfo"></param> Outputs a summary to be displayed in the GUI.
        public void CheckStatusOfDataProcessingAndBundling(out bool isReadyForBundling, out bool isBundled, out string summaryInfo)
        {
            isBundled = false;
            summaryInfo = string.Empty;
            bundledAssetsNames = new List<string>();
            bundledAssetsMethodTypes = new List<System.Type>();
            isReadyForBundling = (Directory.Exists(processedDataDirectory) && File.Exists(processingInfoFilePath));
            // If the source data has not yet been processed, notify the user.
            if(!isReadyForBundling)
            {
                summaryInfo = "Source data has not yet been processed.";
            }
            // Otherwise, continue.
            else 
            {
                // Check whether assets were processed and bundled.
                string[] lines = File.ReadAllLines(processingInfoFilePath);
                foreach(string line in lines)
                {
                    if(line == processingInfoSuccessfulBundle)
                    {
                        isBundled = true;
                    }
                }
                isBundled = isBundled && Directory.Exists(bundleDirectory);
                // If the processed data has not yet been bundled, notify the user.
                if(!isBundled)
                {
                    summaryInfo = "Processed data has not yet been bundled.";
                }
                // Otherwise, gather information on the bundled assets, and indicate that the data is ready to be rendered.
                else
                {
                    foreach(string line in lines)
                    {
                        if(!line.StartsWith("#"))
                        {
                            bundledAssetsNames.Add(line);
                            bundledAssetsMethodTypes.Add(GetTypeFromBundledAssetName(line));
                        }
                    }
                    summaryInfo = "Data is processed and bundled. Ready for rendering.";
                }
            }
        }

        /// <summary>
        /// Returns the name of the processed asset in the asset bundle.
        /// </summary>
        /// <param name="caller"></param> The caller object.
        /// <param name="assetName"></param> The name of the processed asset.
        /// <returns></returns> The name of the bundled asset.
        public string GetBundledAssetName(Object caller, string assetName)
        {
            return GetBundledAssetPrefixFromType(caller.GetType()) + assetName;
        }

        /// <summary>
        /// Checks whether an asset has already been processed.
        /// </summary>
        /// <param name="bundledAssetName"></param> The name of the asset in question.
        /// <returns></returns> True if the asset is already processed, false otherwise.
        public bool IsAssetAlreadyProcessed(string bundledAssetName)
        {
            if(File.Exists(bundledAssetName))
            {
                string debug = "Skipping asset \"" + bundledAssetName +"\" because it is already processed. To overwrite it, delete it first.";
                Debug.Log(GeneralToolkit.FormatScriptMessage(this.GetType(), debug));
                return true;
            }
            return false;
        }

        /// <summary>
        /// Updates the processing information file with the processed asset names.
        /// </summary>
        public void UpdateProcessedAssets()
        {
            FileInfo[] processedAssetFiles = GeneralToolkit.GetFilesByExtension(processedDataDirectory, ".asset");
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(processingInfoHeader);
            for(int i = 0; i < processedAssetFiles.Length; i++)
                stringBuilder.AppendLine(processedAssetFiles[i].Name.Replace(processedAssetFiles[i].Extension, string.Empty));
            File.WriteAllText(processingInfoFilePath, stringBuilder.ToString());
        }

        /// <summary>
        /// Coroutine that loads assets of a given type from a given bundle.
        /// </summary>
        /// <param name="OutputAssets"></param> Outputs the assets.
        /// <param name="bundledAssetsNames"></param> The bundled asset names.
        /// <typeparam name="T"></typeparam> The types of the assets.
        /// <returns></returns>
        public IEnumerator LoadAssetsFromBundleCoroutine<T>(System.Action<T[]> OutputAssets, params string[] bundledAssetsNames) where T : UnityEngine.Object
        {
            string bundlePath = Path.Combine(bundleDirectory, bundleName);
            yield return StartCoroutine(GeneralToolkit.LoadAssetBundleIntoMemory(bundlePath));
            yield return StartCoroutine(GeneralToolkit.LoadAssetsFromBundleInMemory<T>((results => OutputAssets(results)), bundledAssetsNames));
        }

#endregion //METHODS

    }

}
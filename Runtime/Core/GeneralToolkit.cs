/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using UnityEngine;
using UnityEngine.Networking;
using UnityEditor;

namespace COLIBRIVR
{

#region ENUMS

        public enum PathType {File, Directory};
        public enum ImageProcessingKernelType {Gaussian, Box};

#endregion //ENUMS

    /// <summary>
    /// Static helper class for general Unity operations.
    /// </summary>
    public static class GeneralToolkit
    {

#region GENERAL

        /// <summary>
        /// Deactivates active gameobjects with a given component, excluding the calling gameobject.
        /// </summary>
        /// <param name="selfGO"></param> The gameobject calling the method.
        /// <typeparam name="T"></typeparam> The type of component to look for.
        /// <returns></returns> Returns the list of objects deactivated with this method.
        public static List<GameObject> DeactivateOtherActiveComponents<T>(GameObject selfGO) where T : Component
        {
            List<GameObject> otherActiveGOs = new List<GameObject>();
            foreach(T comp in Object.FindObjectsOfType<T>())
            {
                if(comp.gameObject != selfGO && comp.gameObject.activeInHierarchy)
                {
                    otherActiveGOs.Add(comp.gameObject);
                    comp.gameObject.SetActive(false);
                }
            }
            return otherActiveGOs;
        }

        /// <summary>
        /// Reactivates objects previously deactivated.
        /// </summary>
        /// <param name="deactivatedGOs"></param> The list of objects that were deactivated in a previous step.
        public static void ReactivateOtherActiveComponents(List<GameObject> deactivatedGOs)
        {
            if(deactivatedGOs != null)
                foreach(GameObject otherActiveGO in deactivatedGOs)
                    if(otherActiveGO != null)
                        otherActiveGO.SetActive(true);
        }

        /// <summary>
        /// Gets or adds a component to a given gameobject.
        /// </summary>
        /// <param name="type"></param> The type of the component to add.
        /// <param name="caller"></param> The caller gameobject.
        /// <returns></returns> The desired component.
        public static Component GetOrAddComponent(System.Type type, GameObject caller)
        {
            Component component = caller.GetComponent(type);
            if(component == null)
                component = caller.AddComponent(type);
            return component;
        }

        /// <summary>
        /// Gets or adds a component to a given gameobject (generic equivalent).
        /// </summary>
        /// <param name="caller"></param> The caller gameobject.
        /// <typeparam name="T"></typeparam> The type of the component.
        /// <returns></returns> The desired component.
        public static T GetOrAddComponent<T>(GameObject caller) where T : Component
        {
            return GetOrAddComponent(typeof(T), caller) as T;
        }

        /// <summary>
        /// Checks whether the given path is valid, i.e. not null or empty, and informs the user.
        /// </summary>
        /// <param name="callerType"></param> The type of the caller object.
        /// <param name="path"></param> The path to check.
        /// <param name="objectID"></param> An identifier for the object that would be located at that path.
        /// <returns></returns> True if the path is not null or empty, false otherwise.
        public static bool CheckPathIsValid(System.Type callerType, string path, string objectID)
        {
            if(string.IsNullOrEmpty(path))
            {
                Debug.LogError(GeneralToolkit.FormatScriptMessage(callerType, "Path to " + objectID + " is null or empty"));
                return false;
            }
            return true;
        }

#if UNITY_EDITOR

        /// <summary>
        /// Removes missing components on the target gameobject.
        /// </summary>
        /// <param name="targetObject"></param> The gameobject from which to remove missing components.
        public static void RemoveMissingComponents(GameObject targetObject)
        {
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(targetObject);
        }

#endif //UNITY_EDITOR

        /// <summary>
        /// Creates a component of the given type as a child object of the given parent transform.
        /// </summary>
        /// <param name="type"></param> The type of the component to add.
        /// <param name="parentTransform"></param> The parent transform for the object.
        /// <param name="objectName"></param> The name to give to the object.
        /// <returns></returns> The created component.
        public static Component CreateChildComponent(System.Type type, Transform parentTransform = null, string objectName = "")
        {
            if(string.IsNullOrEmpty(objectName))
                objectName = type.Name;
            GameObject newObject = new GameObject(objectName);
            if(parentTransform != null)
            {
                newObject.transform.parent = parentTransform;
                GeneralToolkit.SetTransformValues(newObject.transform, true, Vector3.zero, Quaternion.identity, Vector3.one);
            }
            return newObject.AddComponent(type);
        }

        /// <summary>
        /// Creates a component of the given type as a child object of the given parent transform (generic equivalent).
        /// </summary>
        /// <param name="parentTransform"></param> The parent transform for the object.
        /// <param name="objectName"></param> The name to give to the object.
        /// <typeparam name="T"></typeparam> The type of component to create.
        /// <returns></returns> The created component.
        public static T CreateChildComponent<T>(Transform parentTransform = null, string objectName = "") where T : Component
        {
            return CreateChildComponent(typeof(T), parentTransform, objectName) as T;
        }

        /// <summary>
        /// Gets a component by searching the given transform's children only, not the transform itself.
        /// </summary>
        /// <param name="type"></param> The type of the searched component.
        /// <param name="parentTransform"></param> The parent transform whose children to search.
        /// <returns></returns> The found component.
        public static Component GetComponentInChildrenOnly(System.Type type, Transform parentTransform)
        {
            Component childComponent = null;
            if(parentTransform != null)
            {
                Component[] childComponents = parentTransform.GetComponentsInChildren(type);
                if(childComponents != null)
                {
                    for(int i = 0; i < childComponents.Length; i++)
                    {
                        childComponent = childComponents[i];
                        if(childComponent.transform != parentTransform)
                            break;
                        else
                            childComponent = null;
                    }
                }
            }
            return childComponent;
        }

        /// <summary>
        /// Gets a component by searching the given transform's children only, not the transform itself (generic equivalent).
        /// </summary>
        /// <param name="parentTransform"></param> The parent transform whose children to search.
        /// <typeparam name="T"></typeparam> The type of the searched component.
        /// <returns></returns> The found component.
        public static T GetComponentInChildrenOnly<T>(Transform parentTransform) where T : Component
        {
            return GetComponentInChildrenOnly(typeof(T), parentTransform) as T;
        }

        /// <summary>
        /// Gets or creates a transform as a child of the given parent transform.
        /// </summary>
        /// <param name="parentTransform"></param> The parent transform.
        /// <param name="transformName"></param> The child transform's name.
        /// <returns></returns> The created transform.
        public static Transform GetOrCreateChildTransform(Transform parentTransform, string transformName)
        {
            Transform newTransform = parentTransform.Find(transformName);
            if(newTransform == null)
            {
                newTransform = new GameObject(transformName).transform;
                newTransform.parent = parentTransform;
                SetTransformValues(newTransform, true, Vector3.zero, Quaternion.identity, Vector3.one);
            }
            return newTransform;
        }

        /// <summary>
        /// Gets or creates a component of the given type, as a child of the given transform.
        /// </summary>
        /// <param name="componentType"></param> The component's type.
        /// <param name="parentTransform"></param> The parent transform.
        /// <returns></returns> The component.
        public static Component GetOrCreateChildComponent(System.Type componentType, Transform parentTransform)
        {
            Component childComponent = GetComponentInChildrenOnly(componentType, parentTransform);
            if(childComponent == null)
                childComponent = CreateChildComponent(componentType, parentTransform);
            return childComponent;
        }

        /// <summary>
        /// Gets or creates a component of the given type, as a child of the given transform (generic equivalent).
        /// </summary>
        /// <param name="parentTransform"></param> The parent transform.
        /// <typeparam name="T"></typeparam> The component's type.
        /// <returns></returns> The component.
        public static T GetOrCreateChildComponent<T>(Transform parentTransform) where T : Component
        {
            return GetOrCreateChildComponent(typeof(T), parentTransform) as T;
        }

        /// <summary>
        /// Gets or creates a group of components of the given types, as children of a group transform that is itself child of the given parent transform.
        /// </summary>
        /// <param name="componentTypes"></param> The types of the components.
        /// <param name="groupName"></param> The name of the group transform.
        /// <param name="parentTransform"></param> The parent transform.
        /// <typeparam name="T"></typeparam> The shared type in which to cast each component.
        /// <returns></returns>
        public static T[] GetOrCreateChildComponentGroup<T>(System.Type[] componentTypes, string groupName, Transform parentTransform) where T : Component
        {
            Transform groupTransform = GeneralToolkit.GetOrCreateChildTransform(parentTransform, groupName);
            T[] components = new T[componentTypes.Length];
            for(int iter = 0; iter < components.Length; iter++)
                components[iter] = (T) GeneralToolkit.GetOrCreateChildComponent(componentTypes[iter], groupTransform);
            return components;
        }

        /// <summary>
        /// Removes the children of the given parent transform that are of a given type.
        /// </summary>
        /// <param name="parentTransform"></param> The parent transform.
        /// <param name="componentTypes"></param> The list of types to remove.
        public static void RemoveChildComponents(Transform parentTransform, params System.Type[] componentTypes)
        {
            for(int iter = 0; iter < componentTypes.Length; iter++)
            {
                Component childComponent = GeneralToolkit.GetComponentInChildrenOnly(componentTypes[iter], parentTransform);
                if(childComponent != null)
                    GameObject.DestroyImmediate(childComponent.gameObject);
            }
        }
        /// <summary>
        /// Removes the children of the given parent transform that have the given names.
        /// </summary>
        /// <param name="parentTransform"></param> The parent transform.
        /// <param name="childrenNames"></param> The list of names to remove.
        public static void RemoveChildren(Transform parentTransform, params string[] childrenNames)
        {
            for(int iter = 0; iter < childrenNames.Length; iter++)
            {
                Transform childTransform = parentTransform.Find(childrenNames[iter]);
                if(childTransform != null)
                    GameObject.DestroyImmediate(childTransform.gameObject);
            }
        }

        /// <summary>
        /// Gets a parent object of the given type.
        /// </summary>
        /// <param name="childTransform"></param> The child transform.
        /// <typeparam name="T"></typeparam> The type.
        /// <returns></returns> The parent object of the given type.
        public static T GetParentOfType<T>(Transform childTransform) where T : Component
        {
            T comp = null;
            Transform currentTransform = childTransform;
            while(currentTransform.parent != null)
            {
                currentTransform = currentTransform.parent;
                comp = currentTransform.GetComponent<T>();
                if(comp != null)
                    break;
            }
            return comp;
        }

        /// <summary>
        /// Sets the position, rotation, and scale values of a given transform.
        /// </summary>
        /// <param name="transform"></param> The transform of which to modify the values.
        /// <param name="local"></param> True if the values are local, false if they are global.
        /// <param name="position"></param> The new position value.
        /// <param name="rotation"></param> The new rotation value.
        /// <param name="scale"></param> The new scale value.
        public static void SetTransformValues(Transform transform, bool local, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if(local)
            {
                transform.localPosition = position;
                transform.localRotation = rotation;
            }
            else
            {
                transform.position = position;
                transform.rotation = rotation;
            }
            transform.localScale = scale;
        }

        /// <summary>
        /// Checks if the target transform has changed position.
        /// </summary>
        /// <param name="targetTransform"></param> The target transform.
        /// <param name="previousPosition"></param> References the target's previous position.
        /// <returns></returns> True if the target has changed, false otherwise.
        public static bool HasTransformChanged(Transform targetTransform, ref Vector3 previousPosition)
        {
            bool hasChanged = false;
            if(targetTransform.position != previousPosition)
            {
                previousPosition = targetTransform.position;
                hasChanged = true;
            }
            return hasChanged;
        }

        /// <summary>
        /// Checks if the target transform has changed in position or rotation.
        /// </summary>
        /// <param name="targetTransform"></param> The target transform.
        /// <param name="previousPosition"></param> References the target's previous position.
        /// <param name="previousRotation"></param> References the target's previous rotation.
        /// <returns></returns> True if the target has changed, false otherwise.
        public static bool HasTransformChanged(Transform targetTransform, ref Vector3 previousPosition, ref Quaternion previousRotation)
        {
            bool hasChanged = HasTransformChanged(targetTransform, ref previousPosition);
            if(targetTransform.rotation != previousRotation)
            {
                previousRotation = targetTransform.rotation;
                hasChanged = true;
            }
            return hasChanged;
        } 

        /// <summary>
        /// Checks if the target transform has changed in position, rotation, or scale.
        /// </summary>
        /// <param name="targetTransform"></param> The target transform.
        /// <param name="previousPosition"></param> References the target's previous position.
        /// <param name="previousRotation"></param> References the target's previous rotation.
        /// <param name="previousLossyScale"></param> References the target's previous lossy scale.
        /// <returns></returns> True if the target has changed, false otherwise.
        public static bool HasTransformChanged(Transform targetTransform, ref Vector3 previousPosition, ref Quaternion previousRotation, ref Vector3 previousLossyScale)
        {
            bool hasChanged = HasTransformChanged(targetTransform, ref previousPosition, ref previousRotation);
            if(targetTransform.lossyScale != previousLossyScale)
            {
                previousLossyScale = targetTransform.lossyScale;
                hasChanged = true;
            }
            return hasChanged;
        }

        /// <summary>
        /// Parses an int from a culture-invariant text string. If unsuccessful, sends an error message.
        /// </summary>
        /// <param name="inputString"></param> The text string to parse.
        /// <returns></returns> The parsed int.
        public static int ParseInt(string inputString)
        {
            int output = 0;
            bool success = int.TryParse(inputString, NumberStyles.Integer, CultureInfo.InvariantCulture, out output);
            if(!success)
                UnsuccessfulParse(inputString, typeof(int));
            return output;
        }

        /// <summary>
        /// Parses a float from a culture-invariant text string. If unsuccessful, sends an error message.
        /// </summary>
        /// <param name="inputString"></param> The text string to parse.
        /// <returns></returns> The parsed float.
        public static float ParseFloat(string inputString)
        {
            float output = 0f;
            bool success = float.TryParse(inputString, NumberStyles.Float, CultureInfo.InvariantCulture, out output);
            if(!success)
                UnsuccessfulParse(inputString, typeof(float));
            return output;
        }

        /// <summary>
        /// Sends an error message upon unsuccessful parse.
        /// </summary>
        /// <param name="inputString"></param> The input text string, on which parsing was unsuccessful.
        /// <param name="type"></param> The expected type of the parsed object.
        private static void UnsuccessfulParse(string inputString, System.Type type)
        {
            UnityEngine.Debug.LogError(FormatScriptMessage(typeof(GeneralToolkit), "Parsing unsuccessful! \"" + inputString + "\" is not a valid " + type));
        }

        /// <summary>
        /// Parses a vector from a culture-invariant text string. 
        /// </summary>
        /// <param name="inputString"></param> The text string to parse.
        /// <returns></returns> The parsed vector.
        public static Vector3 ParseVector3(string inputString)
        {
            Vector3 output = Vector3.zero;
            // Remove parentheses if any.
            if(inputString.Contains('(') && inputString.Contains(')'))
                inputString = inputString.Substring(1, inputString.Length - 2);
            // The vector must be separated using commas.
            string[] split = inputString.Split(',');
            if(split.Length == 3)
                for(int i = 0; i < 3; i++)
                    output[i] = ParseFloat(split[i]);
            return output;
        }

        /// <summary>
        /// Returns a culture-invariant text string from an int.
        /// </summary>
        /// <param name="obj"></param> The int to convert to text.
        /// <returns></returns> The text string.
        public static string ToString(int obj)
        {
            return obj.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Returns a culture-invariant text string from a float.
        /// </summary>
        /// <param name="obj"></param> The float to convert to text.
        /// <returns></returns> The text string.
        public static string ToString(float obj)
        {
            return obj.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Gets the current display resolution (depends on whether the user is using a VR headset).
        /// </summary>
        /// <returns></returns> The current display resolution.
        public static Vector2Int GetCurrentDisplayResolution()
        {
            if (UnityEngine.XR.XRSettings.enabled)
                return new Vector2Int(UnityEngine.XR.XRSettings.eyeTextureWidth, UnityEngine.XR.XRSettings.eyeTextureHeight);
            else
                return new Vector2Int(Screen.width, Screen.height);
        }

        public static bool IsStartingNewScene()
        {
            return (Time.timeSinceLevelLoad == 0);
        }

#endregion //GENERAL

#region FILE_IO

#if UNITY_EDITOR

        /// <summary>
        /// Deletes a file or directory and the associated Unity meta files.
        /// </summary>
        /// <param name="path"></param> The path to delete.
        public static void Delete(string path)
        {
            // Delete the object.
            FileUtil.DeleteFileOrDirectory(path);
            // Find the corresponding meta file, and delete it if it exists.
            string metaPath = path + ".meta";
            if(File.Exists(metaPath))
                FileUtil.DeleteFileOrDirectory(metaPath);
        }

        /// <summary>
        /// Copies a file or directory.
        /// </summary>
        /// <param name="srcPath"></param> The source path to copy from.
        /// <param name="dstPath"></param> The destination path to copy into.
        public static void Copy(string srcPath, string dstPath)
        {
            FileUtil.CopyFileOrDirectory(srcPath, dstPath);
        }

        /// <summary>
        /// Replaces a file or directory. Does not require the destination path to exist before replacing.
        /// </summary>
        /// <param name="pathType"></param> Whether the path is a file or a directory.
        /// <param name="srcPath"></param> The source path to copy from.
        /// <param name="dstPath"></param> The destination path to copy into.
        public static void Replace(PathType pathType, string srcPath, string dstPath)
        {
            if(pathType == PathType.Directory)
                FileUtil.ReplaceDirectory(srcPath, dstPath);
            else
                FileUtil.ReplaceFile(srcPath, dstPath);
        }

        /// <summary>
        /// Creates or clears a file or directory at a given path.
        /// </summary>
        /// <param name="pathType"></param> Whether the path is a file or a directory.
        /// <param name="path"></param> The path.
        public static void CreateOrClear(PathType pathType, string path)
        {
            // The path can only be deleted if it is in the Unity project.
            bool pathNotInProject = !path.StartsWith(GetProjectDirectoryPath());
            // If it is in the Assets folder, it must also be in a temporary folder to be deleted (the settings folder is considered a temp folder).
            bool pathInAssetsButNotTemp = path.Contains(Path.GetFullPath(Application.dataPath)) && !path.Contains(_tempDirectoryName) && !path.StartsWith(COLIBRIVRSettings.settingsFolderAbsolutePath);
            // If neither of these conditions is verified, return an error message.
            if(pathNotInProject || pathInAssetsButNotTemp)
            {
                if(pathNotInProject)
                    UnityEngine.Debug.LogError(FormatScriptMessage(typeof(GeneralToolkit), "Could not clear path. Path is not in project folder: " + path));
                if(pathInAssetsButNotTemp)
                    UnityEngine.Debug.LogError(FormatScriptMessage(typeof(GeneralToolkit), "Could not clear path. Path is not a temporary folder: " + path));
                return;
            }
            // Delete the object at the given path.
            Delete(path);
            // Because the process of creating a path sometimes fails, we attempt it multiple times.
            int attempt = 0;
            int maxAttemptCount = 20;
            bool tryAgain = true;
            while(tryAgain && attempt < maxAttemptCount)
            {
                try
                {
                    if(pathType == PathType.Directory)
                        Directory.CreateDirectory(path);
                    else if(pathType == PathType.File)
                        File.Create(path).Close();
                    tryAgain = false;
                }
                catch (System.Exception e)
                {
                    UnityEngine.Debug.LogWarning(FormatScriptMessage(typeof(GeneralToolkit), "Trying again due to exception: " + e.ToString()));
                    tryAgain = true;
                    attempt++;
                }
            }
            // Sometimes, the max number of attempts will be exceeded. If this happens, check that the file is not open in another application.
            if(attempt >= maxAttemptCount)
            {
                UnityEngine.Debug.LogError(FormatScriptMessage(typeof(GeneralToolkit), "Number of attempts exceeded. Could not create or clear path: " + path));
            }
        }

        /// <summary>
        /// Moves a file or directory to another location, and handles Unity meta files.
        /// </summary>
        /// <param name="pathType"></param> Whether the path is a file or a directory.
        /// <param name="srcPath"></param> The source path.
        /// <param name="dstPath"></param> The destination path.
        /// <param name="keepMetaFiles"></param> True if meta files should be moved as well, false otherwise.
        public static void Move(PathType pathType, string srcPath, string dstPath, bool keepMetaFiles = false)
        {
            string formattedSrcPath = srcPath.Replace('\\', '/');
            string formattedDstPath = dstPath.Replace('\\', '/');
            // Move the path to another location.
            Delete(dstPath);
            try
            {
                FileUtil.MoveFileOrDirectory(formattedSrcPath, formattedDstPath);
            }
            catch(System.Exception e)
            {
                UnityEngine.Debug.LogError(FormatScriptMessage(typeof(GeneralToolkit), e.ToString()));
                displayGUI = true;
            }
            // Find the source meta path.
            string metaSrcPath = formattedSrcPath + ".meta";
            // If desired, move it as well.
            if(keepMetaFiles)
            {
                string metaDstPath = formattedDstPath + ".meta";
                FileUtil.MoveFileOrDirectory(metaSrcPath, metaDstPath);
            }
            // Otherwise, delete it.
            else
            {
                FileUtil.DeleteFileOrDirectory(metaSrcPath);
                if(pathType == PathType.Directory)
                    foreach(string file in Directory.GetFiles(dstPath))
                        if(file.Contains(".meta"))
                            FileUtil.DeleteFileOrDirectory(file);
            }
        }

#endif //UNITY_EDITOR

        public static string tempDirectoryRelativePath { get{ return Path.Combine(ToRelativePath(COLIBRIVRSettings.settingsFolderAbsolutePath), _tempDirectoryName); } }
        public static string tempDirectoryAbsolutePath { get{ return Path.Combine(COLIBRIVRSettings.settingsFolderAbsolutePath, _tempDirectoryName); } }
        
        private static string _tempDirectoryName = "COLIBRIVRTempDirectory";

        /// <summary>
        /// Returns the path to the directory preceding the one specified as a parameter.
        /// </summary>
        /// <param name="path"></param> Absolute path to a directory.
        /// <returns></returns> The path to the parent directory.
        public static string GetDirectoryBefore(string path)
        {
            path = Path.GetFullPath(path);
            string[] directories = path.Split(Path.DirectorySeparatorChar);
            // If there is no parent directory, return the given one.
            if(directories.Length < 2)
                return path;
            // Reach the parent directory by iterating from root.
            string outputPath = string.Empty;
            for(int i = 0; i < directories.Length - 1; i++)
            {
                outputPath += directories[i];
                if(i < directories.Length - 2)
                    outputPath += Path.DirectorySeparatorChar;
            }
            return outputPath;
        }
        /// <summary>
        /// Returns an array containing the names of files at the given path that have one of the given extensions.
        /// </summary>
        /// <param name="path"></param> The path at which to find the files.
        /// <param name="extensions"></param> The choice of extensions.
        /// <returns></returns>
        public static FileInfo[] GetFilesByExtension(string path, params string[] extensions)
        {
            // Return empty info if the path cannot be found.
            if(!Directory.Exists(path))
                return new FileInfo[0];
            // Return all files in the directory with the given extensions.
            DirectoryInfo dirInfo = new DirectoryInfo(path);
            IEnumerable<FileInfo> files = dirInfo.EnumerateFiles();
            files = files.Where(f => extensions.Contains(f.Extension) || extensions.Contains(f.Extension.ToLower()));
            return files.ToArray();
        } 

        /// <summary>
        /// Gets the path to the Unity project.
        /// </summary>
        /// <returns></returns> The path to the Unity project.
        public static string GetProjectDirectoryPath()
        {
            return GetDirectoryBefore(Application.dataPath);
        }

        /// <summary>
        /// Transforms an absolute path into a path relative to the Unity project folder.
        /// </summary>
        /// <param name="pathAbsolute"></param> The absolute path.
        /// <returns></returns> The relative path.
        public static string ToRelativePath(string pathAbsolute)
        {
            return pathAbsolute.Replace(GetProjectDirectoryPath() + Path.DirectorySeparatorChar, string.Empty);
        }

#endregion //FILE_IO

#region COMMAND_LINE

#if UNITY_EDITOR

        private static List<string> _outputDataReceived;
        private static List<string> _errorDataReceived;

        /// <summary>
        /// Coroutine that runs a command-line process.
        /// </summary>
        /// <param name="callerType"></param> The type of the caller object.
        /// <param name="command"></param> The command as a text string.
        /// <param name="workingDirectory"></param> The working directory from which to run the command.
        /// <param name="displayProgressBar"></param> True if a progress bar is to be displayed, false otherwise.
        /// <param name="stopOnError"></param> True if the process should be stopped if it returns an error, false otherwise.
        /// <param name="progressBarParams"></param> The progress bar parameters to display if there is a progress bar.
        /// <returns></returns>
        public static IEnumerator RunCommandCoroutine(System.Type callerType, string command, string workingDirectory = null, bool displayProgressBar = false,
            System.Diagnostics.DataReceivedEventHandler actionToPerform = null, string harmlessWarnings = null, bool stopOnError = false, string[] progressBarParams = null)
        {
            // Indicate to the user that the command has been launched.
            Debug.Log(FormatScriptMessage(callerType, "Running command: " + command));
            // Create a process with the specified command.
            System.Diagnostics.ProcessStartInfo processStartInfo = new System.Diagnostics.ProcessStartInfo("cmd.exe", "/c " + command);
            processStartInfo.UseShellExecute = false;
            processStartInfo.CreateNoWindow = true;
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.RedirectStandardError = true;
            if(workingDirectory != null)
                processStartInfo.WorkingDirectory = workingDirectory;
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            _outputDataReceived = new List<string>();
            _errorDataReceived = new List<string>();
            // Try to launch the process.
            try
            {
                process = System.Diagnostics.Process.Start(processStartInfo);
                // Catch any errors or log messages that the process may return.
                process.OutputDataReceived += ProcessOutputDataReceivedEventHandler;
                process.ErrorDataReceived += ProcessErrorDataReceivedEventHandler;
                if(actionToPerform != null)
                    process.OutputDataReceived += actionToPerform;
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                // If a progress bar is to be displayed, start displaying it.
                if(displayProgressBar && progressBarParams != null && progressBarParams.Length == 3)
                {
                    int progressBarMaxIter = ParseInt(progressBarParams[0]);
                    string progressBarTitle = progressBarParams[1];
                    string exitMessage = progressBarParams[2];
                    UpdateCancelableProgressBar(callerType, true, false, false, progressBarMaxIter, progressBarTitle, "", exitMessage);
                }
            }
            // Add any returned exception to the list of errors.
            catch (System.Exception e)
            {
                _errorDataReceived.Add(e.ToString());
            }
            // Wait for the process to end naturally or for the user to end it, while monitoring its output.
            string logSegment = string.Empty;
            bool forceExit = false;
            bool naturalExit = false;
            int outputDataLineCount = 0;
            int maxLineCountBeforeDebug = 50000;
            string logHeader = "===== Log block (" + maxLineCountBeforeDebug  + " characters) =====\n\n";
            while(!(forceExit || naturalExit))
            {
                // If there are error messages, display them. If desired, stop the process afterwards.
                if(_errorDataReceived.Count > 0)
                {
                    UnityEngine.Debug.LogError(FormatScriptMessage(callerType, "Errors were returned, please consult the log message for details."));
                    string errorMessages = string.Empty;
                    for(int i = 0; i < _errorDataReceived.Count; i++)
                        errorMessages += _errorDataReceived[i] + "\n";
                    UnityEngine.Debug.LogError(FormatScriptMessage(callerType, logHeader + errorMessages));
                    forceExit = (stopOnError) ? true : forceExit;
                    _errorDataReceived.Clear();
                }
                // If there are log messages, concatenate them, and display the concatenated list when it becomes too large.
                if(_outputDataReceived.Count > 0)
                {
                    for(int i = 0; i < _outputDataReceived.Count; i++)
                    {
                        string logMessage = _outputDataReceived[i];
                        if(!string.IsNullOrEmpty(logMessage))
                        {
                            logSegment += logMessage + "\n";
                            // Display the log segment if it becomes too large.
                            bool segmentTooLong = (logSegment.Length > maxLineCountBeforeDebug);
                            if(segmentTooLong)
                            {
                                Debug.Log(FormatScriptMessage(callerType, logHeader + logSegment));
                                logSegment = string.Empty;
                            }
                            // Display any warnings using the warning message in the console.
                            if(logMessage.ToUpperInvariant().StartsWith("WARNING") || (harmlessWarnings != null && harmlessWarnings.Contains(logMessage)))
                            {
                                Debug.LogWarning(FormatScriptMessage(callerType, "Warnings were returned, please consult the log message for details."));
                                Debug.LogWarning(FormatScriptMessage(callerType, logMessage));
                            }
                            // Handle any error messages appearing in the log as if the process itself had launched an error in the console.
                            else if(logMessage.ToUpperInvariant().StartsWith("ERROR"))
                            {
                                _errorDataReceived.Add(logMessage);
                            }
                        }
                    }
                    // If a progress bar is displayed, use it to display log messages before they are cleared.
                    if(displayProgressBar)
                    {
                        string progressBarInfo = "Log " + ToString(outputDataLineCount) + ": " + _outputDataReceived[_outputDataReceived.Count - 1];
                        UpdateCancelableProgressBar(callerType, false, false, false, -1, "", progressBarInfo, "");
                    }
                    outputDataLineCount += _outputDataReceived.Count;
                    _outputDataReceived.Clear();
                }
                // If the user cancels the process using the progress bar, force the process to exit.
                if(displayProgressBar && progressBarCanceled)
                    forceExit = true;
                // If the process naturally finishes, prepare to exit the loop.
                if(process.HasExited)
                    naturalExit = true;
                yield return null;
            }
            // Display the final log.
            if(!string.IsNullOrEmpty(logSegment))
                Debug.Log(FormatScriptMessage(callerType, logHeader + logSegment));
            // If the exit was not forced, indicate that the process has successfully finished.
            if(naturalExit)
                Debug.Log(FormatScriptMessage(callerType, "Finished command: " + command));
            // If the exit was forced, indicate that it was canceled by the user, as this prevents further processes from being launched immediately afterwards.
            else
                progressBarCanceled = true;
            // Clean up any created objects.
            process.Dispose();
            process = null;
            _outputDataReceived = null;
            _errorDataReceived = null;
        }

        /// <summary>
        /// On log messages being sent from a process, add them to a list of messages.
        /// </summary>
        /// <param name="sendingProcess"></param> The process sending data.
        /// <param name="outLine"></param> The data sent by the process.
        private static void ProcessOutputDataReceivedEventHandler(object sendingProcess, System.Diagnostics.DataReceivedEventArgs outLine)
        {
            if(!string.IsNullOrEmpty(outLine.Data))
            {
                if(_outputDataReceived == null)
                    _outputDataReceived = new List<string>();
                _outputDataReceived.Add(outLine.Data);
            }
        }

        /// <summary>
        /// On error messages being sent from a process, add them to a list of errors.
        /// </summary>
        /// <param name="sendingProcess"></param> The process sending data.
        /// <param name="outLine"></param> The data sent by the process.
        private static void ProcessErrorDataReceivedEventHandler(object sendingProcess, System.Diagnostics.DataReceivedEventArgs outLine)
        {
            if(!string.IsNullOrEmpty(outLine.Data))
            {
                if(_errorDataReceived == null)
                    _errorDataReceived = new List<string>();
                _errorDataReceived.Add(outLine.Data);
            }
        }

        /// <summary>
        /// Formats a path for use as an argument in a command line call.
        /// </summary>
        /// <param name="path"></param> The path to format.
        /// <returns></returns> The formatted path.
        public static string FormatPathForCommand(string path)
        {
            return "\"" + path + "\"";
        }

#endif //UNITY_EDITOR

#endregion //COMMAND_LINE

#region DEBUG_DIAGNOSTICS

        /// <summary>
        /// Exits the standalone application or exits play mode in the Editor, and writes an error message in the Debug console.
        /// </summary>
        /// <param name="errorMessage"></param> The error message to be displayed.
        public static void ExitWithError(string errorMessage)
        {
            UnityEngine.Debug.LogError(FormatScriptMessage(typeof(GeneralToolkit), errorMessage));
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#endif //UNITY_EDITOR
#if UNITY_STANDALONE
            Application.Quit();
#endif //UNITY_STANDALONE
        }

#if UNITY_EDITOR

        /// <summary>
        /// Clears the Debug console in the Editor.
        /// </summary>
        public static void ClearConsole()
        {
            System.Type logEntries = System.Type.GetType("UnityEditor.LogEntries, UnityEditor.dll");
            System.Reflection.MethodInfo clearMethod = logEntries.GetMethod("Clear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            clearMethod.Invoke(null, null);
        }

        /// <summary>
        /// Executes a given Method and displays its execution time in the Debug console.
        /// </summary>
        /// <param name="process"></param> The process to be executed (e.g. use (params) => method(params) to specify a method).
        /// <param name="processName"></param> The name of the process to be displayed in the console.
        public static void ExecuteAndDisplayExecutionTime(System.Action process, string processName = "Process")
        {
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            process.Invoke();
            stopwatch.Stop();
            UnityEngine.Debug.Log(FormatScriptMessage(typeof(GeneralToolkit), "Process (" + processName + ") executed in " + stopwatch.ElapsedMilliseconds + " ms."));
        }

#endif //UNITY_EDITOR

        /// <summary>
        /// Formats a message to display a timestamp and the type of the script that sent it.
        /// </summary>
        /// <param name="scriptType"></param> The type of the object sending the message.
        /// <param name="message"></param> The message to be displayed.
        public static string FormatScriptMessage(System.Type scriptType, string message)
        {
            return "<" + scriptType + "> at " + System.DateTime.Now.ToLongTimeString() + ": " + message;
        }

#if UNITY_EDITOR

        public static bool progressBarCanceled = false;

        private static float _progressBarValue;
        private static int _progressBarMaxIter;
        private static System.DateTime _progressBarStartTime;
        private static string _progressBarTitle;
        private static string _progressBarInfo;
        private static string _progressBarExitMessage;

        /// <summary>
        /// Displays a cancelable progress bar, and increments its value.true The progress bar's current value is a static variable.
        /// </summary>
        /// <param name="callerType"></param> The type of the object calling for the progress bar to be updated.
        /// <param name="incrementValue"></param> True if the progress bar's value should be incremented by this process, false otherwise.
        /// <param name="displayTimeEstimation"></param> True if the progress bar should attempt to estimate remaining time, false otherwise.
        /// <param name="displayStepNumber"></param> True if the progress bar should display an iteration number, false otherwise.
        /// <param name="progressBarMaxIter"></param> Maximum iteration, upon which the progress bar will be cleared.
        /// <param name="progressBarTitle"></param> Title for the progress bar.
        /// <param name="progressBarInfo"></param> Information to display on the progress bar.
        /// <param name="exitMessage"></param> Message to display should the progress bar be canceled.
        public static void UpdateCancelableProgressBar(System.Type callerType, bool incrementValue = true, bool displayTimeEstimation = true, bool displayStepNumber = true, int progressBarMaxIter = -1, string progressBarTitle = "", string progressBarInfo = "", string exitMessage = "")
        {
            // If some values were not specified, use the values from the previous progress bar.
            if(progressBarTitle == string.Empty)
                progressBarTitle = _progressBarTitle;
            _progressBarTitle = progressBarTitle;
            if(progressBarInfo == string.Empty)
                progressBarInfo = _progressBarInfo;
            _progressBarInfo = progressBarInfo;
            if(exitMessage == string.Empty)
                exitMessage = _progressBarExitMessage;
            _progressBarExitMessage = exitMessage;
            if(progressBarMaxIter == -1)
                progressBarMaxIter = _progressBarMaxIter;
            _progressBarMaxIter = progressBarMaxIter;
            // If the progress bar's value is zero, store the current time as its start time.
            if(_progressBarValue == 0f)
                _progressBarStartTime = System.DateTime.Now;
            // If the progress bar's value should be incremented, do so.
            if(incrementValue)
                _progressBarValue += 1f / progressBarMaxIter;
            // If the step number should be displayed, do so.
            if(displayStepNumber)
                progressBarInfo += ": " + Mathf.RoundToInt(_progressBarValue * progressBarMaxIter) + "/" + progressBarMaxIter + ".";
            // If remaining time should be estimated, do so.
            System.TimeSpan elapsedTime = System.DateTime.Now.Subtract(_progressBarStartTime);
            if(displayTimeEstimation && elapsedTime.TotalSeconds > 5f)
            {
                System.TimeSpan remainingTime = System.TimeSpan.FromSeconds(((1f - _progressBarValue) / _progressBarValue) * elapsedTime.TotalSeconds);
                int minutes = remainingTime.Minutes;
                int seconds = 30 * Mathf.RoundToInt(remainingTime.Seconds / 30f);
                if(seconds == 60)
                {
                    minutes += 1;
                    seconds = 0;
                }
                if(seconds == 0 && minutes == 0)
                {
                    progressBarInfo += " Less than 30 seconds left.";
                }
                else
                {
                    string minutesLabel = (minutes == 0) ? string.Empty : (minutes == 1) ? "1 minute " : minutes.ToString() + " minutes ";
                    string secondsLabel = (seconds == 0) ? string.Empty : (minutes == 0) ? "30 seconds " : "and 30 seconds ";
                    progressBarInfo += " About " + minutesLabel + secondsLabel + "left.";
                }
            }
            // Display the progress bar. If the user clicks to cancel it, display the exit message.
            if(EditorUtility.DisplayCancelableProgressBar(progressBarTitle, progressBarInfo, _progressBarValue))
            {
                Debug.Log(FormatScriptMessage(callerType, exitMessage));
                progressBarCanceled = true;
            }
        }

        /// <summary>
        /// Resets the progress bar value and clears the progress bar.
        /// </summary>
        /// <param name="hideGUI"></param> True if the GUI should be hidden, false otherwise.
        /// <param name="clearConsole"></param> True if the console should be cleared, false otherwise.
        public static void ResetCancelableProgressBar(bool hideGUI, bool clearConsole)
        {
            displayGUI = !hideGUI;
            if(clearConsole)
                ClearConsole();
            _progressBarValue = 0f;
            _progressBarMaxIter = 1;
            _progressBarTitle = string.Empty;
            _progressBarInfo = string.Empty;
            _progressBarExitMessage = string.Empty;
            progressBarCanceled = false;
            EditorUtility.ClearProgressBar();
        }
        
#endif //UNITY_EDITOR

#endregion //DEBUG_DIAGNOSTICS
 
#region SHADERS

        public static Shader shaderStandard { get { return Shader.Find("Standard"); } }
        public static Shader shaderUnlitTexture { get { return Shader.Find("Unlit/Texture"); } }
        public static Shader shaderNormalizeByAlpha { get { return Shader.Find("COLIBRIVR/Core/NormalizeByAlpha"); } }
        public static Shader shaderImageProcessing { get { return Shader.Find("COLIBRIVR/Core/ImageProcessing"); } }
        public static Shader shaderDebuggingConvertPreciseToVisualization { get { return Shader.Find("COLIBRIVR/Debugging/ConvertPreciseToVisualization"); } }
        public static Shader shaderAcquisitionRenderDistance { get { return Shader.Find("COLIBRIVR/Acquisition/RenderDistance"); } }
        public static Shader shaderAcquisitionConvert01ToColor { get { return Shader.Find("COLIBRIVR/Acquisition/Convert01ToColor"); } }
        public static Shader shaderProcessingGlobalTextureMap { get { return Shader.Find("COLIBRIVR/Processing/GlobalTextureMap"); } }
        public static Shader shaderRenderingTexturedGlobalMesh { get { return Shader.Find("COLIBRIVR/Rendering/TexturedGlobalMesh"); } }
        public static Shader shaderRenderingTexturedProxies { get { return Shader.Find("COLIBRIVR/Rendering/TexturedProxies"); } }
        public static Shader shaderRenderingTexturedPerViewMeshesDT { get { return Shader.Find("COLIBRIVR/Rendering/TexturedPerViewMeshesDT"); } }
        public static Shader shaderRenderingDiskBlendedFocalSurfaces { get { return Shader.Find("COLIBRIVR/Rendering/DiskBlendedFocalSurfaces"); } }
        public static Shader shaderRenderingDiskBlendedPerViewMeshes { get { return Shader.Find("COLIBRIVR/Rendering/DiskBlendedPerViewMeshes"); } }
        public static Shader shaderRenderingULR { get { return Shader.Find("COLIBRIVR/Rendering/ULR"); } }
        public static Shader shaderRenderingULRPerFragment { get { return Shader.Find("COLIBRIVR/Rendering/ULRPerFragment"); } }
        public static Shader shaderEvaluationYCbCr { get { return Shader.Find("COLIBRIVR/Evaluation/YCbCr"); } }
#if UNITY_EDITOR
        public static ComputeShader computeShaderPerViewMeshesQSTR { get { return (ComputeShader)Resources.Load("PerViewMeshesQSTR"); } }
#endif //UNITY_EDITOR

        /// <summary>
        /// CPU copy of the built-in shader method EncodeFloatRGBA.
        /// </summary>
        /// <param name="value01"></param> The value to encode.
        /// <returns></returns> The encoded vector.
        public static Vector4 UnityEncodeFloatRGBA(float value01)
        {
            Vector4 kEncodeMul = new Vector4(1f, 255f, 65025f, 16581375f);
            float kEncodeBit = 1f/255f;
            Vector4 enc = kEncodeMul * value01;
            enc.x = Mathf.Repeat(enc.x, 1.0f);
            enc.y = Mathf.Repeat(enc.y, 1.0f);
            enc.z = Mathf.Repeat(enc.z, 1.0f);
            enc.w = Mathf.Repeat(enc.w, 1.0f);
            enc -= new Vector4(enc.y, enc.z, enc.w, enc.w) * kEncodeBit;
            return enc;
        }

        /// <summary>
        /// CPU copy of the built-in shader method DecodeFloatRGBA.
        /// </summary>
        /// <param name="enc"></param> The value to decode.
        /// <returns></returns> The decoded float value (between 0 and 1).
        public static float UnityDecodeFloatRGBA(Vector4 enc)
        {
            Vector4 kDecodeDot = new Vector4(1.0f, 1/255.0f, 1/65025.0f, 1/16581375.0f);
            return Vector4.Dot(enc, kDecodeDot);
        }

        /// <summary>
        /// Decodes an RGB color with 24-bit precision as a 0-1 float.
        /// </summary>
        /// <param name="colorValue"></param> The color value to decode.
        /// <returns></returns> The decoded float value.
        public static float Decode01FromPreciseColor(Color colorValue)
        {
            return UnityDecodeFloatRGBA(new Vector4(colorValue.r, colorValue.g, colorValue.b, 1));
        }

#endregion //SHADERS

#region MESHES

        /// <summary>
        /// Transforms a mesh's vertices and normals by the values of the given transform.
        /// </summary>
        /// <param name="mesh"></param> The mesh to modify.
        /// <param name="tempTransform"></param> The transform to use as basis.
        public static void TransformMeshByTransform(ref Mesh mesh, Transform tempTransform)
        {
            Vector3[] vertices = mesh.vertices;
            for(int i = 0; i < mesh.vertexCount; i++)
                vertices[i] = tempTransform.TransformPoint(vertices[i]);
            Vector3[] normals = mesh.normals;
            for(int i = 0; i < mesh.vertexCount; i++)
                normals[i] = tempTransform.TransformDirection(normals[i]);
            mesh.vertices = vertices;
            mesh.normals = normals;
        }

        /// <summary>
        /// Transforms a mesh's vertices and normals by the given translation, rotation, and scale.
        /// </summary>
        /// <param name="mesh"></param> The mesh to modify.
        /// <param name="translation"></param> The translation by which to modify the mesh.
        /// <param name="rotation"></param> The rotation by which to modify the mesh.
        /// <param name="scale"></param> The scale by which to modify the mesh.
        public static void TransformMeshByTransformValues(ref Mesh mesh, Vector3 translation, Quaternion rotation, Vector3 scale)
        {
            Transform tempTransform = new GameObject("TempTransform").transform;
            GeneralToolkit.SetTransformValues(tempTransform, false, translation, rotation, scale);
            TransformMeshByTransform(ref mesh, tempTransform);
            GameObject.DestroyImmediate(tempTransform.gameObject);
        }

#endregion //MESHES

#region COLORS

        /// <summary>
        /// Computes a color from a given index and maximum index.
        /// </summary>
        /// <param name="index"></param> The index.
        /// <param name="maxIndex"></param> The maximum index.
        /// <returns></returns> The color.
        public static Color GetColorForIndex(int index, int maxIndex)
        {
            int baseColorCount = 6;
            int skipCount = 1;
            int skipIndex = index * (skipCount + 1);
            int skipIters = Mathf.FloorToInt(skipIndex * 1f / baseColorCount);
            int hLoopCount = Mathf.FloorToInt(index * 1f / baseColorCount);
            float h = ((skipIndex + skipIters) % baseColorCount + 1f - Mathf.Pow(0.5f, hLoopCount)) * 1f / baseColorCount;
            float s = 1f;
            float v = 1f - 0.5f * (index * 1f / maxIndex);
            return Color.HSVToRGB(h, s, v);
        }

#endregion //COLORS

#region TEXTURE_MANAGEMENT

        /// <summary>
        /// Loads the image at the specified path into a Texture2D.
        /// </summary>
        /// <param name="path"></param> Absolute path to the image on disk.
        /// <param name="output"></param> Texture2D in which to load the image.
        public static void LoadTexture(string path, ref Texture2D output)
        {
            output.LoadImage(File.ReadAllBytes(path));
        }

        /// <summary>
        /// Loads a texture asynchronously from file.
        /// </summary>
        /// <param name="path"></param> The path to the texture.
        /// <param name="outputTexture"></param> The reference to the output texture.
        /// <returns></returns>
        public static IEnumerator LoadTextureAsync(string path, Texture2D[] outputTextureRef)
        {
            if(outputTextureRef == null || outputTextureRef.Length < 1)
                outputTextureRef = new Texture2D[1];
            if(outputTextureRef[0] != null)
                Texture2D.DestroyImmediate(outputTextureRef[0]);
            using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(path))
            {
                yield return uwr.SendWebRequest();
                if (uwr.isNetworkError || uwr.isHttpError)
                    Debug.LogError(FormatScriptMessage(typeof(GeneralToolkit), uwr.error));
                else
                    outputTextureRef[0] = DownloadHandlerTexture.GetContent(uwr);
            }
        }

        /// <summary>
        /// Saves a specified Texture2D into a PNG file.
        /// </summary>
        /// <param name="texture"></param> The Texture2D to save as a PNG file.
        /// <param name="filePath"></param> Absolute path in which to save the file.
        public static void SaveTexture2DToPNG(Texture2D texture, string filePath)
        {
            File.WriteAllBytes(filePath, texture.EncodeToPNG());
        }

        /// <summary>
        /// Saves a specified RenderTexture into a PNG file.
        /// </summary>
        /// <param name="renderTexture"></param> The RenderTexture to save as a PNG file.
        /// <param name="filePath"></param> Absolute path in which to save the file.
        public static void SaveRenderTextureToPNG(RenderTexture renderTexture, string filePath)
        {
            Texture2D tempTex = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.ARGB32, true, renderTexture.sRGB);
            CopyRenderTextureToTexture2D (renderTexture, ref tempTex);
            SaveTexture2DToPNG(tempTex, filePath);
            Texture2D.DestroyImmediate (tempTex);
        }

        /// <summary>
        /// Copies the specified RenderTexture contents and format into the specified Texture2D.
        /// The RenderTexture and Texture2D must already have the same width and height. 
        /// </summary>
        /// <param name="renderTexture"></param> The RenderTexture to copy from.
        /// <param name="output"></param> The Texture2D to copy into.
        /// <returns></returns> Returns true if the operation was successful, false if the textures had different dimensions.
        public static bool CopyRenderTextureToTexture2D(RenderTexture renderTexture, ref Texture2D output)
        {
            if(renderTexture.width != output.width || renderTexture.height != output.height)
            {
                UnityEngine.Debug.LogError(FormatScriptMessage(typeof(GeneralToolkit), "Copying RenderTexture (" + renderTexture.width + "," + renderTexture.height + ") into Texture2D (" + output.width + "," + output.height + ") is not allowed."));
                return false;
            }
            output.filterMode = renderTexture.filterMode;
            output.wrapMode = renderTexture.wrapMode;
            RenderTexture.active = renderTexture;
            output.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            output.Apply();
            RenderTexture.active = null;
            return true;
        }

        /// <summary>
        /// Creates a Texture2D with the specified parameters.
        /// </summary>
        /// <param name="output"></param> The Texture2D that is created.
        /// <param name="resolution"></param> Texture width and height in pixels.
        /// <param name="textureFormat"></param> Texture format.
        /// <param name="linear"></param> True if the texture contains non-color data, false if not.
        /// <param name="filterMode"></param> Filtering mode of the texture.
        /// <param name="wrapMode"></param> Texture coordinate wrapping mode.
        /// <param name="mipChain"></param> True if with mipmaps, false if without.
        public static void CreateTexture2D(ref Texture2D output, Vector2Int resolution, TextureFormat textureFormat = TextureFormat.RGBA32,
            bool linear = false, FilterMode filterMode = FilterMode.Bilinear, TextureWrapMode wrapMode = TextureWrapMode.Clamp, bool mipChain = true)
        {
            Texture2D.DestroyImmediate(output);
            output = new Texture2D(resolution.x, resolution.y, textureFormat, mipChain, linear);
            output.filterMode = filterMode;
            output.wrapMode = wrapMode;
        }

        /// <summary>
        /// Creates a RenderTexture with the specified parameters.
        /// </summary>
        /// <param name="output"></param> The render texture that is created.
        /// <param name="resolution"></param> Texture width and height in pixels.
        /// <param name="depth"></param> Number of bits in depth buffer (0, 16 or 24).
        /// <param name="format"></param> Texture format.
        /// <param name="linear"></param> True if the texture contains non-color data, false if not.
        /// <param name="filterMode"></param> Filtering mode of the texture.
        /// <param name="wrapMode"></param> Texture coordinate wrapping mode.
        public static void CreateRenderTexture(ref RenderTexture output, Vector2Int resolution, int depth = 0, RenderTextureFormat format = RenderTextureFormat.Default,
            bool linear = false, FilterMode filterMode = FilterMode.Bilinear, TextureWrapMode wrapMode = TextureWrapMode.Clamp)
        {
            RenderTexture.active = null;
            if(output != null)
                RenderTexture.DestroyImmediate(output);
            RenderTextureReadWrite readWrite = linear ? RenderTextureReadWrite.Linear : RenderTextureReadWrite.sRGB;
            output = new RenderTexture(resolution.x, resolution.y, depth, format, readWrite);
            output.filterMode = filterMode;
            output.wrapMode = wrapMode;
        }

        /// <summary>
        /// Creates a Texture2DArray with the specified parameters.
        /// </summary>
        /// <param name="output"></param> The Texture2DArray that is created.
        /// <param name="resolution"></param> Texture array width and height in pixels.
        /// <param name="depth"></param> Number of elements in the texture array.
        /// <param name="textureFormat"></param> Texture format.
        /// <param name="linear"></param> True if the texture contains non-color data, false if not.
        /// <param name="filterMode"></param> Filtering mode of the texture.
        /// <param name="wrapMode"></param> Texture coordinate wrapping mode.
        /// <param name="mipChain"></param> True if with mipmaps, false if without.
        public static void CreateTexture2DArray(ref Texture2DArray output, Vector2Int resolution, int depth = 1, TextureFormat textureFormat = TextureFormat.RGBA32,
            bool linear = false, FilterMode filterMode = FilterMode.Bilinear, TextureWrapMode wrapMode = TextureWrapMode.Clamp, bool mipChain = true)
        {
            Texture2DArray.DestroyImmediate(output);
            output = new Texture2DArray(resolution.x, resolution.y, depth, textureFormat, mipChain, linear);
            output.filterMode = filterMode;
            output.wrapMode = wrapMode;
        }

        /// <summary>
        /// Resizes a given Texture into a referenced output Texture2D.
        /// </summary>
        /// <param name="input"></param> The input texture, from which the color data will be taken.
        /// <param name="output"></param> The output texture, which has the desired dimensions.
        public static void ResizeTexture2D(Texture input, ref Texture2D output)
        {
            RenderTexture temp = new RenderTexture(1, 1, 0);
            CreateRenderTexture(ref temp, new Vector2Int(output.width, output.height), 0, RenderTextureFormat.Default, false, output.filterMode, output.wrapMode);
            Graphics.Blit(input, temp);
            CopyRenderTextureToTexture2D(temp, ref output);
            RenderTexture.DestroyImmediate(temp);
        }

        /// <summary>
        /// Applies an image processing operation to the given render texture.
        /// </summary>
        /// <param name="renderTex"></param> The render texture on which to apply the effect.
        /// <param name="iterationCount"></param> The number of times the effect should be applied.
        /// <param name="kernelType"></param> The type of kernel to use for the effect.
        /// <param name="operationType"></param> The index of the type of operation to apply.
        /// <param name="isZeroMask"></param> True if zero values are special in that they are used by this effect as a mask, false otherwise.
        /// <param name="ignoreAlphaChannel"></param> True if the alpha channel should be ignored for this effect, false otherwise.
        private static void RenderTextureApplyImageProcessing(ref RenderTexture renderTex, int iterationCount, ImageProcessingKernelType kernelType, int operationType, bool isZeroMask, bool ignoreAlphaChannel)
        {
            const string shaderNamePixelResolution = "_PixelResolution";
            const string shaderNameOperationType = "_OperationType";
            const string shaderNameKernelType = "_KernelType";
            const string shaderNameIsZeroMask = "_IsZeroMask";
            const string shaderNameIgnoreAlphaChannel = "_IgnoreAlphaChannel";
            Material mat = new Material(shaderImageProcessing);
            mat.SetVector(shaderNamePixelResolution, new Vector4(renderTex.width, renderTex.height, 0, 0));
            mat.SetInt(shaderNameOperationType, operationType);
            mat.SetInt(shaderNameKernelType, kernelType == ImageProcessingKernelType.Gaussian ? 0 : 1);
            mat.SetInt(shaderNameIsZeroMask, isZeroMask ? 1 : 0);
            mat.SetInt(shaderNameIgnoreAlphaChannel, ignoreAlphaChannel ? 1 : 0);
            RenderTexture tempRT = new RenderTexture(1, 1, 0);
            CreateRenderTexture(ref tempRT, new Vector2Int(renderTex.width, renderTex.height), renderTex.depth, renderTex.format, !renderTex.sRGB, renderTex.filterMode, renderTex.wrapMode);
            for(int i = 0; i < iterationCount; i++)
            {
                Graphics.Blit(renderTex, tempRT, mat);
                Graphics.Blit(tempRT, renderTex);
            }
            Material.DestroyImmediate(mat);
            RenderTexture.DestroyImmediate(tempRT);
        }

        /// <summary>
        /// Applies a blur effect to the given render texture.
        /// </summary>
        /// <param name="renderTex"></param> The render texture on which to apply the effect.
        /// <param name="iterationCount"></param> The number of times the effect should be applied.
        /// <param name="kernelType"></param> The type of kernel to use for the effect.
        /// <param name="isZeroMask"></param> True if zero values are special in that they are used by this effect as a mask, false otherwise.
        /// <param name="ignoreAlphaChannel"></param> True if the alpha channel should be ignored for this effect, false otherwise.
        public static void RenderTextureApplyBlur(ref RenderTexture renderTex, int iterationCount, ImageProcessingKernelType kernelType, bool isZeroMask, bool ignoreAlphaChannel)
        {
            RenderTextureApplyImageProcessing(ref renderTex, iterationCount, kernelType, 1, isZeroMask, ignoreAlphaChannel);
        }
        
        /// <summary>
        /// Applies a morphological dilation effect to the given render texture.
        /// </summary>
        /// <param name="renderTex"></param> The render texture on which to apply the effect.
        /// <param name="iterationCount"></param> The number of times the effect should be applied.
        /// <param name="kernelType"></param> The type of kernel to use for the effect.
        /// <param name="ignoreAlphaChannel"></param> True if the alpha channel should be ignored for this effect, false otherwise.
        public static void RenderTextureApplyMorphologicalDilation(ref RenderTexture renderTex, int iterationCount, ImageProcessingKernelType kernelType, bool ignoreAlphaChannel)
        {
            RenderTextureApplyImageProcessing(ref renderTex, iterationCount, kernelType, 2, true, ignoreAlphaChannel);
        }
        
        /// <summary>
        /// Applies a morphological erosion effect to the given render texture.
        /// </summary>
        /// <param name="renderTex"></param> The render texture on which to apply the effect.
        /// <param name="iterationCount"></param>  The number of times the effect should be applied.
        /// <param name="ignoreAlphaChannel"></param> True if the alpha channel should be ignored for this effect, false otherwise.
        public static void RenderTextureApplyMorphologicalErosion(ref RenderTexture renderTex, int iterationCount, bool ignoreAlphaChannel)
        {
            RenderTextureApplyImageProcessing(ref renderTex, iterationCount, ImageProcessingKernelType.Gaussian, 3, true, ignoreAlphaChannel);
        }

#endregion //TEXTURE_MANAGEMENT
 
#region ASSET_BUNDLES

#if UNITY_EDITOR

        /// <summary>
        /// Creates an asset and unloads it immediately from memory.
        /// </summary>
        /// <param name="asset"></param> The asset to create.
        /// <param name="path"></param> The path at which to create the asset.
        public static void CreateAndUnloadAsset(Object asset, string path)
        {
            AssetDatabase.CreateAsset(asset, path);
            UnloadAsset(asset);
            Resources.UnloadUnusedAssets();
            EditorUtility.UnloadUnusedAssetsImmediate();
        }

        /// <summary>
        /// Unloads an asset from memory.
        /// </summary>
        /// <param name="asset"></param> The asset to unload.
        public static void UnloadAsset(Object asset)
        {
            Resources.UnloadAsset(asset);
        }

        /// <summary>
        /// Creates an asset bundle at the specified path with the specified assets.
        /// </summary>
        /// <param name="bundleDirectory"></param> Absolute path to the directory in which to create the AssetBundle.
        /// <param name="bundleName"></param> Name that will be given to the AssetBundle.
        /// <param name="assetRelativePaths"></param> Relative (e.g. Assets/...) paths of the assets to include in the AssetBundle.
        /// <returns></returns> Returns true if the operation was successful, false if the directory cannot be modified or an exception was caught.
        public static bool CreateAssetBundle(string bundleDirectory, string bundleName, string[] assetRelativePaths)
        {
            // Create or clear the destination directory.
            CreateOrClear(PathType.Directory, bundleDirectory);
            // Compute the objects required to build the asset bundle.
            string relativeBundleDirectory = ToRelativePath(bundleDirectory);
            AssetBundleBuild[] bundles = new AssetBundleBuild[] { new AssetBundleBuild() { assetBundleName = bundleName, assetNames = assetRelativePaths } };
            // Try to build the asset bundles for the current editor's platform.
            try
            {
                BuildTarget platform = BuildTarget.StandaloneWindows;
#if UNITY_EDITOR_OSX
                platform = BuildTarget.StandaloneOSX;
#endif //UNITY_EDITOR_OSX
                BuildPipeline.BuildAssetBundles(relativeBundleDirectory, bundles, BuildAssetBundleOptions.None, platform);
                return true;
            }
            // Display any exceptions.
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError(FormatScriptMessage(typeof(GeneralToolkit), e.ToString()));
                return false;
            }
        }
#endif //UNITY_EDITOR

        private static AssetBundle _loadedBundle;
        private static string _loadedBundlePath;

        /// <summary>
        /// Coroutine that loads the asset bundle corresponding to the given path.
        /// </summary>
        /// <param name="bundlePath"></param> The path to the bundle.
        /// <returns></returns>
        public static IEnumerator LoadAssetBundleIntoMemory(string bundlePath)
        {
            // Wait for other bundles to be loaded and unloaded.
            bool isLoadingThread = false;
            while(_loadedBundlePath != bundlePath)
            {
                if (string.IsNullOrEmpty(_loadedBundlePath))
                {
                    _loadedBundlePath = bundlePath;
                    isLoadingThread = true;
                }
                yield return null;
            }
            // If another coroutine is charged with loading the bundle, wait for it to complete.
            if(!isLoadingThread)
            {
                while (_loadedBundle == null && _loadedBundlePath == bundlePath)
                {
                    yield return null;
                }
            }
            // Otherwise, load the bundle.
            else
            {
                string suffix = " asset bundle at path: " + bundlePath + ".";
                UnityEngine.Debug.Log(FormatScriptMessage(typeof(GeneralToolkit), "Please wait, started to load" + suffix));
                // Wait for the bundle to load.
                AssetBundleCreateRequest request = AssetBundle.LoadFromFileAsync(bundlePath);
                yield return request;
                _loadedBundle = request.assetBundle;
                // If the bundle could not be loaded, inform the user.
                if(_loadedBundle == null)
                {
                    UnityEngine.Debug.LogError(FormatScriptMessage(typeof(GeneralToolkit), "Failed to load" + suffix));
                    UnloadAssetBundleInMemory(true);
                }
                // Otherwise, inform the user.
                else
                {
                    UnityEngine.Debug.Log(FormatScriptMessage(typeof(GeneralToolkit), "Successfully loaded" + suffix));
                }
            }
        }

        /// <summary>
        /// Unloads the compressed file data for assets from the asset bundle currently in memory.
        /// </summary>
        /// <param name="unloadAllLoadedObjects"></param> True if all objects loaded from the bundle should also be destroyed, false otherwise.
        public static void UnloadAssetBundleInMemory(bool unloadAllLoadedObjects)
        {
            if(_loadedBundle != null)
            {
                _loadedBundle.Unload(unloadAllLoadedObjects);
                UnityEngine.Debug.Log(FormatScriptMessage(typeof(GeneralToolkit), "Unloaded asset bundle that had been loaded from path:" + _loadedBundlePath + "."));
            }
            _loadedBundle = null;
            _loadedBundlePath = null;
        }

        /// <summary>
        /// Loads assets from the specified bundle.
        /// </summary>
        /// <param name="OutputAssets"></param> Outputs the desired assets.
        /// <param name="assetNames"></param> The names of the assets to retrieve in the asset bundle.
        /// <typeparam name="T"></typeparam> The type of the assets to load.
        /// <returns></returns>
        public static IEnumerator LoadAssetsFromBundleInMemory<T>(System.Action<T[]> OutputAssets, params string[] assetNames) where T : UnityEngine.Object
        {
            // Check that the bundle is loaded.
            if(_loadedBundle == null)
            {
                UnityEngine.Debug.LogError(FormatScriptMessage(typeof(GeneralToolkit), "Failed to load assets because bundle at path: " + _loadedBundlePath + " was not in memory."));
            }
            // If a bundle is loaded, load each asset by name.
            else
            {
                List<T> output = new List<T>();
                for(int i = 0; i < assetNames.Length; i++)
                {
                    AssetBundleRequest request = _loadedBundle.LoadAssetAsync<T>(assetNames[i]);
                    yield return request;
                    T loadedAsset = (T)request.asset;
                    output.Add(loadedAsset);
                    string suffix = " asset: " + assetNames[i] + " of type: " + typeof(T) + " from asset bundle at path: " + _loadedBundlePath + ".";
                    if(loadedAsset == null)
                        UnityEngine.Debug.LogError(FormatScriptMessage(typeof(GeneralToolkit), "Failed to load" + suffix));
                    else
                        UnityEngine.Debug.Log(FormatScriptMessage(typeof(GeneralToolkit), "Successfully loaded" + suffix));
                }
                OutputAssets(output.ToArray());
            }
        }

#endregion //ASSET_BUNDLES

#region EDITOR_WINDOWS

#if UNITY_EDITOR

        /// <summary>
        /// Returns an Editor window given its type as a string.
        /// </summary>
        /// <param name="windowType"></param> The type of the Editor window.
        /// <returns></returns>
        public static EditorWindow GetEditorWindowByType(string windowType)
        {
            return EditorWindow.GetWindow(typeof(UnityEditor.EditorWindow).Assembly.GetType(windowType));
        }

        public static EditorWindow gameWindow { get { return GetEditorWindowByType("UnityEditor.GameView"); } }
        public static EditorWindow inspectorWindow { get { return GetEditorWindowByType("UnityEditor.InspectorWindow"); } }

#endif //UNITY_EDITOR

#endregion //EDITOR_WINDOWS

#region EDITOR_GUI_LAYOUT

#if UNITY_EDITOR

        public static float addedSpace = 0f;
        public static GUIStyle wordWrapStyle { get { GUIStyle outStyle = new GUIStyle(); outStyle.wordWrap = true; return outStyle; } }
        public static string backgroundGUIColor { get { return EditorGUIUtility.isProSkin ? "#383838" : "#c2c2c2"; } }
        public static bool displayGUI
        {
            private get { return _displayGUI; }
            set { _displayGUI = value; }
        }
        private static bool _displayGUI = true;

        /// <summary>
        /// Adds the first building block of a custom editor for a given MonoBehaviour.
        /// </summary>
        /// <param name="serializedObject"></param> The concerned serialized object.
        /// <param name="monoBehaviour"></param> The concerned MonoBehaviour.
        public static void EditorStart(SerializedObject serializedObject, MonoBehaviour monoBehaviour)
        {
            EditorStart(serializedObject, MonoScript.FromMonoBehaviour(monoBehaviour));
        }

        /// <summary>
        /// Adds the first building block of a custom editor for a given ScriptableObject.
        /// </summary>
        /// <param name="serializedObject"></param> The concerned serialized object.
        /// <param name="scriptableObject"></param> The concerned ScriptableObject.
        public static void EditorStart(SerializedObject serializedObject, ScriptableObject scriptableObject)
        {
            EditorStart(serializedObject, MonoScript.FromScriptableObject(scriptableObject));
        }

        /// <summary>
        /// Adds the first building block of a custom editor for a given MonoScript.
        /// </summary>
        /// <param name="serializedObject"></param> The concerned serialized object.
        /// <param name="monoScript"></param> The concerned MonoScript.
        private static void EditorStart(SerializedObject serializedObject, MonoScript monoScript)
        {
            serializedObject.Update();
            EditorGUILayout.Space();
            GUI.enabled = false;
            EditorGUILayout.ObjectField("Script", monoScript, typeof(MonoScript), false);
            GUI.enabled = displayGUI;
        }

        /// <summary>
        /// Adds a line separator to the custom editor.
        /// </summary>
        public static void EditorLineSeparator()
        {
            int thickness = 2;
            int padding = 10;
            Rect rect = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            rect.height = thickness;
            rect.y += padding/2;
            rect.x -= 2;
            rect.width += 6;
            EditorGUI.DrawRect(rect, Color.gray);
        }

        /// <summary>
        /// Adds a new section to the custom editor.
        /// </summary>
        /// <param name="sectionTitle"></param> The section's title.
        public static void EditorNewSection(string sectionTitle)
        {
            EditorLineSeparator();
            EditorGUILayout.LabelField(sectionTitle, EditorStyles.boldLabel);
        }

        /// <summary>
        /// Gets the width of the current scope, accounting for indent levels and added space.
        /// </summary>
        /// <returns></returns> The width of the current scope.
        public static float EditorGetCurrentScopeWidth()
        {
            float scopeWidth = EditorGUIUtility.currentViewWidth;
            scopeWidth -= 6f * EditorGUI.indentLevel;
            scopeWidth -= addedSpace;
            return scopeWidth;
        }

        /// <summary>
        /// Adds a left toggle with word wrapping.
        /// </summary>
        /// <param name="guiContent"></param> The GUIContent to display.
        /// <param name="toggleProperty"></param> The property that the toggle modifies.
        public static void EditorWordWrapLeftToggle(GUIContent guiContent, SerializedProperty toggleProperty)
        {
            using (var horizontalScope = new EditorGUILayout.HorizontalScope())
            {
                float scopeWidth = EditorGetCurrentScopeWidth();
                toggleProperty.boolValue = EditorGUILayout.ToggleLeft(string.Empty, toggleProperty.boolValue, GUILayout.MaxWidth(scopeWidth));
                GUILayout.Space(- scopeWidth + 6f * EditorGUI.indentLevel + 42f);
                using (var verticalScope = new EditorGUILayout.VerticalScope())
                {
                    GUILayout.Space(3f);
                    EditorGUILayout.LabelField(guiContent, wordWrapStyle, GUILayout.MaxWidth(scopeWidth));
                }
            }
        }

        /// <summary>
        /// Adds a left button with word wrapping.
        /// </summary>
        /// <param name="buttonContent"></param> The GUIContent to display in the label.
        /// <param name="labelContent"></param> The GUIContent to display in the label.
        public static bool EditorWordWrapLeftButton(GUIContent buttonContent, GUIContent labelContent)
        {
            bool hasPressed = false;
            using (var horizontalScope = new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.Space();
                float scopeWidth = EditorGetCurrentScopeWidth();
                hasPressed = GUILayout.Button(buttonContent);
                using (var verticalScope = new EditorGUILayout.VerticalScope())
                {
                    GUILayout.Space(6f);
                    EditorGUILayout.LabelField(labelContent, wordWrapStyle, GUILayout.MaxWidth(scopeWidth));
                }
            }
            return hasPressed;
        }

        /// <summary>
        /// Adds a directory search feature in the custom editor.
        /// </summary>
        /// <param name="clicked"></param> Outputs true if the button was clicked, false otherwise.
        /// <param name="outPath"></param> Outputs the result of the path search.
        /// <param name="pathType"></param> The type of path being searched for.
        /// <param name="currentPath"></param> The current path value.
        /// <param name="searchTitle"></param> The information to display as title of the search panel.
        /// <param name="tooltip"></param> The information to display as a tooltip for the button.
        /// <param name="restrictToProject"></param> True if the search is valid only within the project, false otherwise.
        /// <param name="extensions"></param> Extensions to search for.
        /// <returns></returns> The new value of the path.
        public static void EditorPathSearch(out bool clicked, out string outPath,
            PathType pathType, string currentPath, string searchTitle, string tooltip, Color textColor = default(Color),
            bool restrictToProject = true, string extensions = "")
        {
            if(string.IsNullOrEmpty(currentPath))
                currentPath = Application.dataPath;
            outPath = currentPath;
            using (var horizontalScope = new EditorGUILayout.HorizontalScope())
            {
                // Display the name of the current path.
                using (var verticalScope = new EditorGUILayout.VerticalScope())
                {
                    GUILayout.Space(5f);
                    float labelWidth = EditorGetCurrentScopeWidth() * 2f / 3;
                    GUIStyle textStyle = wordWrapStyle;
                    if(textColor != default(Color))
                        textStyle.normal.textColor = textColor;
                    EditorGUILayout.LabelField(currentPath, textStyle, GUILayout.MaxWidth(labelWidth));
                    GUILayout.Space(5f);
                }
                GUILayout.Space(10f);
                // Display a button to change the path.
                string label = "Change...";
                clicked = GUILayout.Button(new GUIContent(label, tooltip));
                if(clicked)
                {
                    string path = string.Empty;
                    // Open a panel, with filters if there are any.
                    if(pathType == PathType.Directory)
                        path = EditorUtility.OpenFolderPanel(searchTitle, currentPath, "");
                    else
                        path = EditorUtility.OpenFilePanel(searchTitle, GetDirectoryBefore(currentPath), extensions);
                    // Only change the path's value if it is valid.
                    if(string.IsNullOrEmpty(path))
                    {
                        Debug.LogWarning(FormatScriptMessage(typeof(GeneralToolkit), "Specified path is empty. Path was not changed."));
                    }
                    else
                    {
                        path = Path.GetFullPath(path);
                        if (restrictToProject && !path.Contains(GetProjectDirectoryPath()))
                            Debug.LogWarning(FormatScriptMessage(typeof(GeneralToolkit), "Specified path " + path + " is not in project folder. Path was not changed."));
                        else
                            outPath = path;
                    }
                }
            }
        }

        /// <summary>
        /// Adds the final building block of a custom editor.
        /// </summary>
        /// <param name="serializedObject"></param> The concerned serialized object.
        public static void EditorEnd(SerializedObject serializedObject)
        {
            EditorGUILayout.Space();
            serializedObject.ApplyModifiedProperties();
            GUI.enabled = true;
        }

        /// <summary>
        /// Disables display and provides a corresponding tooltip if the editor is not in Play mode.
        /// </summary>
        /// <param name="tooltip"></param>
        public static void EditorRequirePlayMode(ref string tooltip)
        {
            if(!Application.isPlaying)
            {
                GUI.enabled = false;
                tooltip += "\nThis functionality requires Play mode to be launched.";
            }
        }

#endif //UNITY_EDITOR

#endregion //EDITOR_GUI_LAYOUT

    }

}
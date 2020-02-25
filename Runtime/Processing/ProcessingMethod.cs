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
    /// Abstract class to be used as parent for color and geometry processing methods.
    /// </summary>
    public abstract class ProcessingMethod : Method, IMethodGUI
    {

#region CONST_FIELDS

        public const string PMTransformName = "ProcessingMethods";
        public const int indexColorTextureArray = 1;
        public const int indexPerViewMeshesFS = 2;
        public const int indexPerViewMeshesQSTR = 3;
        public const int indexGlobalMeshEF = 4;
        public const int indexDepthTextureArray = 5;
        public const int indexGlobalTextureMap = 6;
        public const int indexPerViewMeshesQSTRDTA = 7;

#endregion //CONST_FIELDS

#region STATIC_PROPERTIES

        public static System.Type[] PMTypes
        {
            get
            {
                return new System.Type[]
                {
                    typeof(ProcessingMethodTemplate),
                    typeof(ColorTextureArray),
                    typeof(PerViewMeshesFS),
                    typeof(PerViewMeshesQSTR),
                    typeof(GlobalMeshEF),
                    typeof(DepthTextureArray),
                    typeof(GlobalTextureMap),
                    typeof(PerViewMeshesQSTRDTA)
                };
            }
        }

#endregion //STATIC_PROPERTIES

#region STATIC_METHODS

        /// <summary>
        /// Creates or resets the entire set of processing methods as children of the given parent transform.
        /// </summary>
        /// <param name="parentTransform"></param> The parent transform.
        /// <returns></returns> The array of processing methods.
        public static ProcessingMethod[] CreateOrResetProcessingMethods(Transform parentTransform)
        {
            ProcessingMethod[] methods = GeneralToolkit.GetOrCreateChildComponentGroup<ProcessingMethod>(PMTypes, PMTransformName, parentTransform);
            for(int iter = 0; iter < methods.Length; iter++)
                methods[iter].Reset();
            return methods;
        }

#endregion //STATIC_METHODS

#region FIELDS

        public bool shouldExecute;

        [SerializeField] protected ProcessingMethod[] _nestedMethodsToDisplay;

#endregion //FIELDS

#if UNITY_EDITOR

#region INHERITANCE_METHODS

        /// <inheritdoc/>
        public override void Reset()
        {
            base.Reset();
            shouldExecute = false;
        }

        /// <inheritdoc/>
        public abstract GUIContent GetGUIInfo();

        /// <inheritdoc/>
        public abstract bool HasAdditionalParameters();
        
        /// <inheritdoc/>
        public abstract void SectionAdditionalParameters();

        /// <summary>
        /// Gets the name of the type of data that is processed by this method.
        /// </summary>
        /// <returns></returns> A GUIContent containing this name.
        public abstract GUIContent GetProcessedDataName();

        /// <summary>
        /// Indicates whether this method is compatible with the given number of data samples.
        /// </summary>
        /// <param name="colorDataCount"></param> The number of color data samples.
        /// <param name="depthDataCount"></param> The number of source depth maps (per-view).
        /// <param name="meshDataCount"></param> The number of source meshes (global).
        /// <returns></returns> True if the method is compatible, false otherwise.
        public abstract bool IsCompatible(int colorDataCount, int depthDataCount, int meshDataCount);

        /// <summary>
        /// Indicates whether this method's GUI should be displayed on the topmost level, or is nested at a lower level (and thus displayed by first activating a required method).
        /// </summary>
        /// <returns></returns> True if the GUI is nested, false otherwise.
        public abstract bool IsGUINested();

        /// <summary>
        /// Deactivates processing methods on the same object that cannot be executed at the same time as this one.
        /// </summary>
        public abstract void DeactivateIncompatibleProcessingMethods();

        /// <summary>
        /// Coroutine that executes the processing method.
        /// </summary>
        /// <param name="caller"></param> The processing component calling this processing method.
        /// <returns></returns>
        protected abstract IEnumerator ExecuteMethodCoroutine();

        /// <summary>
        /// Indicates whether this method's nested GUI is enabled, based on the currently activated processing methods.
        /// That the parent processing method has to be enabled is also a pre-requisite, but does not have to be indicated here (it is already taken into account elsewhere).
        /// </summary>
        public virtual bool IsNestedGUIEnabled()
        {
            return true;
        }

#endregion //INHERITANCE_METHODS

#region METHODS

#if UNITY_EDITOR

        /// <summary>
        /// Enables the user to choose whether or not to activate this processing method.
        /// </summary>
        /// <param name="isCompatible"></param> True if the method is compatible with the source data, false otherwise.
        /// <returns></returns> True if the method is selected (i.e. should execute), false otherwise.
        public bool SectionShouldExecute(bool isCompatible)
        {
            SerializedObject serializedObject = new SerializedObject(this);
            serializedObject.Update();
            SerializedProperty propertyShouldExecute = serializedObject.FindProperty("shouldExecute");
            GeneralToolkit.EditorWordWrapLeftToggle(GetGUIInfo(), propertyShouldExecute);
            propertyShouldExecute.boolValue = propertyShouldExecute.boolValue && isCompatible;
            bool outIsSelected = propertyShouldExecute.boolValue;
            if(outIsSelected)
                DeactivateIncompatibleProcessingMethods();
            serializedObject.ApplyModifiedProperties();
            return outIsSelected;
        }

        /// <summary>
        /// Enables the user to activate this processing method and define its parameters.
        /// </summary>
        /// <param name="isCompatible"></param> True if the method is compatible with the source data, false otherwise.
        public void SectionProcessingMethod(bool isCompatible)
        {
            bool isSelected = SectionShouldExecute(isCompatible);
            if(isSelected && HasAdditionalParameters())
            {
                using (var horizontalScope = new EditorGUILayout.HorizontalScope())
                {
                    GeneralToolkit.addedSpace = 20f * EditorGUI.indentLevel;
                    GUILayout.Space(GeneralToolkit.addedSpace);
                    using (var verticalScope = new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        SectionAdditionalParameters();
                    }
                    GeneralToolkit.addedSpace = 0f;
                }
            }
            if(_nestedMethodsToDisplay != null && _nestedMethodsToDisplay.Length > 0)
            {
                using(var indentScope = new EditorGUI.IndentLevelScope())
                {
                    for(int iter = 0; iter < _nestedMethodsToDisplay.Length; iter++)
                    {
                        bool isNestedMethodCompatible = shouldExecute && _nestedMethodsToDisplay[iter].IsNestedGUIEnabled();
                        bool isGUIEnabled = GUI.enabled;
                        GUI.enabled = isGUIEnabled && isNestedMethodCompatible;
                        _nestedMethodsToDisplay[iter].SectionProcessingMethod(isNestedMethodCompatible);
                        GUI.enabled = isGUIEnabled;
                    }
                }
            }
        }

#endif //UNITY_EDITOR

        /// <summary>
        /// Executes the processing method and displays a log.
        /// </summary>
        /// <returns></returns>
        public IEnumerator ExecuteAndDisplayLog()
        {
            Debug.Log(GeneralToolkit.FormatScriptMessage(typeof(Processing), "Executing processing method: " + this.GetType() + "."));
            yield return StartCoroutine(ExecuteMethodCoroutine());
        }

        /// <summary>
        /// Gets the name of the bundled asset.
        /// </summary>
        /// <param name="assetName"></param> The name of the asset.
        /// <returns></returns> The name of the bundled asset.
        protected string GetBundledAssetName(string assetName)
        {
            return processingCaller.dataHandler.GetBundledAssetName(this, assetName);
        }

        /// <summary>
        /// Gets the absolute path to the bundled asset.
        /// </summary>
        /// <param name="bundledAssetName"></param> The name of the bundled asset.
        /// <returns></returns>
        protected string GetAssetPathAbsolute(string bundledAssetName)
        {
            return Path.Combine(GeneralToolkit.tempDirectoryAbsolutePath, bundledAssetName + ".asset");
        }

        /// <summary>
        /// Gets the path to the bundled asset, relative to the project folder.
        /// </summary>
        /// <param name="bundledAssetName"></param> The name of the bundled asset.
        /// <returns></returns>
        protected string GetAssetPathRelative(string bundledAssetName)
        {
            return Path.Combine(GeneralToolkit.tempDirectoryRelativePath, bundledAssetName + ".asset");
        }

        public IEnumerator ExecuteAndWaitForMemoryRelease(IEnumerator executeCoroutine)
        {
            // Reset the progress bar.
            GeneralToolkit.ResetCancelableProgressBar(true, false);
            // Display the progress bar.
            string progressBarTitle = "COLIBRI VR - Wait for memory cleanup";
            string progressBarInfo = "Waiting for memory cleanup...";
            string exitMessage = "Skipping wait for memory cleanup.";
            GeneralToolkit.UpdateCancelableProgressBar(typeof(ProcessingMethod), true, false, false, 2, progressBarTitle, progressBarInfo, exitMessage);
            // Get the initial memory.
            float BtoGB = Mathf.Pow(10, -9);
            float initialAllocatedMemoryGB = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() * BtoGB;
            float initialReservedMemoryGB = UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong() * BtoGB;
            // Execute the process.
            yield return StartCoroutine(executeCoroutine);
            // Prepare waiting for memory release.
            int averageOverNFrames = 20;
            int currentFrameIter = 0;
            int thresholdWaitTimeMs = 10000;
            float thresholdMemoryDiffGB = 0.001f;
            float currentAllocatedMemoryDiffGB = 2 * thresholdMemoryDiffGB;
            float currentReservedMemoryDiffGB = currentAllocatedMemoryDiffGB;
            float iterAllocatedMemoryGB = 0;
            float iterReservedMemoryGB = 0;
            // Start the stopwatch.
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            // Continue while the memory hasn't been released.
            while((currentAllocatedMemoryDiffGB > thresholdMemoryDiffGB || currentReservedMemoryDiffGB > thresholdMemoryDiffGB) && !GeneralToolkit.progressBarCanceled)
            {
                // Get an average over several frames of the current memory.
                float frameAllocatedMemoryGB = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() * BtoGB;
                iterAllocatedMemoryGB += frameAllocatedMemoryGB;
                float frameReservedMemoryGB = UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong() * BtoGB;
                iterReservedMemoryGB += frameReservedMemoryGB;
                currentFrameIter++;
                if(currentFrameIter >= averageOverNFrames)
                {
                    float currentAllocatedMemoryGB = iterAllocatedMemoryGB / currentFrameIter;
                    currentAllocatedMemoryDiffGB = Mathf.Max(0, currentAllocatedMemoryGB - initialAllocatedMemoryGB);
                    float currentReservedMemoryGB = iterReservedMemoryGB / currentFrameIter;
                    currentReservedMemoryDiffGB = Mathf.Max(0, currentReservedMemoryGB - initialReservedMemoryGB);
                    iterAllocatedMemoryGB = 0;
                    iterReservedMemoryGB = 0;
                    currentFrameIter = 0;
                    // If too long a time has elapsed, exit with a warning.
                    if(stopwatch.ElapsedMilliseconds > thresholdWaitTimeMs)
                    {
                        string warningMessage = "This object may be leaking memory during processing. ";
                        warningMessage += "Allocated went from " + initialAllocatedMemoryGB + "GB to " + currentAllocatedMemoryGB + "GB. ";
                        warningMessage += "Reserved went from " + initialReservedMemoryGB + "GB to " + currentReservedMemoryGB + "GB.";
                        Debug.LogWarning(GeneralToolkit.FormatScriptMessage(this.GetType(), warningMessage));
                        break;
                    }
                }
                yield return null;
            }
            // Stop the stopwatch.
            stopwatch.Stop();
            // Reset the progress bar.
            GeneralToolkit.ResetCancelableProgressBar(true, false);
        }

#endregion //METHODS

#endif //UNITY_EDITOR

    }

}

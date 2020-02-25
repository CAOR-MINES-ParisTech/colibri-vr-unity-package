/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using COLIBRIVR.Evaluation;
using COLIBRIVR.Processing;

namespace COLIBRIVR.Rendering
{

#if UNITY_EDITOR

    /// <summary>
    /// Editor class for Rendering.
    /// </summary>
    [CustomEditor(typeof(Rendering))]
    public class RenderingEditor : Editor
    {

#region FIELDS

        private Rendering _targetObject;
        private SerializedProperty _propertyLaunchOrderIndex;
        private SerializedProperty _propertyLaunchOnAwake;
        private SerializedProperty _propertyRenderingMethodIndex;
        private SerializedProperty _propertyEvaluationMethodIndex;

#endregion //FIELDS

#region INHERITANCE_METHODS

        /// <summary>
        /// On selection, sets up the target properties.
        /// </summary>
        void OnEnable()
        {
            _targetObject = (Rendering)serializedObject.targetObject;
            // Get the target properties.
            _propertyLaunchOrderIndex = serializedObject.FindProperty("launchOrderIndex");
            _propertyLaunchOnAwake = serializedObject.FindProperty("_launchOnAwake");
            _propertyRenderingMethodIndex = serializedObject.FindProperty("_renderingMethodIndex");
            _propertyEvaluationMethodIndex = serializedObject.FindProperty("_evaluationMethodIndex");
        }

        /// <summary>
        /// On deselection, inform the target object that it has been deselected.
        /// </summary>
        void OnDisable()
        {
            if(_targetObject != null)
                _targetObject.Deselected();
        }

        /// <summary>
        /// Displays a GUI enabling the user to modify various rendering parameters.
        /// </summary>
        public override void OnInspectorGUI()
        {
            // Start the GUI.
            GeneralToolkit.EditorStart(serializedObject, _targetObject);

            // Enable the user to select the source data directory.
            GeneralToolkit.EditorNewSection("Source data");
            SectionDataDirectory();
            // Enable the user to specify launch options.
            GeneralToolkit.EditorNewSection("Launch options");
            SectionLaunchOptions();
            // Start a change check.
            EditorGUI.BeginChangeCheck();
            // Enable the user to select a blending method.
            GeneralToolkit.EditorNewSection("Blending method");
            SectionBlendingMethod();
            // Enable the user to select an evaluation method.
            GeneralToolkit.EditorNewSection("Evaluation method");
            SectionEvaluationMethod();
            // End the change check.
            bool shouldPreviewChange = EditorGUI.EndChangeCheck();

            // End the GUI.
            GeneralToolkit.EditorEnd(serializedObject);

            // If the preview should change, inform the target object to update the preview images.
            if(shouldPreviewChange)
                _targetObject.processing.cameraSetup.onPreviewIndexChangeEvent.Invoke();
        }

#endregion //INHERITANCE_METHODS

#region METHODS

        /// <summary>
        /// Enables the user to specify launch options.
        /// </summary>
        private void SectionLaunchOptions()
        {
            string label = "Launch on awake:";
            string tooltip = "Whether to launch the rendering process on awake.";
            _propertyLaunchOnAwake.boolValue = EditorGUILayout.Toggle(new GUIContent(label, tooltip), _propertyLaunchOnAwake.boolValue);
            if(_propertyLaunchOnAwake.boolValue)
            {
                label = "Launch order index:";
                tooltip = "Index specifying the order in which the different rendering objects will launch.";
                int newLaunchOrderIndex = EditorGUILayout.IntSlider(new GUIContent(label, tooltip), _propertyLaunchOrderIndex.intValue, 0, Rendering.renderingObjectCount - 1);
                if(_propertyLaunchOrderIndex.intValue != newLaunchOrderIndex)
                {
                    Rendering.ChangeIndexInQueue(_targetObject, newLaunchOrderIndex);
                    _propertyLaunchOrderIndex.intValue = newLaunchOrderIndex;
                }
            }
        }

        /// <summary>
        /// Enables the user to select the source data directory.
        /// </summary>
        private void SectionDataDirectory()
        {
            bool isGUIEnabled = GUI.enabled;
            GUI.enabled = isGUIEnabled && !Application.isPlaying;
            EditorGUILayout.LabelField(_targetObject.processing.processedDataInfo, GeneralToolkit.wordWrapStyle, GUILayout.MaxWidth(GeneralToolkit.EditorGetCurrentScopeWidth()));
            using(var verticalScope = new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                string searchTitle = "Select directory that contains color/depth data";
                string searchTooltip = "Select the base directory where the acquisition data was stored.";
                bool clicked;
                string outPath;
                GeneralToolkit.EditorPathSearch(out clicked, out outPath, PathType.Directory, _targetObject.processing.dataHandler.dataDirectory, searchTitle, searchTooltip, Color.grey);
                _targetObject.processing.dataHandler.ChangeDataDirectory(outPath, clicked);
            }
            GUI.enabled = isGUIEnabled;
        }

        /// <summary>
        /// Displays a rendering method in the inspector.
        /// </summary>
        /// <param name="popupLabel"></param> The label to display as popup title.
        /// <param name="popupTooltip"></param> The tooltip to display alongside the label.
        /// <param name="allMethods"></param> Array of all methods of the given type.
        /// <param name="compatibleMethods"></param> Array of compatible methods of the given type.
        /// <param name="propertyMethodIndex"></param> The serialized property that should be modified by the popup choice.
        /// <typeparam name="T"></typeparam> The type of method being displayed.
        /// <returns></returns> The selected method.
        public T DisplayMethodGUI<T>(string popupLabel, string popupTooltip, T[] allMethods, T[] compatibleMethods, SerializedProperty propertyMethodIndex) where T : MonoBehaviour, IMethodGUI
        {
            // Enable the GUI only if the application is not playing and there are multiple compatible methods.
            bool isGUIEnabled = GUI.enabled;
            GUI.enabled = isGUIEnabled && !Application.isPlaying && (compatibleMethods.Length > 1);
            // Get the popup options from the list of compatible methods, and determine which one is currently selected.
            List<GUIContent> popupOptions = new List<GUIContent>();
            int popupIndex = 0;
            for(int i = 0; i < compatibleMethods.Length; i++)
            {
                popupOptions.Add(compatibleMethods[i].GetGUIInfo());
                if(compatibleMethods[i] == allMethods[propertyMethodIndex.intValue])
                    popupIndex = i;
            }
            // Display the popup, and get the new selected method.
            T selectedMethod = compatibleMethods[EditorGUILayout.Popup(new GUIContent(popupLabel, popupTooltip), popupIndex, popupOptions.ToArray())];
            // Compute the new selected method index.
            for(int i = 0; i < allMethods.Length; i++)
                if(allMethods[i] == selectedMethod)
                    propertyMethodIndex.intValue = i;
            // Now enable the GUI in any case.
            GUI.enabled = isGUIEnabled;
            // Display the method's additional parameters in an indented block.
            if(selectedMethod.HasAdditionalParameters())
            {
                using (var horizontalScope = new EditorGUILayout.HorizontalScope())
                {
                    GeneralToolkit.addedSpace = 20f * EditorGUI.indentLevel;
                    GUILayout.Space(GeneralToolkit.addedSpace);
                    using (var verticalScope = new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        selectedMethod.SectionAdditionalParameters();
                    }
                    GeneralToolkit.addedSpace = 0f;
                }
            }
            // Return the selected method.
            return selectedMethod;
        }

        /// <summary>
        /// Enables the user to select a blending method.
        /// </summary>
        private void SectionBlendingMethod()
        {
            RenderingMethod[] compatibleBlendingMethods = RenderingMethod.GetCompatibleRenderingMethods(_targetObject);
            string label = "Type:";
            string tooltip = "Choice of a blending method.";
            _targetObject.selectedBlendingMethod = DisplayMethodGUI<RenderingMethod>(label, tooltip, _targetObject.renderingMethods, compatibleBlendingMethods, _propertyRenderingMethodIndex);
            _targetObject.processing.cameraSetup.SectionColorIsIndices();
            _targetObject.processing.dataHandler.SectionGenerateColliders();
            label = "Underlying scene representation:";
            tooltip = "Set of processed assets that this blending method will use for rendering.";
            bool isGUIEnabled = GUI.enabled;
            GUI.enabled = false;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(new GUIContent(label, tooltip));
            ProcessingMethod[] sceneRepresentationMethods = _targetObject.selectedBlendingMethod.sceneRepresentationMethods;
            foreach(ProcessingMethod method in sceneRepresentationMethods)
            {
                if(method == null)
                    continue;
                using (var horizontalScope = new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.ToggleLeft(string.Empty, true, GUILayout.MaxWidth(30f));
                    GeneralToolkit.addedSpace = -15f;
                    GUILayout.Space(GeneralToolkit.addedSpace);
                    using (var verticalScope = new EditorGUILayout.VerticalScope())
                    {
                        GUILayout.Space(3f);
                        GUIContent labelGUIContent = method.GetProcessedDataName();
                        EditorGUILayout.LabelField(labelGUIContent, GeneralToolkit.wordWrapStyle, GUILayout.MaxWidth(GeneralToolkit.EditorGetCurrentScopeWidth() - 35f));
                    }
                    GeneralToolkit.addedSpace = 0f;
                }
            }
            GUI.enabled = isGUIEnabled;
            EditorGUILayout.Space();
        }

        /// <summary>
        /// Enables the user to select an evaluation method.
        /// </summary>
        private void SectionEvaluationMethod()
        {
            string label = "Type:";
            string tooltip = "Choice of an evaluation method.";
            _targetObject.selectedEvaluationMethod = DisplayMethodGUI<EvaluationMethod>(label, tooltip, _targetObject.evaluationMethods, _targetObject.evaluationMethods, _propertyEvaluationMethodIndex);
        }

#endregion //METHODS

    }

#endif //UNITY_EDITOR

}

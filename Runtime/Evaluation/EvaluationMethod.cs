/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

using UnityEngine;
using UnityEditor;

namespace COLIBRIVR.Evaluation
{

    /// <summary>
    /// Abstract class to be used as parent for evaluation methods.
    /// </summary>
    public abstract class EvaluationMethod : MonoBehaviour, IMethodGUI
    {

#region CONST_FIELDS

        public const string EMTransformName = "EvaluationMethods";

#endregion //CONST_FIELDS

#region STATIC_PROPERTIES

        public static System.Type[] EMTypes
        {
            get
            {
                return new System.Type[]
                {
                    typeof(EvaluationMethodTemplate),
                    typeof(YCbCr)
                };
            }
        }

#endregion //STATIC_PROPERTIES

#region STATIC_METHODS

        /// <summary>
        /// Gets or creates the entire set of evaluation methods as children of the given parent transform.
        /// </summary>
        /// <param name="parentTransform"></param> The parent transform.
        /// <returns></returns> The array of evaluation methods.
        public static EvaluationMethod[] GetOrCreateEvaluationMethods(Transform parentTransform)
        {
            return GeneralToolkit.GetOrCreateChildComponentGroup<EvaluationMethod>(EMTypes, EMTransformName, parentTransform);
        }

#endregion //STATIC_METHODS

#region FIELDS

        public Material evaluationMaterial;
        public bool excludeSourceView;

        [SerializeField] private float _multFactor;

#endregion //FIELDS

#region INHERITANCE_METHODS

#if UNITY_EDITOR

        /// <inheritdoc/>
        public abstract GUIContent GetGUIInfo();

        /// <inheritdoc/>
        public bool HasAdditionalParameters()
        {
            return true;
        }

        /// <inheritdoc/>
        public virtual void SectionAdditionalParameters()
        {
            SerializedObject serializedObject = new SerializedObject(this);
            serializedObject.Update();
            // Enable the user to choose whether to exclude the currently-rendered source view.
            // This is important to properly evaluate a rendering solution.
            string label = "Exclude source:";
            string tooltip = "For evaluation, exclude source view when rendering at its viewpoint.";
            SerializedProperty propertyExcludeSourceView = serializedObject.FindProperty("excludeSourceView");
            propertyExcludeSourceView.boolValue = EditorGUILayout.Toggle(new GUIContent(label, tooltip), propertyExcludeSourceView.boolValue);
            // Enable the user to choose a multiplication factor for the computed error.
            // This is simply for visualization purposes.
            label = "Mult. factor:";
            tooltip = "Multiplication factor for the computed evaluation error.";
            SerializedProperty propertyMultFactor = serializedObject.FindProperty("_multFactor");
            propertyMultFactor.floatValue = EditorGUILayout.Slider(new GUIContent(label, tooltip), propertyMultFactor.floatValue, 0.01f, 100);
            serializedObject.ApplyModifiedProperties();
        }

        /// <inheritdoc/>
        public bool IsCompatible(params object[] dataCounts)
        {
            return true;
        }

        /// <summary>
        /// Initializes the evaluation method on play.
        /// </summary>
        public abstract void InitializeEvaluationMethod();

        /// <summary>
        /// Clears the evaluation method on destroy.
        /// </summary>
        public virtual void ClearEvaluationMethod()
        {
            if(evaluationMaterial != null)
                DestroyImmediate(evaluationMaterial);
        }

#endif //UNITY_EDITOR

        /// <inheritdoc/>
        public void Reset()
        {
            excludeSourceView = true;
            _multFactor = 10;
        }

        /// <summary>
        /// Updates the evaluation method on each frame.
        /// </summary>
        public virtual void UpdateEvaluationMethod()
        {
            if(evaluationMaterial != null)
                evaluationMaterial.SetFloat("_MultFactor", _multFactor);
        }

#endregion //INHERITANCE_METHODS

    }

}



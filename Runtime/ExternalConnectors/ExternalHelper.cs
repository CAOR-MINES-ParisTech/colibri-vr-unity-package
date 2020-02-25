/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

using UnityEngine;
using UnityEditor;

namespace COLIBRIVR.ExternalConnectors
{

    /// <summary>
    /// Class that should be used as parent for the helper classes of external tools.
    /// </summary>
    public abstract class ExternalHelper : Method
    {

#region CONST_FIELDS

        public const string EHTransformName = "ExternalHelpers";

#endregion //CONST_FIELDS

#region STATIC_PROPERTIES

        public static System.Type[] EHTypes
        {
            get
            {
                return new System.Type[]
                {
                    typeof(COLMAPHelper),
                    typeof(BlenderHelper),
                    typeof(InstantMeshesHelper)
                };
            }
        }

#endregion //STATIC_PROPERTIES

#region STATIC_METHODS

        /// <summary>
        /// Creates or resets the entire set of external helper methods as children of the given parent transform.
        /// </summary>
        /// <param name="parentTransform"></param> The parent transform.
        /// <returns></returns> The array of external helper methods.
        public static ExternalHelper[] CreateOrResetExternalHelpers(Transform parentTransform)
        {
            ExternalHelper[] methods = GeneralToolkit.GetOrCreateChildComponentGroup<ExternalHelper>(EHTypes, EHTransformName, parentTransform);
            for(int iter = 0; iter < methods.Length; iter++)
                methods[iter].Reset();
            return methods;
        }

        // /// <summary>
        // /// Creates or resets an external helper object as a child of the given transform.
        // /// </summary>
        // /// <param name="parentTransform"></param> The parent transform.
        // /// <typeparam name="T"></typeparam> The type of helper object to create.
        // /// /// <returns></returns> The external helper object.
        // public static T CreateOrResetHelper<T>(Transform parentTransform = null) where T : ExternalHelper
        // {
        //     T existingHelper = GeneralToolkit.GetOrCreateChildComponent<T>(parentTransform);
        //     existingHelper.Reset();
        //     return existingHelper;
        // }

#endregion //STATIC_METHODS

#region FIELDS

        [SerializeField] private bool _foldout;

#endregion //FIELDS

#if UNITY_EDITOR

#region INHERITANCE_METHODS

        /// <summary>
        /// Resets this object's properties.
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            _foldout = false;
        }

        /// <summary>
        /// Displays the editor foldout for the helper class.
        /// </summary>
        public abstract void DisplayEditorFoldout();

        /// <summary>
        /// Displays editor subsections for the helper class.
        /// </summary>
        public abstract void DisplaySubsections();

#endregion //INHERITANCE_METHODS

#region METHODS

        /// <summary>
        /// Enables the user to choose the path to the external software's executable.
        /// </summary>
        /// <param name="settings"></param> The settings object linked to this external helper.
        protected void EditorFoldout(ExternalSettings settings)
        {
            if(!settings.isLinked)
                return;
            SerializedObject serializedObject = new SerializedObject(this);
            serializedObject.Update();
            SerializedProperty propertyFoldout = serializedObject.FindProperty("_foldout");
            using (var verticalScope = new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                string label = settings.toolkitName;
                string tooltip = "Selection of tools provided by the " + settings.toolkitName + " toolkit.";
                propertyFoldout.boolValue = EditorGUILayout.Foldout(_foldout, new GUIContent(label, tooltip));
                if(propertyFoldout.boolValue)
                    DisplaySubsections();
            }
            serializedObject.ApplyModifiedProperties();
        }

#endregion //METHODS

#endif //UNITY_EDITOR

    }

}

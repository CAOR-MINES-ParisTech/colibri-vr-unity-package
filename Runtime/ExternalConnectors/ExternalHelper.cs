/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

using UnityEngine;
using UnityEditor;
using System.IO;

namespace COLIBRIVR.ExternalConnectors
{

    /// <summary>
    /// Class that should be used as parent for the helper classes of external tools.
    /// </summary>
    public abstract class ExternalHelper : Method
    {

#region CONST_FIELDS

        public const string EHTransformName = "ExternalHelpers";
        public const int indexBlender = 1;

        private const string _propertyNameFoldout = "_foldout";

#endregion //CONST_FIELDS

#region STATIC_FIELDS

        private static string _loadedMeshFullPath;
        private static MeshFilter _loadedMeshFilter;

#endregion //STATIC_FIELDS

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

#endregion //STATIC_METHODS

#region FIELDS

        [SerializeField] private bool _foldout;

#endregion //FIELDS

#if UNITY_EDITOR

#region PROPERTIES

        public int loadedMeshFaceCount { get { return _loadedMeshFilter.mesh.triangles.Length / 3; } }

#endregion //PROPERTIES

#region INHERITANCE_METHODS

        /// <summary>
        /// Resets this object's properties.
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            _foldout = false;
            _loadedMeshFullPath = string.Empty;
            _loadedMeshFilter = null;
        }

        /// <summary>
        /// On destroy, destroys the potentially copied mesh in the Resources folder.
        /// </summary>
        public virtual void OnDestroy()
        {
            if(_loadedMeshFullPath != null)
            {
                bool isInResources = _loadedMeshFullPath.Contains(COLIBRIVRSettings.settingsResourcesAbsolutePath);
                if(isInResources && EditorApplication.isPlaying && GeneralToolkit.IsStartingNewScene())
                {
                    GeneralToolkit.Delete(_loadedMeshFullPath);
                    AssetDatabase.Refresh();
                }
            }
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
            SerializedProperty propertyFoldout = serializedObject.FindProperty(_propertyNameFoldout);
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

        /// <summary>
        /// Displays the mesh at the given path in the Scene view.
        /// </summary>
        /// <param name="meshFullPath"></param> The full path to the mesh.
        public void DisplayMeshInSceneView(string meshFullPath)
        {
            _loadedMeshFullPath = GeneralToolkit.CopyObjectFromPathIntoResources(meshFullPath);
            GeneralToolkit.MakeMeshReadable(_loadedMeshFullPath);
            Mesh loadedMesh = Resources.Load<Mesh>(Path.GetFileNameWithoutExtension(_loadedMeshFullPath));
            if(_loadedMeshFilter == null)
            {
                _loadedMeshFilter = new GameObject("Visualization Mesh").AddComponent<MeshFilter>();
                _loadedMeshFilter.gameObject.AddComponent<MeshRenderer>().material = new Material(GeneralToolkit.shaderStandard);
            }
            _loadedMeshFilter.mesh = loadedMesh;
            Debug.Log(GeneralToolkit.FormatScriptMessage(this.GetType(), "Displayed mesh for visualization, with: " + loadedMeshFaceCount + " faces."));
        }

#endregion //METHODS

#endif //UNITY_EDITOR

    }

}

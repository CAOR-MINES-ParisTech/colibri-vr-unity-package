/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

using UnityEditor;
using UnityEngine;
using System.IO;
using COLIBRIVR.ExternalConnectors;

namespace COLIBRIVR
{
    /// <summary>
    /// Class that enables specifying settings for the COLIBRI VR package.
    /// </summary>
    public class COLIBRIVRSettings : ScriptableObject
    {

#region CONST_FIELDS

        private const string _propertyNamePreviewMaxResolution = "previewMaxResolution";

#endregion //CONST_FIELDS

#if UNITY_EDITOR

#region STATIC_PROPERTIES

        public static COLIBRIVRSettings packageSettings { get { return GetOrCreateSettings(); } }

#endregion //STATIC_PROPERTIES

#region STATIC_METHODS

        /// <summary>
        /// Enables the user to specify settings for the COLIBRI VR package.
        /// </summary>
        /// <param name="packageSettings"></param> The package settings object.
        public static void SectionPackageSettings(COLIBRIVRSettings packageSettings)
        {
            SerializedObject serializedSettings = new SerializedObject(packageSettings);
            serializedSettings.Update();

            GeneralToolkit.EditorNewSection("Package settings");
            string label = "Max. resolution for preview";
            string tooltip = "Maximum resolution for the preview images.";
            SerializedProperty propertyPreviewMaxResolution = serializedSettings.FindProperty(_propertyNamePreviewMaxResolution);
            propertyPreviewMaxResolution.intValue = EditorGUILayout.IntSlider(new GUIContent(label, tooltip), propertyPreviewMaxResolution.intValue, 1, 8192);

            GeneralToolkit.EditorNewSection("External helper tools");
            packageSettings.COLMAPSettings.EditorSettingsFoldout();
            packageSettings.BlenderSettings.EditorSettingsFoldout();
            packageSettings.InstantMeshesSettings.EditorSettingsFoldout();

            serializedSettings.ApplyModifiedProperties();
        }

        /// <summary>
        /// Gets or creates the package settings, stored as an asset in the project folder.
        /// </summary>
        /// <returns></returns> The package settings.
        private static COLIBRIVRSettings GetOrCreateSettings()
        {
            string packagePathRelative = PackageReference.GetPackagePath(true);
            string settingsAssetPath = Path.Combine(Path.Combine(Path.Combine(packagePathRelative, "Editor"), "Core"), "COLIBRIVRSettings.asset");
            COLIBRIVRSettings settings = AssetDatabase.LoadAssetAtPath<COLIBRIVRSettings>(settingsAssetPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<COLIBRIVRSettings>();
                settings.COLMAPSettings = (COLMAPSettings) ScriptableObject.CreateInstance<COLMAPSettings>().Initialize();
                settings.BlenderSettings = (BlenderSettings) ScriptableObject.CreateInstance<BlenderSettings>().Initialize();
                settings.InstantMeshesSettings = (InstantMeshesSettings) ScriptableObject.CreateInstance<InstantMeshesSettings>().Initialize();
                settings.previewMaxResolution = 256;
                AssetDatabase.CreateAsset(settings, settingsAssetPath);
                AssetDatabase.AddObjectToAsset(settings.COLMAPSettings, settingsAssetPath);
                AssetDatabase.AddObjectToAsset(settings.BlenderSettings, settingsAssetPath);
                AssetDatabase.AddObjectToAsset(settings.InstantMeshesSettings, settingsAssetPath);
                AssetDatabase.SaveAssets();
            }
            return settings;
        }

#endregion //STATIC_METHODS

#endif //UNITY_EDITOR

#region FIELDS

        public COLMAPSettings COLMAPSettings;
        public BlenderSettings BlenderSettings;
        public InstantMeshesSettings InstantMeshesSettings;
        public int previewMaxResolution;

#endregion //FIELDS

    }

}
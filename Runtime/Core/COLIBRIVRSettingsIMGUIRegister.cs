using System.Collections.Generic;
using UnityEditor;

namespace COLIBRIVR
{

    /// <summary>
    /// Class that adds the package settings as a tab in the Project Settings window.
    /// </summary>
    public static class COLIBRIVRSettingsIMGUIRegister
    {

#if UNITY_EDITOR

#region STATIC_METHODS

        /// <summary>
        /// Enables the package settings tab.
        /// </summary>
        /// <returns></returns> The corresponding settings provider.
        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            // First parameter is the path in the Settings window.
            // Second parameter is the scope of this setting: it only appears in the Project Settings window.
            SettingsProvider provider = new SettingsProvider("Project/COLIBRIVRSettings", SettingsScope.Project)
            {
                // By default the last token of the path is used as display name if no label is provided.
                label = "COLIBRI VR",

                // Create the SettingsProvider and initialize its drawing (IMGUI) function in place:
                guiHandler = (searchContext) =>
                {
                    COLIBRIVRSettings packageSettings = COLIBRIVRSettings.packageSettings;
                    COLIBRIVRSettings.SectionPackageSettings(packageSettings);
                },

                // Populate the search keywords to enable smart search filtering and label highlighting:
                keywords = new HashSet<string>(new[] { "COLIBRI VR", "COLMAP", "Blender", "Instant Meshes" })
            };

            return provider;
        }

#endregion //STATIC_METHODS

#endif //UNITY_EDITOR

    }

}

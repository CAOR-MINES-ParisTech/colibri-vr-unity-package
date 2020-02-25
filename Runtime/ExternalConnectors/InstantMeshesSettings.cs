/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

namespace COLIBRIVR.ExternalConnectors
{
    /// <summary>
    /// Class that enables specifying settings for the link to Instant Meshes.
    /// </summary>
    public class InstantMeshesSettings : ExternalSettings
    {

#if UNITY_EDITOR

#region STATIC_PROPERTIES

        public static string InstantMeshesExePath { get { return COLIBRIVRSettings.packageSettings.InstantMeshesSettings.exePath; } }
        public static string formattedInstantMeshesExePath { get { return GeneralToolkit.FormatPathForCommand(InstantMeshesExePath); } }

#endregion //STATIC_PROPERTIES

#region INHERITANCE_PROPERTIES

        public override string toolkitName { get { return "Instant Meshes"; } }

#endregion //INHERITANCE_PROPERTIES

#endif //UNITY_EDITOR

    }
}
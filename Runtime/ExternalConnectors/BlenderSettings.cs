/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

namespace COLIBRIVR.ExternalConnectors
{
    /// <summary>
    /// Class that enables specifying settings for the link to Blender.
    /// </summary>
    public class BlenderSettings : ExternalSettings
    {

#if UNITY_EDITOR

#region STATIC_PROPERTIES

        public static string BlenderExePath { get { return COLIBRIVRSettings.packageSettings.BlenderSettings.exePath; } }
        public static string formattedBlenderExePath { get { return GeneralToolkit.FormatPathForCommand(BlenderExePath); } }

#endregion //STATIC_PROPERTIES

#region INHERITANCE_PROPERTIES

        public override string toolkitName { get { return "Blender 2.8"; } }

#endregion //INHERITANCE_PROPERTIES

#endif //UNITY_EDITOR

    }
}
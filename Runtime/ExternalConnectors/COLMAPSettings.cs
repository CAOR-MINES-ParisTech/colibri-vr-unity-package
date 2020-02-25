/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

namespace COLIBRIVR.ExternalConnectors
{
    /// <summary>
    /// Class that enables specifying settings for the link to COLMAP.
    /// </summary>
    public class COLMAPSettings : ExternalSettings
    {

#if UNITY_EDITOR

#region STATIC_PROPERTIES

        public static string COLMAPExePath { get { return COLIBRIVRSettings.packageSettings.COLMAPSettings.exePath; } }
        public static string formattedCOLMAPExePath { get { return GeneralToolkit.FormatPathForCommand(COLMAPExePath); } }

#endregion //STATIC_PROPERTIES

#region INHERITANCE_PROPERTIES

        public override string toolkitName { get { return "COLMAP 3.6"; } }

#endregion //INHERITANCE_PROPERTIES

#endif //UNITY_EDITOR

    }
}
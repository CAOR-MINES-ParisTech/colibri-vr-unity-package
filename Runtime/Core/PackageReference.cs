/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

using UnityEditor;
using UnityEngine;

namespace COLIBRIVR
{
    /// <summary>
    /// Class that can be used to get the absolute or relative path to the COLIBRI VR Unity package.
    /// </summary>
    public class PackageReference : ScriptableObject
    {
        /// <summary>
        /// Returns the package's path.
        /// </summary>
        /// <param name="relative"></param> True if the path should be relative to the project folder, false otherwise.
        /// <returns></returns> The desired path.
        public static string GetPackagePath(bool relative)
        {
            string packagePath = Application.dataPath;
#if UNITY_EDITOR
            PackageReference packageReference = ScriptableObject.CreateInstance<PackageReference>();
            MonoScript thisScript = MonoScript.FromScriptableObject(packageReference);
            packagePath = GeneralToolkit.GetDirectoryBefore(GeneralToolkit.GetDirectoryBefore(GeneralToolkit.GetDirectoryBefore(AssetDatabase.GetAssetPath(thisScript))));
            if(relative)
                packagePath = GeneralToolkit.ToRelativePath(packagePath);
            ScriptableObject.DestroyImmediate(packageReference);
#endif //UNITY_EDITOR
            return packagePath;
        }
    }

}

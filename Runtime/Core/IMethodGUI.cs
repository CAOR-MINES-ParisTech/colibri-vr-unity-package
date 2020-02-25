/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

using UnityEngine;

namespace COLIBRIVR
{
    /// <summary>
    /// Interface to be used as parent for method classes, that need to display GUI information on the Rendering component.
    /// </summary>
    public interface IMethodGUI
    {

#region INHERITANCE_METHODS

#if UNITY_EDITOR

        /// <summary>
        /// Returns this method's label and tooltip, to be displayed in the GUI.
        /// </summary>
        /// <returns></returns> The method's label and tooltip.
        GUIContent GetGUIInfo();

        /// <summary>
        /// Indicates whether this method has additional parameters.
        /// </summary>
        /// <returns></returns> True if it does, false otherwise.
        bool HasAdditionalParameters();

        /// <summary>
        /// Enables the user to set additional method parameters.
        /// </summary>
        void SectionAdditionalParameters();

#endif //UNITY_EDITOR

#endregion //INHERITANCE_METHODS

    }

}


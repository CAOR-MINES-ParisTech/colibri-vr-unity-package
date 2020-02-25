/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using COLIBRIVR.Processing;

namespace COLIBRIVR.Rendering
{

    /// <summary>
    /// Template class illustrating how to add a basic rendering method.
    /// </summary>
    public class RenderingMethodTemplate : RenderingMethod
    {
        
#region INHERITANCE_METHODS

#if UNITY_EDITOR

        /// <inheritdoc/>
        public override GUIContent GetGUIInfo()
        {
            string label = "None";
            string tooltip = "No blending will be performed from the selected scene representation.";
            return new GUIContent(label, tooltip);
        }

        /// <inheritdoc/>
        public override bool HasAdditionalParameters()
        {
            return false;
        }

        /// <inheritdoc/>
        public override void SectionAdditionalParameters()
        {

        }

#endif //UNITY_EDITOR

        /// <inheritdoc/>
        public override void InitializeLinks()
        {
            base.InitializeLinks();
            sceneRepresentationMethods = new ProcessingMethod[] {  };
        }

        /// <inheritdoc/>
        public override IEnumerator InitializeRenderingMethodCoroutine()
        {
            blendingMaterial = new Material(GeneralToolkit.shaderStandard);
            yield return null;
        }

        /// <inheritdoc/>
        public override void UpdateRenderingMethod()
        {

        }

        /// <inheritdoc/>
        public override void ClearRenderingMethod()
        {
            base.ClearRenderingMethod();
        }

#endregion //INHERITANCE_METHODS

    }

}

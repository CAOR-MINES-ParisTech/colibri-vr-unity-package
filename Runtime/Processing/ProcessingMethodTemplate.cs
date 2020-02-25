/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

using System.Collections;
using UnityEngine;

namespace COLIBRIVR.Processing
{

    /// <summary>
    /// Template class illustrating how to add a basic processing method.
    /// </summary>
    public class ProcessingMethodTemplate : ProcessingMethod
    {

#region INHERITANCE_METHODS   

#if UNITY_EDITOR     

        /// <inheritdoc/>
        public override bool IsCompatible(int colorDataCount, int depthDataCount, int meshDataCount)
        {
            return true;
        }

        /// <inheritdoc/>
        public override bool IsGUINested()
        {
            return false;
        }

        /// <inheritdoc/>
        public override GUIContent GetGUIInfo()
        {
            string label = "None";
            string tooltip = "No color processing will be performed from the source data.";
            return new GUIContent(label, tooltip);
        }

        /// <inheritdoc/>
        public override GUIContent GetProcessedDataName()
        {
            return null;
        }

        /// <inheritdoc/>
        protected override IEnumerator ExecuteMethodCoroutine()
        {
            yield return null;
        }
        
        /// <inheritdoc/>
        public override void DeactivateIncompatibleProcessingMethods()
        {

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

#endregion //INHERITANCE_METHODS

    }

}

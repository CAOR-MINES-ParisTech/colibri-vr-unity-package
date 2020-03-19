/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

using UnityEngine;

namespace COLIBRIVR.Evaluation
{

    /// <summary>
    /// Template class illustrating how to add a basic evaluation method.
    /// </summary>
    public class EvaluationMethodTemplate : EvaluationMethod
    {

#if UNITY_EDITOR
        
#region INHERITANCE_METHODS

        /// <inheritdoc/>
        public override GUIContent GetGUIInfo()
        {
            string label = "None";
            string tooltip = "No evaluation method will be used to compare rendered views and source images.";
            return new GUIContent(label, tooltip);
        }

        /// <inheritdoc/>
        public override bool HasAdditionalParameters()
        {
            return false;
        }

        /// <inheritdoc/>
        public override void InitializeEvaluationMethod()
        {

        }

        /// <inheritdoc/>
        public override void UpdateEvaluationMethod()
        {
            
        }

        /// <inheritdoc/>
        public override void ClearEvaluationMethod()
        {
            base.ClearEvaluationMethod();
        }

#endregion //INHERITANCE_METHODS

#endif //UNITY_EDITOR

    }

}

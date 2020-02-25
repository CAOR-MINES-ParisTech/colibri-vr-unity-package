/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

using UnityEngine;

namespace COLIBRIVR.Evaluation
{

    /// <summary>
    /// Class that implements the YCbCr evaluation method.
    /// This method computes the error in YCbCr space between rendered view and ground truth.
    /// </summary>
    public class YCbCr : EvaluationMethod
    {

#if UNITY_EDITOR

#region INHERITANCE_METHODS

        /// <inheritdoc/>
        public override GUIContent GetGUIInfo()
        {
            string label = "Cb+Cr";
            string tooltip = "Evaluation based on the absolute sum of differences on the Cb and Cr channels in YCbCr color space.";
            return new GUIContent(label, tooltip);
        }

        /// <inheritdoc/>
        public override void InitializeEvaluationMethod()
        {
            // Create the evaluation material from the corresponding shader.
            evaluationMaterial = new Material(GeneralToolkit.shaderEvaluationYCbCr);
        }

#endregion //INHERITANCE_METHODS

#endif //UNITY_EDITOR

    }

}

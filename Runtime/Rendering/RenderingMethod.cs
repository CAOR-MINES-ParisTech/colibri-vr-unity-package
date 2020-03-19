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
    /// Abstract class to be used as a parent for rendering methods.
    /// </summary>
    public abstract class RenderingMethod : Method, IMethodGUI
    {

#region CONST_FIELDS

        public const string RMTransformName = "RenderingMethods";

        private const string _shaderNameExcludedSourceView = "_ExcludedSourceView";

#endregion //CONST_FIELDS

#region STATIC_PROPERTIES

        public static System.Type[] RMTypes
        {
            get
            {
                return new System.Type[]
                {
                    typeof(RenderingMethodTemplate),
                    typeof(TexturedFocalSurfaces),
                    typeof(TexturedPerViewMeshes),
                    typeof(TexturedPerViewMeshesDT),
                    typeof(TexturedGlobalMesh),
                    typeof(DiskBlendedFocalSurfaces),
                    typeof(DiskBlendedPerViewMeshes),
                    typeof(ULRGlobalMesh)
                };
            }
        }

#endregion //STATIC_PROPERTIES

#region STATIC_METHODS

#if UNITY_EDITOR

        /// <summary>
        /// Gets the rendering methods compatible with the current bundle of assets.
        /// </summary>
        /// <param name="caller"></param> The rendering object calling this method.
        /// <returns></returns> The array of compatible rendering methods.
        public static RenderingMethod[] GetCompatibleRenderingMethods(Rendering caller)
        {
            List<RenderingMethod> compatibleMethods = new List<RenderingMethod>();

            foreach(RenderingMethod method in caller.renderingMethods)
                if(method.IsRenderingMethodCompatible())
                    compatibleMethods.Add(method);
            return compatibleMethods.ToArray();
        }

#endif //UNITY_EDITOR

        /// <summary>
        /// Creates or resets the entire set of rendering methods as children of the given parent transform.
        /// </summary>
        /// <param name="parentTransform"></param> The parent transform.
        /// <returns></returns> The array of rendering methods.
        public static RenderingMethod[] CreateOrResetRenderingMethods(Transform parentTransform)
        {
            RenderingMethod[] methods = GeneralToolkit.GetOrCreateChildComponentGroup<RenderingMethod>(RMTypes, RMTransformName, parentTransform);
            for(int iter = 0; iter < methods.Length; iter++)
                methods[iter].Reset();
            return methods;
        }

#endregion //STATIC_METHODS

#region FIELDS

        public Material blendingMaterial;
        public ProcessingMethod[] sceneRepresentationMethods;

#endregion //FIELDS

#region INHERITANCE_METHODS

#if UNITY_EDITOR

        /// <inheritdoc/>
        public abstract GUIContent GetGUIInfo();
        
        /// <inheritdoc/>
        public abstract bool HasAdditionalParameters();

        /// <inheritdoc/>
        public abstract void SectionAdditionalParameters();

#endif //UNITY_EDITOR

        /// <summary>
        /// Initializes the blending method on play.
        /// </summary>
        /// <returns></returns>
        public abstract IEnumerator InitializeRenderingMethodCoroutine();

        /// <summary>
        /// Updates the blending method on each frame.
        /// </summary>
        public abstract void UpdateRenderingMethod();

        /// <summary>
        /// Clears the blending method on destroy.
        /// </summary>
        public virtual void ClearRenderingMethod()
        {
            if(blendingMaterial != null)
                DestroyImmediate(blendingMaterial);
        }

        /// <summary>
        /// Excludes a source camera during rendering, by notifying the blending material.
        /// </summary>
        /// <param name="excludedSourceView"></param> The index of the excluded source camera.
        public void ExcludeSourceView(int excludedSourceView)
        {
            if(blendingMaterial != null)
                blendingMaterial.SetInt(_shaderNameExcludedSourceView, excludedSourceView);
        }

        /// <summary>
        /// Indicates whether this rendering method is compatible with the given scene representation.
        /// </summary>
        /// <returns></returns> True if the rendering method is compatible, false otherwise.
        public bool IsRenderingMethodCompatible()
        {
            if(sceneRepresentationMethods != null && sceneRepresentationMethods.Length < 1)
                return true;
            else if(dataHandler == null || dataHandler.bundledAssetsMethodTypes == null || dataHandler.bundledAssetsMethodTypes.Count < 1)
                return false;
            else
                for(int iter = 0; iter < sceneRepresentationMethods.Length; iter++)
                    if(!dataHandler.bundledAssetsMethodTypes.Contains(sceneRepresentationMethods[iter].GetType()))
                        return false;
            return true;
        }

#endregion //INHERITANCE_METHODS

    }

}
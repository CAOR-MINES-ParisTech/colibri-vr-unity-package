/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org
 
using UnityEngine;
using COLIBRIVR.Processing;

namespace COLIBRIVR
{

    public class Method : MonoBehaviour
    {

#region CONST_FIELDS
        
        protected const string _propertyNameMaxBlendAngle = "_maxBlendAngle";
        protected const string _shaderNameSourceCamIndex = "_SourceCamIndex";
        protected const string _shaderNameSourceCamPosXYZ = "_SourceCamPosXYZ";
        protected const string _shaderNameSourceCamCount = "_SourceCamCount";
        protected const string _shaderNameSourceCamIsOmnidirectional = "_SourceCamIsOmnidirectional";
        protected const string _shaderNameMaxBlendAngle = "_MaxBlendAngle";
        protected const string _shaderNameFocalLength = "_FocalLength";

#endregion //CONST_FIELDS
        
#region FIELDS

        public Rendering.Rendering renderingCaller;
        public Processing.Processing processingCaller;
        public DataHandler dataHandler;
        public CameraSetup cameraSetup;

        public ColorTextureArray PMColorTextureArray;
        public PerViewMeshesFS PMPerViewMeshesFS;
        public PerViewMeshesQSTR PMPerViewMeshesQSTR;
        public GlobalMeshEF PMGlobalMeshEF;
        public DepthTextureArray PMDepthTextureArray;
        public GlobalTextureMap PMGlobalTextureMap;
        public PerViewMeshesQSTRDTA PMPerViewMeshesQSTRDTA;

#endregion //FIELDS

#region INHERITANCE_METHODS

        /// <summary>
        /// On reset, reset the object's properties.
        /// </summary>
        public virtual void Reset()
        {
            renderingCaller = GeneralToolkit.GetParentOfType<Rendering.Rendering>(transform);
            if(renderingCaller != null)
                processingCaller = renderingCaller.processing;
            else
                processingCaller = GeneralToolkit.GetParentOfType<Processing.Processing>(transform);
            if(processingCaller != null)
            {
                dataHandler = processingCaller.dataHandler;
                cameraSetup = processingCaller.cameraSetup;
            }
        }

        /// <summary>
        /// Initializes the links to other methods.
        /// </summary>
        public virtual void InitializeLinks()
        {
            if(processingCaller != null)
            {
                ProcessingMethod[] processingMethods = processingCaller.processingMethods;
                if(processingMethods != null)
                {
                    PMColorTextureArray = (ColorTextureArray) processingMethods[ProcessingMethod.indexColorTextureArray];
                    PMPerViewMeshesFS = (PerViewMeshesFS) processingMethods[ProcessingMethod.indexPerViewMeshesFS];
                    PMPerViewMeshesQSTR = (PerViewMeshesQSTR) processingMethods[ProcessingMethod.indexPerViewMeshesQSTR];
                    PMGlobalMeshEF = (GlobalMeshEF) processingMethods[ProcessingMethod.indexGlobalMeshEF];
                    PMDepthTextureArray = (DepthTextureArray) processingMethods[ProcessingMethod.indexDepthTextureArray];
                    PMGlobalTextureMap = (GlobalTextureMap) processingMethods[ProcessingMethod.indexGlobalTextureMap];
                    PMPerViewMeshesQSTRDTA = (PerViewMeshesQSTRDTA) processingMethods[ProcessingMethod.indexPerViewMeshesQSTRDTA];
                }
            }
        }

#endregion //INHERITANCE_METHODS

    }

}

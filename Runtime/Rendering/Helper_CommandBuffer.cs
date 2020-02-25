/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

using UnityEngine;
using UnityEngine.Rendering;

namespace COLIBRIVR.Rendering
{

    /// <summary>
    /// Helper class to be called by blending methods that rely on a rendering command buffer instead of the standard rendering pipeline.
    /// </summary>
    public class Helper_CommandBuffer : Method
    {

#region FIELDS

        public CommandBuffer commandBuffer;

#endregion //FIELDS

#region INHERITANCE_METHODS

        /// <inheritdoc/>
        public override void Reset()
        {
            base.Reset();
            commandBuffer = null;
        }

#endregion //INHERITANCE_METHODS
          
#region METHODS

        /// <summary>
        /// Initializes the command buffer used for rendering.
        /// </summary>
        /// <param name="cameraEvent"></param> The camera event on which the command buffer is to be executed.
        public void InitializeCommandBuffer(CameraEvent cameraEvent)
        {
			commandBuffer = new CommandBuffer();
			commandBuffer.name = gameObject.name + " - Execute rendering";
			renderingCaller.mainCamera.AddCommandBuffer(cameraEvent, commandBuffer);
        }

        /// <summary>
        /// Deactivates the geometry created by the scene representation, to prevent it from being rendered.
        /// </summary>
        /// <param name="geometryDataTransform"></param> The transform of the created geometry.
        public void DeactivateCreatedGeometry(Transform geometryDataTransform)
        {
            geometryDataTransform.gameObject.SetActive(false);
        }

#endregion //METHODS

    }

}
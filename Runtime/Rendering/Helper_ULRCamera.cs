using System.Collections.Generic;
using UnityEngine;

namespace COLIBRIVR.Rendering
{

    /// <summary>
    /// Helper class to be attached to cameras that render objects using the Unstructured Lumigraph Rendering method.
    /// </summary>
    public class Helper_ULRCamera : MonoBehaviour
    {

#region FIELDS

        private bool _initialized;
        private Camera _attachedCam;
        private bool _isSceneViewCamera;
        private bool _isStereo;
        private List<Helper_ULR> _helperULRList;
        private List<ComputeBuffer> _vertexCamWeightsBufferList;
        private List<ComputeBuffer> _vertexCamIndicesBufferList;
        private List<Vector3> _vertexFrontIndexAndCountPerFrameList;

#endregion //FIELDS

#region INHERITANCE_METHODS

        /// <summary>
        /// When the attached camera renders, updates the properties of the blending materials of objects currently rendered with ULR.
        /// </summary>
        void OnPreRender()
        {
            if(_initialized)
            {
                UpdateValuesOnPreRender();
            }
        }

        /// <summary>
        /// On disable, clears all created objects.
        /// </summary>
        void OnDisable()
        {
            ClearAll();
        }

#endregion //INHERITANCE_METHODS

#region METHODS

        /// <summary>
        /// Initializes the lists and attached components required by this class.
        /// </summary>
        private void Initialize()
        {
            ClearAll(); 
            _attachedCam = GetComponent<Camera>();
            if(_attachedCam != null && _attachedCam.name != "Preview Scene Camera")
            {
                _helperULRList = new List<Helper_ULR>();
                _vertexCamWeightsBufferList = new List<ComputeBuffer>();
                _vertexCamIndicesBufferList = new List<ComputeBuffer>();
                _vertexFrontIndexAndCountPerFrameList = new List<Vector3>();
                _isSceneViewCamera = (_attachedCam.name == "SceneCamera");
                _isStereo = (_attachedCam.stereoEnabled);
                _initialized = true;
            }
        }

        /// <summary>
        /// Clears all the objects created by this class.
        /// </summary>
        private void ClearAll()
        {
            if(_helperULRList != null)
                _helperULRList.Clear();
            if(_vertexCamWeightsBufferList != null)
            {
                for(int i = 0; i < _vertexCamWeightsBufferList.Count; i++)
                    if(_vertexCamWeightsBufferList[i] != null)
                        _vertexCamWeightsBufferList[i].Release();
                _vertexCamWeightsBufferList.Clear();
            }
            if(_vertexCamIndicesBufferList != null)
            {
                for(int i = 0; i < _vertexCamIndicesBufferList.Count; i++)
                    if(_vertexCamIndicesBufferList[i] != null)
                        _vertexCamIndicesBufferList[i].Release();
                _vertexCamIndicesBufferList.Clear();
            }
            if(_vertexFrontIndexAndCountPerFrameList != null)
                _vertexFrontIndexAndCountPerFrameList.Clear();
            _initialized = false;
        }

        /// <summary>
        /// Adds an object rendered with ULR to the list of objects currently rendering to this camera.
        /// </summary>
        /// <param name="helperULR"></param> The helper ULR method attached to the object rendering to this camera.
        /// <param name="totalVertexCount"></param> The total vertex count of the mesh attached to the object rendering to this camera.
        public void AddRenderedObject(Helper_ULR helperULR, int totalVertexCount)
        {
            if(!_initialized)
                Initialize();
            if(_initialized)
            {
                int addCount = _isStereo ? 2 : 1;
                for(int addIter = 0; addIter < addCount; addIter++)
                {
                    _helperULRList.Add(helperULR);
                    _vertexCamWeightsBufferList.Add(new ComputeBuffer(totalVertexCount, 4 * sizeof(float)));
                    _vertexCamIndicesBufferList.Add(new ComputeBuffer(totalVertexCount, 4 * sizeof(uint)));
                    _vertexFrontIndexAndCountPerFrameList.Add(new Vector3(0, totalVertexCount, totalVertexCount));
                }
                OnPreRender();
            }
        }

        /// <summary>
        /// Updates the values of the blending materials used by the helper ULR classes of objects rendering to this camera.
        /// </summary>
        public void UpdateValuesOnPreRender()
        {
            // Compute the increment and starting index based on whether the attached camera is stereo.
            int increment = _isStereo ? 2 : 1;
            int renderedObjIndex = (_isStereo && _attachedCam.stereoActiveEye == Camera.MonoOrStereoscopicEye.Right) ? 1 : 0;
            // Loop over all the helper ULR classes.
            while(renderedObjIndex < _helperULRList.Count)
            {
                // If this helper ULR class is null, remove it from the list and skip.
                Helper_ULR helperULR = _helperULRList[renderedObjIndex];
                if(helperULR == null)
                {
                    _helperULRList.RemoveAt(renderedObjIndex);
                }
                else
                {
                    // Otherwise, if its blending material is also not null, update this material's properties.
                    Material currentBlendingMat = helperULR.currentBlendingMaterial;
                    if(currentBlendingMat != null)
                    {
                        // Compute the number of vertices to process this frame, based on the current framerate.
                        Vector3 vertexFrontIndexAndCountPerFrame = _vertexFrontIndexAndCountPerFrameList[renderedObjIndex];
                        int blendFieldFrontVertexIndex = (int)vertexFrontIndexAndCountPerFrame.x;
                        int blendFieldVertexCountPerFrame = (int)vertexFrontIndexAndCountPerFrame.y;
                        int totalVertexCount = (int)vertexFrontIndexAndCountPerFrame.z;
                        float minVertexCountForTotalRendering = totalVertexCount * 1f / helperULR.maxFrameCountForProcessing;
                        blendFieldFrontVertexIndex = (blendFieldFrontVertexIndex + blendFieldVertexCountPerFrame) % totalVertexCount;
                        float blendFieldFrameRatio = 1f / (helperULR.targetFramerate * Time.smoothDeltaTime);
                        blendFieldVertexCountPerFrame = Mathf.RoundToInt(Mathf.Max(blendFieldFrameRatio * blendFieldVertexCountPerFrame, minVertexCountForTotalRendering));
                        blendFieldVertexCountPerFrame = Mathf.Clamp(blendFieldVertexCountPerFrame, 1, totalVertexCount);
                        int nextBlendFieldFrontVertexIndex = (blendFieldFrontVertexIndex + blendFieldVertexCountPerFrame) % totalVertexCount;
                        Vector2 blendFieldComputationParams = new Vector2(blendFieldFrontVertexIndex, nextBlendFieldFrontVertexIndex);
                        _vertexFrontIndexAndCountPerFrameList[renderedObjIndex] = new Vector3(blendFieldFrontVertexIndex, blendFieldVertexCountPerFrame, totalVertexCount);
                        // Update the blending material's properties.
                        Graphics.ClearRandomWriteTargets();
                        Graphics.SetRandomWriteTarget(1, _vertexCamWeightsBufferList[renderedObjIndex]);
                        Graphics.SetRandomWriteTarget(2, _vertexCamIndicesBufferList[renderedObjIndex]);
                        currentBlendingMat.SetBuffer("_VertexCamWeightsBuffer", _vertexCamWeightsBufferList[renderedObjIndex]);
                        currentBlendingMat.SetBuffer("_VertexCamIndicesBuffer", _vertexCamIndicesBufferList[renderedObjIndex]);
                        currentBlendingMat.SetVector("_BlendFieldComputationParams", blendFieldComputationParams);
                        currentBlendingMat.SetInt("_IsSceneViewCamera", _isSceneViewCamera ? 1 : 0);
                    }
                    renderedObjIndex += increment;
                }
            }
        }

#endregion //METHODS

    }

}

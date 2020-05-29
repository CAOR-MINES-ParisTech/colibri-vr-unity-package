/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEditor;

namespace COLIBRIVR
{

#region ENUMS

        public enum SetupType {Grid, Sphere, Cylinder};
        public enum SetupDirection {Inwards, Outwards};

#endregion //ENUMS

    /// <summary>
    /// Class that enables objects to use camera setups and display them in the scene.
    /// </summary>
    [ExecuteInEditMode]
    public class CameraSetup : MonoBehaviour, IPreviewCaller
    {

#region CONST_FIELDS

        private const string _propertyNameIsColorSourceCamIndices = "_isColorSourceCamIndices";
        private const string _shaderNameIsColorSourceCamIndices = "_IsColorSourceCamIndices";

#endregion //CONST_FIELDS

#region STATIC_METHODS

        /// <summary>
        /// Creates or resets a camera setup object as a child of the given transform.
        /// </summary>
        /// <param name="parentTransform"></param> The parent transform.
        /// <returns></returns> The camera setup object.
        public static CameraSetup CreateOrResetCameraSetup(Transform parentTransform = null)
        {
            CameraSetup existingSetup = GeneralToolkit.GetOrCreateChildComponent<CameraSetup>(parentTransform);
            existingSetup.Reset();
            return existingSetup;
        }

#endregion //STATIC_METHODS

#region INHERITANCE_PROPERTIES

        public int previewIndex 
        { 
            get
            {
                if(cameraModels != null)
                    _previewIndex = Mathf.Min(_previewIndex, cameraModels.Length - 1);
                _previewIndex = Mathf.Max(0, _previewIndex);
                return _previewIndex;
            } 
            set
            {
                _previewIndex = value;
            }
        }

        public UnityEvent onPreviewIndexChangeEvent
        { 
            get
            {
                if(_onPreviewIndexChangeEvent == null)
                    _onPreviewIndexChangeEvent = new UnityEvent();
                return _onPreviewIndexChangeEvent;
            }
        }

#endregion //INHERITANCE_PROPERTIES

#region FIELDS


        public CameraModel[] cameraModels;
        public Vector3 initialViewingPosition;

        [SerializeField] private int _previewIndex;
        [SerializeField] private UnityEvent _onPreviewIndexChangeEvent;
        [SerializeField] private bool _isColorSourceCamIndices;

        private Vector3 _previousLossyScale;

#endregion //FIELDS

#region INHERITANCE_METHODS

        /// <summary>
        /// Resets the object's properties.
        /// </summary>
        public void Reset()
        {
            ResetCameraModels();
#if UNITY_EDITOR
            _isColorSourceCamIndices = false;
#endif //UNITY_EDITOR
            initialViewingPosition = Vector3.zero;
        }

        /// <summary>
        /// On update, prevent the global scale from being negative on either of the axes.
        /// </summary>
        void Update()
        {
            if(transform.lossyScale != _previousLossyScale)
            {
                Vector3 localScale = transform.localScale;
                localScale.x *= Mathf.Sign(transform.lossyScale.x);
                localScale.y *= Mathf.Sign(transform.lossyScale.y);
                localScale.z *= Mathf.Sign(transform.lossyScale.z);
                transform.localScale = localScale;
                _previousLossyScale = transform.lossyScale;
            }
        }

        /// <summary>
        /// Remove camera models on destroy.
        /// </summary>
        void OnDestroy()
        {
            if(!GeneralToolkit.IsStartingNewScene())
                ResetCameraModels();
        }

#endregion //INHERITANCE_METHODS

#region METHODS

#if UNITY_EDITOR

        /// <summary>
        /// On selection, update the gizmos and add a preview listener.
        /// </summary>
        public void Selected()
        {
            onPreviewIndexChangeEvent.AddListener(OnPreviewIndexChange);
            UpdateGizmosColor();
        }

        /// <summary>
        /// On deselection, remove the preview listener.
        /// </summary>
        public void Deselected()
        {
            onPreviewIndexChangeEvent.RemoveListener(OnPreviewIndexChange);
        }

        /// <summary>
        /// Returns the frame bounds of the camera model. This will enable the scene view to focus on the object on double-click.
        /// </summary>
        /// <returns></returns> The object's frame bounds.
        public Bounds GetFrameBounds()
        {
            Bounds frameBounds = new Bounds();
            if(cameraModels != null && cameraModels.Length > 0)
            {
                frameBounds.center = cameraModels[0].transform.position;
                for(int iter = 0; iter < cameraModels.Length; iter++)
                    frameBounds.Encapsulate(cameraModels[iter].GetFrameBounds());
            }
            frameBounds.extents *= 1f;
            return frameBounds;
        }

        /// <summary>
        /// On preview index change, update the gizmos' color;
        /// </summary>
        public void OnPreviewIndexChange()
        {
            UpdateGizmosColor();
        }

        /// <summary>
        /// Updates the gizmo's color for each of the setup's camera models. 
        /// </summary>
        public void UpdateGizmosColor()
        {
            if(cameraModels != null)
                for(int iter = 0; iter < cameraModels.Length; iter++)
                    UpdateGizmoColor(iter);
        }

        /// <summary>
        /// Updates the given camera model's gizmo color.
        /// </summary>
        /// <param name="setupIndex"></param>
        private void UpdateGizmoColor(int setupIndex)
        {
            CameraModel cameraModel = cameraModels[setupIndex];
            cameraModel.gizmoColor = CameraModel.baseColor;
            if (_isColorSourceCamIndices)
            {
                cameraModel.gizmoColor = GeneralToolkit.GetColorForIndex(setupIndex, cameraModels.Length);
            }
            else
            {
                Transform activeTransform = Selection.activeTransform;
                if(activeTransform != null && transform.IsChildOf(activeTransform) && setupIndex == previewIndex)
                    cameraModel.gizmoColor = CameraModel.selectedColor;
            }
        }

        /// <summary>
        /// Enables the user to choose whether the displayed color helps visualize the source camera indices.
        /// </summary>
        public void SectionColorIsIndices()
        {
            SerializedObject serializedObject = new SerializedObject(this);
            serializedObject.Update();
            string label = "Color is indices:";
            string tooltip = "Whether the displayed colors help visualize the source camera indices instead of the actual texture colors.";
            SerializedProperty propertyIsColorSourceCamIndices = serializedObject.FindProperty(_propertyNameIsColorSourceCamIndices);
            propertyIsColorSourceCamIndices.boolValue = EditorGUILayout.Toggle(new GUIContent(label, tooltip), propertyIsColorSourceCamIndices.boolValue);
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Indicates to the material whether the color to render should help visualize the camera indices instead of the texture color.
        /// </summary>
        /// <param name="blendingMaterial"></param> The blending material to notify.
        public void SetColorIsIndices(ref Material blendingMaterial)
        {
            blendingMaterial.SetInt(_shaderNameIsColorSourceCamIndices, _isColorSourceCamIndices ? 1 : 0);
        }

#endif //UNITY_EDITOR

        /// <summary>
        /// Adds a camera model to the setup.
        /// </summary>
        /// <param name="setupIndex"></param> Index of the newly-created camera in the setup.
        /// <returns></returns> The created camera model.
        public CameraModel AddCameraModel(int setupIndex)
        {
            CameraModel cameraModel = CameraModel.CreateCameraModel(transform);
            cameraModels[setupIndex] = cameraModel;
#if UNITY_EDITOR
            UpdateGizmoColor(setupIndex);
#endif //UNITY_EDITOR
            return cameraModel;
        }

        /// <summary>
        /// Resets the setup's camera models;
        /// </summary>
        public void ResetCameraModels()
        {
            if(cameraModels != null)
                for(int i = 0; i < cameraModels.Length; i++)
                    if(cameraModels[i] != null)
                        DestroyImmediate(cameraModels[i].gameObject);
            cameraModels = null;
#if UNITY_EDITOR
            previewIndex = 0;
#endif //UNITY_EDITOR
        }

        /// <summary>
        /// Finds the camera models in the object's children.
        /// </summary>
        public void FindCameraModels()
        {
            cameraModels = gameObject.GetComponentsInChildren<CameraModel>();
            if(cameraModels.Length == 0)
                cameraModels = null;
#if UNITY_EDITOR
            UpdateGizmosColor();
#endif //UNITY_EDITOR
        }

        /// <summary>
        /// Sets additional parameters for the camera setup.
        /// </summary>
        /// <param name="newInitialViewingPosition"></param> The new value to set for the initial viewing position.
        public void SetAdditionalParameters(Vector3 newInitialViewingPosition)
        {
            initialViewingPosition = newInitialViewingPosition;
        }

        /// <summary>
        /// Computes camera poses on a grid.
        /// </summary>
        /// <param name="cameraCount"></param> The number of cameras on each of the axes of the grid.
        public void ComputeGridPoses(Vector2Int cameraCount)
        {
            // Compute several preliminary values.
            Transform parentTransform = cameraModels[0].transform.parent;
            Vector3 lossyScale = parentTransform.lossyScale;
            Vector2 absScale = new Vector2(Mathf.Abs(lossyScale.x), Mathf.Abs(lossyScale.y));
            Vector2 numberOfIntervals = cameraCount - Vector2.one;
            Vector2 intervalSize = new Vector2(1f / Mathf.Max(1, numberOfIntervals.x), 1f / Mathf.Max(1, numberOfIntervals.y));
            // Compute the minimum inter-camera distance.
            float distanceToClosestCam = Mathf.Min(absScale.x * intervalSize.x, absScale.y * intervalSize.y);
            // Update the camera model of all source cameras.
            for(int j = 0; j < cameraCount.y; j++)
            {
                for(int i = 0; i < cameraCount.x; i++)
                {
                    int index = j * cameraCount.x + i;
                    CameraModel cameraModel = cameraModels[index];
                    cameraModel.SetCameraReferenceIndexAndImageName(index + 1, index.ToString("0000") + ".png");
                    Vector2 planePos = (new Vector2(i, j) - 0.5f * numberOfIntervals) * intervalSize;
                    cameraModel.transform.localPosition = planePos.x * Vector3.right + planePos.y * Vector3.up;
                    cameraModel.transform.localRotation = Quaternion.identity;
                    cameraModel.UpdateDistanceToClosestCam(distanceToClosestCam);
                }
            }
            // Set the initial viewing position so that the viewer will initially look at the center of the grid.
            float largestDim = Mathf.Max(absScale.x, absScale.y);
            initialViewingPosition = parentTransform.position - 0.5f * largestDim * parentTransform.forward;
        }

        /// <summary>
        /// Computes camera poses on a sphere.
        /// </summary>
        /// <param name="cameraCount"></param> The number of cameras on each of the arcs of the sphere.
        /// <param name="setupDirection"></param> The direction of the sphere setup, inward or outward.
        public void ComputeSpherePoses(Vector2Int cameraCount, SetupDirection setupDirection)
        {
            // Compute several preliminary values.
            Transform parentTransform = cameraModels[0].transform.parent;
            Vector3 lossyScale = parentTransform.lossyScale;
            Vector2 intervalArcDistance = Vector2.one / cameraCount;
            Vector2 degreesPerIteration = new Vector2(360f, 180f) * intervalArcDistance;
            int facingDirection = (setupDirection == SetupDirection.Outwards) ? 1 : -1;
            // Update the camera model of all source cameras.
            for(int j = 0; j < cameraCount.y; j++)
            {
                for(int i = 0; i < cameraCount.x; i++)
                {
                    int index = j * cameraCount.x + i;
                    CameraModel cameraModel = cameraModels[index];
                    cameraModel.SetCameraReferenceIndexAndImageName(index + 1, index.ToString("0000") + ".png");
                    cameraModel.transform.localRotation = Quaternion.AngleAxis(i * degreesPerIteration.x, -Vector3.up) * Quaternion.AngleAxis(-90f + (j + 0.5f) * degreesPerIteration.y, -Vector3.right);
                    cameraModel.transform.localPosition = facingDirection * (cameraModel.transform.localRotation * Vector3.forward);
                    CheckCamDistanceWithOthersInSetup(index, Mathf.Max(0, index - cameraCount.x), index);
                }
            }
            // Set the initial viewing position so that the viewer will look outwards from within, or inwards from without, based on the specified setup direction.
            initialViewingPosition = (facingDirection == 1) ? parentTransform.position : parentTransform.position - 1.5f * parentTransform.localScale.magnitude * parentTransform.forward;
        }

        /// <summary>
        /// Computes camera poses on a cylinder.
        /// </summary>
        /// <param name="cameraCount"></param> The number of cameras on the circular section and on the vertical side of the cylinder.
        /// <param name="setupDirection"></param> The direction of the cylinder setup, inward or outward.
        public void ComputeCylinderPoses(Vector2Int cameraCount, SetupDirection setupDirection)
        {
            // Compute several preliminary values.
            Transform parentTransform = cameraModels[0].transform.parent;
            float horizontalDegreesPerIteration = 360f / cameraCount.x;
            int facingDirection = (setupDirection == SetupDirection.Outwards) ? 1 : -1;
            int numberOfVerticalIntervals = cameraCount.y - 1;
            float verticalIntervalSize = 1f / Mathf.Max(1, numberOfVerticalIntervals);
            float distanceToClosestVerticalCam = Mathf.Abs(parentTransform.lossyScale.y) * verticalIntervalSize;
            // Update the camera model of all source cameras.
            for(int j = 0; j < cameraCount.y; j++)
            {
                for(int i = 0; i < cameraCount.x; i++)
                {
                    int index = j * cameraCount.x + i;
                    CameraModel cameraModel = cameraModels[index];
                    cameraModel.SetCameraReferenceIndexAndImageName(index + 1, index.ToString("0000") + ".png");
                    cameraModel.transform.localRotation = Quaternion.AngleAxis(i * horizontalDegreesPerIteration, -Vector3.up);
                    cameraModel.transform.localPosition = facingDirection * (cameraModel.transform.localRotation * Vector3.forward) + ((j - 0.5f * numberOfVerticalIntervals) * verticalIntervalSize) * Vector3.up;
                    cameraModel.UpdateDistanceToClosestCam(distanceToClosestVerticalCam);
                    CheckCamDistanceWithOthersInSetup(index, Mathf.Max(0, index - 1), index);
                }
            }
            // Set the initial viewing position so that the viewer will look outwards from within, or inwards from without, based on the specified setup direction.
            initialViewingPosition = (facingDirection == 1) ? parentTransform.position : parentTransform.position - 1.5f * parentTransform.localScale.magnitude * parentTransform.forward;
        }

        /// <summary>
        /// Updates the "distance to closest camera" attribute for each camera model in the setup, by comparison with the camera at the given index.
        /// </summary>
        /// <param name="indexToCompare"></param> The index of the source camera with which to check relative distance.
        /// <param name="startSourceCamIndex"></param> The source camera index at which to start checking.
        /// <param name="endSourceCamIndex"></param> The source camera index before which to end checking.
        public void CheckCamDistanceWithOthersInSetup(int indexOfCameraToCompare, int startSourceCamIndex, int endSourceCamIndex)
        {
            CameraModel cameraModel = cameraModels[indexOfCameraToCompare];
            for(int sourceCamIndex = startSourceCamIndex; sourceCamIndex < endSourceCamIndex; sourceCamIndex++)
            {
                if(sourceCamIndex != indexOfCameraToCompare)
                {
                    CameraModel otherCameraModel = cameraModels[sourceCamIndex];
                    float distanceBetweenCams = (cameraModel.transform.position - otherCameraModel.transform.position).magnitude;
                    if(cameraModel.distanceToClosestCam == 0f || distanceBetweenCams < cameraModel.distanceToClosestCam)
                        cameraModel.UpdateDistanceToClosestCam(distanceBetweenCams);
                    if(otherCameraModel.distanceToClosestCam == 0f || distanceBetweenCams < otherCameraModel.distanceToClosestCam)
                        otherCameraModel.UpdateDistanceToClosestCam(distanceBetweenCams);
                }
            }
        }

        /// <summary>
        /// Updates the camera setup to have the given number of cameras.
        /// </summary>
        /// <param name="cameraCount"></param> The number of cameras for the setup.
        public void ChangeCameraCount(int cameraCount)
        {
            CameraModel cameraParams = CameraModel.CreateCameraModel();
            // If there are already cameras in the setup, use one of them as the model.
            if(cameraModels != null && cameraModels.Length > 0)
                cameraParams.ParametersFromCameraModel(cameraModels[0]);
            else
                cameraParams.SetCameraReferenceIndexAndImageName(cameraParams.cameraReferenceIndex, "Image");
            // Update the camera setup, using this camera as a model.
            ResetCameraModels();
            cameraModels = new CameraModel[cameraCount];
            for(int iter = 0; iter < cameraModels.Length; iter++)
            {
                CameraModel cameraModel = AddCameraModel(iter);
                cameraModel.ParametersFromCameraModel(cameraParams);
                cameraModel.SetCameraReferenceIndexAndImageName(cameraModel.cameraReferenceIndex, cameraModel.imageName + GeneralToolkit.ToString(iter));
            }
            // Destroy the temporary camera model.
            DestroyImmediate(cameraParams.gameObject);
        }

#endregion //METHODS

    }

}

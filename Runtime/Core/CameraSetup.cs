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

        public enum SetupType {Grid, Sphere};
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

#if UNITY_EDITOR
 
        /// <summary>
        /// Computes the size of the camera gizmos for the given setup.
        /// To make computations reasonable, a certain number of assumptions are made.
        /// </summary>
        /// <param name="selectedCameraModels"></param> The array of selected camera models.
        /// <param name="interCamDistanceFactor"></param> Inter-camera distance factor, computed empirically so that gizmos take a reasonable size.
        /// <returns></returns> The gizmo size.
        public static float ComputeGizmoSize(CameraModel[] selectedCameraModels, float interCamDistanceFactor = -1f)
        {
            if(selectedCameraModels == null || selectedCameraModels.Length < 1)
                return -1;
            float outputGizmoSize = 0f;
            // If the inter-camera distance factor is not provided, compute it (here: one of the smallest inter-camera distances).
            if(interCamDistanceFactor < 0)
            {
                int reasonableNumberOfTests = 100;
                int iterationNumber = 0;
                List<float> distanceList = new List<float>();
                for(int indexA = 0; indexA < selectedCameraModels.Length; indexA++)
                {
                    for(int indexB = indexA + 1; indexB < selectedCameraModels.Length; indexB++)
                    {
                        Vector3 positionA = selectedCameraModels[indexA].transform.position;
                        Vector3 positionB = selectedCameraModels[indexB].transform.position;
                        float distance = (positionB - positionA).magnitude;
                        distanceList.Add(distance);
                        iterationNumber++;
                        if(iterationNumber > reasonableNumberOfTests)
                            break;     
                    }       
                }
                if(distanceList.Count > 0)
                {
                    float[] distanceArray = distanceList.ToArray();
                    Array.Sort(distanceArray);
                    interCamDistanceFactor = distanceArray[Mathf.RoundToInt(distanceArray.Length / 10)];
                }
                else
                {
                    interCamDistanceFactor = 1f;
                }
            }
            // Hypothesis: all cameras share projection type.
            bool isOmnidirectional = selectedCameraModels[0].isOmnidirectional;
            // If cameras are omnidirectional, compute the radius of a sphere gizmo.
            if(isOmnidirectional)
            {
                // Set gizmo size to a quarter of the distance factor.
                outputGizmoSize = 0.25f * interCamDistanceFactor;
            }
            // If cameras are perspective, compute the max range of a frustum gizmo.
            else
            {
                // Hypothesis: all cameras share aspect ratio.
                float aspectRatio = selectedCameraModels[0].pixelResolution.x * 1f / selectedCameraModels[0].pixelResolution.y;
                // Set sensor size to half of the distance factor made smaller by the aspect ratio.
                float sensorSize = 0.5f * interCamDistanceFactor * Mathf.Min(aspectRatio, 1f / aspectRatio);
                // Hypothesis: all cameras share field of view.
                Vector2 fieldOfView = selectedCameraModels[0].fieldOfView;
                // Set gizmo size as corresponding focal length.
                if(aspectRatio > 1)
                    outputGizmoSize = Camera.FieldOfViewToFocalLength(fieldOfView.y, sensorSize);
                else
                    outputGizmoSize = Camera.FieldOfViewToFocalLength(fieldOfView.x, sensorSize);
                // Set twice the sensor size as a maximum for gizmo size.
                outputGizmoSize = Mathf.Min(outputGizmoSize, 2 * sensorSize);
            }
            // Return the computed gizmo size.
            return outputGizmoSize;
        }

#endif //UNITY_EDITOR

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
        public float gizmoSize;

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
            UpdateGizmosSize();
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
        /// Updates the gizmo's size for each of the setup's camera models.
        /// </summary>
        public void UpdateGizmosSize()
        {
            if(cameraModels != null)
                for(int iter = 0; iter < cameraModels.Length; iter++)
                    cameraModels[iter].focalDistance = gizmoSize;
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
            gizmoSize = ComputeGizmoSize(cameraModels);
            UpdateGizmosSize();
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
            Vector2 numberOfIntervals = cameraCount - Vector2.one;
            Vector2 intervalSize = new Vector2(transform.localScale.x / Mathf.Max(1, numberOfIntervals.x), transform.localScale.y / Mathf.Max(1, numberOfIntervals.y));
            // Update the camera model of all source cameras.
            for(int j = 0; j < cameraCount.y; j++)
            {
                for(int i = 0; i < cameraCount.x; i++)
                {
                    int index = j * cameraCount.x + i;
                    CameraModel cameraModel = cameraModels[index];
                    cameraModel.SetCameraReferenceIndexAndImageName(index, index.ToString("0000") + ".png");
                    Vector2 planePos = (new Vector2(i, j) - 0.5f * numberOfIntervals) * intervalSize;
                    cameraModel.transform.position = transform.position + planePos.x * transform.right + planePos.y * transform.up;
                    cameraModel.transform.rotation = transform.rotation;
                }
            }
            // Set the initial viewing position so that the viewer will initially look at the center of the grid.
            Vector2 absScale = new Vector2(Mathf.Abs(transform.localScale.x), Mathf.Abs(transform.localScale.y));
            float largestDim = Mathf.Max(absScale.x, absScale.y);
            initialViewingPosition = transform.position - 0.5f * largestDim * transform.forward;
            // Compute inter-camera distance factor (here: minimum inter-camera distance).
            float interCamDistanceFactor = Mathf.Min(intervalSize.x, intervalSize.y);
#if UNITY_EDITOR
            // Compute the gizmo size.
            gizmoSize = ComputeGizmoSize(cameraModels, interCamDistanceFactor);
            UpdateGizmosSize();
#endif //UNITY_EDITOR
        }

        /// <summary>
        /// Computes camera poses on a sphere.
        /// </summary>
        /// <param name="cameraCount"></param> The number of cameras on each of the arcs of the sphere.
        /// <param name="setupDirection"></param> The direction of the sphere setup, inward or outward.
        public void ComputeSpherePoses(Vector2Int cameraCount, SetupDirection setupDirection)
        {
            // Compute several preliminary values. 
            Vector2 intervalArcDistance = Vector2.one / cameraCount;
            Vector2 degreesPerIteration = new Vector2(360f, 180f) * intervalArcDistance;
            int facingDirection = (setupDirection == SetupDirection.Outwards) ? 1 : -1;
            Vector3 absScale = new Vector3(Mathf.Abs(transform.localScale.x), Mathf.Abs(transform.localScale.y), Mathf.Abs(transform.localScale.z));
            // Update the camera model of all source cameras.
            for(int j = 0; j < cameraCount.y; j++)
            {
                for(int i = 0; i < cameraCount.x; i++)
                {
                    int index = j * cameraCount.x + i;
                    CameraModel cameraModel = cameraModels[index];
                    cameraModel.SetCameraReferenceIndexAndImageName(index, index.ToString("0000") + ".png");
                    cameraModel.transform.rotation = Quaternion.AngleAxis(i * degreesPerIteration.x, -transform.up) * Quaternion.AngleAxis(-90f + (j + 0.5f) * degreesPerIteration.y, -transform.right) * transform.rotation;
                    cameraModel.transform.position = transform.position + Vector3.Scale(cameraModels[index].transform.rotation * Vector3.forward, facingDirection * absScale);
                }
            }
            // Set the initial viewing position so that the viewer will look outwards from within, or inwards from without, based on the specified setup direction.
            initialViewingPosition = (facingDirection == 1) ? transform.position : transform.position - 1.5f * transform.localScale.magnitude * transform.forward;
            // Compute the inter-camera distance factor (here: minimum inter-camera distance).
            float interCamDistanceFactor = 1f;
            int totalCameraCount = cameraCount.x * cameraCount.y;
            if(totalCameraCount > 1)
            {
                float xDistance = (cameraModels[1].transform.position - cameraModels[0].transform.position).magnitude;
                if(xDistance > 0)
                    interCamDistanceFactor = xDistance;
                int halfHeightIndexOne = (Mathf.FloorToInt(cameraCount.y / 2) - 1) * cameraCount.x;
                int halfHeightIndexTwo = (halfHeightIndexOne + cameraCount.x);
                float yDistance = (cameraModels[halfHeightIndexOne % totalCameraCount].transform.position - cameraModels[halfHeightIndexTwo % totalCameraCount].transform.position).magnitude;
                if(yDistance > 0)
                    interCamDistanceFactor = Mathf.Min(interCamDistanceFactor, yDistance);
            }
#if UNITY_EDITOR
            // Compute the gizmo size.
            gizmoSize = ComputeGizmoSize(cameraModels, interCamDistanceFactor);
            UpdateGizmosSize();
#endif //UNITY_EDITOR
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
# if UNITY_EDITOR
            // Update the gizmo size.
            gizmoSize = ComputeGizmoSize(cameraModels);
            UpdateGizmosSize();
#endif //UNITY_EDITOR
            // Destroy the temporary camera model.
            DestroyImmediate(cameraParams.gameObject);
        }

#endregion //METHODS

    }

}

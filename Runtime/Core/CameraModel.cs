/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

using UnityEngine;
using UnityEditor;

namespace COLIBRIVR
{

    /// <summary>
    /// Class that stores the camera parameters required by the acquisition and rendering pipelines.
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    public class CameraModel : MonoBehaviour
    {

#region STATIC_PROPERTIES

        public static Color baseColor { get { return Color.cyan; } }
        public static Color selectedColor { get { return Color.yellow; } }

#endregion //STATIC_PROPERTIES

#region STATIC_METHODS

        /// <summary>
        /// Creates a camera model, eventually set as the child of a parent transform.
        /// </summary>
        /// <param name="parentTransform"></param> The parent transform.
        /// <returns></returns> The created camera model.
        public static CameraModel CreateCameraModel(Transform parentTransform = null)
        {
            CameraModel cameraModel = new GameObject().AddComponent<CameraModel>();
            cameraModel.Reset();
            if(parentTransform != null)
            {
                cameraModel.transform.parent = parentTransform;
                GeneralToolkit.SetTransformValues(cameraModel.transform, true, Vector3.zero, Quaternion.identity, Vector3.one);
            }
            return cameraModel;
        }

        /// <summary>
        /// Returns the definition of the omnidirectional field-of-view.
        /// </summary>
        /// <returns></returns> A field-of-view with 360-degrees horizontally, 180-degrees vertically.
        public static Vector2 GetOmnidirectionalFieldOfView()
        {
            return new Vector2(360f, 180f);
        }

        /// <summary>
        /// Checks whether a given field-of-view is omnidirectional.
        /// </summary>
        /// <param name="fieldOfView"></param> The input field-of-view.
        /// <returns></returns> True if the field-of-view is omnidirectional, false otherwise.
        public static bool IsOmnidirectional(Vector2 fieldOfView)
        {
            return (fieldOfView.x >= 180f);
        }

        /// <summary>
        /// Returns a field-of-view from the given focal surface parameters.
        /// </summary>
        /// <param name="focalSurfaceParams"></param> Focal surface parameters: (image plane width, image plane height, focal distance).
        /// <returns></returns> The corresponding field-of-view.
        public static Vector2 FocalSurfaceParamsToFieldOfView(Vector3 focalSurfaceParams)
        {
            if(focalSurfaceParams.x < 0 || focalSurfaceParams.y < 0)
            {
                return GetOmnidirectionalFieldOfView();
            }
            else
            {
                float halfFieldOfViewRadX = Mathf.Atan2(focalSurfaceParams.x, (2f * focalSurfaceParams.z));
                float halfFieldOfViewRadY = Mathf.Atan2(focalSurfaceParams.y, (2f * focalSurfaceParams.z));
                return 2f * Mathf.Rad2Deg * new Vector2(halfFieldOfViewRadX, halfFieldOfViewRadY);
            }
        }

        /// <summary>
        /// Returns focal surface parameters from the given field-of-view.
        /// </summary>
        /// <param name="fieldOfView"></param> The input field-of-view.
        /// <param name="focalDistance"></param> The input focal distance.
        /// <returns></returns> The corresponding focal surface parameters: (image plane width, image plane height, focal distance).
        public static Vector3 FieldOfViewToFocalSurfaceParams(Vector2 fieldOfView, float focalDistance)
        {
            if(IsOmnidirectional(fieldOfView))
            {
                return new Vector3(-1, -1, focalDistance);
            }
            else
            {
                Vector2 halfFieldOfViewRad = 0.5f * Mathf.Deg2Rad * fieldOfView;
                Vector2 halfScaleFocalOne = new Vector2(Mathf.Tan(halfFieldOfViewRad.x), Mathf.Tan(halfFieldOfViewRad.y));
                Vector2 imagePlaneScale = 2f * focalDistance * halfScaleFocalOne;
                return new Vector3(imagePlaneScale.x, imagePlaneScale.y, focalDistance);
            }
        }

#endregion //STATIC_METHODS

#region FIELDS

        public string imageName;
        public int cameraReferenceIndex;
        public bool isOmnidirectional;
        public float focalDistance;
        public Color gizmoColor;
        public Vector2 distanceRange;
        public MeshRenderer meshRenderer;

        [SerializeField] private Vector2Int _omnidirectionalPixelResolution;
        [SerializeField] private Vector2Int _perspectivePixelResolution;
        [SerializeField] private Vector2 _perspectiveFOV;

#endregion //FIELDS

#region PROPERTIES

        public Vector2Int pixelResolution
        {
            get { return isOmnidirectional ? _omnidirectionalPixelResolution : _perspectivePixelResolution; }
            set { if(isOmnidirectional) { _omnidirectionalPixelResolution = value; } else { _perspectivePixelResolution = value; } }
        }

        public Vector2 fieldOfView
        {
            get { return isOmnidirectional ? new Vector2(360f, 180f) : _perspectiveFOV; }
            set { if(!isOmnidirectional) {_perspectiveFOV = value;} }
        }

        public string modelName { get { return isOmnidirectional ? "OMNIDIRECTIONAL" : "SIMPLE_PINHOLE"; } }

        public Vector3 focalSurfaceParams { get { return FieldOfViewToFocalSurfaceParams(fieldOfView, focalDistance); } }
        public float aspect { get { return focalSurfaceParams.x / focalSurfaceParams.y; } }

#endregion //PROPERTIES

#region INHERITANCE_METHODS

#if UNITY_EDITOR

        /// <summary>
        /// Draws the camera model's gizmo.
        /// </summary>
        void OnDrawGizmos()
        {
            Transform activeTransform = Selection.activeTransform;
            if(activeTransform != null)
            {
                if(transform == activeTransform)
                    gizmoColor = baseColor;
                if(transform.IsChildOf(activeTransform))
                    DrawGizmo();
            }
        }

#endif //UNITY_EDITOR

        /// <summary>
        /// Resets the camera parameters.
        /// </summary>
        public void Reset()
        {
            isOmnidirectional = false;
            focalDistance = 1f;
            gizmoColor = baseColor;
            _omnidirectionalPixelResolution = new Vector2Int(512, 256);
            _perspectivePixelResolution = new Vector2Int(128, 128);
            _perspectiveFOV = new Vector2(60f, 60f);
            distanceRange = new Vector2(0.3f, 1000f);
            SetCameraReferenceIndexAndImageName(1, string.Empty);
            meshRenderer = GeneralToolkit.GetOrAddComponent<MeshRenderer>(gameObject);
#if UNITY_EDITOR
            UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(meshRenderer, false);
#endif //UNITY_EDITOR
        }

        /// <summary>
        /// Displays a string with the camera parameters.
        /// </summary>
        /// <returns></returns> The corresponding string.
        public override string ToString()
        {
            string toString = "Pixel resolution " + pixelResolution + ", Field of view " + fieldOfView;
            toString += ", Distance range " + distanceRange + ", position " + transform.position + ", rotation " + transform.rotation;
            toString += ", Image name " + imageName + ", index " + cameraReferenceIndex + ", Omnidirectional " + isOmnidirectional;
            return toString;
        }

#endregion //INHERITANCE_METHODS

#region METHODS

        /// <summary>
        /// Returns the frame bounds of the camera model. This will enable the scene view to focus on the object on double-click.
        /// </summary>
        /// <returns></returns> The object's frame bounds.
        public Bounds GetFrameBounds()
        {
            Bounds frameBounds = new Bounds();
            frameBounds.center = transform.position;
            frameBounds.extents = focalDistance * Vector3.one;
            return frameBounds;
        }

        /// <summary>
        /// Sets the camera model's camera reference index and image name, and changes the object's name accordingly.
        /// </summary>
        public void SetCameraReferenceIndexAndImageName(int newIndex, string newImageName)
        {
            cameraReferenceIndex = newIndex;
            imageName = newImageName;
            gameObject.name = System.IO.Path.GetFileNameWithoutExtension(imageName) + "_Camera" + GeneralToolkit.ToString(cameraReferenceIndex);
        }

        /// <summary>
        /// Copies another camera model's parameters.
        /// </summary>
        /// <param name="cameraModel"></param> The camera model to copy parameters from.
        public void ParametersFromCameraModel(CameraModel cameraModel)
        {
            isOmnidirectional = cameraModel.isOmnidirectional;
            focalDistance = cameraModel.focalDistance;
            if(isOmnidirectional)
            {
                _omnidirectionalPixelResolution = cameraModel.pixelResolution;
                cameraModel.isOmnidirectional = false;
                _perspectivePixelResolution = cameraModel.pixelResolution;
                _perspectiveFOV = cameraModel.fieldOfView;
            }
            else
            {
                _perspectivePixelResolution = cameraModel.pixelResolution;
                _perspectiveFOV = cameraModel.fieldOfView;
                cameraModel.isOmnidirectional = true;
                _omnidirectionalPixelResolution = cameraModel.pixelResolution;
            }
            cameraModel.isOmnidirectional = isOmnidirectional;
            distanceRange = cameraModel.distanceRange;
            transform.position = cameraModel.transform.position;
            transform.rotation = cameraModel.transform.rotation;
            SetCameraReferenceIndexAndImageName(cameraModel.cameraReferenceIndex, cameraModel.imageName);
        }

        /// <summary>
        /// Transfers the model's parameters to a camera component.
        /// </summary>
        /// <param name="cam"></param> The camera component to transfer the parameters to.
        public void TransferParametersToCamera(ref Camera cam)
        {
            cam.fieldOfView = fieldOfView.y;
            cam.aspect = aspect;
            cam.nearClipPlane = Mathf.Max(0.01f, distanceRange.x - 0.1f);
            cam.farClipPlane = Mathf.Min(1000f, distanceRange.y + 0.1f);
            cam.transform.position = transform.position;
            cam.transform.rotation = transform.rotation;
        }

        /// <summary>
        /// Gets model parameters from a camera component.
        /// </summary>
        /// <param name="cam"></param> The camera component.
        /// <param name="pixelHeight"></param>
        public void GetParametersFromCamera(Camera cam)
        {
            pixelResolution = new Vector2Int((int)(pixelResolution.y * cam.aspect), pixelResolution.y);
            fieldOfView = new Vector2(Camera.VerticalToHorizontalFieldOfView(cam.fieldOfView, cam.aspect), cam.fieldOfView);
            distanceRange = new Vector2(cam.nearClipPlane + 0.1f, cam.farClipPlane - 0.1f);
            transform.position = cam.transform.position;
            transform.rotation = cam.transform.rotation;
        }

        /// <summary>
        /// Draws a gizmo in the scene view.
        /// </summary>
        public void DrawGizmo()
        {
            Matrix4x4 tempMatrix = Gizmos.matrix;
            Color tempColor = Gizmos.color;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.color = gizmoColor;
            if(isOmnidirectional)
                Gizmos.DrawWireSphere(Vector3.zero, focalDistance);
            else
                Gizmos.DrawFrustum(Vector3.zero, fieldOfView.y, focalDistance, 0, aspect);
            Gizmos.matrix = tempMatrix;
            Gizmos.color = tempColor;
        }

        /// <summary>
        /// Updates the camera's field of view for the given transform values.
        /// </summary>
        /// <param name="parentTransform"></param>
        public void UpdateFieldOfViewForScale(Transform parentTransform)
        {
            // If the camera is omnidirectional, do nothing.
            if(isOmnidirectional)
                return;
            // If the camera is perspective, update the field of view.
            Vector3 localSpaceParams = Quaternion.Inverse(transform.rotation) * focalSurfaceParams;
            Vector3 scaledParams = Vector3.Scale(parentTransform.lossyScale, localSpaceParams);
            Vector3 rotatedParams = transform.rotation * scaledParams;
            fieldOfView = FocalSurfaceParamsToFieldOfView(new Vector3(rotatedParams.x, rotatedParams.y, rotatedParams.z));
        }

#endregion //METHODS

    }

}

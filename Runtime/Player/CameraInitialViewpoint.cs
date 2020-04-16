using System.Collections;
using UnityEngine;

namespace COLIBRIVR.Player
{

    /// <summary>
    /// Class that sets the camera's initial viewpoint to this object's transform values.
    /// This is done after the camera's position is automatically changed as a result of VR tracking, by modifying the camera's parent transform.
    /// </summary>
    public class CameraInitialViewpoint : MonoBehaviour
    {

#region FIELDS

        public bool setPosition = true;
        public bool setRotation = true;

#endregion //FIELDS

#region INHERITANCE_METHODS

        /// <summary>
        /// On start, launch the coroutine to set the camera's initial viewpoint..
        /// </summary>
        void Start()
        {
            StartCoroutine(SetCameraToInitialViewpointCoroutine());
        }

#endregion //INHERITANCE_METHODS

#region METHODS

        /// <summary>
        /// Sets the camera's initial viewpoint to that of this object's transform.
        /// </summary>
        /// <returns></returns>
        private IEnumerator SetCameraToInitialViewpointCoroutine()
        {
            // Set the target position and rotation to those of this object.
            Vector3 targetCameraPos = transform.position;
            Quaternion targetCameraRot = transform.rotation;
            // Wait for a main camera to be created.
            while(Camera.main == null)
                yield return null;
            Transform mainCameraTransform = Camera.main.transform;
            // Get or create the transform of the camera's parent player object.
            Transform playerTransform = mainCameraTransform.parent;
            if(playerTransform == null)
            {
                playerTransform = new GameObject("Player").transform;
                mainCameraTransform.parent = playerTransform;
            }
            // If in XR, wait for the current camera position to change automatically as a result of tracking.
            Vector3 initialCameraPos = mainCameraTransform.position;
            if (UnityEngine.XR.XRSettings.enabled)
            {
                while (mainCameraTransform.position == initialCameraPos)
                    yield return null;
                // To be robust, wait a small additional time after it has changed.
                yield return new WaitForSeconds(0.1f);
            }
            // Make the camera's viewpoint match the target by modifying the player transform.
            if(setPosition)
            {
                Vector3 addPosition = targetCameraPos - mainCameraTransform.position;
                playerTransform.position += addPosition;
            }
            if(setRotation)
            {
                Quaternion addRotation = targetCameraRot * Quaternion.Inverse(mainCameraTransform.rotation);
                Vector3 axis; float angle;
                addRotation.ToAngleAxis(out angle, out axis);
                playerTransform.RotateAround(mainCameraTransform.position, axis, angle);
            }
        }

#endregion //METHODS

    }

}

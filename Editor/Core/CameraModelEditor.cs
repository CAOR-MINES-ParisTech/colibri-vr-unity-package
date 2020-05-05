/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

using UnityEngine;
using UnityEditor;

namespace COLIBRIVR
{

#if UNITY_EDITOR

    /// <summary>
    /// Editor class for CameraModel.
    /// </summary>
    [CustomEditor(typeof(CameraModel))]
    public class CameraModelEditor : Editor
    {

#region CONST_FIELDS

        private const int _perspectiveMinResolutionWidth = 1;
        private const int _perspectiveMaxResolutionWidth = 8192;
        private const int _perspectiveMinResolutionHeight = 1;
        private const int _perspectiveMaxResolutionHeight = 8192;
        private const float _perspectiveMinHorizontalFOV = 1f;
        private const float _perspectiveMaxHorizontalFOV = 179f;
        private const float _perspectiveMinVerticalFOV = 1f;
        private const float _perspectiveMaxVerticalFOV = 179f;
        private const int _omnidirectionalMinResolutionWidth = 8;
        private const int _omnidirectionalMaxResolutionWidth = 8192;

#endregion //CONST_FIELDS

#region STATIC_METHODS

        /// <summary>
        /// Enables the user to choose the camera's projection type.
        /// </summary>
        /// <param name="isOmnidirectional"></param> True if the camera model is omnidirectional, false otherwise.
        /// <returns></returns> True if the camera model is omnidirectional, false otherwise.
        private static bool SubsectionCameraHeader(bool isOmnidirectional)
        {
            // Get the property.
            int popupIndex = isOmnidirectional ? 1 : 0;
            // Define GUI contents.
            GUIContent popupLabel = new GUIContent("Camera projection: ", "Camera projection type.");
            GUIContent perspectiveGUIContent = new GUIContent("Perspective", "Perspective camera (standard, pinhole).");
            GUIContent omnidirectionalGUIContent = new GUIContent("Omnidirectional", "Omnidirectional camera (360-degree, equirectangular).");
            // Display the popup.
            int popupVal = EditorGUILayout.Popup(popupLabel, popupIndex, new GUIContent[] {perspectiveGUIContent, omnidirectionalGUIContent});
            // Modify the property's value and return it.
            return (popupVal == 1);
        }

        /// <summary>
        /// Enables the user to choose the camera's distance range.
        /// </summary>
        /// <param name="distanceRange"></param> The camera model's distance range.
        /// <returns></returns> The camera model's distance range.
        private static Vector2 SubsectionCameraFooter(Vector2 distanceRange)
        {
            // Rich text is used to hide some parts of the text.
            EditorStyles.label.richText = true;
            // Display a slider for minimum distance range.
            string label = "Range: minimum: ";
            string tooltip = "Minimum capture distance (meters) for both color and depth.";
            float newMinDistance = EditorGUILayout.Slider(new GUIContent(label, tooltip), distanceRange.x, 0.01f, distanceRange.y);
            // Display a slider for maximum distance range.
            label = "<color=" + GeneralToolkit.backgroundGUIColor + ">Range:</color> maximum: ";
            tooltip = "Maximum capture distance (meters) for both color and depth.";
            float newMaxDistance = EditorGUILayout.Slider(new GUIContent(label, tooltip), distanceRange.y, newMinDistance, 1000f);
            // Return the property's value.
            return new Vector2(newMinDistance, newMaxDistance);
        }

        /// <summary>
        /// Enables the user to choose a perspective camera's resolution and field-of-view.
        /// </summary>
        /// <param name="perspectivePixelResolution"></param> The input perspective pixel resolution.
        /// <param name="perspectiveFOV"></param> The input perspective field-of-view.
        /// <param name="outPerspectivePixelResolution"></param> Outputs the new perspective pixel resolution.
        /// <param name="outPerspectiveFOV"></param> Outputs the new perspective field-of-view.
        private static void SubsectionPerspective(Vector2Int perspectivePixelResolution, Vector2 perspectiveFOV, out Vector2Int outPerspectivePixelResolution, out Vector2 outPerspectiveFOV)
        {
            // Rich text is used to hide some parts of the text.
            EditorStyles.label.richText = true;
            // Get the properties.
            float oldAspect = perspectivePixelResolution.x * 1f / perspectivePixelResolution.y;
            // Prepare new values for the properties.
            int newPixelWidth = Mathf.Clamp(perspectivePixelResolution.x, _perspectiveMinResolutionWidth, _perspectiveMaxResolutionWidth);
            int newPixelHeight = Mathf.Clamp(perspectivePixelResolution.y, _perspectiveMinResolutionHeight, _perspectiveMaxResolutionHeight);
            float newHorizontalFOV = Mathf.Clamp(perspectiveFOV.x, _perspectiveMinHorizontalFOV, _perspectiveMaxHorizontalFOV);
            float newVerticalFOV = Mathf.Clamp(perspectiveFOV.y, _perspectiveMinVerticalFOV, _perspectiveMaxVerticalFOV);
            // Begin a change check.
            EditorGUI.BeginChangeCheck();
            // Display an int slider for resolution width.
            string label = "Resolution: width: ";
            string tooltip = "Sensor width in pixels.";
            newPixelWidth = EditorGUILayout.IntSlider(new GUIContent(label, tooltip), perspectivePixelResolution.x, _perspectiveMinResolutionWidth, _perspectiveMaxResolutionWidth);
            // End the change check. If resolution width was changed, modify several other parameters. 
            if(EditorGUI.EndChangeCheck())
            {
                // Try to change resolution height (this may not be possible because it might change to non-valid values) to fit with the new resolution width.
                newPixelHeight = (int)Mathf.Clamp(newPixelWidth / oldAspect, _perspectiveMinResolutionHeight, _perspectiveMaxResolutionHeight);
                // Because the aspect ratio may have been forced to change, compute it anew, and update the vertical field of view accordingly.
                float newAspect = newPixelWidth * 1f / newPixelHeight;
                newVerticalFOV = Mathf.Clamp(Camera.HorizontalToVerticalFieldOfView(perspectiveFOV.x, newAspect), _perspectiveMinVerticalFOV, _perspectiveMaxVerticalFOV);
            }
            // Otherwise, continue.
            else
            {
                // Begin a change check.
                EditorGUI.BeginChangeCheck();
                // Display an int slider for resolution height.
                label = "<color=" + GeneralToolkit.backgroundGUIColor + ">Resolution:</color> height: ";
                tooltip = "Sensor height in pixels.";
                int potentialNewPixelHeight = EditorGUILayout.IntSlider(new GUIContent(label, tooltip), perspectivePixelResolution.y, _perspectiveMinResolutionHeight, _perspectiveMaxResolutionHeight);
                // End the change check. If an attempt to modify resolution height was made, modify several other parameters. 
                if(EditorGUI.EndChangeCheck())
                {
                    // Try to change the vertical field of view (this may not be possible because it might change to non-valid values) to fit with the new resolution height.
                    float potentialNewAspectRatio = perspectivePixelResolution.x * 1f / potentialNewPixelHeight;
                    newVerticalFOV = Mathf.Clamp(Camera.HorizontalToVerticalFieldOfView(perspectiveFOV.x, potentialNewAspectRatio), _perspectiveMinVerticalFOV, _perspectiveMaxVerticalFOV);
                    // Based on the new vertical field of view, assign a valid value to the resolution height.
                    newPixelHeight = (int) (2 * Mathf.Tan(Mathf.Deg2Rad * 0.5f * newVerticalFOV) * Camera.FieldOfViewToFocalLength(perspectiveFOV.y, perspectivePixelResolution.y));
                    newPixelHeight = Mathf.Clamp(newPixelHeight, _perspectiveMinResolutionHeight, _perspectiveMaxResolutionHeight);
                }
                // Otherwise, continue.
                else
                {
                    // Display a slider for horizontal field of view.
                    label = "Field of view: H:";
                    tooltip = "Horizontal field of view (degrees).";
                    float potentialNewHorizontalFOV = EditorGUILayout.Slider(new GUIContent(label, tooltip), perspectiveFOV.x, _perspectiveMinHorizontalFOV, _perspectiveMaxHorizontalFOV);
                    // Try to change the vertical field of view (this may not be possible because it might change to non-valid values) to fit with the new horizontal field of view.
                    newVerticalFOV = Mathf.Clamp(Camera.HorizontalToVerticalFieldOfView(potentialNewHorizontalFOV, oldAspect), _perspectiveMinVerticalFOV, _perspectiveMaxVerticalFOV);
                    // Based on the new vertical field of view, assign a valid value to the horizontal field of view.
                    newHorizontalFOV = Camera.VerticalToHorizontalFieldOfView(newVerticalFOV, oldAspect);
                    newHorizontalFOV = Mathf.Clamp(newHorizontalFOV, _perspectiveMinHorizontalFOV, _perspectiveMaxHorizontalFOV);
                    // Display a disabled slider showing the value of the vertical field of view.
                    bool isGUIEnabled = GUI.enabled;
                    GUI.enabled = false;
                    label = "<color=" + GeneralToolkit.backgroundGUIColor + ">Field of view:</color> V:";
                    tooltip = "Vertical field of view (degrees). Modify vertical resolution and horizontal field of view to modify this field.";
                    EditorGUILayout.Slider(new GUIContent(label, tooltip), newVerticalFOV, _perspectiveMinVerticalFOV, _perspectiveMaxVerticalFOV);
                    GUI.enabled = isGUIEnabled;
                }
            }
            // Modify the properties' values.
            outPerspectivePixelResolution = new Vector2Int(newPixelWidth, newPixelHeight);
            outPerspectiveFOV = new Vector2(newHorizontalFOV, newVerticalFOV);
        }

        /// <summary>
        /// Enables the user to choose an omnidirectional camera's resolution.
        /// </summary>
        /// <param name="omnidirectionalPixelResolution"></param> The omnidirectional pixel resolution.
        /// <returns></returns> The omnidirectional pixel resolution.
        private static Vector2Int SubsectionOmnidirectional(Vector2Int omnidirectionalPixelResolution)
        {
            // Rich text is used to hide some parts of the text.
            EditorStyles.label.richText = true;
            // Display a slider for resolution width.
            string label = "Resolution: width: ";
            string tooltip = "Sensor width in pixels.";
            int newPixelWidth = Mathf.Clamp(Mathf.ClosestPowerOfTwo(omnidirectionalPixelResolution.x), _omnidirectionalMinResolutionWidth, _omnidirectionalMaxResolutionWidth);
            newPixelWidth = EditorGUILayout.IntSlider(new GUIContent(label, tooltip), newPixelWidth, _omnidirectionalMinResolutionWidth, _omnidirectionalMaxResolutionWidth);
            newPixelWidth = Mathf.Clamp(Mathf.ClosestPowerOfTwo(newPixelWidth), _omnidirectionalMinResolutionWidth, _omnidirectionalMaxResolutionWidth);
            // Display a disabled slider showing the value of the resolution height.
            int newPixelHeight = newPixelWidth/2;
            bool isGUIEnabled = GUI.enabled;
            GUI.enabled = false;
            label = "<color=" + GeneralToolkit.backgroundGUIColor + ">Resolution:</color> height: ";
            tooltip = "Sensor height in pixels.";
            EditorGUILayout.IntSlider(new GUIContent(label, tooltip), newPixelHeight, _omnidirectionalMinResolutionWidth/2, _omnidirectionalMaxResolutionWidth/2);
            GUI.enabled = isGUIEnabled;
            // Return the value.
            return new Vector2Int(newPixelWidth, newPixelHeight);
        }

        /// <summary>
        /// Enables the user to choose the camera model's parameters.
        /// </summary>
        /// <param name="objectCameraModel"></param> The camera model serialized object to modify.
        public static void SectionCamera(SerializedObject objectCameraModel)
        {
            objectCameraModel.Update();
            SerializedProperty propertyIsOmnidirectional = objectCameraModel.FindProperty(CameraModel.propertyNameIsOmnidirectional);
            SerializedProperty propertyOmnidirectionalPixelResolution = objectCameraModel.FindProperty(CameraModel.propertyNameOmnidirectionalPixelResolution);
            SerializedProperty propertyPerspectivePixelResolution = objectCameraModel.FindProperty(CameraModel.propertyNamePerspectivePixelResolution);
            SerializedProperty propertyPerspectiveFOV = objectCameraModel.FindProperty(CameraModel.propertyNamePerspectiveFOV);
            SerializedProperty propertyDistanceRange = objectCameraModel.FindProperty(CameraModel.propertyNameDistanceRange);
            bool isOmnidirectional = SubsectionCameraHeader(propertyIsOmnidirectional.boolValue);
            propertyIsOmnidirectional.boolValue = isOmnidirectional;
            if(isOmnidirectional)
            {
                propertyOmnidirectionalPixelResolution.vector2IntValue = SubsectionOmnidirectional(propertyOmnidirectionalPixelResolution.vector2IntValue);
            }
            else
            {
                Vector2Int outPerspectivePixelResolution;
                Vector2 outPerspectiveFOV;
                SubsectionPerspective(propertyPerspectivePixelResolution.vector2IntValue, propertyPerspectiveFOV.vector2Value, out outPerspectivePixelResolution, out outPerspectiveFOV);
                propertyPerspectivePixelResolution.vector2IntValue = outPerspectivePixelResolution;
                propertyPerspectiveFOV.vector2Value = outPerspectiveFOV;
            }
            propertyDistanceRange.vector2Value = SubsectionCameraFooter(propertyDistanceRange.vector2Value);
            objectCameraModel.ApplyModifiedProperties();
        }

#endregion //STATIC_METHODS

#region FIELDS

        private CameraModel _targetObject;

#endregion //FIELDS

#region INHERITANCE_METHODS

        /// <summary>
        /// On selection, sets up the target properties.
        /// </summary>
        void OnEnable()
        {
            // Get the target object.
            _targetObject = (CameraModel)serializedObject.targetObject;
        }

        /// <summary>
        /// Displays a GUI enabling the user to see the camera model parameters.
        /// Intentionally, this interface cannot be used to modify the parameters.
        /// </summary>
        public override void OnInspectorGUI()
        {
            // Start the GUI.
            GeneralToolkit.EditorStart(serializedObject, _targetObject);
            bool isGUIEnabled = GUI.enabled;
            GUI.enabled = false;

            // Enable the user to see the camera parameters.
            GeneralToolkit.EditorNewSection("Camera parameters");
            SectionCamera(serializedObject);

            // End the GUI.
            GUI.enabled = isGUIEnabled;
            GeneralToolkit.EditorEnd(serializedObject);
        }

        /// <summary>
        /// Indicates that the object has frame bounds.
        /// </summary>
        /// <returns></returns> True.
        private bool HasFrameBounds()
        {
            return true;
        }

        /// <summary>
        /// On being asked for them, returns the object's frame bounds.
        /// </summary>
        /// <returns></returns>
        private Bounds OnGetFrameBounds()
        {
            return _targetObject.GetFrameBounds();
        }

#endregion //INHERITANCE_METHODS

    }

#endif //UNITY_EDITOR

}
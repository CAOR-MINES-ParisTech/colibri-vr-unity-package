/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace COLIBRIVR.Debugging
{

#if UNITY_EDITOR

    /// <summary>
    /// Editor class for PerViewMeshesQSTRDebug.
    /// </summary>
    [CustomEditor(typeof(PerViewMeshesQSTRDebug))]
    public class PerViewMeshesQSTRDebugEditor : Editor
    {

#region FIELDS

        protected PerViewMeshesQSTRDebug _targetObject;
        protected SerializedProperty _propertyGeometryProcessingMethod;

#endregion //FIELDS
 
#region INHERITANCE_METHODS

        /// <summary>
        /// On selection, sets up the target properties.
        /// </summary>
        public virtual void OnEnable()
        {
            _targetObject = (PerViewMeshesQSTRDebug)serializedObject.targetObject;
            _targetObject.Selected();
            _propertyGeometryProcessingMethod = serializedObject.FindProperty(PerViewMeshesQSTRDebug.propertyNameGeometryProcessingMethod);
        }

        /// <summary>
        /// On deselection, notifies the gameobject.
        /// </summary>
        void OnDisable()
        {
            if(_targetObject != null)
                _targetObject.Deselected();
        }

#endregion //INHERITANCE_METHODS

#region METHODS

        /// <summary>
        /// Enables the user to choose the depth processing method's parameters.
        /// </summary>
        /// <param name="isModelOmnidirectional"></param> True if the camera model is omnidirectional, false otherwise.
        protected void SectionDepthProcessing(bool isModelOmnidirectional)
        {
            // Find the quadtree mesh processing method.
            SerializedObject objectGeometryProcessingMethod = new SerializedObject(_propertyGeometryProcessingMethod.objectReferenceValue);
            objectGeometryProcessingMethod.Update();
            // Display a popup to choose whether to display the generated mesh as a 3D per-view mesh, as a 3D focal surface, or as a 2D image.
            SerializedProperty propertyMeshProjectionType = objectGeometryProcessingMethod.FindProperty(Processing.PerViewMeshesQSTR.propertyNameMeshProjectionType);
            string label = "Projection type:";
            string tooltip;
            List<string> displayedOptions = new List<string> { "Per-view mesh", "Focal surface" };
            if(isModelOmnidirectional)
            {
                tooltip = "Whether to project the created mesh as a 3D per-view mesh, as a 3D focal surface, or as a 2D image.";
                displayedOptions.Add("2D image");
            }
            else
            {
                tooltip = "Whether to project the created mesh as a per-view mesh or as a focal surface.";
            }
            int popupIndex = Mathf.Min(displayedOptions.Count - 1, propertyMeshProjectionType.intValue);
            propertyMeshProjectionType.intValue = EditorGUILayout.Popup(new GUIContent(label, tooltip), popupIndex, displayedOptions.ToArray());
            // Apply the modified properties.
            objectGeometryProcessingMethod.ApplyModifiedProperties();
            // Display the additional editor parameters of the quadtree mesh processing method.
            _targetObject.SectionAdditionalParameters();
        }

        /// <summary>
        /// Enables the user to generate a mesh from the depth information.
        /// </summary>
        protected void SectionGenerateButton()
        {
            // Display a button to enable the user to generate the mesh.
            EditorGUILayout.HelpBox("Note: generated mesh will be destroyed on deselection.", MessageType.Info);
            string label = "Create mesh from Z-buffer";
            string tooltip = "Creates a mesh from the game engine's Z-buffer, using the given depth processing parameters.";
            if(GUILayout.Button(new GUIContent(label, tooltip)))
                _targetObject.CreateMeshFromZBuffer();
            EditorGUILayout.Space();
            // Display the compression information generated during the process.
            _targetObject.DisplayCompressionInfo();
        }

#endregion //METHODS

    }

#endif //UNITY_EDITOR

}

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
    /// Editor class for CameraSetup.
    /// </summary>
    [CustomEditor(typeof(CameraSetup))]
    public class CameraSetupEditor : Editor
    {

#region FIELDS

        private CameraSetup _targetObject;

#endregion //FIELDS

#region INHERITANCE_METHODS

        /// <summary>
        /// On selection, sets up the target properties and notify the object.
        /// </summary>
        void OnEnable()
        {
            // Get the target object.
            _targetObject = (CameraSetup)serializedObject.targetObject;
            // Notify the object that it has been selected.
            _targetObject.Selected();
        }

        /// <summary>
        /// On deselection, notify the object.
        /// </summary>
        void OnDisable()
        {
            _targetObject.Deselected();
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
/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif //UNITY_EDITOR

namespace COLIBRIVR
{

#if UNITY_EDITOR

    /// <summary>
    /// Editor Window Class that displays the images sent by other classes for preview.
    /// </summary>
    [InitializeOnLoad]
    public class PreviewWindow : EditorWindow
    {

#region STATIC_PROPERTIES

        private static PreviewWindow _instance
        {
            get
            {
                if(_storedInstance == null)
                {
                    System.Type dockNextToType = System.Type.GetType("UnityEditor.ConsoleWindow,UnityEditor.dll");
                    _storedInstance = EditorWindow.GetWindow<PreviewWindow>("COLIBRI VR Preview", false, dockNextToType);
                }
                return _storedInstance;
            }
            
        }

#endregion //STATIC_PROPERTIES

#region STATIC_FIELDS

        private static PreviewWindow _storedInstance;

#endregion //STATIC_FIELDS

#region STATIC_METHODS

        /// <summary>
        /// On being called from the Unity menu, initializes the preview window.
        /// </summary>
        [MenuItem("Window/COLIBRI VR/Preview")]
        private static void Init()
        {
            if(_storedInstance != null)
                DestroyImmediate(_storedInstance);
            _instance.WindowInitIfNeeded();
            _instance.Show();
        }

        /// <summary>
        /// Clears the preview window.
        /// </summary>
        private static void Clear()
        {
            _instance.WindowClear();
        }

        /// <summary>
        /// Adds a preview caller to the list, enabling this caller object to provide an image for preview.
        /// </summary>
        /// <param name="caller"></param> The caller object wishing to display an image for preview.
        /// <param name="imageName"></param> The name of the image that the caller wishes to register.
        public static void AddCaller(IPreviewCaller caller, string imageName)
        {
            _instance.WindowAddCaller(caller, imageName);
        }

        /// <summary>
        /// Displays the provided preview image. The image must already be registered, i.e. the corresponding caller must already have been added.
        /// </summary>
        /// <param name="imageName"></param> The image's name, used as an identifier.
        /// <param name="image"></param> The image to display.
        /// <param name="previewMaxIndex"></param> The max preview index, used to determine the upper bound of the corresponding slider.
        public static void DisplayImage(string imageName, Texture image, int previewMaxIndex)
        {
            _instance.WindowDisplayImage(imageName, image, previewMaxIndex);
        }

        /// <summary>
        /// Removes a preview caller from the list.
        /// </summary>
        /// <param name="imageName"></param> The name of the image that had been registered by this caller.
        public static void RemoveCaller(string imageName)
        {
            _instance.WindowRemoveCaller(imageName);
        }

        /// <summary>
        /// Gets the resolution of the preview images in the preview window.
        /// </summary>
        /// <param name="originalResolution"></param> The resolution of the original images.
        /// <returns></returns> The new resolution.
        public static Vector2Int GetPreviewResolution(Vector2Int originalResolution)
        {
            float resolutionMultFactor = Mathf.Min(1, COLIBRIVRSettings.packageSettings.previewMaxResolution * 1f / Mathf.Max(originalResolution.x, originalResolution.y));
            Vector2Int outResolution = new Vector2Int(Mathf.RoundToInt(resolutionMultFactor * originalResolution.x), Mathf.RoundToInt(resolutionMultFactor * originalResolution.y));
            return outResolution;
        }

#endregion //STATIC_METHODS

#region FIELDS

        private List<IPreviewCaller> _registeredCallers;
        private List<string> _registeredCallerNames;
        private List<Texture> _registeredTextures;
        private Rect _menuBar;
        private float _indentValue = 20f;
        private float _menuBarHeight = 20f;
        private float _previewIndexPanelHeight = 30f;
        private Rect _texturePanel;
        private Rect _previewIndexPanel;
        private int _previewMaxIndex;

#endregion //FIELDS

#region INHERITANCE_METHODS

        /// <summary>
        /// On GUI, draws the window.
        /// </summary>
        void OnGUI()
        {
            // Draw a menu bar on the top of the window.
            DrawMenuBar();
            // If there are callers, display the window's content.
            if(_registeredCallers != null && _registeredCallers.Count > 0)
            {
                SectionTexturePanel();
                SectionPreviewIndexPanel();
            }
        }

        /// <summary>
        /// May be useful to track if the window is made visible.
        /// </summary>
        void OnBecameVisible()
        {

        }

        /// <summary>
        /// May be useful to track if the window is made invisible.
        /// </summary>
        void OnBecameInvisible()
        {

        }

#endregion //INHERITANCE_METHODS

#region METHODS

        /// <summary>
        /// Clears the lists used by the preview window.
        /// </summary>
        private void WindowClear()
        {
            if(_registeredCallers != null)
                _registeredCallers.Clear();
            if(_registeredCallerNames != null)
                _registeredCallerNames.Clear();
            if(_registeredTextures != null)
                _registeredTextures.Clear();
        }

        /// <summary>
        /// Initializes the lists, if they have not already been initialized.
        /// </summary>
        private void WindowInitIfNeeded()
        {
            // If any one of the lists is null, reset all of them.
            if(_registeredCallers == null || _registeredCallerNames == null || _registeredTextures == null)
            {
                // Clear any existing list.
                WindowClear();
                // Initialize the lists.
                _registeredCallers = new List<IPreviewCaller>();
                _registeredCallerNames = new List<string>();
                _registeredTextures = new List<Texture>();
            }
        }

        /// <summary>
        /// Adds a caller to the lists.
        /// </summary>
        /// <param name="caller"></param> The caller to be added.
        /// <param name="imageName"></param> The corresponding name, to be used later on as an identifier.
        private void WindowAddCaller(IPreviewCaller caller, string imageName)
        {
            // Initialize the lists if needed.
            WindowInitIfNeeded();
            // If the caller is already in the lists, do not register it again.
            int listIndex = _registeredCallerNames.IndexOf(imageName);
            if(listIndex > -1 && (caller == _registeredCallers[listIndex]))
                return;
            // Register a new caller in the lists.
            _registeredCallers.Add(caller);
            _registeredCallerNames.Add(imageName);
            _registeredTextures.Add(null);
            // Repaint the window.
            Repaint();
        }

        /// <summary>
        /// Displays the given image.
        /// </summary>
        /// <param name="imageName"></param> The image's name, used as an identifier.
        /// <param name="image"></param> The image to be displayed.
        /// <param name="previewMaxIndex"></param> The max preview index.
        private void WindowDisplayImage(string imageName, Texture image, int previewMaxIndex)
        {
            // Initialize the lists if needed.
            WindowInitIfNeeded();
            // Update the max preview index.
            _previewMaxIndex = previewMaxIndex;
            // Add the image to the list of registered images.
            int listIndex = _registeredCallerNames.IndexOf(imageName);
            if(listIndex >= 0 && listIndex < _registeredTextures.Count)
                _registeredTextures[listIndex] = image;
            // Repaint the window.
            Repaint();
        }

        /// <summary>
        /// Removes a caller from the lists.
        /// </summary>
        /// <param name="imageName"></param>
        private void WindowRemoveCaller(string imageName)
        {
            // Initialize the lists if needed.
            WindowInitIfNeeded();
            // Remove the identified caller from the lists.
            int listIndex = _registeredCallerNames.IndexOf(imageName);
            if(listIndex >= 0 && listIndex < _registeredTextures.Count)
            {
                _registeredCallers.RemoveAt(listIndex);
                _registeredCallerNames.RemoveAt(listIndex);
                _registeredTextures.RemoveAt(listIndex);
            }
            // Repaint the window.
            Repaint();
        }

        /// <summary>
        /// Draws a menu bar at the top of the window.
        /// </summary>
        private void DrawMenuBar()
        {
            // Start the menu bar area.
            _menuBar = new Rect(0, 0, position.width, _menuBarHeight);
            GUILayout.BeginArea(_menuBar, EditorStyles.toolbar);
            GUILayout.BeginHorizontal();
            // Add a button to clear the preview window.
            if(GUILayout.Button("Clear", EditorStyles.toolbarButton))
                Clear();
            // Add an empty button on the right.
            GUILayout.FlexibleSpace();
            GUILayout.Button(string.Empty, EditorStyles.toolbarButton);
            // End the menu bar area.
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        /// <summary>
        /// Computes the rectangle area to allocate for the given texture.
        /// </summary>
        /// <param name="displayIndex"></param> The number of images already displayed by the preview window.
        /// <param name="rectDims"></param> The maximum dimensions of the rectangle area.
        /// <param name="textureWidth"></param> The texture's width.
        /// <param name="textureHeight"></param> The texture's height.
        /// <returns></returns>
        private Rect ComputeTextureRect(int displayIndex, Vector2 rectDims, int textureWidth, int textureHeight)
        {
            Vector2 start = Vector2.zero;
            Vector2 length = Vector2.one;
            // Compute the offset corresponding to the number of images already displayed by the window.
            Vector2 offset = Vector2.zero;
            offset.x = displayIndex * rectDims.x;
            // Compute the aspect ratios of the texture and of the maximum area.
            float rectAspect = rectDims.x * 1f / rectDims.y;
            float expectedAspect = textureWidth * 1f / textureHeight;
            // Compute the ratio between the two aspects.
            // Supposing that they have the same height, this ratio is < 1 if the texture is wider than the maximum area, and > 1 otherwise.
            float ratio = rectAspect / expectedAspect;
            // If the texture has a wider ratio than the maximum area, select a horizontal slice of the maximum area that fits the texture's ratio.
            if(ratio < 1)
            {
                length.y = ratio;
                start.y = 0.5f * (1 - length.y);
            }
            // If the maximum area has a wider ratio than the texture, select a vertical slice of the maximum area that fits the texture's ratio.
            else if(ratio > 1)
            {
                length.x = (1f / ratio);
                start.x = 0.5f * (1 - length.x);
            }
            // Return the selected area slice.
            start *= rectDims;
            length *= rectDims;
            return new Rect(start.x + offset.x, start.y + offset.y, length.x, length.y);
        }

        /// <summary>
        /// Lays out the preview textures in a section of the preview window.
        /// </summary>
        private void SectionTexturePanel()
        {
            // Start the texture panel area.
            float textHeight = 20;
            float startX = _indentValue;
            float startY = _menuBarHeight + _indentValue;
            float width = position.width - 2 * _indentValue;
            float height = position.height - _menuBarHeight - _previewIndexPanelHeight - 2 * _indentValue - textHeight;
            _texturePanel = new Rect(startX, startY, width, height + textHeight);
            GUILayout.BeginArea(_texturePanel);
            // Determine the number of non-null textures.
            int displayCount = 0;
            for(int i = 0; i < _registeredTextures.Count; i++)
                if(_registeredTextures[i] != null)
                    displayCount++;
            // Prepare the layout of the textures.
            // For now, textures are simply mapped out horizontally.
            Vector2 rectDims = Vector2.one;
            rectDims.x = width / displayCount;
            rectDims.y = height;
            // Prepare the label style for the images' names.
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.fontStyle = FontStyle.Bold;
            // Display the textures.
            int displayIndex = 0;
            for(int i = 0; i < _registeredTextures.Count; i++)
            {
                Texture tex = _registeredTextures[i];
                if(tex != null)
                {
                    // Start the layout.
                    GUILayout.BeginHorizontal();
                    // Display the texture.
                    Rect textureRect = ComputeTextureRect(displayIndex, rectDims, tex.width, tex.height);
                    EditorGUI.DrawPreviewTexture(textureRect, tex);
                    // Display the image's name as a label.
                    Vector2 labelPosition = textureRect.position + new Vector2((textureRect.width - rectDims.x)/2f, textureRect.height);
                    Vector2 labelSize = new Vector2(rectDims.x, textHeight);
                    EditorGUI.LabelField(new Rect(labelPosition, labelSize), _registeredCallerNames[i], labelStyle);
                    // End the layout.
                    GUILayout.EndHorizontal();
                    displayIndex++;
                }
            }
            // End the texture panel area.
            GUILayout.EndArea();
        }

        /// <summary>
        /// Displays a slider enabling the user to change the preview index.
        /// </summary>
        private void SectionPreviewIndexPanel()
        {
            // Start the index panel area.
            float startX = _indentValue;
            float startY = position.height - _previewIndexPanelHeight;
            float width = position.width - 2 * _indentValue;
            float height = _previewIndexPanelHeight;
            _previewIndexPanel = new Rect(startX, startY, width, height);
            GUILayout.BeginArea(_previewIndexPanel);
            // Check which source camera is currently being used for preview, i.e. determine the preview index.
            int previewIndex = -1;
            int channel = 0;
            while(channel < _registeredCallers.Count)
            {
                IPreviewCaller caller = _registeredCallers[channel];
                if(caller != null)
                {
                    previewIndex = caller.previewIndex;
                    break;
                }
                channel++;
            }
            // If the caller objects indicate that they use a preview index, display a slider to enable the user to modify it.
            if(previewIndex > -1)
            {
                // Display a slider to enable the user to change the preview index.
                string label = "Preview camera index: ";
                string tooltip = "Index of the source camera to be used for preview.";
                int newPreviewIndex = EditorGUILayout.IntSlider(new GUIContent(label, tooltip), previewIndex, 0, _previewMaxIndex);
                // If the preview index was changed, notify the caller objects.
                if(newPreviewIndex != previewIndex)
                {
                    foreach(IPreviewCaller caller in _registeredCallers)
                    {
                        if(caller != null)
                        {
                            // Set the caller object with a new preview index.
                            caller.previewIndex = newPreviewIndex;
                            // Notify objects that the preview index has changed.
                            if(caller.onPreviewIndexChangeEvent != null)
                                caller.onPreviewIndexChangeEvent.Invoke();
                            // Mark the current scene as dirty.
                            if(!Application.isPlaying)
                                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                        }
                    }
                }
            }
            // End the index panel area.
            GUILayout.EndArea();
        }

#endregion //METHODS

    }

#endif //UNITY_EDITOR

}



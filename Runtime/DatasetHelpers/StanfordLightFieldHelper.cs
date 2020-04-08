/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

using UnityEngine;
using System.IO;

namespace COLIBRIVR.DatasetHelpers
{

    /// <summary>
    /// Class that can be used to parse datasets from the Stanford Light Field Archive: http://lightfield.stanford.edu/lfs.html
    /// </summary>
    [ExecuteInEditMode]
    public class StanfordLightFieldHelper : MonoBehaviour
    {

#region CONST_FIELDS

        public const string propertyNameScaleFactor = "scaleFactor";
        public const string propertyNameRepositionAroundCenter = "repositionAroundCenter";

#endregion //CONST_FIELDS

#region PROPERTIES

        public DataHandler dataHandler { get { return _dataHandler; } }
        public CameraSetup cameraSetup { get { return _cameraSetup; } }

#endregion //PROPERTIES

#region FIELDS

        public int colorCount;
        public float scaleFactor;
        public bool repositionAroundCenter;

        [SerializeField] private DataHandler _dataHandler;
        [SerializeField] private CameraSetup _cameraSetup;

#endregion //FIELDS

#region INHERITANCE_METHODS

        /// <summary>
        /// Resets the object's properties.
        /// </summary>
        void Reset()
        {
            // Reset the key child components.
            _cameraSetup = CameraSetup.CreateOrResetCameraSetup(transform);
            _dataHandler = DataHandler.CreateOrResetDataHandler(transform);
            // Reset other properties.
            colorCount = 0;
            scaleFactor = 0.01f;
            repositionAroundCenter = true;
        }

        /// <summary>
        /// On destroy, destroys the created objects.
        /// </summary>
        void OnDestroy()
        {
            if (!GeneralToolkit.IsStartingNewScene())
                GeneralToolkit.RemoveChildComponents(transform, typeof(CameraSetup), typeof(DataHandler));
        }

#endregion //INHERITANCE_METHODS

#region METHODS

#if UNITY_EDITOR

        /// <summary>
        /// Parses the camera setup from a directory containing an "images" folder with an image dataset from the Stanford Light Field Archive, and saves the parsed setup in this directory.
        /// </summary>
        public void ParseCameraSetup()
        {
            // Inform of process start.
            Debug.Log(GeneralToolkit.FormatScriptMessage(this.GetType(), "Started parsing camera setup for an image dataset from the Stanford Light Field Archive located at: " + dataHandler.colorDirectory + "."));
            // Get the files in the "images" folder.
            FileInfo[] fileInfos = GeneralToolkit.GetFilesByExtension(dataHandler.colorDirectory, ".png");
            // Determine the pixel resolution of the images.
            Texture2D tempTex = new Texture2D(1, 1);
            GeneralToolkit.LoadTexture(fileInfos[0].FullName, ref tempTex);
            Vector2Int pixelResolution = new Vector2Int(tempTex.width, tempTex.height);
            DestroyImmediate(tempTex);
            // Prepare repositioning around center if it is selected.
            Vector3 meanPos = Vector3.zero;
            // Reset the camera models to fit the color count.
            _cameraSetup.ResetCameraModels();
            _cameraSetup.cameraModels = new CameraModel[colorCount];
            // Iteratively add each camera model to the setup.
            for(int iter = 0; iter < colorCount; iter++)
            {
                CameraModel cameraModel = _cameraSetup.AddCameraModel(iter);
                // Store the image's pixel resolution in the camera model.
                cameraModel.pixelResolution = pixelResolution;
                // Store the image's name in the camera model.
                FileInfo fileInfo = fileInfos[iter];
                cameraModel.SetCameraReferenceIndexAndImageName(cameraModel.cameraReferenceIndex, fileInfo.Name);
                // Store the image's position in the model.
                string[] split = fileInfo.Name.Split('_');
                float positionY = - GeneralToolkit.ParseFloat(split[split.Length-3]);
                float positionX = GeneralToolkit.ParseFloat(split[split.Length-2]);
                Vector3 pos = scaleFactor * new Vector3(positionX, positionY, 0);
                cameraModel.transform.position = pos;
                meanPos += pos;
            }
            // If it is selected, reposition the camera setup around its center position.
            if(repositionAroundCenter)
            {
                meanPos /= colorCount;
                for(int iter = 0; iter < colorCount; iter++)
                {
                    CameraModel cameraModel = _cameraSetup.cameraModels[iter];
                    cameraModel.transform.position = cameraModel.transform.position - meanPos;
                }
            }
            // Temporarily move the color images to a safe location.
            string tempDirectoryPath = Path.Combine(GeneralToolkit.GetDirectoryBefore(dataHandler.dataDirectory), "temp");
            GeneralToolkit.Move(PathType.Directory, dataHandler.colorDirectory, tempDirectoryPath);
            // Save the camera setup information (this would also have cleared the "images" folder if it was still there).
            Acquisition.Acquisition.SaveAcquisitionInformation(dataHandler, cameraSetup);
            // Move the color images back into their original location.
            GeneralToolkit.Delete(dataHandler.colorDirectory);
            GeneralToolkit.Move(PathType.Directory, tempDirectoryPath, dataHandler.colorDirectory);
            // Update the camera models of the setup object.
            _cameraSetup.FindCameraModels();
            // Inform of end of process.
            Debug.Log(GeneralToolkit.FormatScriptMessage(this.GetType(), "Finished parsing camera setup. Result can be previewed in the Scene view."));
        }

#endif //UNITY_EDITOR

#endregion //METHODS

    }

}

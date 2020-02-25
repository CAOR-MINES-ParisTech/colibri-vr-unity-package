/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org

using System.Collections;
using System.IO;
using UnityEngine;

namespace COLIBRIVR.Processing
{

    /// <summary>
    /// Class that implements the Per-View Meshes from Focal Surfaces geometry processing method.
    /// This method processes a mesh for each source image based on the image's focal surface (plane or sphere).
    /// </summary>
    public class PerViewMeshesFS : ProcessingMethod
    {

#region CONST_FIELDS

        public const string quadFocalSurfaceName = "QuadFocalSurface";
        public const string sphereFocalSurfaceName = "SphereFocalSurface";

#endregion //CONST_FIELDS

#region FIELDS

        public Transform[] meshTransforms;

#endregion //FIELDS

#if UNITY_EDITOR

#region INHERITANCE_METHODS

        /// <inheritdoc/>
        public override bool IsCompatible(int colorDataCount, int depthDataCount, int meshDataCount)
        {
            // Indicate that this method is available only if there is one or more color data samples.
            return (colorDataCount >= 1);
        }

        /// <inheritdoc/>
        public override bool IsGUINested()
        {
            return false;
        }

        /// <inheritdoc/>
        public override GUIContent GetGUIInfo()
        {
            string label = "Per-view meshes from underlying focal surfaces";
            string tooltip = "The focal surface (plane or sphere) of each image will be exported as a mesh asset.";
            if(!GUI.enabled)            
                tooltip = "Prerequisites are not satisfied.\nPrerequisites: color data files (.JPG, .PNG).";
            return new GUIContent(label, tooltip);
        }

        /// <inheritdoc/>
        public override GUIContent GetProcessedDataName()
        {
            string label = "Per-view focal surface meshes";
            string tooltip = GetGUIInfo().text;
            return new GUIContent(label, tooltip);
        }

        /// <inheritdoc/>
        protected override IEnumerator ExecuteMethodCoroutine()
        {
            bool hasStoredQuad = false;
            bool hasStoredInsideOutSphere = false;
            CameraModel[] cameraModels = cameraSetup.cameraModels;
            for(int sourceIndex = 0; sourceIndex < cameraModels.Length; sourceIndex++)
            {
                CameraModel cameraModel = cameraModels[sourceIndex];
                // If the camera model is omnidirectional and the sphere mesh has not yet been bundled, do so.
                if(!hasStoredInsideOutSphere && cameraModel.isOmnidirectional)
                {
                    StoreInsideOutSphereMesh(dataHandler);
                    hasStoredInsideOutSphere = true;
                }
                // If the camera model is not omnidirectional and the quad mesh has not yet been bundled, do so.
                else if(!hasStoredQuad && !cameraModel.isOmnidirectional)
                {
                    StoreQuadMesh(dataHandler);
                    hasStoredQuad = true;
                }
                yield return null;
            }
        }

        /// <inheritdoc/>
        public override void DeactivateIncompatibleProcessingMethods()
        {
            
        }
        
        /// <inheritdoc/>
        public override bool HasAdditionalParameters()
        {
            return false;
        }

        /// <inheritdoc/>
        public override void SectionAdditionalParameters()
        {
            
        }

#endregion //INHERITANCE_METHODS

#endif //UNITY_EDITOR

#region METHODS

#if UNITY_EDITOR

        /// <summary>
        /// Stores a quad mesh in the processed assets.
        /// </summary>
        /// <param name="dataHandler"></param> The data handler with which to store the asset.
        public void StoreQuadMesh(DataHandler dataHandler)
        {
            // Check if the asset has already been processed.
            string bundledAssetName = dataHandler.GetBundledAssetName(this, quadFocalSurfaceName);
            string meshRelativePath = Path.Combine(GeneralToolkit.tempDirectoryRelativePath, bundledAssetName + ".asset");
            if(dataHandler.IsAssetAlreadyProcessed(meshRelativePath))
                return;
            // Get the quad mesh from a quad primitive.
            Transform primitiveTransform = GameObject.CreatePrimitive(PrimitiveType.Quad).transform;
            MeshFilter quadMeshFilter = primitiveTransform.GetComponent<MeshFilter>();
            Mesh quadMesh = (Mesh)Instantiate(quadMeshFilter.sharedMesh);
            // Save the mesh as an asset.
            GeneralToolkit.CreateAndUnloadAsset(quadMesh, meshRelativePath);
            // Destroy the created primitive.
            DestroyImmediate(primitiveTransform.gameObject);
        }

        /// <summary>
        /// Stores an inside-out sphere mesh in the processed assets.
        /// </summary>
        /// <param name="dataHandler"></param> The data handler with which to store the asset.
        public void StoreInsideOutSphereMesh(DataHandler dataHandler)
        {
            // Check if the asset has already been processed.
            string bundledAssetName = dataHandler.GetBundledAssetName(this, sphereFocalSurfaceName);
            string meshRelativePath = Path.Combine(GeneralToolkit.tempDirectoryRelativePath, bundledAssetName + ".asset");
            if(dataHandler.IsAssetAlreadyProcessed(meshRelativePath))
                return;
            // Get the sphere mesh from a sphere primitive.
            Transform primitiveTransform = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
            MeshFilter sphereMeshFilter = primitiveTransform.GetComponent<MeshFilter>();
            Mesh sphereMesh = (Mesh)Instantiate(sphereMeshFilter.sharedMesh);
            // Correct for rotation.
            primitiveTransform.rotation = Quaternion.AngleAxis(-90f, Vector3.up);
            GeneralToolkit.TransformMeshByTransform(ref sphereMesh, primitiveTransform);
            // Make the sphere inside-out.
            int[] triangles = sphereMesh.triangles;
            for(int i = 0; i < triangles.Length; i+=3)
            {
                int t = triangles[i];
                triangles[i] = triangles[i+2];
                triangles[i+2] = t;
            }
            Vector3[] normals = sphereMesh.normals;
            for(int i = 0; i < normals.Length; i++)
            {
                normals[i] = -normals[i];
            }
            Vector2[] UVs = sphereMesh.uv;
            for(int i = 0; i < UVs.Length; i++)
            {
                UVs[i] = new Vector2(1f - UVs[i].x, UVs[i].y);
            }
            sphereMesh.triangles = triangles;
            sphereMesh.normals = normals;
            sphereMesh.uv = UVs;
            // Save the mesh as an asset.
            GeneralToolkit.CreateAndUnloadAsset(sphereMesh, meshRelativePath);
            // Destroy the created primitive.
            DestroyImmediate(primitiveTransform.gameObject);
        }

#endif //UNITY_EDITOR

        /// <summary>
        /// Loads the processed focal surface meshes for play.
        /// </summary>
        /// <returns></returns>
        public IEnumerator LoadProcessedFocalSurfacesCoroutine()
        {
            CameraModel[] cameraModels = cameraSetup.cameraModels;
            meshTransforms = new Transform[cameraModels.Length];
            // Check that the application is playing.
            if(!Application.isPlaying)
                yield break;
            // Only continue if there are source images.
            if(cameraModels.Length > 0)
            {
                // Create a gameobject for the geometric data, and set it as a child of this transform.
                Transform geometricDataTransform = new GameObject("Geometric data").transform;
                geometricDataTransform.parent = dataHandler.transform;
                // For each source image, check whether to load the quad or the sphere focal surface.
                Mesh quadFocalSurface = null;
                Mesh sphereFocalSurface = null;
                string bundledAssetsPrefix = DataHandler.GetBundledAssetPrefixFromType(typeof(PerViewMeshesFS));
                for(int sourceCamIndex = 0; sourceCamIndex < cameraModels.Length; sourceCamIndex++)
                {
                    // Create a gameobject for the mesh, and set it as a child of the geometric data transform.
                    Transform meshTransform = new GameObject(bundledAssetsPrefix + "FocalSurface" + sourceCamIndex).transform;
                    meshTransform.parent = geometricDataTransform;
                    // Determine the mesh's transform values from the camera model.
                    CameraModel cameraModel = cameraModels[sourceCamIndex];
                    Vector3 meshPosition = cameraModel.transform.position;
                    Quaternion meshRotation = cameraModel.transform.rotation;
                    Vector3 meshScale = Vector3.one;
                    // If the camera model is omnidirectional, load the sphere focal surface mesh.
                    if(cameraModel.isOmnidirectional)
                    {
                        if(sphereFocalSurface == null)
                            yield return dataHandler.StartCoroutine(dataHandler.LoadAssetsFromBundleCoroutine<Mesh>((result => sphereFocalSurface = result[0]), bundledAssetsPrefix + sphereFocalSurfaceName));
                        meshTransform.gameObject.AddComponent<MeshFilter>().sharedMesh = sphereFocalSurface;
                    }
                    // If the camera model is perspective, load the quad focal surface mesh.
                    else
                    {
                        if(quadFocalSurface == null)
                            yield return dataHandler.StartCoroutine(dataHandler.LoadAssetsFromBundleCoroutine<Mesh>((result => quadFocalSurface = result[0]), bundledAssetsPrefix + quadFocalSurfaceName));
                        meshTransform.gameObject.AddComponent<MeshFilter>().sharedMesh = quadFocalSurface;
                        // Scale the quad based on the camera model's aspect ratio.
                        meshScale = new Vector3(1, cameraModel.pixelResolution.y * 1f / cameraModel.pixelResolution.x, 1);
                    }
                    // Set the mesh's transform to the computed values.
                    GeneralToolkit.SetTransformValues(meshTransform, false, meshPosition, meshRotation, meshScale);
                    // Assign to the output array.
                    meshTransforms[sourceCamIndex] = meshTransform;
                }
                // Reset the geometric data's local position, rotation, and scale, to fit that of the parent object.
                GeneralToolkit.SetTransformValues(geometricDataTransform.transform, true, Vector3.zero, Quaternion.identity, Vector3.one);
            }
        }

#endregion //METHODS

    }

}

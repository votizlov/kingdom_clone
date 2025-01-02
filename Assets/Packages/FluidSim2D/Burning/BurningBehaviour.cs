using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BurningBehaviour : MonoBehaviour
{
    public RenderTexture renderTexture;
    public MeshRenderer meshRenderer;
    public MeshFilter meshFilter;
    public Camera orthographicCamera;
    public bool isBurned = false;

    private void OnValidate()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        meshFilter = GetComponent<MeshFilter>();
    }
    public void DuplicateMeshToChild()
    {
        // Create new child object
        GameObject childObj = new GameObject("DuplicatedMesh");
        childObj.transform.SetParent(transform);
        childObj.transform.localPosition = Vector3.zero;
        childObj.transform.localRotation = Quaternion.identity;
        childObj.transform.localScale = Vector3.one;

        // Add and setup MeshFilter
        MeshFilter newMeshFilter = childObj.AddComponent<MeshFilter>();
        newMeshFilter.sharedMesh = meshFilter.sharedMesh;

        // Add and setup MeshRenderer
        MeshRenderer newMeshRenderer = childObj.AddComponent<MeshRenderer>();
        newMeshRenderer.sharedMaterial = meshRenderer.sharedMaterial;
    }

    void CreateOrthographicCamera()
    {
        // Check if the camera already exists
        if (orthographicCamera == null)
        {
            // Create a new GameObject for the camera
            GameObject cameraObj = new GameObject("BurningObjectCamera");
            cameraObj.transform.SetParent(transform);
            cameraObj.transform.localPosition = Vector3.zero;
            cameraObj.transform.localRotation = Quaternion.identity;

            // Add a Camera component
            orthographicCamera = cameraObj.AddComponent<Camera>();
            //orthographicCamera.enabled = false; // Disabled by default

            // Set camera properties
            orthographicCamera.orthographic = true;
            orthographicCamera.clearFlags = CameraClearFlags.Nothing;
            orthographicCamera.backgroundColor = Color.clear;
            orthographicCamera.cullingMask = LayerMask.GetMask("BurningMask"); // Adjust the culling mask as needed
            orthographicCamera.targetTexture = renderTexture;
            Debug.Log($"set tgt {renderTexture}");
            orthographicCamera.nearClipPlane = -10f;
            orthographicCamera.farClipPlane = 10f;
        }

        // Calculate the orthographic size and position based on the mesh's bounding box
        Bounds bounds = meshFilter.sharedMesh.bounds;

        // Only consider the XY plane (side scroller)
        float sizeX = bounds.size.x * transform.localScale.x;
        float sizeY = bounds.size.y * transform.localScale.y;

        // Set the orthographic size (half of the height)
        orthographicCamera.orthographicSize = sizeY / 2f;

        // Position the camera so it looks at the center of the mesh
        Vector3 localCenter = bounds.center;
        Vector3 worldCenter = transform.TransformPoint(localCenter);

        orthographicCamera.transform.position = new Vector3(
            worldCenter.x,
            worldCenter.y,
            transform.position.z - 1f // Adjust z position as needed
        );

        orthographicCamera.transform.rotation = Quaternion.Euler(0f, 0f, 0f); // Looking along the Z-axis
    }
    
    Texture2D toTexture2D(RenderTexture rTex)
    {
        Texture2D tex = new Texture2D(rTex.width, rTex.height, TextureFormat.RGB24, false);
        // ReadPixels looks at the active RenderTexture.
        RenderTexture.active = rTex;
        tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
        tex.filterMode = FilterMode.Point;
        tex.Apply();
        return tex;
    }
}

using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

public class ProjectionUVUpdater : MonoBehaviour
{
    public Camera mainCamera;
    public float texMultiplier = 100;
    [SerializeField] private ComputeShader colorReductionShader;
    [SerializeField] private float reductionSpeed = 0.05f;
    [SerializeField] private ComputeShader burnCounterShader;
    [SerializeField] private float burnThreshold = 0.5f;
    private int kernelID;
    private uint[] countData = new uint[1];
    private ComputeBuffer countBuffer;
    private List<BurningBehaviour> burningObjects;
    private int initOffset = 5;

    void Start()
    {
        kernelID = burnCounterShader.FindKernel("CountBurned");
        countBuffer = new ComputeBuffer(1, sizeof(uint), ComputeBufferType.Counter);

        burningObjects = new List<BurningBehaviour>(FindObjectsOfType<BurningBehaviour>());

        foreach (var burning in burningObjects)
        {
            Material matInstance = new Material(burning.meshRenderer.sharedMaterial);
            burning.meshRenderer.material = matInstance;
            var tex = matInstance.GetTexture("_MainTex");
            Bounds bounds = burning.meshFilter.sharedMesh.bounds;
            burning.renderTexture = new RenderTexture(Mathf.RoundToInt(bounds.size.x*burning.transform.localScale.x*texMultiplier),
                Mathf.RoundToInt(bounds.size.y*burning.transform.localScale.x*texMultiplier), 0,GraphicsFormat.R8_UNorm);
            //burning.renderTexture.enableRandomWrite = true;
            burning.renderTexture.Create();
            Debug.Log($"created texture with dimensions: {burning.renderTexture.width} / {burning.renderTexture.height}");
            matInstance.SetTexture("_FireTex",burning.renderTexture);
            
            CreateOrthographicCamera(burning);
            DuplicateMeshToChild(burning);
        }
    }
    
    void Update()
    {
        if(initOffset > 0)
        {
            initOffset--;
            return;
        }
        foreach (var burning in burningObjects)
        {
            if (burning.isBurned) continue;
            var test = burning.GetComponent<BurnCounterTester>();
            if (test == null)
                return;

            var t = test.GetBurnedCount();///CountBurned(burning.renderTexture);
            if (t > 20000)
            {
                burning.MarkAsBurned();
                Debug.Log($"burned! {burning.name} with pixel count {t}");
            }
            //colorReductionShader.SetTexture(kernelID, "_SourceTexture", burning.renderTexture);
            //colorReductionShader.SetFloat("_ReductionSpeed", reductionSpeed);
            //colorReductionShader.Dispatch(kernelID, 64, 64, 1);
        }

    }
    
    private void OnDestroy()
    {
        if(countBuffer != null)
            countBuffer.Release();
    }
    
    public void DuplicateMeshToChild(BurningBehaviour burning)
    {
        // Create new child object
        GameObject childObj = new GameObject("DuplicatedMesh");
        childObj.transform.SetParent(burning.transform);
        childObj.transform.localPosition = Vector3.zero;
        childObj.transform.localRotation = Quaternion.identity;
        childObj.transform.localScale = Vector3.one;
        childObj.layer = LayerMask.NameToLayer("2DFluid");

        // Add and setup MeshFilter
        MeshFilter newMeshFilter = childObj.AddComponent<MeshFilter>();
        newMeshFilter.sharedMesh = burning.meshFilter.sharedMesh;

        // Add and setup MeshRenderer
        MeshRenderer newMeshRenderer = childObj.AddComponent<MeshRenderer>();
        newMeshRenderer.material = burning.meshRenderer.material;
        newMeshRenderer.material.shader = Shader.Find("Custom/BurningEmitter");
    }
    
    void CreateOrthographicCamera(BurningBehaviour burning)
    {
        // Check if the camera already exists
        if (burning.orthographicCamera == null)
        {
            // Create a new GameObject for the camera
            GameObject cameraObj = new GameObject("BurningObjectCamera");
            cameraObj.transform.SetParent(burning.transform);
            cameraObj.transform.localPosition = Vector3.zero;
            cameraObj.transform.localRotation = Quaternion.identity;

            // Add a Camera component
            burning.orthographicCamera = cameraObj.AddComponent<Camera>();
            //orthographicCamera.enabled = false; // Disabled by default

            // Set camera properties
            burning.orthographicCamera.orthographic = true;
            burning.orthographicCamera.clearFlags = CameraClearFlags.Nothing;
            //burning.orthographicCamera.backgroundColor = Color.clear;
            burning.orthographicCamera.cullingMask = LayerMask.GetMask("BurningMask"); // Adjust the culling mask as needed
            burning.orthographicCamera.targetTexture = burning.renderTexture;
            Debug.Log($"set tgt {burning.renderTexture}");
            burning.orthographicCamera.nearClipPlane = -10f;
            burning.orthographicCamera.farClipPlane = 10f;
        }

        // Calculate the orthographic size and position based on the mesh's bounding box
        Bounds bounds = burning.meshFilter.sharedMesh.bounds;

        // Only consider the XY plane (side scroller)
        float sizeX = bounds.size.x * burning.transform.localScale.x;
        float sizeY = bounds.size.y * burning.transform.localScale.y;

        // Set the orthographic size (half of the height)
        burning.orthographicCamera.orthographicSize = sizeY / 2f;

        // Position the camera so it looks at the center of the mesh
        Vector3 localCenter = bounds.center;
        Vector3 worldCenter = burning.transform.TransformPoint(localCenter);

        burning.orthographicCamera.transform.position = new Vector3(
            worldCenter.x,
            worldCenter.y,
            transform.position.z + 5f // Adjust z position as needed
        );

        burning.orthographicCamera.transform.rotation = Quaternion.Euler(0f, 0f, 0f); // Looking along the Z-axis
    }
    public static int CountPixelsBelowThreshold(RenderTexture rt, float threshold)
    {
        // Backup the current RenderTexture
        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = rt;

        // Create a temporary Texture2D to read from the RenderTexture
        Texture2D tempTex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
        tempTex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tempTex.Apply();

        // Restore previous RenderTexture
        RenderTexture.active = currentRT;

        int count = 0;
        Color[] pixels = tempTex.GetPixels();
        for(int i = 0; i < pixels.Length; i++)
        {
            if (pixels[i].r > threshold)
                count++;
        }

        return count;
    }

    /// <summary>
    /// Returns the number of pixels considered "burned."
    /// </summary>
    public uint CountBurned(RenderTexture rt)
    {/*
        EnsureBuffer();
        countBuffer.SetCounterValue(0);

        burnCounterShader.SetTexture(kernelID, "_Source", rt);
        burnCounterShader.SetBuffer(kernelID, "_BurnedCount", countBuffer);
        burnCounterShader.SetFloat("_BurnThreshold", burnThreshold);
        burnCounterShader.SetInts("_SourceSize", rt.width, rt.height);

        int dispatchX = Mathf.Max(1, Mathf.CeilToInt(rt.width / 8.0f));
        int dispatchY = Mathf.Max(1, Mathf.CeilToInt(rt.height / 8.0f));
        burnCounterShader.Dispatch(kernelID, dispatchX, dispatchY, 1);

        countBuffer.GetData(countData);*/

        // Reset the buffer counter
        countBuffer.SetCounterValue(0);

        // Set textures & buffers
        burnCounterShader.SetTexture(kernelID, "_Source", rt);
        burnCounterShader.SetBuffer(kernelID, "_BurnedCount", countBuffer);
        burnCounterShader.SetFloat("_BurnThreshold", burnThreshold);
        burnCounterShader.SetInts("_SourceSize", rt.width, rt.height);

        // Dispatch threads over the entire texture
        int threadGroupX = Mathf.Max(1, Mathf.CeilToInt(rt.width / 8.0f));
        int threadGroupY = Mathf.Max(1, Mathf.CeilToInt(rt.height / 8.0f));
        burnCounterShader.Dispatch(kernelID, threadGroupX, threadGroupY, 1);

        // Get the counter value from the GPU (we're only reading back 1 integer)
        countBuffer.GetData(countData);

        // countData[0] now holds the number of burned pixels
        return countData[0];
    }
    private void EnsureBuffer()
    {
        if (countBuffer == null)
        {
            countBuffer = new ComputeBuffer(1, sizeof(uint), ComputeBufferType.Counter);
        }
    }

    private void ReleaseBuffer()
    {
        if (countBuffer != null)
        {
            countBuffer.Release();
            countBuffer = null;
        }
    }
    void UpdateOld()
    {
        if (mainCamera == null)
            return;

        // Get the camera's projection and view matrices
        Matrix4x4 projectionMatrix = mainCamera.projectionMatrix;
        Matrix4x4 viewMatrix = mainCamera.worldToCameraMatrix;
        Matrix4x4 viewProjectionMatrix = projectionMatrix * viewMatrix;

        foreach (BurningBehaviour obj in burningObjects)
        {
            MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
            Mesh mesh = null;

            if (meshFilter != null)
            {
                // Ensure we are working with an instance of the mesh
                mesh = meshFilter.mesh;
            }
            else
            {
                SkinnedMeshRenderer skinnedMeshRenderer = obj.GetComponent<SkinnedMeshRenderer>();
                if (skinnedMeshRenderer != null)
                {
                    // For skinned meshes, use the shared mesh
                    mesh = skinnedMeshRenderer.sharedMesh;
                }
            }

            if (mesh == null)
                continue;

            Vector3[] vertices = mesh.vertices;
            Vector2[] uv2 = new Vector2[vertices.Length];

            // Get the object's world transformation matrix
            Matrix4x4 worldMatrix = obj.transform.localToWorldMatrix;

            for (int i = 0; i < vertices.Length; i++)
            {
                // Transform vertex to world space
                Vector3 worldPos = worldMatrix.MultiplyPoint(vertices[i]);

                // Transform to view projection space
                Vector4 viewPos = viewProjectionMatrix * new Vector4(worldPos.x, worldPos.y, worldPos.z, 1.0f);

                // Perform perspective divide
                float w = viewPos.w != 0 ? viewPos.w : 0.0001f; // Prevent division by zero
                float x = viewPos.x / w * 0.5f + 0.5f;
                float y = viewPos.y / w * 0.5f + 0.5f;

                // Flip the Y coordinate if necessary (depends on your texture coordinate system)
                // y = 1.0f - y;

                uv2[i] = new Vector2(x, y);
            }

            // Assign the calculated UVs to uv2
            mesh.uv2 = uv2;
        }
    }
}

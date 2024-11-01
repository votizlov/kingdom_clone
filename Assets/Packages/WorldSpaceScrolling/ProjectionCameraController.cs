using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class ProjectionCameraController : MonoBehaviour
{
    [SerializeField] private Camera targetCam;
    [SerializeField] private bool SingleTexMode = true;
    [SerializeField] private ChunkMode chunkMode = ChunkMode.OFF;
    [SerializeField] private ComputeShader offsetTexShader;
    public RenderTexture[] renderTextures;
    [SerializeField] private Vector3[] cameraPositions;
    [SerializeField] private Transform playerPosition;
    [SerializeField] private float chunkWidth = 14;
    [SerializeField] private int chunksXCount;
    [SerializeField] private int chunksYCount;

    [Header("ScrollUVSetup")] 
    [SerializeField] float moveThreshold = 8;

    [SerializeField] private Material mat;
    [SerializeField] private float worldToUVMultiplier = 0.25f;
    private int lastActiveChunk = 0;
    private int currentActiveChunk;
    private Vector3 lastPlayerPosition;
    private Vector3 baseCameraPositon;
    [SerializeField] private Vector2 cachedOffset;

    enum ChunkMode
    {
        OFF,
        GRID,
        PREDICTIVE
    }
    void Start()
    {
        //CreateChunks();
        cachedOffset = Vector2.zero;
        lastPlayerPosition = playerPosition.position;
        baseCameraPositon = targetCam.transform.position;
    }


    void Update()
    {
        if (chunkMode == ChunkMode.OFF)
        {
            var t = playerPosition.position;
            targetCam.transform.position = new Vector3(t.x, targetCam.transform.position.y, t.z);
            Vector3 offset = lastPlayerPosition - t;
            //var clampedOffset = new Vector2(Mathf.Clamp((int) (offset.z * 256 * worldToUVMultiplier), -255, 255),
             //   Mathf.Clamp((int) (offset.x * 256 * worldToUVMultiplier), -255, 255));
            //Debug.Log(clampedOffset);
            //ClearRT();
            OffsetProjection(new Vector2(offset.z,offset.x)*worldToUVMultiplier);//cachedOffset+=
            /*
            Vector2 UV1 = GetUvFromPos(playerPosition.position);
            Vector2 UV2 = GetUvFromPos(lastPlayerPosition);
            Vector2 pos1 = new Vector2(playerPosition.position.x, playerPosition.position.z);
            Vector2 pos2 = new Vector2(lastPlayerPosition.x, lastPlayerPosition.z);
            Vector2 diffUV = UV1 - UV2;
            Vector2 diffPos = pos1 - pos2;
            Vector2 scaleDiff = diffPos / diffUV;
        
            Debug.Log(UV1);
            Debug.Log(UV2);
            Debug.Log("DIFFERENCE scale: " + scaleDiff);*/
            lastPlayerPosition = t;
            return;
        }

        if (chunkMode == ChunkMode.PREDICTIVE)
        {
            
        }
        currentActiveChunk = GetChunkIdFromPosition(playerPosition.position);
        if (currentActiveChunk != lastActiveChunk)
        {
            ChangeCameraPosAndRt(currentActiveChunk);
            Vector3 offset = lastPlayerPosition - playerPosition.position;

            lastActiveChunk = currentActiveChunk;
            lastPlayerPosition = playerPosition.position;
        }

        
    }

    private Vector2 GetUvFromPos(Vector3 pos)
    {
        Vector2 result = new Vector2();
        
        RaycastHit hit;
        Debug.DrawRay(pos, Vector3.down,Color.red);
        int layerMask = 1 << 7;
        if (Physics.Raycast(pos,Vector3.down,out hit,1,layerMask))
        {
            Renderer rend = hit.transform.GetComponent<Renderer>();
            MeshCollider meshCollider = hit.collider as MeshCollider;

            if (rend != null && rend.sharedMaterial != null && rend.sharedMaterial.mainTexture != null && meshCollider != null)
            {
                Texture2D tex = rend.material.mainTexture as Texture2D;
                Vector2 pixelUV = hit.textureCoord;
                
                //pixelUV.x *= tex.width;
                //pixelUV.y *= tex.height;
                result = pixelUV;
                // You can now use pixelUV to do something with the texture at the point of collision
            }
        }

        return result;
    }

    private int GetChunkIdFromPosition(Vector3 pos)
    {
        float minDist = 1000;
        int minChunkId = 0;
        for (int i = 0; i < cameraPositions.Length; i++)
        {
            var distance = Vector3.Distance(pos, cameraPositions[i]);
            if (distance < minDist)
            {
                minDist = distance;
                minChunkId = i;
            }
        }

        return minChunkId;
    }

    [ContextMenu("CreateChunks")]
    private void CreateChunks()
    {
        cameraPositions = new Vector3[chunksXCount * chunksYCount];
        //renderTextures = new RenderTexture[chunksXCount * chunksYCount];

        for (int y = 0; y < chunksYCount; y++)
        {
            for (int x = 0; x < chunksXCount; x++)
            {
                // Create a new GameObject for each camera position
                cameraPositions[y * chunksXCount + x] = new Vector3(x * chunkWidth +transform.position.x, baseCameraPositon.y, y * chunkWidth + transform.position.z);

                // Create a new RenderTexture for each position
                //renderTextures[y * chunksXCount + x] = new RenderTexture(1024, 1024, 24);
            }
        }
    }

    private void ChangeCameraPosAndRt(int chunkId)
    {
        if (chunkId >= 0 && chunkId < cameraPositions.Length)
        {
            var camPos = cameraPositions[chunkId];
            camPos.y = baseCameraPositon.y;
            targetCam.transform.position =camPos;
            //targetCam.targetTexture = renderTextures[chunkId];
        }
        else
        {
            Debug.LogError("Invalid chunk ID");
        }
    }

    private bool flip = false;
    private void OffsetProjection(Vector2 offset)
    {
        if (offsetTexShader != null)
        {
            int kernelHandle = 1;
            //offsetTexShader.SetVector("_offset", new Vector4(offset.x, offset.y, 0, 0));
            //offsetTexShader.SetTexture(kernelHandle, "_BeforeOffsetTexture", renderTextures[0]);
            //offsetTexShader.SetTexture(kernelHandle, "_Result2", renderTextures[1]);
            if (false)
            {
                
                offsetTexShader.SetTexture(1,"_BeforeOffsetTexture",renderTextures[1]);
                offsetTexShader.SetTexture(1,"_Result2",renderTextures[0]);
            }else{
                offsetTexShader.SetTexture(1,"_BeforeOffsetTexture",renderTextures[0]);
                offsetTexShader.SetTexture(1,"_Result2",renderTextures[1]);}

            flip = !flip;
            
            offsetTexShader.SetVector("_offset",new Vector4( offset.x,offset.y,0,0));
            offsetTexShader.Dispatch(kernelHandle, renderTextures[0].width / 8, renderTextures[0].height / 8, 1);
        }
    }
    #if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        var pos = transform.position;
        var size = new Vector3(chunkWidth, 200, chunkWidth);
            //Gizmos.DrawWireCube(pos, size);
			
        if (Selection.activeGameObject != gameObject)
            return;
			
        if (cameraPositions == null || cameraPositions.Length == 0)
            return;
			
        for (int i = 0; i < cameraPositions.Length; i++)
        {
            var xy = cameraPositions[i];
            var sizeX = chunkWidth;
            var sizeY =  chunkWidth;
            var center = xy;//new Vector3(xy.x * sizeX + sizeX / 2, 0, xy.y * sizeY + sizeY / 2);
            var boxSize = new Vector3(sizeX, sizeY, sizeY);
            if (i == lastActiveChunk)
            {
                Gizmos.color = Color.green;
            }
            else
            {
                Gizmos.color = Color.red;
            }
				
            //Gizmos.color = i == _currentChunkId ? _currentChunkGizmosColor : _chunkGizmosColor;
            Gizmos.DrawWireCube(center, boxSize);
        }
    }
    #endif
}

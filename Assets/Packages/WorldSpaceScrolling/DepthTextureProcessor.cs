using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.VFX;

public class DepthTextureProcessor : MonoBehaviour
{
    public Transform target;
    public float SwapRadius = 7;
    public float ScaleFix = 0.1f;
    public bool IsUseContinuousScroll = true;
    public ComputeShader computeShader;
    public RenderTexture inputTexture;
    public RenderTexture resultTexture;
    public Material targetMat;
    public CustomProjector customProjector;
    public ProjectionCameraController projectionCameraController;
    public VisualEffect vfx;
    private bool flip = false;
    public RenderTexture[] depthTextures;
    private Vector3 lastPos = Vector3.zero;
    private Vector3 lastCamPos = Vector3.zero;
    private Camera depthCam;
    [SerializeField]private Camera depthUpCam;
    public Vector3 offsetAccumulation;
    private float initHeightOffset;
    public static DepthTextureProcessor Instance;
    public int pixelsInUnit;
    [Header("debug")]
    public Texture2D test;

    public RawImage debugImage1;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        depthCam = GetComponent<Camera>();
        initHeightOffset = target.position.y - transform.position.y;
        offsetAccumulation = Vector3.zero;
        lastPos = transform.position;
        var depth1 = new RenderTexture(resultTexture.descriptor);
        var depth2 = new RenderTexture(resultTexture.descriptor);

        depth1.wrapMode = TextureWrapMode.Clamp;
        depth2.wrapMode = TextureWrapMode.Clamp;
        depth1.anisoLevel = 0;
        depth2.anisoLevel = 0;

        depth1.enableRandomWrite = true;
        depth2.enableRandomWrite = true;

        depth1.Create();
        depth2.Create();
        depthTextures = new[] { depth1, depth2 };
        if (vfx != null)
        {
            vfx.SetTexture("vectorField",depth1);
        }

        if (IsUseContinuousScroll)
        {
            targetMat.SetTexture("_ProjectionTexture", depthTextures[0]);
            Shader.SetGlobalTexture("_ProjectionTexture",depthTextures[0]);
            targetMat.SetTexture("_HeightMap", depthTextures[0]);
            debugImage1.texture = depthTextures[0];

            computeShader.SetTexture(0, "_InputTexture", depthCam.targetTexture);
            computeShader.SetTexture(0, "_InputTexture_2", depthUpCam.targetTexture);
            computeShader.SetTexture(0, "_Result", depthTextures[0]);
            computeShader.SetTexture(2, "_InputTexture3", depthTextures[0]);
            computeShader.SetTexture(2, "_Result3", depthTextures[1]);
            computeShader.SetTexture(3, "_Result4", depthTextures[0]);
            computeShader.SetTexture(3, "_InputTexture4", depthTextures[1]);
        }
        else
        {
            targetMat.SetTexture("_ProjectionTexture", depthTextures[0]);
            targetMat.SetTexture("_HeightMap", depthTextures[0]);
            targetMat.SetTexture("_ProjectionTexture2", depthTextures[1]);
            targetMat.SetTexture("_HeightMap2", depthTextures[1]);
            computeShader.SetTexture(0, "_InputTexture", depthCam.targetTexture);
            computeShader.SetTexture(0, "_Result", depthTextures[0]);
        }
    }

    private int hasBeenFlipped = 2;

    void Update()
    {
        var t = target.position;
        if (IsUseContinuousScroll)
        {
            if (Vector3.Distance(t, lastPos) > SwapRadius)
            {
                transform.position = new Vector3(Mathf.Round(t.x), t.y - initHeightOffset, Mathf.Round(t.z));

                offsetAccumulation = transform.position - lastCamPos;
                lastPos = t;
                lastCamPos = transform.position;

                computeShader.Dispatch(0, 16, 16, 1);
                Shift(new Vector4(offsetAccumulation.z, offsetAccumulation.x, 0, 0));
                computeShader.Dispatch(3, 16, 16, 1);
                flip = !flip;
            }
            else
            {
                //computeShader.Dispatch(3, 16, 16, 1);
                computeShader.Dispatch(0, 16, 16, 1);
            }
        }
    }

    private void LateUpdate()
    {
        if (IsUseContinuousScroll) return;
        if (hasBeenFlipped != 2)
        {
            ClearRT(hasBeenFlipped);
            hasBeenFlipped = 2;
        }
    }

    [ContextMenu("TestClear")]
    public void TestClearRt()
    {
        ClearRT(0);
        ClearRT(1);
    }

    [ContextMenu("TestShift")]
    public void TestShiftrRt()
    {
        
        computeShader.SetTexture(2, "_InputTexture3", test);
        computeShader.SetTexture(2, "_Result3", depthTextures[1]);
        Shift(new Vector3(offsetAccumulation.x, offsetAccumulation.y, 0));
        //computeShader.Dispatch(3, 16, 16, 1);
    }

    void ClearRT(int index)
    {
        //Graphics.SetRenderTarget(depthTextures[index]);
        //GL.Clear(false, true, Color.clear);
        computeShader.SetTexture(1, "_Result2", depthTextures[index]);
        computeShader.Dispatch(1, inputTexture.width / 16, inputTexture.height / 16, 1);
    }

    void Shift(Vector4 offset)
    {/*
        if (Math.Abs(offset.x) < 0.001f)
        {
            offset.x = 0;
        }
        else
        {
            offset.x = offset.x / depthCam.orthographicSize / 2;
        }

        if (Math.Abs(offset.y) < 0.001f)
        {
            offset.y = 0;
        }
        else
        {
            offset.y = offset.y / depthCam.orthographicSize / 2;
        }*/

        //offsetAccumulation = offset;
        //if(Math.Abs(offset.y) < 0.001f ||Math.Abs(offset.x) < 0.001f)
        //    return;
        computeShader.SetVector("_offset", offset);
        computeShader.SetFloat("_offsetFix", ScaleFix);
        var a = WorldToPixelCoords(offset);
        computeShader.SetInt("_pixelOffsetX",  a[0]);
        computeShader.SetInt("_pixelOffsetY", a[1]);
        computeShader.Dispatch(2, 16, 16, 1);
    }

    int[] WorldToPixelCoords(Vector3 i)
    {
        return new[] {Mathf.RoundToInt(pixelsInUnit*i.x), Mathf.RoundToInt(pixelsInUnit*i.y)};
    }
}
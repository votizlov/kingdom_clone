using UnityEngine;
using UnityEngine.Rendering;

public class BurnCounterTester : MonoBehaviour
{
    [SerializeField] private ComputeShader burnCounterShader;
    [SerializeField] private RenderTexture targetTexture;
    [SerializeField] private float burnThreshold = 0.5f;

    private ComputeBuffer countBuffer;
    private uint[] countData = new uint[1];
    private static readonly uint[] Zero = { 0u };
    private int kernelID;
    private AsyncGPUReadbackRequest burnCountReadbackRequest;
    private int lastBurnedCount = -1;

    private void Awake()
    {
        if (burnCounterShader != null)
        {
            kernelID = burnCounterShader.FindKernel("CountBurned");
        }
    }

    private void OnDestroy()
    {
        ReleaseBuffer();
    }

    [ContextMenu("Log Burned Pixel Count")]
    private void LogBurnedPixelCount()
    {
        var comp = GetComponent<BurningBehaviour>();
        targetTexture = comp?.renderTexture;
        if (!ValidateInputs())
        {
            return;
        }

        EnsureBuffer();
        countBuffer.SetData(Zero);

        burnCounterShader.SetTexture(kernelID, "_Source", targetTexture);
        burnCounterShader.SetBuffer(kernelID, "_BurnedCount", countBuffer);
        burnCounterShader.SetFloat("_BurnThreshold", burnThreshold);
        burnCounterShader.SetInts("_SourceSize", targetTexture.width, targetTexture.height);

        int dispatchX = Mathf.Max(1, Mathf.CeilToInt(targetTexture.width / 8.0f));
        int dispatchY = Mathf.Max(1, Mathf.CeilToInt(targetTexture.height / 8.0f));
        burnCounterShader.Dispatch(kernelID, dispatchX, dispatchY, 1);

        countBuffer.GetData(countData);
    }

    public int GetBurnedCount()
    {
        var comp = GetComponent<BurningBehaviour>();
        targetTexture = comp?.renderTexture;
        if (!ValidateInputs())
        {
            return lastBurnedCount;
        }

        if (burnCountReadbackRequest.valid && burnCountReadbackRequest.done)
        {
            if (burnCountReadbackRequest.hasError)
            {
                Debug.LogWarning("AsyncGPUReadback for burn count failed.");
            }
            else
            {
                var data = burnCountReadbackRequest.GetData<uint>();
                if (data.Length > 0)
                {
                    lastBurnedCount = (int)data[0];
                }
            }

            burnCountReadbackRequest = default;
        }

        if (burnCountReadbackRequest.valid && !burnCountReadbackRequest.done)
        {
            return lastBurnedCount;
        }

        EnsureBuffer();
        countBuffer.SetData(Zero);

        burnCounterShader.SetTexture(kernelID, "_Source", targetTexture);
        burnCounterShader.SetBuffer(kernelID, "_BurnedCount", countBuffer);
        burnCounterShader.SetFloat("_BurnThreshold", burnThreshold);
        burnCounterShader.SetInts("_SourceSize", targetTexture.width, targetTexture.height);

        int dispatchX = Mathf.Max(1, Mathf.CeilToInt(targetTexture.width / 8.0f));
        int dispatchY = Mathf.Max(1, Mathf.CeilToInt(targetTexture.height / 8.0f));
        burnCounterShader.Dispatch(kernelID, dispatchX, dispatchY, 1);

        burnCountReadbackRequest = AsyncGPUReadback.Request(countBuffer);

        return lastBurnedCount;
    }

    private bool ValidateInputs()
    {
        if (burnCounterShader == null)
        {
            Debug.LogWarning("Burn counter ComputeShader reference is missing.");
            return false;
        }

        if (targetTexture == null)
        {
            Debug.LogWarning("Target RenderTexture reference is missing.");
            return false;
        }

        if (kernelID == 0 && burnCounterShader != null)
        {
            kernelID = burnCounterShader.FindKernel("CountBurned");
        }

        if (kernelID < 0)
        {
            Debug.LogWarning("Unable to find CountBurned kernel on the provided ComputeShader.");
            return false;
        }

        return true;
    }

    private void EnsureBuffer()
    {
        if (countBuffer == null)
        {
            countBuffer = new ComputeBuffer(1, sizeof(uint), ComputeBufferType.Structured);
        }
    }

    private void ReleaseBuffer()
    {
        burnCountReadbackRequest = default;

        if (countBuffer != null)
        {
            countBuffer.Release();
            countBuffer = null;
        }
    }
}

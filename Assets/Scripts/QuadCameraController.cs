using UnityEngine;

/// <summary>
/// Positions and scales a unit quad so that it always matches the view of an orthographic camera.
/// </summary>
[ExecuteAlways]
public class QuadCameraController : MonoBehaviour
{
    [SerializeField]
    private Camera targetCamera;

    [SerializeField]
    private float zOffset = 1f;

    private Camera TargetCamera
    {
        get
        {
            if (targetCamera != null)
            {
                return targetCamera;
            }

            if (Camera.main != null)
            {
                return Camera.main;
            }

            return null;
        }
    }

    private void OnEnable()
    {
        UpdateQuad();
    }

    private void LateUpdate()
    {
        UpdateQuad();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        UpdateQuad();
    }
#endif

    private void UpdateQuad()
    {
        var camera = TargetCamera;
        if (camera == null)
        {
            return;
        }

        if (!camera.orthographic)
        {
            return;
        }

        Transform cameraTransform = camera.transform;
        transform.SetPositionAndRotation(
            cameraTransform.position + cameraTransform.forward * zOffset,
            cameraTransform.rotation);

        float height = camera.orthographicSize * 2f;
        float width = height * camera.aspect;
        transform.localScale = new Vector3(width, height, 1f);
    }
}

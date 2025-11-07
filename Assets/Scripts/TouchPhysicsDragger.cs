using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Allows a player to grab physics objects with a single touch and drag them around.
/// The grabbed object keeps its rigidbody behaviour and is pulled towards the touch
/// position with a configurable spring joint like effect.
/// </summary>
public class TouchPhysicsDragger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera targetCamera;

    [Header("Drag Settings")]
    [SerializeField] private LayerMask draggableLayers = Physics2D.DefaultRaycastLayers;
    [SerializeField, Tooltip("Layer assigned to the grabbed object while it is being dragged.")] private int grabbedLayer = 2;
    [SerializeField] private float jointFrequency = 5f;
    [SerializeField] private float jointDampingRatio = 0.7f;
    [SerializeField] private float maxForceMultiplier = 1000f;

    private Rigidbody2D grabbedRigidbody;
    private TargetJoint2D targetJoint;
    private int originalLayer;
    private bool layerChanged;

    private bool IsDragging => grabbedRigidbody != null;

    private void Update()
    {
        var touchScreen = Touchscreen.current;
        if (touchScreen == null)
        {
            if (IsDragging)
            {
                ReleaseGrabbedObject();
            }

            return;
        }

        var primaryTouch = touchScreen.primaryTouch;
        var pressControl = primaryTouch.press;

        if (pressControl.wasPressedThisFrame)
        {
            TryBeginDrag(primaryTouch.position.ReadValue());
        }
        else if (pressControl.isPressed && IsDragging)
        {
            UpdateDrag(primaryTouch.position.ReadValue());
        }
        else if (pressControl.wasReleasedThisFrame || primaryTouch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Canceled)
        {
            if (IsDragging)
            {
                ReleaseGrabbedObject();
            }
        }
    }

    private void TryBeginDrag(Vector2 screenPosition)
    {
        if (IsDragging)
            return;

        if (!TryGetWorldPoint(screenPosition, out var worldPoint))
            return;

        var collider = Physics2D.OverlapPoint(worldPoint, draggableLayers);
        if (collider == null)
            return;

        var body = collider.attachedRigidbody;
        if (body == null)
            return;

        grabbedRigidbody = body;
        CreateJoint(worldPoint);
        ChangeLayer(grabbedRigidbody.gameObject);
    }

    private void UpdateDrag(Vector2 screenPosition)
    {
        if (targetJoint == null)
        {
            ReleaseGrabbedObject();
            return;
        }

        if (!TryGetWorldPoint(screenPosition, out var worldPoint))
        {
            ReleaseGrabbedObject();
            return;
        }

        targetJoint.target = worldPoint;
    }

    private void CreateJoint(Vector2 worldPoint)
    {
        targetJoint = grabbedRigidbody.gameObject.AddComponent<TargetJoint2D>();
        targetJoint.autoConfigureTarget = false;
        targetJoint.maxForce = Mathf.Max(0f, maxForceMultiplier * grabbedRigidbody.mass);
        targetJoint.dampingRatio = jointDampingRatio;
        targetJoint.frequency = jointFrequency;
        targetJoint.anchor = grabbedRigidbody.transform.InverseTransformPoint(worldPoint);
        targetJoint.target = worldPoint;
    }

    private void ReleaseGrabbedObject()
    {
        if (grabbedRigidbody != null)
        {
            RestoreLayer(grabbedRigidbody.gameObject);
        }

        if (targetJoint != null)
        {
            Destroy(targetJoint);
        }

        targetJoint = null;
        grabbedRigidbody = null;
        layerChanged = false;
    }

    private bool TryGetWorldPoint(Vector2 screenPosition, out Vector2 worldPoint)
    {
        var cameraToUse = targetCamera != null ? targetCamera : Camera.main;
        if (cameraToUse == null)
        {
            worldPoint = default;
            return false;
        }

        var position = new Vector3(screenPosition.x, screenPosition.y, cameraToUse.orthographic ? 0f : Mathf.Abs(cameraToUse.transform.position.z));
        var world = cameraToUse.ScreenToWorldPoint(position);
        world.z = 0f;
        worldPoint = world;
        return true;
    }

    private void ChangeLayer(GameObject grabbedObject)
    {
        if (grabbedObject.layer == grabbedLayer)
        {
            layerChanged = false;
            return;
        }

        originalLayer = grabbedObject.layer;
        grabbedObject.layer = grabbedLayer;
        layerChanged = true;
    }

    private void RestoreLayer(GameObject grabbedObject)
    {
        if (!layerChanged)
            return;

        grabbedObject.layer = originalLayer;
    }

    private void OnDisable()
    {
        if (IsDragging)
        {
            ReleaseGrabbedObject();
        }
    }
}

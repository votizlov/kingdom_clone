using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Provides simple drag and drop behaviour that spawns a prefab when the pointer
/// is held down on the attached UI element and moved into a configured drop area.
/// Works with the Unity Input System so both mouse and touch input are supported.
/// </summary>
public class DragAndDropBurningObject : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("References")]
    [SerializeField] private RectTransform dropArea;
    [SerializeField] private GameObject burningObjectPrefab;
    [SerializeField] private Camera worldCamera;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnZ = 0f;

    private bool isDragging;
    private int activePointerId = -1;

    private GameObject spawnedObject;
    private Rigidbody2D spawnedRigidbody;

    private struct ColliderState
    {
        public Collider2D Collider;
        public bool WasEnabled;
    }

    private List<ColliderState> colliderStates = new List<ColliderState>();

    private RigidbodyType2D originalBodyType;
    private bool hadRigidbody;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isDragging)
            return;

        isDragging = true;
        activePointerId = eventData.pointerId;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || eventData.pointerId != activePointerId)
            return;

        bool overDropArea = IsPointerOverDropArea(eventData);

        if (overDropArea)
        {
            if (spawnedObject == null)
            {
                SpawnObject(eventData);
            }

            UpdateSpawnedObjectPosition(eventData);
        }
        else if (spawnedObject != null)
        {
            DestroySpawnedObject();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isDragging || eventData.pointerId != activePointerId)
            return;

        bool overDropArea = IsPointerOverDropArea(eventData);

        if (spawnedObject != null)
        {
            if (overDropArea)
            {
                ReleaseSpawnedObject();
            }
            else
            {
                DestroySpawnedObject();
            }
        }

        ResetDragState();
    }

    private void SpawnObject(PointerEventData eventData)
    {
        if (burningObjectPrefab == null)
        {
            Debug.LogWarning("DragAndDropBurningObject requires a prefab to spawn.");
            return;
        }

        spawnedObject = Instantiate(burningObjectPrefab);
        UpdateSpawnedObjectPosition(eventData);

        spawnedRigidbody = spawnedObject.GetComponent<Rigidbody2D>();
        if (spawnedRigidbody != null)
        {
            hadRigidbody = true;
            originalBodyType = spawnedRigidbody.bodyType;
            spawnedRigidbody.velocity = Vector2.zero;
            spawnedRigidbody.angularVelocity = 0f;
            spawnedRigidbody.bodyType = RigidbodyType2D.Kinematic;
        }
        else
        {
            hadRigidbody = false;
        }

        colliderStates.Clear();
        foreach (var collider in spawnedObject.GetComponentsInChildren<Collider2D>())
        {
            colliderStates.Add(new ColliderState
            {
                Collider = collider,
                WasEnabled = collider.enabled
            });
            collider.enabled = false;
        }
    }

    private void UpdateSpawnedObjectPosition(PointerEventData eventData)
    {
        if (spawnedObject == null)
            return;

        Camera cameraToUse = worldCamera != null ? worldCamera : Camera.main;
        if (cameraToUse == null)
            return;

        float distance = spawnZ - cameraToUse.transform.position.z;
        if (Mathf.Approximately(distance, 0f))
        {
            distance = cameraToUse.orthographic ? cameraToUse.nearClipPlane : 0.1f;
        }
        else
        {
            distance = Mathf.Abs(distance);
        }

        Vector3 worldPoint = cameraToUse.ScreenToWorldPoint(new Vector3(eventData.position.x, eventData.position.y, distance));
        worldPoint.z = spawnZ;
        spawnedObject.transform.position = worldPoint;
    }

    private void ReleaseSpawnedObject()
    {
        if (hadRigidbody && spawnedRigidbody != null)
        {
            spawnedRigidbody.bodyType = originalBodyType;
        }

        foreach (var state in colliderStates)
        {
            if (state.Collider != null)
            {
                state.Collider.enabled = state.WasEnabled;
            }
        }

        ClearSpawnedReferences();
    }

    private void DestroySpawnedObject()
    {
        if (spawnedObject != null)
        {
            Destroy(spawnedObject);
        }

        ClearSpawnedReferences();
    }

    private void ClearSpawnedReferences()
    {
        spawnedObject = null;
        spawnedRigidbody = null;
        colliderStates.Clear();
        hadRigidbody = false;
    }

    private void ResetDragState()
    {
        isDragging = false;
        activePointerId = -1;
    }

    private bool IsPointerOverDropArea(PointerEventData eventData)
    {
        if (dropArea == null)
            return false;

        Camera eventCamera = eventData.pressEventCamera ?? eventData.enterEventCamera;
        return RectTransformUtility.RectangleContainsScreenPoint(dropArea, eventData.position, eventCamera);
    }
}

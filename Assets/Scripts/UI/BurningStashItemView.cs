using System;
using UnityEngine;
using UnityEngine.UI;

public class BurningStashItemView : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private DragAndDropBurningObject dragAndDrop;

    private BurningData burningData;
    private Action<BurningStashItemView> onItemPlaced;

    public BurningData Data => burningData;

    public void Initialize(BurningData data, RectTransform dropArea, Camera worldCamera, Action<BurningStashItemView> onPlaced)
    {
        burningData = data;
        onItemPlaced = onPlaced;

        if (iconImage != null)
        {
            iconImage.sprite = data != null ? data.Icon : null;
        }

        ConfigureDragAndDrop(dropArea, worldCamera);
    }

    public void ConfigureDragAndDrop(RectTransform dropArea, Camera worldCamera)
    {
        if (dragAndDrop == null)
        {
            return;
        }

        var prefab = burningData != null && burningData.Prefab != null
            ? burningData.Prefab.gameObject
            : null;

        dragAndDrop.Configure(dropArea, prefab, worldCamera);
        dragAndDrop.enabled = prefab != null;

        dragAndDrop.DragCompleted -= HandleDragCompleted;

        if (prefab != null)
        {
            dragAndDrop.DragCompleted += HandleDragCompleted;
        }
    }

    private void HandleDragCompleted(bool success)
    {
        if (!success)
        {
            return;
        }

        onItemPlaced?.Invoke(this);
    }

    private void OnDestroy()
    {
        if (dragAndDrop != null)
        {
            dragAndDrop.DragCompleted -= HandleDragCompleted;
        }
    }
}

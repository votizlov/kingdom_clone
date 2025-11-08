using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BurningStashView : MonoBehaviour
{
    private const string StashKey = "BurningShop.Stash";

    [SerializeField] private Transform contentRoot;
    [SerializeField] private BurningStashItemView itemViewPrefab;
    [SerializeField] private RectTransform dropArea;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private List<BurningData> initialAvailableItems = new List<BurningData>();
    [SerializeField] private UnityEvent<BurningData> onItemPlaced = new UnityEvent<BurningData>();

    private readonly List<string> ownedItemIds = new List<string>();
    private readonly List<BurningStashItemView> activeItemViews = new List<BurningStashItemView>();
    private readonly Dictionary<string, BurningData> availableItems = new Dictionary<string, BurningData>();

    public UnityEvent<BurningData> OnItemPlaced => onItemPlaced;

    private void Awake()
    {
        LoadOwnedItems();

        if (initialAvailableItems != null && initialAvailableItems.Count > 0)
        {
            SetAvailableItems(initialAvailableItems);
        }
        else
        {
            RebuildViews();
        }
    }

    public void SetAvailableItems(IEnumerable<BurningData> items)
    {
        availableItems.Clear();

        if (items != null)
        {
            foreach (var item in items)
            {
                if (item == null)
                {
                    continue;
                }

                var id = GetId(item);
                availableItems[id] = item;
            }
        }

        RebuildViews();
    }

    public void SetDropConfiguration(RectTransform newDropArea, Camera camera)
    {
        dropArea = newDropArea;
        worldCamera = camera;

        foreach (var view in activeItemViews)
        {
            if (view != null)
            {
                view.ConfigureDragAndDrop(dropArea, worldCamera);
            }
        }
    }

    public void AddItem(BurningData data)
    {
        if (data == null)
        {
            return;
        }

        var id = GetId(data);
        ownedItemIds.Add(id);
        SaveOwnedItems();
        CreateViewFor(id);
    }

    private void LoadOwnedItems()
    {
        ownedItemIds.Clear();

        var json = PlayerPrefs.GetString(StashKey, string.Empty);
        if (string.IsNullOrEmpty(json))
        {
            return;
        }

        try
        {
            var saveData = JsonUtility.FromJson<StashSaveData>(json);
            if (saveData?.Items == null)
            {
                return;
            }

            foreach (var id in saveData.Items)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    ownedItemIds.Add(id);
                }
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Failed to load stash contents: {exception.Message}");
        }
    }

    private void SaveOwnedItems()
    {
        var saveData = new StashSaveData
        {
            Items = new List<string>(ownedItemIds)
        };

        var json = JsonUtility.ToJson(saveData);
        PlayerPrefs.SetString(StashKey, json);
        PlayerPrefs.Save();
    }

    private void RebuildViews()
    {
        ClearViews();

        foreach (var id in ownedItemIds)
        {
            CreateViewFor(id);
        }
    }

    private void ClearViews()
    {
        activeItemViews.Clear();

        if (contentRoot != null)
        {
            for (int i = contentRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(contentRoot.GetChild(i).gameObject);
            }
        }
    }

    private void CreateViewFor(string id)
    {
        if (contentRoot == null || itemViewPrefab == null)
        {
            return;
        }

        if (!availableItems.TryGetValue(id, out var data) || data == null)
        {
            Debug.LogWarning($"Unable to find burning data for stash item id '{id}'.");
            return;
        }

        var view = Instantiate(itemViewPrefab, contentRoot);
        view.Initialize(data, dropArea, worldCamera, HandleItemPlaced);
        activeItemViews.Add(view);
    }

    private void HandleItemPlaced(BurningStashItemView view)
    {
        if (view == null)
        {
            return;
        }

        var id = GetId(view.Data);

        if (RemoveFirstOwnedItem(id))
        {
            SaveOwnedItems();
        }

        activeItemViews.Remove(view);
        var data = view.Data;
        Destroy(view.gameObject);

        onItemPlaced?.Invoke(data);
    }

    private bool RemoveFirstOwnedItem(string id)
    {
        for (int i = 0; i < ownedItemIds.Count; i++)
        {
            if (ownedItemIds[i] == id)
            {
                ownedItemIds.RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    private static string GetId(BurningData data)
    {
        return data != null ? data.name : string.Empty;
    }

    [Serializable]
    private class StashSaveData
    {
        public List<string> Items = new List<string>();
    }
}

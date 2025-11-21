using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BurningShopView : MonoBehaviour
{
    private const string PointsKey = "BurningShop.Points";

    public Transform contentRoot;
    [SerializeField] private BurningShopItemView itemViewPrefab;
    [SerializeField] private List<BurningData> burningItems = new List<BurningData>();
    [SerializeField] private TMP_Text pointsLabel;
    [SerializeField] private Transform spawnTransform;
    [SerializeField] private Transform spawnParent;
    [SerializeField] private int defaultBurnReward = 1;
    [SerializeField] private ComboManager comboManager;
    [SerializeField] private BurningStashView stashView;
    [SerializeField] private ProjectionUVUpdater burnManager;

    private readonly List<ItemBinding> itemBindings = new List<ItemBinding>();
    private int currentPoints;

    private void Awake()
    {
        if (comboManager == null)
        {
            comboManager = FindObjectOfType<ComboManager>();
        }
        LoadPoints();
        if (stashView != null)
        {
            stashView.SetAvailableItems(burningItems);
        }
        BuildShop();
        RefreshPointsLabel();
        UpdateItemInteractivity();
    }

    private void OnEnable()
    {
        BurningBehaviour.Burned += HandleBurned;
        if (comboManager != null)
        {
            comboManager.ComboPointsAwarded += HandleComboPointsAwarded;
        }
    }

    private void OnDisable()
    {
        BurningBehaviour.Burned -= HandleBurned;
        if (comboManager != null)
        {
            comboManager.ComboPointsAwarded -= HandleComboPointsAwarded;
        }
    }

    private void LoadPoints()
    {
        currentPoints = PlayerPrefs.GetInt(PointsKey, 0);
    }

    private void SavePoints()
    {
        PlayerPrefs.SetInt(PointsKey, currentPoints);
        PlayerPrefs.Save();
    }

    private void BuildShop()
    {
        if (contentRoot == null || itemViewPrefab == null)
        {
            Debug.LogWarning("BurningShopView is missing references to build the shop list.");
            return;
        }

        foreach (Transform child in contentRoot)
        {
            Destroy(child.gameObject);
        }

        itemBindings.Clear();

        foreach (var data in burningItems)
        {
            if (data == null)
            {
                continue;
            }

            var itemView = Instantiate(itemViewPrefab, contentRoot);
            itemView.Initialize(data, HandleBuyRequested);
            itemView.SetInteractable(CanAfford(data));
            itemBindings.Add(new ItemBinding(data, itemView));
        }
    }

    private void HandleBuyRequested(BurningData data)
    {
        if (data == null)
        {
            return;
        }

        if (!CanAfford(data))
        {
            return;
        }

        SpendPoints(data.BuyCost);

        if (stashView != null)
        {
            stashView.AddItem(data);
        }
        else
        {
            SpawnBurningObject(data);
        }
    }

    private void SpawnBurningObject(BurningData data)
    {
        if (data.Prefab == null)
        {
            Debug.LogWarning($"BurningData '{data.name}' does not have a prefab assigned.");
            return;
        }

        var position = spawnTransform != null ? spawnTransform.position : Vector3.zero;
        var rotation = spawnTransform != null ? spawnTransform.rotation : Quaternion.identity;
        var parent = spawnParent != null ? spawnParent : null;

        var instance = Instantiate(data.Prefab, position, rotation, parent);
        if (burnManager != null)
        {
            burnManager.Track(instance.GetComponent<BurningBehaviour>());
        }
        instance.SetBurningData(data);
    }

    private void HandleBurned(BurningBehaviour behaviour)
    {
        var reward = behaviour != null && behaviour.BurningData != null
            ? behaviour.BurningData.BurnCost
            : defaultBurnReward;

        AddPoints(reward);
    }

    private void HandleComboPointsAwarded(int points)
    {
        AddPoints(points);
    }

    public void AddPoints(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        currentPoints += amount;
        SavePoints();
        RefreshPointsLabel();
        UpdateItemInteractivity();
    }

    public void SpendPoints(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        currentPoints = Mathf.Max(0, currentPoints - amount);
        SavePoints();
        RefreshPointsLabel();
        UpdateItemInteractivity();
    }

    private void RefreshPointsLabel()
    {
        if (pointsLabel != null)
        {
            pointsLabel.text = currentPoints.ToString();
        }
    }

    private bool CanAfford(BurningData data)
    {
        return data != null && currentPoints >= data.BuyCost;
    }

    private void UpdateItemInteractivity()
    {
        foreach (var binding in itemBindings)
        {
            binding.View.SetInteractable(CanAfford(binding.Data));
        }
    }

    private readonly struct ItemBinding
    {
        public BurningData Data { get; }
        public BurningShopItemView View { get; }

        public ItemBinding(BurningData data, BurningShopItemView view)
        {
            Data = data;
            View = view;
        }
    }
}

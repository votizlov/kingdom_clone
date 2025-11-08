using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class BurningShopView : MonoBehaviour
{
    [SerializeField] private Transform contentRoot;
    [SerializeField] private BurningShopItemView itemViewPrefab;
    [SerializeField] private List<BurningData> burningItems = new List<BurningData>();
    [SerializeField] private TMP_Text pointsLabel;
    [SerializeField] private Transform spawnTransform;
    [SerializeField] private Transform spawnParent;
    [SerializeField] private int defaultBurnReward = 1;
    [SerializeField] private BurningStashView stashView;
    [SerializeField] private PointsWallet pointsWallet;
    [SerializeField] private UnityEvent<BurningData> onItemPurchased = new UnityEvent<BurningData>();

    private readonly List<ItemBinding> itemBindings = new List<ItemBinding>();
    private bool walletWarningIssued;

    public UnityEvent<BurningData> OnItemPurchased => onItemPurchased;

    private void Awake()
    {
        EnsureWalletReference();

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
        EnsureWalletReference();

        if (pointsWallet != null)
        {
            walletWarningIssued = false;
            pointsWallet.OnPointsChanged.AddListener(HandlePointsChanged);
        }
    }

    private void OnDisable()
    {
        BurningBehaviour.Burned -= HandleBurned;
        if (pointsWallet != null)
        {
            pointsWallet.OnPointsChanged.RemoveListener(HandlePointsChanged);
        }
    }

    private void BuildShop()
    {
        EnsureWalletReference();

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

        EnsureWalletReference();

        if (pointsWallet != null && !pointsWallet.SpendPoints(data.BuyCost))
        {
            return;
        }

        if (stashView != null)
        {
            stashView.AddItem(data);
        }
        else
        {
            SpawnBurningObject(data);
        }

        onItemPurchased?.Invoke(data);
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
        instance.SetBurningData(data);
    }

    private void HandleBurned(BurningBehaviour behaviour)
    {
        var reward = behaviour != null && behaviour.BurningData != null
            ? behaviour.BurningData.BurnCost
            : defaultBurnReward;

        EnsureWalletReference();

        if (pointsWallet != null)
        {
            pointsWallet.AddPoints(reward);
        }
    }

    private void HandlePointsChanged(int _)
    {
        RefreshPointsLabel();
        UpdateItemInteractivity();
    }

    private bool HasWalletPoints()
    {
        if (pointsWallet != null)
        {
            return true;
        }

        if (!walletWarningIssued)
        {
            walletWarningIssued = true;
            Debug.LogWarning("BurningShopView requires a PointsWallet to function correctly.");
        }
        return false;
    }

    private void EnsureWalletReference()
    {
        if (pointsWallet != null)
        {
            return;
        }

        pointsWallet = PointsWallet.Instance;

        if (pointsWallet == null)
        {
            pointsWallet = FindObjectOfType<PointsWallet>();
        }

        if (pointsWallet != null)
        {
            walletWarningIssued = false;
        }
    }

    private void RefreshPointsLabel()
    {
        EnsureWalletReference();

        if (pointsLabel != null)
        {
            var value = pointsWallet != null ? pointsWallet.CurrentPoints : 0;
            pointsLabel.text = value.ToString();
        }
    }

    private bool CanAfford(BurningData data)
    {
        EnsureWalletReference();

        if (data == null)
        {
            return false;
        }

        if (pointsWallet == null)
        {
            return false;
        }

        return pointsWallet.CanAfford(data.BuyCost);
    }

    private void UpdateItemInteractivity()
    {
        EnsureWalletReference();

        if (!HasWalletPoints())
        {
            return;
        }

        foreach (var binding in itemBindings)
        {
            if (binding.View != null)
            {
                binding.View.SetInteractable(CanAfford(binding.Data));
            }
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

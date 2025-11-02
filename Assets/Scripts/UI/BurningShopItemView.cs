using System;
using UnityEngine;
using UnityEngine.UI;

public class BurningShopItemView : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Text buyCostLabel;
    [SerializeField] private Button buyButton;

    private BurningData burningData;
    private Action<BurningData> onBuyRequested;

    public void Initialize(BurningData data, Action<BurningData> onBuy)
    {
        burningData = data;
        onBuyRequested = onBuy;

        if (iconImage != null)
        {
            iconImage.sprite = data != null ? data.Icon : null;
        }

        if (buyCostLabel != null && data != null)
        {
            buyCostLabel.text = data.BuyCost.ToString();
        }

        if (buyButton != null)
        {
            buyButton.onClick.AddListener(HandleBuyClicked);
        }
    }

    public void SetInteractable(bool isInteractable)
    {
        if (buyButton != null)
        {
            buyButton.interactable = isInteractable;
        }
    }

    private void HandleBuyClicked()
    {
        onBuyRequested?.Invoke(burningData);
    }

    private void OnDestroy()
    {
        if (buyButton != null)
        {
            buyButton.onClick.RemoveListener(HandleBuyClicked);
        }
    }
}

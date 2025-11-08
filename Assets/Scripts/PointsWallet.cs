using UnityEngine;
using UnityEngine.Events;

public class PointsWallet : MonoBehaviour
{
    private const string DefaultPrefsKey = "BurningShop.Points";

    private static PointsWallet instance;

    [SerializeField] private string playerPrefsKey = DefaultPrefsKey;
    [SerializeField] private int startingPoints;
    [SerializeField] private UnityEvent<int> onPointsChanged = new UnityEvent<int>();
    [SerializeField] private UnityEvent<int> onPointsAdded = new UnityEvent<int>();
    [SerializeField] private UnityEvent<int> onPointsSpent = new UnityEvent<int>();

    private int currentPoints;

    public static PointsWallet Instance => instance;

    public int CurrentPoints => currentPoints;

    public UnityEvent<int> OnPointsChanged => onPointsChanged;

    public UnityEvent<int> OnPointsAdded => onPointsAdded;

    public UnityEvent<int> OnPointsSpent => onPointsSpent;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning("Multiple PointsWallet instances detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        instance = this;
        LoadPoints();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public void LoadPoints()
    {
        currentPoints = PlayerPrefs.GetInt(playerPrefsKey, startingPoints);
        NotifyPointsChanged();
    }

    public void SavePoints()
    {
        PlayerPrefs.SetInt(playerPrefsKey, currentPoints);
        PlayerPrefs.Save();
    }

    public void AddPoints(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        currentPoints += amount;
        SavePoints();
        NotifyPointsChanged();
        onPointsAdded?.Invoke(amount);
    }

    public bool SpendPoints(int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (currentPoints < amount)
        {
            return false;
        }

        currentPoints -= amount;
        SavePoints();
        NotifyPointsChanged();
        onPointsSpent?.Invoke(amount);
        return true;
    }

    public bool CanAfford(int amount)
    {
        return currentPoints >= amount;
    }

    public void ForceBroadcast()
    {
        NotifyPointsChanged();
    }

    private void NotifyPointsChanged()
    {
        onPointsChanged?.Invoke(currentPoints);
    }
}

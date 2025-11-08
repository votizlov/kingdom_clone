using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class TapToCollectPoints : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private int points = 1;
    [SerializeField] private PointsWallet pointsWallet;
    [SerializeField] private TutorialManager tutorialManager;
    [SerializeField] private UnityEvent onCollected = new UnityEvent();

    private bool collected;

    public void OnPointerClick(PointerEventData eventData)
    {
        TryCollect();
    }

    private void OnMouseDown()
    {
        // Fallback for scenes without an EventSystem + PhysicsRaycaster.
        TryCollect();
    }

    private void TryCollect()
    {
        if (collected)
        {
            return;
        }

        collected = true;

        if (pointsWallet == null)
        {
            pointsWallet = PointsWallet.Instance;
        }

        if (pointsWallet != null)
        {
            pointsWallet.AddPoints(points);
        }

        tutorialManager?.NotifyPointsCollected();
        onCollected?.Invoke();

        Destroy(gameObject);
    }
}

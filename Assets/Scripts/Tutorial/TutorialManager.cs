using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TutorialManager : MonoBehaviour
{
    [Serializable]
    public class TutorialStage
    {
        public TutorialStageType StageType;
        public UnityEvent OnStageStarted = new UnityEvent();
        public UnityEvent OnStageCompleted = new UnityEvent();

        public void InvokeStarted()
        {
            OnStageStarted?.Invoke();
        }

        public void InvokeCompleted()
        {
            OnStageCompleted?.Invoke();
        }
    }

    public enum TutorialStageType
    {
        BurnObject,
        CollectPoints,
        OpenShop,
        BuyBurningObject,
        PlaceFromStash
    }

    [SerializeField] private List<TutorialStage> stages = new List<TutorialStage>();
    [SerializeField] private BurningShopView shopView;
    [SerializeField] private BurningStashView stashView;
    [SerializeField] private UnityEvent onTutorialCompleted = new UnityEvent();

    private int currentStageIndex = -1;

    public UnityEvent OnTutorialCompleted => onTutorialCompleted;

    private void Reset()
    {
        EnsureDefaultStages();
    }

    private void Awake()
    {
        EnsureDefaultStages();

        if (shopView == null)
        {
            shopView = FindObjectOfType<BurningShopView>();
        }

        if (stashView == null)
        {
            stashView = FindObjectOfType<BurningStashView>();
        }
    }

    private void OnEnable()
    {
        BurningBehaviour.Burned += HandleBurned;

        if (shopView != null)
        {
            shopView.OnItemPurchased.AddListener(HandleItemPurchased);
        }

        if (stashView != null)
        {
            stashView.OnItemPlaced.AddListener(HandleItemPlaced);
        }
    }

    private void OnDisable()
    {
        BurningBehaviour.Burned -= HandleBurned;

        if (shopView != null)
        {
            shopView.OnItemPurchased.RemoveListener(HandleItemPurchased);
        }

        if (stashView != null)
        {
            stashView.OnItemPlaced.RemoveListener(HandleItemPlaced);
        }
    }

    private void Start()
    {
        AdvanceToNextStage();
    }

    public void NotifyPointsCollected()
    {
        if (GetCurrentStageType() == TutorialStageType.CollectPoints)
        {
            CompleteCurrentStage();
        }
    }

    public void NotifyShopOpened()
    {
        if (GetCurrentStageType() == TutorialStageType.OpenShop)
        {
            CompleteCurrentStage();
        }
    }

    private void HandleBurned(BurningBehaviour _)
    {
        if (GetCurrentStageType() == TutorialStageType.BurnObject)
        {
            CompleteCurrentStage();
        }
    }

    private void HandleItemPurchased(BurningData _)
    {
        if (GetCurrentStageType() == TutorialStageType.BuyBurningObject)
        {
            CompleteCurrentStage();
        }
    }

    private void HandleItemPlaced(BurningData _)
    {
        if (GetCurrentStageType() == TutorialStageType.PlaceFromStash)
        {
            CompleteCurrentStage();
        }
    }

    private void AdvanceToNextStage()
    {
        currentStageIndex++;

        if (currentStageIndex >= stages.Count)
        {
            onTutorialCompleted?.Invoke();
            return;
        }

        var stage = GetCurrentStage();
        stage?.InvokeStarted();
    }

    private void CompleteCurrentStage()
    {
        var stage = GetCurrentStage();
        stage?.InvokeCompleted();
        AdvanceToNextStage();
    }

    private TutorialStage GetCurrentStage()
    {
        if (currentStageIndex < 0 || currentStageIndex >= stages.Count)
        {
            return null;
        }

        return stages[currentStageIndex];
    }

    private TutorialStageType? GetCurrentStageType()
    {
        var stage = GetCurrentStage();
        return stage?.StageType;
    }

    private void EnsureDefaultStages()
    {
        if (stages == null)
        {
            stages = new List<TutorialStage>();
        }

        var requiredOrder = new[]
        {
            TutorialStageType.BurnObject,
            TutorialStageType.CollectPoints,
            TutorialStageType.OpenShop,
            TutorialStageType.BuyBurningObject,
            TutorialStageType.PlaceFromStash
        };

        if (stages.Count != requiredOrder.Length)
        {
            stages = new List<TutorialStage>(requiredOrder.Length);
            foreach (var stageType in requiredOrder)
            {
                stages.Add(CreateStage(stageType));
            }
        }
        else
        {
            for (int i = 0; i < requiredOrder.Length; i++)
            {
                if (stages[i] == null)
                {
                    stages[i] = CreateStage(requiredOrder[i]);
                }
                else
                {
                    stages[i].StageType = requiredOrder[i];
                    if (stages[i].OnStageStarted == null)
                    {
                        stages[i].OnStageStarted = new UnityEvent();
                    }

                    if (stages[i].OnStageCompleted == null)
                    {
                        stages[i].OnStageCompleted = new UnityEvent();
                    }
                }
            }
        }
    }

    private static TutorialStage CreateStage(TutorialStageType stageType)
    {
        return new TutorialStage
        {
            StageType = stageType,
            OnStageStarted = new UnityEvent(),
            OnStageCompleted = new UnityEvent()
        };
    }
}

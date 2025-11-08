using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ComboManager : MonoBehaviour
{
    [SerializeField] private ComboConfig comboConfig;
    [SerializeField] private float rememberSeconds = 10f;
    [SerializeField] private ComboTriggeredUnityEvent onComboTriggered = new ComboTriggeredUnityEvent();
    [SerializeField] private UnityEvent<int> onComboPointsAwarded = new UnityEvent<int>();

    private readonly List<BurnRecord> burnHistory = new List<BurnRecord>();
    private readonly List<ComboActivation> triggeredCombos = new List<ComboActivation>();
    private int nextEventId;

    public event Action<ComboDefinition> ComboTriggered;
    public event Action<int> ComboPointsAwarded;

    private void Update()
    {
        PruneExpiredBurns();
    }

    public void NotifyBurned(BurningBehaviour behaviour)
    {
        if (behaviour == null)
        {
            return;
        }

        BurningData data = behaviour.BurningData;
        if (data == null)
        {
            return;
        }

        float time = Time.time;
        BurnRecord record = new BurnRecord(nextEventId++, data, time);
        burnHistory.Add(record);

        EvaluateCombos(record);
        PruneExpiredBurns();
    }

    private void EvaluateCombos(BurnRecord newRecord)
    {
        if (comboConfig == null)
        {
            return;
        }

        IReadOnlyList<ComboDefinition> combos = comboConfig.Combos;
        for (int i = 0; i < combos.Count; i++)
        {
            ComboDefinition combo = combos[i];
            if (combo == null)
            {
                continue;
            }

            if (!combo.RequiresBurningData(newRecord.Data))
            {
                continue;
            }

            if (!TryBuildActivation(combo, newRecord, out ComboActivation activation))
            {
                continue;
            }

            if (ActivationExists(activation))
            {
                continue;
            }

            triggeredCombos.Add(activation);
            InvokeComboEvents(combo);
        }
    }

    private bool TryBuildActivation(ComboDefinition combo, BurnRecord newRecord, out ComboActivation activation)
    {
        activation = null;

        IReadOnlyList<BurningData> requiredBurnings = combo.RequiredBurnings;
        if (requiredBurnings == null || requiredBurnings.Count == 0)
        {
            return false;
        }

        Dictionary<BurningData, int> remainingCounts = BuildRequirementMap(requiredBurnings);
        if (remainingCounts == null)
        {
            return false;
        }
        if (!remainingCounts.TryGetValue(newRecord.Data, out int newRecordRequirement) || newRecordRequirement <= 0)
        {
            return false;
        }

        remainingCounts[newRecord.Data] = newRecordRequirement - 1;

        List<int> usedIds = new List<int> { newRecord.Id };

        for (int i = burnHistory.Count - 1; i >= 0; i--)
        {
            BurnRecord candidate = burnHistory[i];
            if (candidate.Id == newRecord.Id)
            {
                continue;
            }

            float delta = newRecord.Time - candidate.Time;
            if (delta > combo.TimeWindow)
            {
                break;
            }

            if (delta < 0f)
            {
                continue;
            }

            if (!remainingCounts.TryGetValue(candidate.Data, out int requirement) || requirement <= 0)
            {
                continue;
            }

            remainingCounts[candidate.Data] = requirement - 1;
            usedIds.Add(candidate.Id);
        }

        foreach (KeyValuePair<BurningData, int> requirement in remainingCounts)
        {
            if (requirement.Value > 0)
            {
                return false;
            }
        }

        usedIds.Sort();
        activation = new ComboActivation(combo, usedIds);
        return true;
    }

    private static Dictionary<BurningData, int> BuildRequirementMap(IReadOnlyList<BurningData> requiredBurnings)
    {
        Dictionary<BurningData, int> requirements = new Dictionary<BurningData, int>();
        for (int i = 0; i < requiredBurnings.Count; i++)
        {
            BurningData data = requiredBurnings[i];
            if (data == null)
            {
                return null;
            }

            if (requirements.TryGetValue(data, out int count))
            {
                requirements[data] = count + 1;
            }
            else
            {
                requirements[data] = 1;
            }
        }

        return requirements;
    }

    private bool ActivationExists(ComboActivation candidate)
    {
        for (int i = 0; i < triggeredCombos.Count; i++)
        {
            ComboActivation existing = triggeredCombos[i];
            if (!ReferenceEquals(existing.Definition, candidate.Definition))
            {
                continue;
            }

            if (existing.EventIds.Count != candidate.EventIds.Count)
            {
                continue;
            }

            bool matches = true;
            for (int j = 0; j < existing.EventIds.Count; j++)
            {
                if (existing.EventIds[j] != candidate.EventIds[j])
                {
                    matches = false;
                    break;
                }
            }

            if (matches)
            {
                return true;
            }
        }

        return false;
    }

    private void PruneExpiredBurns()
    {
        if (burnHistory.Count == 0)
        {
            return;
        }

        float retention = Mathf.Max(rememberSeconds, comboConfig != null ? comboConfig.MaxTimeWindow : 0f);
        if (retention <= 0f)
        {
            retention = 0.01f;
        }
        float cutoff = Time.time - retention;
        bool removed = false;

        for (int i = burnHistory.Count - 1; i >= 0; i--)
        {
            if (burnHistory[i].Time < cutoff)
            {
                burnHistory.RemoveAt(i);
                removed = true;
            }
        }

        if (removed)
        {
            RemoveInvalidActivations();
        }
    }

    private void RemoveInvalidActivations()
    {
        if (triggeredCombos.Count == 0)
        {
            return;
        }

        HashSet<int> validIds = new HashSet<int>();
        for (int i = 0; i < burnHistory.Count; i++)
        {
            validIds.Add(burnHistory[i].Id);
        }

        for (int i = triggeredCombos.Count - 1; i >= 0; i--)
        {
            ComboActivation activation = triggeredCombos[i];
            bool valid = true;
            for (int j = 0; j < activation.EventIds.Count; j++)
            {
                if (!validIds.Contains(activation.EventIds[j]))
                {
                    valid = false;
                    break;
                }
            }

            if (!valid)
            {
                triggeredCombos.RemoveAt(i);
            }
        }
    }

    private void InvokeComboEvents(ComboDefinition combo)
    {
        ComboTriggered?.Invoke(combo);
        onComboTriggered?.Invoke(combo);

        if (combo == null)
        {
            return;
        }

        int reward = combo.RewardPoints;
        ComboPointsAwarded?.Invoke(reward);
        onComboPointsAwarded?.Invoke(reward);
    }

    [Serializable]
    public class ComboTriggeredUnityEvent : UnityEvent<ComboDefinition>
    {
    }

    private class BurnRecord
    {
        public int Id { get; }
        public BurningData Data { get; }
        public float Time { get; }

        public BurnRecord(int id, BurningData data, float time)
        {
            Id = id;
            Data = data;
            Time = time;
        }
    }

    private class ComboActivation
    {
        public ComboDefinition Definition { get; }
        public List<int> EventIds { get; }

        public ComboActivation(ComboDefinition definition, List<int> eventIds)
        {
            Definition = definition;
            EventIds = eventIds;
        }
    }
}

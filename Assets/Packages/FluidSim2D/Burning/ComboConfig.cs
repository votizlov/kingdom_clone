using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ComboConfig", menuName = "Burning/Combo Config", order = 1)]
public class ComboConfig : ScriptableObject
{
    [SerializeField] private List<ComboDefinition> combos = new List<ComboDefinition>();

    public IReadOnlyList<ComboDefinition> Combos => combos;

    public float MaxTimeWindow
    {
        get
        {
            float maxWindow = 0f;
            for (int i = 0; i < combos.Count; i++)
            {
                ComboDefinition definition = combos[i];
                if (definition == null)
                {
                    continue;
                }

                float window = definition.TimeWindow;
                if (window > maxWindow)
                {
                    maxWindow = window;
                }
            }

            return maxWindow;
        }
    }
}

[System.Serializable]
public class ComboDefinition
{
    [SerializeField] private string comboName;
    [SerializeField] private List<BurningData> requiredBurnings = new List<BurningData>();
    [SerializeField] private float timeWindow = 5f;
    [SerializeField] private int rewardPoints = 0;

    public string ComboName => comboName;
    public IReadOnlyList<BurningData> RequiredBurnings => requiredBurnings;
    public float TimeWindow => timeWindow < 0f ? 0f : timeWindow;
    public int RewardPoints => rewardPoints;

    public bool RequiresBurningData(BurningData data)
    {
        if (data == null)
        {
            return false;
        }

        for (int i = 0; i < requiredBurnings.Count; i++)
        {
            if (requiredBurnings[i] == data)
            {
                return true;
            }
        }

        return false;
    }
}

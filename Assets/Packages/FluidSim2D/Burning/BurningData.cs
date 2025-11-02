using UnityEngine;

[CreateAssetMenu(fileName = "BurningData", menuName = "Burning/Data", order = 0)]
public class BurningData : ScriptableObject
{
    [SerializeField] private BurningBehaviour prefab;
    [SerializeField] private Sprite icon;
    [SerializeField] private int burnCost = 1;
    [SerializeField] private int buyCost = 1;

    public BurningBehaviour Prefab => prefab;
    public Sprite Icon => icon;
    public int BurnCost => burnCost;
    public int BuyCost => buyCost;
}

using UnityEngine;

[CreateAssetMenu(fileName = "New Food", menuName = "Item/Food Definition")]
public sealed class FoodDefinition : ItemDefinition
{
    [SerializeField, Min(1)] private int level = 1;
    [SerializeField, Min(0f)]
    [Tooltip("Happiness value for this food. Stored for future mood behavior.")]
    private float happiness = 0f;

    [SerializeField, Min(0f)]
    [Tooltip("Amount of hunger this food can restore when consumed.")]
    private float nourishment = 15f;

    public float Nourishment => Mathf.Max(0f, nourishment);
    public int Level => Mathf.Max(1, level);
    public float Happiness => Mathf.Max(0f, happiness);

    protected override void OnValidate()
    {
        base.OnValidate();
        level = Mathf.Max(1, level);
        happiness = Mathf.Max(0f, happiness);
        nourishment = Mathf.Max(0f, nourishment);
    }
}

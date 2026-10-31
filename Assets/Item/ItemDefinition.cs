using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Item/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    [SerializeField] private string displayName;
    [SerializeField] private Sprite icon;
    [SerializeField, Min(1)] private int maxStackSize = 20;

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public Sprite Icon => icon;
    public int MaxStackSize => Mathf.Max(1, maxStackSize);

    protected virtual void OnValidate() => maxStackSize = Mathf.Max(1, maxStackSize);
}

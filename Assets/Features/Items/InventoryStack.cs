using System;
using UnityEngine;

[Serializable]
public sealed class InventoryStack
{
    [SerializeField] private ItemDefinition item;
    [SerializeField, Min(1)] private int quantity = 1;

    public ItemDefinition Item => item;
    public int Quantity => quantity;

    internal InventoryStack(ItemDefinition item, int quantity)
    {
        this.item = item;
        this.quantity = quantity;
    }

    internal void Add(int amount) => quantity += amount;
    internal void Remove(int amount) => quantity -= amount;
}

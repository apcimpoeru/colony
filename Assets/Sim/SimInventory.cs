using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class SimInventory : MonoBehaviour
{
    [SerializeField, Min(1)] private int capacity = 8;
    [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("startingItems")]
    [Tooltip("Actual inventory contents. Quantities decrease when items are consumed.")]
    private List<InventoryStack> items = new List<InventoryStack>();

    private List<InventoryStack> stacks => items;
    private IReadOnlyList<InventoryStack> readOnlyStacks;
    public IReadOnlyList<InventoryStack> Stacks => readOnlyStacks
        ?? (readOnlyStacks = stacks.AsReadOnly());
    public int Capacity => Mathf.Max(1, capacity);
    public int UsedSlots => stacks.Count;

    public event Action Changed;

    public bool HasSpaceFor(ItemDefinition item)
    {
        if (item == null)
            return false;
        if (stacks.Count < Capacity)
            return true;
        foreach (InventoryStack stack in stacks)
        {
            if (stack != null && stack.Item == item && stack.Quantity < item.MaxStackSize)
                return true;
        }
        return false;
    }

    private void Awake()
    {
        // Repack serialized contents using the same slot and stack rules as pickups.
        InventoryStack[] initialItems = stacks.ToArray();
        stacks.Clear();
        foreach (InventoryStack entry in initialItems)
        {
            if (entry == null || entry.Item == null || entry.Quantity <= 0)
                continue;

            AddInternal(entry.Item, entry.Quantity, true);
        }
        if (UsedSlots > Capacity)
            Debug.LogWarning($"{name}: initial contents exceed capacity; items were preserved. "
                + "Remove items before adding new stacks.", this);
    }
    /// <summary>Fills matching stacks, then empty slots. Returns the amount accepted.</summary>
    public int Add(ItemDefinition item, int quantity)
    {
        int added = AddInternal(item, quantity);
        if (added > 0)
            Changed?.Invoke();
        return added;
    }

    private int AddInternal(ItemDefinition item, int quantity, bool preserveOverflow = false)
    {
        if (item == null || quantity <= 0)
            return 0;

        int remaining = quantity;
        foreach (InventoryStack stack in stacks)
        {
            if (stack == null || stack.Item != item)
                continue;

            int amount = Mathf.Min(remaining, Mathf.Max(0, item.MaxStackSize - stack.Quantity));
            stack.Add(amount);
            remaining -= amount;
            if (remaining == 0)
                break;
        }

        while (remaining > 0 && (preserveOverflow || stacks.Count < Capacity))
        {
            int amount = Mathf.Min(remaining, item.MaxStackSize);
            stacks.Add(new InventoryStack(item, amount));
            remaining -= amount;
        }

        return quantity - remaining;
    }

    /// <summary>Removes the full amount, or leaves the inventory unchanged.</summary>
    public bool TryRemove(ItemDefinition item, int quantity)
    {
        if (item == null || quantity <= 0 || !Contains(item, quantity))
            return false;

        RemoveInternal(item, quantity);
        Changed?.Invoke();
        return true;
    }

    private void RemoveInternal(ItemDefinition item, int quantity)
    {
        int remaining = quantity;
        for (int i = stacks.Count - 1; i >= 0 && remaining > 0; i--)
        {
            InventoryStack stack = stacks[i];
            if (stack == null || stack.Item != item)
                continue;

            int amount = Mathf.Min(remaining, stack.Quantity);
            stack.Remove(amount);
            remaining -= amount;
            if (stack.Quantity == 0)
                stacks.RemoveAt(i);
        }

    }

    /// <summary>Moves only what fits. Both inventories are updated before notifying observers.</summary>
    public int TransferTo(SimInventory destination, ItemDefinition item, int quantity)
    {
        if (destination == null || destination == this || !isActiveAndEnabled
            || !destination.isActiveAndEnabled || item == null || quantity <= 0)
            return 0;

        int amount = Mathf.Min(quantity, Count(item));
        int moved = destination.AddInternal(item, amount);
        if (moved == 0)
            return 0;
        RemoveInternal(item, moved);
        Changed?.Invoke();
        destination.Changed?.Invoke();
        return moved;
    }

    public int TransferAllTo(SimInventory destination)
    {
        // Transfers can remove stacks and notify listeners; iterate a snapshot.
        InventoryStack[] snapshot = new InventoryStack[stacks.Count];
        for (int i = 0; i < stacks.Count; i++)
            if (stacks[i] != null)
                snapshot[i] = new InventoryStack(stacks[i].Item, stacks[i].Quantity);
        int moved = 0;
        foreach (InventoryStack stack in snapshot)
            if (stack != null)
                moved += TransferTo(destination, stack.Item, stack.Quantity);
        return moved;
    }

    public int Count(ItemDefinition item)
    {
        if (item == null)
            return 0;

        int total = 0;
        foreach (InventoryStack stack in stacks)
        {
            if (stack != null && stack.Item == item)
                total += stack.Quantity;
        }
        return total;
    }

    public bool Contains(ItemDefinition item, int quantity)
        => item != null && quantity > 0 && Count(item) >= quantity;

    private void OnValidate() => capacity = Mathf.Max(1, capacity);
}


using System;
using System.Collections.Generic;
using UnityEngine;

// One food definition at a loose stack or shared storage container.
internal sealed class SimFoodSource
{
    public WorldItem WorldItem { get; }
    public ItemStorage Storage { get; }
    public FoodDefinition Food { get; }
    public UnityEngine.Object Key => WorldItem != null ? (UnityEngine.Object)WorldItem : Storage;
    public Vector3 Position => WorldItem != null ? WorldItem.Position : Storage.Position;

    private SimFoodSource(WorldItem worldItem, ItemStorage storage, FoodDefinition food)
    {
        WorldItem = worldItem;
        Storage = storage;
        Food = food;
    }

    public bool IsAvailable(SimEating sim, SimInventory carrier)
    {
        if (Food == null || Food.Nourishment <= 0f || !carrier.HasSpaceFor(Food))
            return false;
        if (WorldItem != null)
            return WorldItem.Item == Food && WorldItem.IsAvailableFor(sim);
        return Storage != null && Storage.IsAvailable && Storage.Inventory != carrier
            && Storage.Inventory.Contains(Food, 1);
    }

    public bool TryReserve(SimEating sim) => WorldItem == null
        ? Storage != null && Storage.IsAvailable : WorldItem.TryReserve(sim);

    public void Release(SimEating sim)
    {
        if (WorldItem != null)
            WorldItem.Release(sim);
    }

    public float PickupRange(float pickupDistance, float stoppingDistance)
        => Mathf.Max(Storage != null ? Storage.InteractionDistance : pickupDistance, stoppingDistance);

    public int Pickup(SimEating sim, SimInventory carrier, float range)
        => WorldItem != null ? WorldItem.TryPickup(sim, carrier, range)
            : Storage != null ? Storage.Withdraw(carrier, Food, 1) : 0;

    private static IEnumerable<SimFoodSource> Candidates()
    {
        foreach (WorldItem item in WorldItem.ActiveItems)
            if (item != null && item.Item is FoodDefinition food)
                yield return new SimFoodSource(item, null, food);
        foreach (ItemStorage storage in ItemStorage.ActiveStorages)
            if (storage != null && storage.IsAvailable)
                foreach (InventoryStack stack in storage.Inventory.Stacks)
                    if (stack != null && stack.Quantity > 0 && stack.Item is FoodDefinition food)
                        yield return new SimFoodSource(null, storage, food);
    }

    public static SimFoodSource Find(SimEating sim, SimInventory carrier, float preferenceDistance,
        Predicate<UnityEngine.Object> allowed)
    {
        SimFoodSource best = null;
        float nearestDistance = float.PositiveInfinity;
        foreach (SimFoodSource candidate in Candidates())
        {
            if (!candidate.IsAvailable(sim, carrier) || !allowed(candidate.Key))
                continue;
            float distance = Vector3.Distance(candidate.Position, sim.transform.position);
            if (distance < nearestDistance)
                nearestDistance = distance;
        }
        float limit = nearestDistance + Mathf.Max(0f, preferenceDistance);
        float bestDistance = float.PositiveInfinity;
        foreach (SimFoodSource candidate in Candidates())
        {
            if (!candidate.IsAvailable(sim, carrier) || !allowed(candidate.Key))
                continue;
            float distance = Vector3.Distance(candidate.Position, sim.transform.position);
            if (distance > limit)
                continue;
            if (best == null || candidate.Food.Level > best.Food.Level
                || (candidate.Food.Level == best.Food.Level && (distance < bestDistance
                    || (distance == bestDistance && candidate.Food.Nourishment < best.Food.Nourishment))))
            {
                best = candidate;
                bestDistance = distance;
            }
        }
        return best;
    }
}

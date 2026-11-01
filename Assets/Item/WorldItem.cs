using System.Collections.Generic;
using UnityEngine;

public sealed class WorldItem : MonoBehaviour
{
    private static readonly HashSet<WorldItem> activeItems = new HashSet<WorldItem>();
    internal static IEnumerable<WorldItem> ActiveItems => activeItems;

    [SerializeField] private ItemDefinition item;
    [SerializeField, Min(1)] private int quantity = 1;
    [SerializeField, Tooltip("Optional accessible position beside this object.")]
    private Transform pickupPoint;

    private SimEating reservedBy;

    public ItemDefinition Item => item;
    public int Quantity => quantity;
    public Vector3 Position => pickupPoint != null ? pickupPoint.position : transform.position;

    private void OnEnable() => activeItems.Add(this);
    private void OnDisable()
    {
        activeItems.Remove(this);
        reservedBy = null;
    }

    public bool IsAvailableFor(SimEating sim) => isActiveAndEnabled
        && item != null && quantity > 0 && (reservedBy == null || reservedBy == sim);

    public bool TryReserve(SimEating sim)
    {
        if (sim == null || !IsAvailableFor(sim))
            return false;
        reservedBy = sim;
        return true;
    }

    public void Release(SimEating sim)
    {
        if (reservedBy == sim)
            reservedBy = null;
    }

    public int TryPickup(SimEating sim, SimInventory inventory, float range)
    {
        if (sim == null || inventory == null || inventory.gameObject != sim.gameObject
            || reservedBy != sim || !IsAvailableFor(sim))
            return 0;

        Vector3 offset = Position - sim.transform.position;
        offset.y = 0f;
        if (offset.sqrMagnitude > range * range)
            return 0;

        int added = inventory.Add(item, quantity);
        quantity -= added;
        if (quantity == 0)
        {
            // Immediately unavailable, even though destruction happens at end of frame.
            activeItems.Remove(this);
            Destroy(gameObject);
        }
        return added;
    }

    public static WorldItem FindNearestFood(SimEating sim, SimInventory inventory,
        float preferenceDistance = 0f, System.Predicate<WorldItem> filter = null)
    {
        WorldItem nearest = null;
        float nearestDistance = float.PositiveInfinity;
        foreach (WorldItem candidate in activeItems)
        {
            if (candidate == null || !(candidate.item is FoodDefinition food) || food.Nourishment <= 0f
                || (filter != null && !filter(candidate))
                || !candidate.IsAvailableFor(sim) || !inventory.HasSpaceFor(candidate.item))
                continue;
            float distance = (candidate.Position - sim.transform.position).sqrMagnitude;
            if (distance < nearestDistance)
            {
                nearest = candidate;
                nearestDistance = distance;
            }
        }
        if (nearest == null)
            return null;

        // Anchor the neighborhood to the nearest food, so preferences cannot
        // chain across progressively farther candidates.
        float maxDistance = Mathf.Sqrt(nearestDistance) + Mathf.Max(0f, preferenceDistance);
        float maxDistanceSquared = maxDistance * maxDistance;
        int bestLevel = ((FoodDefinition)nearest.item).Level;
        foreach (WorldItem candidate in activeItems)
        {
            if (candidate == null || !(candidate.item is FoodDefinition food)
                || food.Nourishment <= 0f || (filter != null && !filter(candidate))
                || !candidate.IsAvailableFor(sim) || !inventory.HasSpaceFor(candidate.item))
                continue;

            float distance = (candidate.Position - sim.transform.position).sqrMagnitude;
            if (distance > maxDistanceSquared)
                continue;

            if (food.Level > bestLevel || (food.Level == bestLevel && distance < nearestDistance))
            {
                nearest = candidate;
                nearestDistance = distance;
                bestLevel = food.Level;
            }
        }
        return nearest;
    }
}


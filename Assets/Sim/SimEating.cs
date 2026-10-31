using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SimNeeds), typeof(SimInventory), typeof(SimMovement))]
public sealed class SimEating : MonoBehaviour
{
    [SerializeField, Range(0.01f, 100f)]
    [Tooltip("Starts seeking food and eating at this hunger level.")]
    private float hungerThreshold = 60f;

    [SerializeField, Range(0f, 100f)]
    [Tooltip("Continues seeking food and eating until hunger reaches this level or lower.")]
    private float stopEatingThreshold = 30f;

    [SerializeField, Min(0.01f)] private float pickupDistance = 0.75f;
    [SerializeField, Min(0f)]
    [Tooltip("Prefer higher-level food within this extra distance of the nearest food.")]
    private float foodPreferenceDistance = 2f;
    [SerializeField, Min(0.1f)]
    [Tooltip("Release food after this many game seconds without getting closer.")]
    private float noProgressTimeout = 5f;
    [SerializeField, Min(0.1f)]
    [Tooltip("How many game seconds to skip food after failing to reach it.")]
    private float unreachableRetryDelay = 15f;
    private const float ProgressDistance = 0.1f;
    private readonly Dictionary<WorldItem, float> ignoredFood = new Dictionary<WorldItem, float>();
    private readonly List<WorldItem> expiredFood = new List<WorldItem>();
    private float progressDistance;
    private float lastProgressTime;
    private bool wantsFood;
    private const float SearchInterval = 1f;
    private SimMovement movement;
    private WorldItem target;
    private float nextSearchTime;
    public WorldItem CurrentTarget => target;

    private const float ItemCooldown = 1f;
    private SimNeeds needs;
    private SimInventory inventory;
    private float nextEatTime;

    public bool IsEating { get; private set; }
    public float HungerThreshold => hungerThreshold;

    private void Awake()
    {
        needs = GetComponent<SimNeeds>();
        inventory = GetComponent<SimInventory>();
        movement = GetComponent<SimMovement>();
    }

    // Run after all Updates so need decay is applied before checking fullness.
    private void LateUpdate()
    {
        if (Time.deltaTime <= 0f || !needs.isActiveAndEnabled || !inventory.isActiveAndEnabled)
            return;

        if (needs.IsResting)
        {
            IsEating = false;
            return;
        }

        if (!UpdateAppetite())
        {
            IsEating = false;
            return;
        }

        if (Time.time < nextEatTime)
            return;

        FoodDefinition food = FindPreferredFood();
        if (food == null)
        {
            IsEating = false;
            return;
        }

        if (!inventory.TryRemove(food, 1))
            return;

        needs.Hunger -= food.Nourishment;
        nextEatTime = Time.time + ItemCooldown;
        IsEating = UpdateAppetite() && FindPreferredFood() != null;
    }

    private bool UpdateAppetite()
    {
        if (wantsFood)
        {
            if (needs.Hunger <= stopEatingThreshold)
                wantsFood = false;
        }
        else if (needs.Hunger >= hungerThreshold)
            wantsFood = true;

        return wantsFood;
    }

    private FoodDefinition FindPreferredFood()
    {
        FoodDefinition best = null;
        foreach (InventoryStack stack in inventory.Stacks)
        {
            if (stack != null && stack.Quantity > 0 && stack.Item is FoodDefinition food
                && food.Nourishment > 0f
                && (best == null || food.Level > best.Level
                    || (food.Level == best.Level && food.Nourishment < best.Nourishment)))
                best = food;
        }
        return best;
    }

    // Controller owns movement commands; this component only supplies a destination.
    public Vector3? GetDestination()
    {
        if (!isActiveAndEnabled || !needs.isActiveAndEnabled
            || !inventory.isActiveAndEnabled
            || !UpdateAppetite() || FindPreferredFood() != null)
        {
            Cancel();
            return null;
        }

        if (target != null && (!target.IsAvailableFor(this)
            || !(target.Item is FoodDefinition food) || food.Nourishment <= 0f
            || !inventory.HasSpaceFor(target.Item)))
            Cancel();

        if (target == null && Time.time >= nextSearchTime)
        {
            nextSearchTime = Time.time + SearchInterval;
            ClearExpiredIgnoredFood();
            WorldItem candidate = WorldItem.FindNearestFood(this, inventory, foodPreferenceDistance,
                candidateFood => !ignoredFood.ContainsKey(candidateFood));
            if (candidate != null && candidate.TryReserve(this))
            {
                target = candidate;
                progressDistance = HorizontalDistanceToTarget();
                lastProgressTime = Time.time;
            }
        }

        if (target == null)
            return null;

        float range = Mathf.Max(pickupDistance, movement.StoppingDistance);
        Vector3 offset = target.Position - transform.position;
        offset.y = 0f;
        if (offset.sqrMagnitude <= range * range)
        {
            target.TryPickup(this, inventory, range);
            Cancel();
            return null;
        }

        float distance = offset.magnitude;
        if (distance <= progressDistance - ProgressDistance)
        {
            progressDistance = distance;
            lastProgressTime = Time.time;
        }
        else if (Time.time - lastProgressTime >= noProgressTimeout)
        {
            ignoredFood[target] = Time.time + unreachableRetryDelay;
            Cancel();
            nextSearchTime = Time.time;
            return null;
        }

        return target.Position;
    }

    private float HorizontalDistanceToTarget()
    {
        Vector3 offset = target.Position - transform.position;
        offset.y = 0f;
        return offset.magnitude;
    }

    private void ClearExpiredIgnoredFood()
    {
        expiredFood.Clear();
        foreach (KeyValuePair<WorldItem, float> entry in ignoredFood)
        {
            if (entry.Key == null || Time.time >= entry.Value)
                expiredFood.Add(entry.Key);
        }
        foreach (WorldItem item in expiredFood)
            ignoredFood.Remove(item);
    }

    public void Cancel()
    {
        if (target != null)
            target.Release(this);
        target = null;
    }

    private void OnDisable()
    {
        IsEating = false;
        wantsFood = false;
        ignoredFood.Clear();
        expiredFood.Clear();
        Cancel();
    }

    private void OnValidate()
    {
        hungerThreshold = Mathf.Clamp(hungerThreshold, 0.01f, 100f);
        stopEatingThreshold = Mathf.Clamp(stopEatingThreshold, 0f, hungerThreshold - 0.01f);
        pickupDistance = Mathf.Max(0.01f, pickupDistance);
        foodPreferenceDistance = Mathf.Max(0f, foodPreferenceDistance);
        noProgressTimeout = Mathf.Max(0.1f, noProgressTimeout);
        unreachableRetryDelay = Mathf.Max(0.1f, unreachableRetryDelay);
    }
}


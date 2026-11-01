using UnityEngine;

[RequireComponent(typeof(SimMovement))]
[RequireComponent(typeof(SimSleeping))]
[RequireComponent(typeof(SimStatusDisplay))]
[RequireComponent(typeof(SimNeeds), typeof(SimEating), typeof(SimInventory))]
public class SimController : MonoBehaviour
{
    private enum Activity
    {
        None,
        Food,
        Sleep
    }

    [Header("Manual destination")]
    [Tooltip("Move to this GameObject while assigned. Clear it to stop.")]
    public GameObject destinationOverride;

    [Header("Need prioritization")]
    [SerializeField, Range(0f, 1f)]
    [Tooltip("The urgency advantage required before switching between food and sleep.")]
    private float activitySwitchMargin = 0.15f;
    [SerializeField, Range(0f, 100f)]
    [Tooltip("Hunger at or above this value interrupts sleep for available food, even at critical energy.")]
    private float criticalHunger = 85f;
    [SerializeField, Range(0f, 100f)]
    [Tooltip("Energy at or below this value interrupts food seeking for sleep, unless hunger is also critical and food is available.")]
    private float criticalEnergy = 10f;

    [Header("Work priorities")]
    [SerializeField]
    [Tooltip("Ranks work only. Food, sleep, and manual destinations use their existing rules. Mining wins equal priorities.")]
    private SimWorkPriorities workPriorities = new SimWorkPriorities();

    public SimWorkPriorities WorkPriorities => workPriorities;

    [Header("Storage transfer")]
    [SerializeField] private SimStorageTransfer storageTransfer = new SimStorageTransfer();
    public SimStorageTransfer StorageTransfer => storageTransfer;

    private SimMovement movement;
    private SimEating eating;
    private SimSleeping sleeping;
    private Activity activity;
    private bool foodNeeded;
    private bool sleepNeeded;
    private bool foodAvailable;
    private bool sleepAvailable;

    // These availability results come from the last decision, so displaying the
    // explanation never searches for or reserves another target.
    public string ActivityDetails
    {
        get
        {
            if (!isActiveAndEnabled)
                return "Automatic decisions paused.";
            if (destinationOverride != null)
                return "Following a manual destination.";

            string food;
            if (eating == null || !eating.isActiveAndEnabled)
                food = "Food: eating disabled.";
            else if (eating.IsEating)
                food = "Food: eating carried food.";
            else if (!foodNeeded)
                food = "Food: satisfied.";
            else if (eating.HasTarget)
                food = eating.CurrentStorage != null ? "Food: collecting from storage." : "Food: collecting food.";
            else if (!foodAvailable)
                food = "Looking for food: none available.";
            else if (activity == Activity.Sleep)
                food = "Food: waiting while resting.";
            else
                food = "Food: waiting to eat or search again.";

            string rest;
            if (sleeping == null || !sleeping.isActiveAndEnabled)
                rest = "Rest: sleeping disabled.";
            else if (sleeping.IsSleeping)
                rest = $"Rest: recovering {sleeping.CurrentRecoveryPerSecond:0.##} energy/s"
                    + (sleeping.IsSleepingOnFloor ? " on the floor (no bed)." : " in bed.");
            else if (sleeping.CurrentBed != null)
                rest = "Rest: travelling to assigned bed.";
            else if (!sleepNeeded)
                rest = "Rest: satisfied.";
            else if (!sleepAvailable)
                rest = "Rest: sleeping unavailable.";
            else
                rest = "Rest: waiting while finding food.";

            return food + "\n" + rest + (string.IsNullOrEmpty(storageTransfer.Status)
                ? "" : "\n" + storageTransfer.Status);
        }
    }

    // Read actual component state without running the need-selection policy again.
    public string ActivityLabel
    {
        get
        {
            if (!isActiveAndEnabled)
                return "Controller disabled";
            if (destinationOverride != null)
                return movement != null && movement.IsAt(destinationOverride.transform.position)
                    ? "At destination" : "Moving to destination";
            if (sleeping != null && sleeping.isActiveAndEnabled)
            {
                if (sleeping.IsSleeping)
                    return sleeping.IsSleepingOnFloor ? "Sleeping on floor" : "Sleeping";
                if (sleeping.CurrentBed != null)
                    return "Going to bed";
            }
            if (eating != null && eating.isActiveAndEnabled)
            {
                if (eating.IsEating)
                    return "Eating";
                if (eating.HasTarget)
                    return eating.CurrentStorage != null ? "Seeking stored food" : "Seeking food";
            }
            if (storageTransfer.IsTravelling)
                return storageTransfer.ActivityLabel;
            return "Idle";
        }
    }

    public Vector3? Destination => destinationOverride != null
        ? destinationOverride.transform.position
        : sleeping != null && sleeping.CurrentBed != null
            ? sleeping.CurrentBed.Position
        : eating != null && eating.HasTarget
            ? eating.FoodDestination : storageTransfer.Destination;

    private void Awake()
    {
        movement = GetComponent<SimMovement>();
        eating = GetComponent<SimEating>();
        sleeping = GetComponent<SimSleeping>();
    }

    private void Update()
    {
        if (Time.deltaTime <= 0f)
            return;

        if (destinationOverride != null)
        {
            storageTransfer.Cancel();
            activity = Activity.None;
            if (sleeping != null)
                sleeping.Cancel();
            if (eating != null)
                eating.Cancel();
            movement.SetDestination(destinationOverride.transform.position);
        }
        else
        {
            activity = ChooseActivity();
            // Pausing resets the progress timer so time spent eating/sleeping is
            // never counted as a blocked storage trip.
            if (activity != Activity.None || (eating != null && eating.IsEating))
                storageTransfer.Pause();
            Vector3? destination;
            if (activity == Activity.Sleep && sleeping != null
                && sleeping.Tick(Time.deltaTime, out destination))
            {
                if (eating != null)
                    eating.Cancel();
            }
            else
            {
                if (sleeping != null)
                    sleeping.Cancel();
                destination = eating != null ? eating.GetDestination() : null;
                if (activity == Activity.None && !destination.HasValue
                    && (eating == null || !eating.IsEating))
                    destination = storageTransfer.Tick(GetComponent<SimInventory>());
            }
            if (destination.HasValue)
                movement.SetDestination(destination.Value);
            else
                movement.Stop();
        }
    }

    private void OnDisable()
    {
        storageTransfer.Cancel();
        if (sleeping != null)
            sleeping.Cancel();
        if (eating != null)
            eating.Cancel();
        if (movement != null)
            movement.Stop();
    }

    public bool StartConfiguredStorageTransfer()
        => Application.isPlaying && isActiveAndEnabled && destinationOverride == null
            && storageTransfer.BeginConfigured();

    public void CancelStorageTransfer()
    {
        bool wasTravelling = storageTransfer.IsTravelling;
        storageTransfer.Cancel();
        if (wasTravelling && movement != null)
            movement.Stop();
    }

    private Activity ChooseActivity()
    {
        if (eating == null || sleeping == null)
            return Activity.None;

        foodNeeded = eating.WantsFood;
        sleepNeeded = sleeping.WantsSleep;
        foodAvailable = foodNeeded && eating.HasFoodSource();
        sleepAvailable = sleepNeeded && sleeping.CanSleep;

        if (!foodAvailable && !sleepAvailable)
            return Activity.None;
        if (!foodAvailable)
            return Activity.Sleep;
        if (!sleepAvailable)
            return Activity.Food;

        bool foodCritical = eatingNeedsCritical(foodNeeded);
        bool sleepCritical = sleepingNeedsCritical(sleepNeeded);
        // Resolve competing emergencies explicitly. Sleep urgency saturates at 1
        // below its starting threshold, which otherwise defeats food immediately
        // after pickup or a bite. Let hunger leave the critical range first,
        // including the time between bites, then recover from exhaustion.
        if (foodCritical)
            return Activity.Food;
        if (sleepCritical)
            return Activity.Sleep;

        Activity preferred = eating.FoodUrgency >= sleeping.SleepUrgency
            ? Activity.Food : Activity.Sleep;
        if (activity == Activity.Food && !sleepCritical
            && WithinSwitchMargin(sleeping.SleepUrgency, eating.FoodUrgency))
            return Activity.Food;
        if (activity == Activity.Sleep && !foodCritical
            && WithinSwitchMargin(eating.FoodUrgency, sleeping.SleepUrgency))
            return Activity.Sleep;
        return preferred;
    }

    private bool WithinSwitchMargin(float challenger, float current)
    {
        float boundary = current + activitySwitchMargin;
        return challenger <= boundary || Mathf.Approximately(challenger, boundary);
    }

    private bool eatingNeedsCritical(bool needed) => needed && eating != null
        && eating.GetComponent<SimNeeds>().Hunger >= criticalHunger;

    private bool sleepingNeedsCritical(bool needed) => needed && sleeping != null
        && sleeping.GetComponent<SimNeeds>().Energy <= criticalEnergy;

    private void OnValidate()
    {
        activitySwitchMargin = Mathf.Clamp01(activitySwitchMargin);
        criticalHunger = Mathf.Clamp(criticalHunger, 0f, 100f);
        criticalEnergy = Mathf.Clamp(criticalEnergy, 0f, 100f);
    }
}


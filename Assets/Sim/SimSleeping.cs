using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SimNeeds), typeof(SimMovement))]
public sealed class SimSleeping : MonoBehaviour
{
    [SerializeField, Range(0f, 99.99f)]
    [Tooltip("Seek the assigned bed when energy reaches this level or lower.")]
    private float sleepThreshold = 20f;
    [SerializeField, Range(0.01f, 100f)]
    [Tooltip("Wake when energy reaches this level or higher.")]
    private float wakeThreshold = 90f;
    [SerializeField, Min(0.01f)] private float energyRecoveryPerSecond = 5f;
    [SerializeField, Min(0.01f)] private float sleepDistance = 0.75f;

    private SimNeeds needs;
    private SimMovement movement;
    private SimController colonist;
    private bool wantsSleep;

    public bool IsSleeping { get; private set; }
    public SimBed CurrentBed { get; private set; }

    private void Awake()
    {
        needs = GetComponent<SimNeeds>();
        movement = GetComponent<SimMovement>();
        colonist = GetComponent<SimController>();
    }

    // The controller calls this once per frame and owns all movement commands.
    // True means sleep controls the Sim, with a null destination while resting.
    public bool Tick(float deltaTime, out Vector3? destination)
    {
        destination = null;
        if (colonist == null)
            colonist = GetComponent<SimController>();
        if (!isActiveAndEnabled || colonist == null || !colonist.isActiveAndEnabled
            || !needs.isActiveAndEnabled || !movement.isActiveAndEnabled
            || colonist.destinationOverride != null)
        {
            Cancel();
            return false;
        }

        if (deltaTime <= 0f)
        {
            if (CurrentBed != null && !IsSleeping)
                destination = CurrentBed.Position;
            return wantsSleep && CurrentBed != null;
        }

        if (needs.Energy >= wakeThreshold)
        {
            Cancel();
            return false;
        }

        if (!wantsSleep && needs.Energy > sleepThreshold)
            return false;

        wantsSleep = true;
        if (CurrentBed == null || !CurrentBed.IsAssignedTo(colonist))
        {
            SetSleeping(false);
            CurrentBed = SimBed.FindAssignedBed(colonist);
        }
        if (CurrentBed == null)
            return false;

        Vector3 offset = CurrentBed.Position - transform.position;
        offset.y = 0f;
        float range = Mathf.Max(sleepDistance, movement.StoppingDistance);
        if (offset.sqrMagnitude > range * range)
        {
            SetSleeping(false);
            destination = CurrentBed.Position;
            return true;
        }

        SetSleeping(true);
        needs.Energy += energyRecoveryPerSecond * deltaTime;
        if (needs.Energy >= wakeThreshold)
            Cancel();

        // Keep movement stopped for the frame in which the Sim wakes up.
        return true;
    }

    private void SetSleeping(bool sleeping)
    {
        IsSleeping = sleeping;
        needs.SetResting(sleeping);
    }

    public void Cancel()
    {
        IsSleeping = false;
        if (needs != null)
            needs.SetResting(false);
        wantsSleep = false;
        CurrentBed = null;
    }

    private void OnDisable() => Cancel();

    private void OnValidate()
    {
        sleepThreshold = Mathf.Clamp(sleepThreshold, 0f, 99.99f);
        wakeThreshold = Mathf.Clamp(wakeThreshold, sleepThreshold + 0.01f, 100f);
        energyRecoveryPerSecond = Mathf.Max(0.01f, energyRecoveryPerSecond);
        sleepDistance = Mathf.Max(0.01f, sleepDistance);
    }
}

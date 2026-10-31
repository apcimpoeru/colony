using UnityEngine;

[RequireComponent(typeof(SimMovement))]
[RequireComponent(typeof(SimSleeping))]
[RequireComponent(typeof(SimNeeds), typeof(SimEating), typeof(SimInventory))]
public class SimController : MonoBehaviour
{
    [Header("Manual destination")]
    [Tooltip("Move to this GameObject while assigned. Clear it to stop.")]
    public GameObject destinationOverride;

    private SimMovement movement;
    private SimEating eating;
    private SimSleeping sleeping;

    public Vector3? Destination => destinationOverride != null
        ? destinationOverride.transform.position
        : sleeping != null && sleeping.CurrentBed != null
            ? sleeping.CurrentBed.Position
        : eating != null && eating.CurrentTarget != null
            ? eating.CurrentTarget.Position : (Vector3?)null;

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
            if (sleeping != null)
                sleeping.Cancel();
            if (eating != null)
                eating.Cancel();
            movement.SetDestination(destinationOverride.transform.position);
        }
        else
        {
            Vector3? destination;
            if (sleeping != null && sleeping.Tick(Time.deltaTime, out destination))
            {
                if (eating != null)
                    eating.Cancel();
            }
            else
                destination = eating != null ? eating.GetDestination() : null;
            if (destination.HasValue)
                movement.SetDestination(destination.Value);
            else
                movement.Stop();
        }
    }

    private void OnDisable()
    {
        if (sleeping != null)
            sleeping.Cancel();
        if (eating != null)
            eating.Cancel();
        if (movement != null)
            movement.Stop();
    }
}


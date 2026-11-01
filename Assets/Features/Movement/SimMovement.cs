using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SimMovement : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float moveSpeed = 2f;
    [SerializeField, Min(0.01f)] private float stoppingDistance = 0.2f;

    private Rigidbody body;
    private Vector3? destination;
    public float StoppingDistance => stoppingDistance;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.constraints |= RigidbodyConstraints.FreezeRotation;
    }

    public void SetDestination(Vector3 position) => destination = position;

    public bool IsAt(Vector3 position)
    {
        Vector3 offset = position - (body != null ? body.position : transform.position);
        offset.y = 0f;
        // Compare distances directly so squaring the serialized float cannot
        // round the inclusive arrival boundary differently from sqrMagnitude.
        return offset.magnitude <= stoppingDistance;
    }

    public void Stop()
    {
        destination = null;
        SetHorizontalVelocity(Vector3.zero);
    }

    private void FixedUpdate()
    {
        if (!destination.HasValue || IsAt(destination.Value))
        {
            SetHorizontalVelocity(Vector3.zero);
            return;
        }

        Vector3 offset = destination.Value - body.position;
        offset.y = 0f;
        // Cap the final step so the Sim stops within interaction range.
        float speed = Mathf.Min(moveSpeed,
            Mathf.Max(0f, offset.magnitude - stoppingDistance * 0.5f) / Time.fixedDeltaTime);
        SetHorizontalVelocity(offset.normalized * speed);
    }

    private void SetHorizontalVelocity(Vector3 velocity)
    {
        if (body == null || body.isKinematic)
            return;

        velocity.y = body.velocity.y;
        body.velocity = velocity;
    }

    private void OnDisable() => Stop();

    private void OnValidate()
    {
        moveSpeed = Mathf.Max(0.1f, moveSpeed);
        stoppingDistance = Mathf.Max(0.01f, stoppingDistance);
    }
}

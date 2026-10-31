using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SimBed : MonoBehaviour
{
    private static readonly HashSet<SimBed> activeBeds = new HashSet<SimBed>();

    [SerializeField, Tooltip("Only this colonist can sleep in this bed.")]
    private SimController assignedColonist;
    [SerializeField, Tooltip("Optional reachable position beside the bed. Defaults to the bed position.")]
    private Transform sleepPoint;

    public SimController AssignedColonist => assignedColonist;
    public Vector3 Position => sleepPoint != null ? sleepPoint.position : transform.position;

    public bool IsAssignedTo(SimController colonist) => isActiveAndEnabled
        && colonist != null && assignedColonist == colonist;

    private void OnEnable() => activeBeds.Add(this);
    private void OnDisable() => activeBeds.Remove(this);

    public static SimBed FindAssignedBed(SimController colonist)
    {
        if (colonist == null)
            return null;

        SimBed nearest = null;
        float nearestDistance = float.PositiveInfinity;
        foreach (SimBed bed in activeBeds)
        {
            if (bed == null || !bed.IsAssignedTo(colonist))
                continue;

            float distance = (bed.Position - colonist.transform.position).sqrMagnitude;
            if (distance < nearestDistance)
            {
                nearest = bed;
                nearestDistance = distance;
            }
        }
        return nearest;
    }
}

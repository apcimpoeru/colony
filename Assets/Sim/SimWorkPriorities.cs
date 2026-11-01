using System;
using UnityEngine;

public enum SimWorkType
{
    Mining,
    Farming
}

public enum SimWorkPriority
{
    [InspectorName("0 - Disabled")] Disabled = 0,
    [InspectorName("1 - Highest")] Highest = 1,
    [InspectorName("2 - High")] High = 2,
    [InspectorName("3 - Normal")] Normal = 3,
    [InspectorName("4 - Low")] Low = 4
}

[Serializable]
public sealed class SimWorkPriorities
{
    [SerializeField, Tooltip("0 disables mining; 1 is highest and 4 is lowest priority.")]
    private SimWorkPriority mining = SimWorkPriority.Normal;
    [SerializeField, Tooltip("0 disables farming; 1 is highest and 4 is lowest priority.")]
    private SimWorkPriority farming = SimWorkPriority.Normal;

    // This order is explicit so ties do not depend on enum values or Inspector edits.
    private static readonly SimWorkType[] selectionOrder =
    {
        SimWorkType.Mining,
        SimWorkType.Farming
    };

    public int GetPriority(SimWorkType workType)
    {
        SimWorkPriority priority;
        switch (workType)
        {
            case SimWorkType.Mining: priority = mining; break;
            case SimWorkType.Farming: priority = farming; break;
            default: throw new ArgumentOutOfRangeException(nameof(workType));
        }

        // Treat invalid serialized values as disabled rather than scheduling them.
        int value = (int)priority;
        return value >= 1 && value <= 4 ? value : 0;
    }

    /// <summary>
    /// Selects the highest-priority enabled type with available tasks, or null.
    /// Availability must be a read-only check; this method does not reserve tasks.
    /// </summary>
    public SimWorkType? ChooseAvailableWorkType(Func<SimWorkType, bool> hasAvailableTask)
    {
        if (hasAvailableTask == null)
            throw new ArgumentNullException(nameof(hasAvailableTask));

        SimWorkType? selected = null;
        int bestPriority = 5;
        foreach (SimWorkType workType in selectionOrder)
        {
            int priority = GetPriority(workType);
            if (priority == 0 || priority >= bestPriority || !hasAvailableTask(workType))
                continue;

            selected = workType;
            bestPriority = priority;
        }
        return selected;
    }
}

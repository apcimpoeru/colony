using UnityEngine;

public sealed class SimNeeds : MonoBehaviour
{
    [SerializeField, Range(0f, 100f)]
    [Tooltip("0 = full, 100 = starving.")]
    private float hunger = 0f;

    [SerializeField, Range(0f, 100f)]
    [Tooltip("0 = exhausted, 100 = fully rested.")]
    private float energy = 100f;

    [Header("Needs per second")]
    [SerializeField, Min(0f)] private float hungerIncreasePerSecond = 1f;
    [SerializeField, Min(0f)] private float energyDecreasePerSecond = 0.5f;

    public bool IsResting { get; private set; }

    internal void SetResting(bool resting) => IsResting = resting;

    public float Hunger
    {
        get => hunger;
        set => hunger = Mathf.Clamp(value, 0f, 100f);
    }

    public float Energy
    {
        get => energy;
        set => energy = Mathf.Clamp(value, 0f, 100f);
    }

    private void Update()
    {
        Hunger += hungerIncreasePerSecond * Time.deltaTime;
        if (!IsResting)
            Energy -= energyDecreasePerSecond * Time.deltaTime;
    }

    private void OnValidate()
    {
        Hunger = hunger;
        Energy = energy;
        hungerIncreasePerSecond = Mathf.Max(0f, hungerIncreasePerSecond);
        energyDecreasePerSecond = Mathf.Max(0f, energyDecreasePerSecond);
    }
}

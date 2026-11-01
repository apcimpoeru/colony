using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SimInventory))]
public sealed class ItemStorage : MonoBehaviour
{
    private static readonly HashSet<ItemStorage> activeStorages = new HashSet<ItemStorage>();
    internal static IEnumerable<ItemStorage> ActiveStorages => activeStorages;

    [SerializeField, Tooltip("Optional accessible position beside the storage container.")]
    private Transform interactionPoint;
    [SerializeField, Min(0.01f)] private float interactionDistance = 0.75f;

    private SimInventory inventory;
    public SimInventory Inventory => inventory != null ? inventory : (inventory = GetComponent<SimInventory>());
    public Vector3 Position => interactionPoint != null ? interactionPoint.position : transform.position;
    public float InteractionDistance => Mathf.Max(0.01f, interactionDistance);
    public bool IsAvailable => isActiveAndEnabled && Inventory != null && Inventory.isActiveAndEnabled;

    private void OnEnable() => activeStorages.Add(this);
    private void OnDisable() => activeStorages.Remove(this);

    public bool CanInteract(SimInventory carrier)
    {
        if (!IsAvailable || carrier == null || carrier == Inventory || !carrier.isActiveAndEnabled)
            return false;
        SimMovement movement = carrier.GetComponent<SimMovement>();
        float range = Mathf.Max(InteractionDistance, movement != null ? movement.StoppingDistance : 0f);
        Vector3 offset = Position - carrier.transform.position;
        offset.y = 0f;
        return offset.sqrMagnitude <= range * range;
    }

    public int Deposit(SimInventory carrier, ItemDefinition item, int quantity)
        => CanInteract(carrier) ? carrier.TransferTo(Inventory, item, quantity) : 0;

    public int DepositAll(SimInventory carrier)
        => CanInteract(carrier) ? carrier.TransferAllTo(Inventory) : 0;

    public int Withdraw(SimInventory carrier, ItemDefinition item, int quantity)
        => CanInteract(carrier) ? Inventory.TransferTo(carrier, item, quantity) : 0;
}

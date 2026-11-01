using System;
using UnityEngine;

[Serializable]
public sealed class SimStorageTransfer
{
    public enum TransferDirection { DepositAll, DepositItem, PickUpItem }

    [SerializeField] private ItemStorage storage;
    [SerializeField] private TransferDirection direction;
    [SerializeField, Tooltip("Used for Deposit Item and Pick Up Item.")] private ItemDefinition item;
    [SerializeField, Min(1)] private int quantity = 1;
    [SerializeField, Min(0.1f)] private float noProgressTimeout = 5f;

    private ItemStorage requestedStorage;
    private TransferDirection requestedDirection;
    private ItemDefinition requestedItem;
    private int requestedQuantity;
    private float progressDistance;
    private float lastProgressTime;
    private bool trackingProgress;

    public bool HasRequest { get; private set; }
    public bool IsTravelling { get; private set; }
    public string Status { get; private set; }
    public Vector3? Destination => IsTravelling && requestedStorage != null
        ? requestedStorage.Position : (Vector3?)null;
    public string ActivityLabel => requestedDirection == TransferDirection.PickUpItem
        ? "Collecting from storage" : "Going to deposit";

    public bool BeginConfigured() => Request(storage, direction, item, quantity);

    public bool Request(ItemStorage target, TransferDirection mode, ItemDefinition definition = null, int amount = 1)
    {
        Cancel();
        if (target == null || !target.IsAvailable || !Enum.IsDefined(typeof(TransferDirection), mode)
            || (mode != TransferDirection.DepositAll && (definition == null || amount <= 0)))
        {
            Status = "Storage: choose an active container and valid item/quantity.";
            return false;
        }
        requestedStorage = target;
        requestedDirection = mode;
        requestedItem = definition;
        requestedQuantity = amount;
        HasRequest = true;
        Status = "Storage: transfer queued.";
        return true;
    }

    public void Pause()
    {
        IsTravelling = false;
        trackingProgress = false;
        if (HasRequest)
            Status = "Storage: transfer waiting while taking care of needs.";
    }

    public void Cancel()
    {
        if (HasRequest)
            Status = "Storage: transfer cancelled.";
        HasRequest = false;
        requestedStorage = null;
        Pause();
    }

    // The controller calls this only after food and sleep have been handled.
    public Vector3? Tick(SimInventory carrier)
    {
        if (!HasRequest)
            return null;
        if (requestedStorage == null || !requestedStorage.IsAvailable
            || carrier == null || !carrier.isActiveAndEnabled || carrier == requestedStorage.Inventory)
            return Finish("Storage: container or inventory unavailable.");

        SimInventory source = requestedDirection == TransferDirection.PickUpItem
            ? requestedStorage.Inventory : carrier;
        SimInventory destination = requestedDirection == TransferDirection.PickUpItem
            ? carrier : requestedStorage.Inventory;
        bool transferable = false;
        foreach (InventoryStack stack in source.Stacks)
            if (stack != null && stack.Quantity > 0
                && (requestedDirection == TransferDirection.DepositAll || stack.Item == requestedItem)
                && destination.HasSpaceFor(stack.Item))
            {
                transferable = true;
                break;
            }
        if (!transferable)
            return Finish("Storage: no matching items or destination full; items kept.");

        if (requestedStorage.CanInteract(carrier))
        {
            int moved = requestedDirection == TransferDirection.DepositAll
                ? requestedStorage.DepositAll(carrier)
                : requestedDirection == TransferDirection.DepositItem
                    ? requestedStorage.Deposit(carrier, requestedItem, requestedQuantity)
                    : requestedStorage.Withdraw(carrier, requestedItem, requestedQuantity);
            return Finish($"Storage: transferred {moved} item(s); any remainder stays at source.");
        }

        Vector3 offset = requestedStorage.Position - carrier.transform.position;
        offset.y = 0f;
        float distance = offset.magnitude;
        if (!trackingProgress || distance <= progressDistance - 0.1f)
        {
            trackingProgress = true;
            progressDistance = distance;
            lastProgressTime = Time.time;
        }
        else if (Time.time - lastProgressTime >= Mathf.Max(0.1f, noProgressTimeout))
            return Finish("Storage: could not reach container; transfer cancelled.");
        IsTravelling = true;
        Status = "Storage: travelling to " + requestedStorage.name + ".";
        return requestedStorage.Position;
    }

    private Vector3? Finish(string message)
    {
        Cancel();
        Status = message;
        return null;
    }
}

using NUnit.Framework;
using UnityEngine;
using Direction = SimStorageTransfer.TransferDirection;

public sealed class SimStorageTransferTests : SimulationTestFixture
{
    [TestCase("null storage")]
    [TestCase("disabled storage")]
    [TestCase("invalid mode")]
    [TestCase("null item")]
    [TestCase("zero")]
    [TestCase("negative")]
    public void InvalidRequest_RejectsAndClearsPreviousRequest(string reason)
    {
        var storage = Storage(); var item = Item(); var trip = new SimStorageTransfer();
        Assert.That(trip.Request(storage, Direction.DepositAll), Is.True);
        var mode = Direction.DepositItem; int quantity = 1;
        switch (reason)
        {
            case "null storage": storage = null; break;
            case "disabled storage": storage.enabled = false; break;
            case "invalid mode": mode = (Direction)99; break;
            case "null item": item = null; break;
            case "zero": quantity = 0; break;
            case "negative": quantity = -1; break;
        }
        Assert.That(trip.Request(storage, mode, item, quantity), Is.False);
        Assert.That(trip.HasRequest, Is.False); Assert.That(trip.Destination, Is.Null);
        Assert.That(trip.Status, Does.Contain("valid item/quantity"));
    }

    [TestCase(Direction.DepositAll)]
    [TestCase(Direction.DepositItem)]
    [TestCase(Direction.PickUpItem)]
    public void Arrival_TransfersOnceAndClearsDestination(Direction direction)
    {
        var storage = Storage(); var carrier = Inventory(); var item = Item();
        var source = direction == Direction.PickUpItem ? storage.Inventory : carrier;
        var destination = direction == Direction.PickUpItem ? carrier : storage.Inventory;
        source.Add(item, 25);
        var trip = new SimStorageTransfer();
        Assert.That(trip.Request(storage, direction, item, 3), Is.True);
        Assert.That(trip.Tick(carrier), Is.EqualTo(storage.Position));
        Assert.That(trip.IsTravelling, Is.True);
        Assert.That(trip.Destination, Is.EqualTo(storage.Position));
        carrier.transform.position = storage.Position;
        Assert.That(trip.Tick(carrier), Is.Null);
        int moved = direction == Direction.DepositAll ? 25 : 3;
        Assert.That(destination.Count(item), Is.EqualTo(moved));
        Assert.That(source.Count(item), Is.EqualTo(25 - moved));
        Assert.That(trip.Status, Does.Contain("transferred " + moved));
        Assert.That(trip.HasRequest, Is.False); Assert.That(trip.IsTravelling, Is.False);
        Assert.That(trip.Destination, Is.Null);
        trip.Tick(carrier);
        Assert.That(destination.Count(item), Is.EqualTo(moved));
    }

    [Test]
    public void Configuration_IsSnapshottedUntilNextRequestAndRequestsCanBeReplaced()
    {
        var first = Storage(); var second = Storage(8); var carrier = Inventory(); var item = Item();
        carrier.Add(item, 10); var trip = new SimStorageTransfer();
        Set(trip, "storage", first); Set(trip, "direction", Direction.DepositItem);
        Set(trip, "item", item); Set(trip, "quantity", 2);
        Assert.That(trip.BeginConfigured(), Is.True);
        Set(trip, "storage", second); Set(trip, "quantity", 7);
        carrier.transform.position = first.Position; trip.Tick(carrier);
        Assert.That(first.Inventory.Count(item), Is.EqualTo(2));
        Assert.That(second.Inventory.Count(item), Is.Zero);
        Assert.That(trip.BeginConfigured(), Is.True);
        Assert.That(trip.Tick(carrier), Is.EqualTo(second.Position));
        trip.Request(first, Direction.DepositAll);
        trip.Tick(carrier);
        Assert.That(first.Inventory.Count(item), Is.EqualTo(10));
        Assert.That(second.Inventory.Count(item), Is.Zero);
    }

    [TestCase("disabled")]
    [TestCase("destroyed")]
    [TestCase("empty")]
    [TestCase("full")]
    [TestCase("disabled carrier")]
    [TestCase("self")]
    public void UnavailableTrip_EndsAndKeepsItems(string reason)
    {
        var storage = Storage(); var carrier = Inventory(); var item = Item();
        carrier.Add(item, 5); var trip = new SimStorageTransfer();
        trip.Request(storage, Direction.DepositAll);
        switch (reason)
        {
            case "disabled": storage.enabled = false; break;
            case "destroyed": Object.DestroyImmediate(storage.gameObject); break;
            case "empty": carrier.TryRemove(item, 5); break;
            case "full": Set(storage.Inventory, "capacity", 1); storage.Inventory.Add(Item(), 20); break;
            case "disabled carrier": carrier.enabled = false; break;
            case "self": carrier = storage.Inventory; break;
        }
        Assert.That(trip.Tick(carrier), Is.Null);
        Assert.That(trip.HasRequest, Is.False); Assert.That(trip.Destination, Is.Null);
        if (reason != "empty" && reason != "self") Assert.That(carrier.Count(item), Is.EqualTo(5));
        Assert.That(trip.Status, Does.StartWith("Storage:"));
    }

    [Test]
    public void PartialArrival_ReportsMovedQuantityAndKeepsRemainder()
    {
        var storage = Storage(0); var carrier = Inventory(); var item = Item();
        Set(storage.Inventory, "capacity", 1); storage.Inventory.Add(item, 18); carrier.Add(item, 10);
        var trip = new SimStorageTransfer(); trip.Request(storage, Direction.DepositItem, item, 10);
        trip.Tick(carrier);
        Assert.That(carrier.Count(item), Is.EqualTo(8));
        Assert.That(storage.Inventory.Count(item), Is.EqualTo(20));
        Assert.That(trip.Status, Does.Contain("transferred 2"));
    }

    [Test]
    public void NoProgress_TimesOutButPauseAndProgressResetTimer()
    {
        var storage = Storage(); var carrier = Inventory(); var item = Item(); carrier.Add(item, 5);
        var trip = new SimStorageTransfer(); trip.Request(storage, Direction.DepositAll); trip.Tick(carrier);
        Set(trip, "lastProgressTime", Time.time - 100f); trip.Pause();
        Assert.That(trip.HasRequest, Is.True); Assert.That(trip.IsTravelling, Is.False);
        Assert.That(trip.Tick(carrier).HasValue, Is.True);
        Set(trip, "lastProgressTime", Time.time - 100f);
        carrier.transform.position += Vector3.right;
        Assert.That(trip.Tick(carrier).HasValue, Is.True);
        Set(trip, "lastProgressTime", Time.time - 100f);
        Assert.That(trip.Tick(carrier), Is.Null);
        Assert.That(trip.Status, Does.Contain("could not reach"));
        Assert.That(carrier.Count(item), Is.EqualTo(5));
        trip.Request(storage, Direction.DepositAll); trip.Tick(carrier); trip.Cancel();
        Assert.That(trip.HasRequest, Is.False); Assert.That(trip.Destination, Is.Null);
        Assert.That(trip.Status, Does.Contain("cancelled"));
    }
}

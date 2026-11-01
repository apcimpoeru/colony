using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Direction = SimStorageTransfer.TransferDirection;

public sealed class StorageTripTests : SimulationTestFixture
{
    [UnityTest]
    public IEnumerator Controller_TravelsAndTransfersOnceThenCanPickUp()
    {
        var sim = Colonist(true); var item = Item(); var storage = Storage(1.5f);
        Bag(sim).Add(item, 25); sim.StorageTransfer.Request(storage, Direction.DepositAll);
        yield return Until(() => !sim.StorageTransfer.HasRequest, "Deposit trip did not finish");
        Assert.That(storage.Inventory.Count(item), Is.EqualTo(25)); Assert.That(Bag(sim).Count(item), Is.Zero);
        sim.StorageTransfer.Request(storage, Direction.PickUpItem, item, 3);
        yield return Until(() => !sim.StorageTransfer.HasRequest, "Pickup trip did not finish");
        Assert.That(Bag(sim).Count(item), Is.EqualTo(3)); Assert.That(storage.Inventory.Count(item), Is.EqualTo(22));
    }

    [UnityTest]
    public IEnumerator NeedsPauseTrip_ThenResumeAndComplete()
    {
        var sim = Colonist(true); var item = Item(); var storage = Storage(1.5f);
        Bag(sim).Add(item, 5); Needs(sim).Energy = 19;
        Set(Sleeping(sim), "wakeThreshold", 21f); Set(Sleeping(sim), "energyRecoveryPerSecond", 20f);
        sim.StorageTransfer.Request(storage, Direction.DepositAll);
        yield return Until(() => Sleeping(sim).IsSleeping, "Sleep did not interrupt trip");
        Assert.That(sim.StorageTransfer.HasRequest, Is.True);
        Assert.That(sim.StorageTransfer.IsTravelling, Is.False);
        yield return Until(() => !sim.StorageTransfer.HasRequest, "Trip did not resume after sleep");
        Assert.That(storage.Inventory.Count(item), Is.EqualTo(5));
    }

    [UnityTest]
    public IEnumerator GamePause_PreservesTripAndResumesWithoutTimeout()
    {
        var sim = Colonist(true); var item = Item(); var storage = Storage(1.5f);
        Bag(sim).Add(item, 5); sim.StorageTransfer.Request(storage, Direction.DepositAll);
        Time.timeScale = 0; yield return null;
        var position = sim.transform.position;
        yield return null; yield return null;
        Assert.That(sim.StorageTransfer.HasRequest, Is.True);
        Assert.That(sim.transform.position, Is.EqualTo(position));
        Assert.That(storage.Inventory.Count(item), Is.Zero);
        Time.timeScale = 1;
        yield return Until(() => !sim.StorageTransfer.HasRequest, "Paused trip did not resume");
        Assert.That(storage.Inventory.Count(item), Is.EqualTo(5));
    }

    [UnityTest]
    public IEnumerator FoodPausesTrip_UntilMealAndBiteCooldownFinish()
    {
        var sim = Colonist(true); var item = Item(); var meal = Food(1, 100);
        var storage = Storage(1.5f); Bag(sim).Add(item, 5); Bag(sim).Add(meal, 1);
        Needs(sim).Hunger = 80; sim.StorageTransfer.Request(storage, Direction.DepositItem, item, 5);
        yield return Until(() => Eating(sim).IsEating, "Meal did not interrupt storage trip");
        Assert.That(sim.StorageTransfer.HasRequest, Is.True);
        Assert.That(sim.StorageTransfer.IsTravelling, Is.False);
        yield return Until(() => !sim.StorageTransfer.HasRequest, "Trip did not resume after eating");
        Assert.That(storage.Inventory.Count(item), Is.EqualTo(5));
    }

    [UnityTest]
    public IEnumerator ConfiguredStartAndCancel_RespectControllerAndManualDestination()
    {
        var sim = Colonist(true); var item = Item(); var storage = Storage(5);
        Bag(sim).Add(item, 5); Set(sim.StorageTransfer, "storage", storage);
        Assert.That(sim.StartConfiguredStorageTransfer(), Is.True);
        yield return Until(() => sim.StorageTransfer.IsTravelling, "Configured trip did not travel");
        sim.CancelStorageTransfer();
        Assert.That(sim.StorageTransfer.HasRequest, Is.False);
        Assert.That(sim.GetComponent<Rigidbody>().velocity.x, Is.Zero);
        sim.destinationOverride = NewObject();
        Assert.That(sim.StartConfiguredStorageTransfer(), Is.False);
        sim.destinationOverride = null; sim.enabled = false;
        Assert.That(sim.StartConfiguredStorageTransfer(), Is.False);
    }
}

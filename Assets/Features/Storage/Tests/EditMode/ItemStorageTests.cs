using NUnit.Framework;
using UnityEngine;

public sealed class ItemStorageTests : SimulationTestFixture
{
    [Test]
    public void InteractionPoint_OverridesObjectAndIgnoresHeight()
    {
        var storage = Storage(); var carrier = Inventory();
        Assert.That(storage.Position, Is.EqualTo(storage.transform.position));
        Assert.That(storage.CanInteract(carrier), Is.False);
        var point = NewObject(position: new Vector3(0.75f, 100, 0)).transform;
        Set(storage, "interactionPoint", point);
        Assert.That(storage.Position, Is.EqualTo(point.position));
        Assert.That(storage.CanInteract(carrier), Is.True);
        point.position += new Vector3(0.01f, 0, 0);
        Assert.That(storage.CanInteract(carrier), Is.False);
    }

    [Test]
    public void InteractionRange_IncludesCarrierStoppingDistance()
    {
        var storage = Storage(1); var carrier = Inventory();
        var movement = carrier.gameObject.AddComponent<SimMovement>();
        Set(movement, "stoppingDistance", 1f);
        Assert.That(storage.CanInteract(carrier), Is.True);
    }

    [TestCase("storage")]
    [TestCase("inventory")]
    [TestCase("carrier")]
    [TestCase("self")]
    [TestCase("null")]
    public void InvalidInteraction_RejectsEveryTransferDirection(string reason)
    {
        var storage = Storage(0); var carrier = Inventory(); var item = Item();
        carrier.Add(item, 3); storage.Inventory.Add(item, 4);
        switch (reason)
        {
            case "storage": storage.enabled = false; break;
            case "inventory": storage.Inventory.enabled = false; break;
            case "carrier": carrier.enabled = false; break;
            case "self": carrier = storage.Inventory; break;
            case "null": carrier = null; break;
        }
        Assert.That(storage.CanInteract(carrier), Is.False);
        Assert.That(storage.Deposit(carrier, item, 1), Is.Zero);
        Assert.That(storage.DepositAll(carrier), Is.Zero);
        Assert.That(storage.Withdraw(carrier, item, 1), Is.Zero);
        Assert.That(storage.Inventory.Count(item), Is.EqualTo(4));
    }

    [Test]
    public void DepositWithdrawAndDepositAll_PreservePartialResultsAndOtherItems()
    {
        var storage = Storage(0); var carrier = Inventory(); var ore = Item(); var seed = Item();
        Set(storage.Inventory, "capacity", 1); storage.Inventory.Add(ore, 18);
        carrier.Add(ore, 10); carrier.Add(seed, 3);
        Assert.That(storage.Deposit(carrier, ore, 10), Is.EqualTo(2));
        Assert.That(carrier.Count(ore), Is.EqualTo(8));
        Assert.That(storage.DepositAll(carrier), Is.Zero);
        Assert.That(storage.Withdraw(carrier, ore, 100), Is.EqualTo(20));
        Assert.That(storage.Inventory.Stacks, Is.Empty);
        Assert.That(storage.Withdraw(carrier, ore, 1), Is.Zero);
        Assert.That(carrier.Count(ore), Is.EqualTo(28));
        Assert.That(storage.DepositAll(carrier), Is.EqualTo(20));
        Assert.That(carrier.Count(ore), Is.EqualTo(8));
        Assert.That(carrier.Count(seed), Is.EqualTo(3));
    }

    [Test]
    public void CompetingWithdrawals_FinalItemIsTransferredOnlyOnce()
    {
        var storage = Storage(0); var first = Inventory(); var second = Inventory(); var item = Item();
        storage.Inventory.Add(item, 1);
        Assert.That(storage.Withdraw(first, item, 1), Is.EqualTo(1));
        Assert.That(storage.Withdraw(second, item, 1), Is.Zero);
        Assert.That(first.Count(item) + second.Count(item) + storage.Inventory.Count(item), Is.EqualTo(1));
    }
}

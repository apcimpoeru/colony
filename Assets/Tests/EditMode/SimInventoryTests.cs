using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class SimInventoryTests : InventoryTestFixture
{
    [Test]
    public void EmptyInventory_HasEightSlotsAndNoContents()
    {
        var inventory = Inventory();
        var item = Item();
        Assert.That(inventory.Capacity, Is.EqualTo(8));
        Assert.That(inventory.UsedSlots, Is.Zero);
        Assert.That(inventory.Count(item), Is.Zero);
        Assert.That(inventory.Contains(item, 1), Is.False);
        Assert.That(inventory.HasSpaceFor(item), Is.True);
    }

    [Test]
    public void Add_FillsMatchingStacksBeforeOpeningNewSlots()
    {
        var inventory = Inventory(3);
        var ore = Item();
        var seed = Item();
        inventory.Add(ore, 15);
        inventory.Add(seed, 20);
        Assert.That(inventory.Add(ore, 10), Is.EqualTo(10));
        Assert.That(inventory.UsedSlots, Is.EqualTo(3));
        AssertStack(inventory, 0, ore, 20);
        AssertStack(inventory, 1, seed, 20);
        AssertStack(inventory, 2, ore, 5);
    }

    [TestCase(1, 1, 4, 1)]
    [TestCase(2, 3, 8, 6)]
    [TestCase(3, 20, 47, 47)]
    [TestCase(2, 20, 47, 40)]
    public void Add_RespectsStackAndSlotLimits(int slots, int stackSize, int requested, int accepted)
    {
        var inventory = Inventory(slots);
        var item = Item(stackSize);
        Assert.That(inventory.Add(item, requested), Is.EqualTo(accepted));
        Assert.That(inventory.Count(item), Is.EqualTo(accepted));
        Assert.That(inventory.UsedSlots, Is.EqualTo((accepted + stackSize - 1) / stackSize));
        foreach (var stack in inventory.Stacks)
        {
            Assert.That(stack.Item, Is.SameAs(item));
            Assert.That(stack.Quantity, Is.InRange(1, stackSize));
        }
    }

    [Test]
    public void Queries_DistinguishItemsAndRemainingStackSpace()
    {
        var inventory = Inventory(1);
        var ore = Item();
        var seed = Item();
        inventory.Add(ore, 19);
        Assert.That(inventory.Contains(ore, 19), Is.True);
        Assert.That(inventory.Contains(ore, 20), Is.False);
        Assert.That(inventory.Contains(ore, 0), Is.False);
        Assert.That(inventory.Contains(ore, -1), Is.False);
        Assert.That(inventory.Count(seed), Is.Zero);
        Assert.That(inventory.Contains(seed, 1), Is.False);
        Assert.That(inventory.HasSpaceFor(ore), Is.True);
        Assert.That(inventory.HasSpaceFor(seed), Is.False);
        inventory.Add(ore, 1);
        Assert.That(inventory.HasSpaceFor(ore), Is.False);
        Assert.That(inventory.Add(seed, 1), Is.Zero);
        Assert.That(inventory.Add(ore, 1), Is.Zero);
        inventory.TryRemove(ore, 20);
        Assert.That(inventory.HasSpaceFor(seed), Is.True);
        Assert.That(inventory.Stacks, Is.Empty);
    }

    [TestCase(-1)]
    [TestCase(0)]
    public void InvalidQuantities_DoNotMutateOrNotify(int quantity)
    {
        var inventory = Inventory();
        var item = Item();
        inventory.Add(item, 5);
        int notifications = 0;
        inventory.Changed += () => notifications++;
        Assert.That(inventory.Add(item, quantity), Is.Zero);
        Assert.That(inventory.TryRemove(item, quantity), Is.False);
        Assert.That(inventory.Contains(item, quantity), Is.False);
        Assert.That(inventory.Count(item), Is.EqualTo(5));
        Assert.That(notifications, Is.Zero);
    }

    [Test]
    public void NullItems_AreRejectedWithoutNotifications()
    {
        var inventory = Inventory();
        int notifications = 0;
        inventory.Changed += () => notifications++;
        Assert.That(inventory.Add(null, 1), Is.Zero);
        Assert.That(inventory.TryRemove(null, 1), Is.False);
        Assert.That(inventory.Count(null), Is.Zero);
        Assert.That(inventory.Contains(null, 1), Is.False);
        Assert.That(inventory.HasSpaceFor(null), Is.False);
        Assert.That(inventory.UsedSlots, Is.Zero);
        Assert.That(notifications, Is.Zero);
    }

    [TestCase(1)]
    [TestCase(7)]
    [TestCase(20)]
    [TestCase(27)]
    [TestCase(47)]
    public void Remove_AcrossStacksPreservesOtherItemsAndDropsEmptyStacks(int removed)
    {
        var inventory = Inventory();
        var ore = Item();
        var seed = Item();
        inventory.Add(ore, 47);
        inventory.Add(seed, 3);
        Assert.That(inventory.TryRemove(ore, removed), Is.True);
        Assert.That(inventory.Count(ore), Is.EqualTo(47 - removed));
        Assert.That(inventory.Count(seed), Is.EqualTo(3));
        foreach (var stack in inventory.Stacks)
            Assert.That(stack.Quantity, Is.InRange(1, stack.Item.MaxStackSize));
    }

    [Test]
    public void FailedRemoval_IsAtomicIncludingStackIdentityAndNotifications()
    {
        var inventory = Inventory();
        var item = Item();
        inventory.Add(item, 47);
        var before = new List<InventoryStack>(inventory.Stacks);
        int notifications = 0;
        inventory.Changed += () => notifications++;
        Assert.That(inventory.TryRemove(item, 48), Is.False);
        Assert.That(inventory.TryRemove(Item(), 1), Is.False);
        Assert.That(inventory.Stacks, Is.EqualTo(before));
        AssertStack(inventory, 0, item, 20);
        AssertStack(inventory, 1, item, 20);
        AssertStack(inventory, 2, item, 7);
        Assert.That(notifications, Is.Zero);
    }

    [Test]
    public void Changed_FiresOnceAfterEachSuccessfulAddOrRemoval()
    {
        var inventory = Inventory(2);
        var item = Item();
        int notifications = 0;
        int expectedCount = 40;
        inventory.Changed += () =>
        {
            notifications++;
            Assert.That(inventory.Count(item), Is.EqualTo(expectedCount));
        };
        Assert.That(inventory.Add(item, 47), Is.EqualTo(40));
        Assert.That(notifications, Is.EqualTo(1));
        Assert.That(inventory.Add(item, 1), Is.Zero);
        Assert.That(inventory.TryRemove(item, 41), Is.False);
        Assert.That(notifications, Is.EqualTo(1));
        expectedCount = 15;
        Assert.That(inventory.TryRemove(item, 25), Is.True);
        Assert.That(notifications, Is.EqualTo(2));
    }

    [Test]
    public void PartialTransfer_ConservesContentsBeforeBothNotifications()
    {
        var source = Inventory();
        var destination = Inventory(1);
        var item = Item();
        source.Add(item, 47);
        destination.Add(item, 18);
        int sourceEvents = 0, destinationEvents = 0;
        System.Action assertFinalContents = () =>
        {
            Assert.That(source.Count(item), Is.EqualTo(45));
            Assert.That(destination.Count(item), Is.EqualTo(20));
            Assert.That(source.Count(item) + destination.Count(item), Is.EqualTo(65));
        };
        source.Changed += () => { sourceEvents++; assertFinalContents(); };
        destination.Changed += () => { destinationEvents++; assertFinalContents(); };
        Assert.That(source.TransferTo(destination, item, 47), Is.EqualTo(2));
        Assert.That(source.TransferTo(destination, item, 1), Is.Zero);
        Assert.That(sourceEvents, Is.EqualTo(1));
        Assert.That(destinationEvents, Is.EqualTo(1));
        AssertStack(destination, 0, item, 20);
    }

    [TestCase("self")]
    [TestCase("null destination")]
    [TestCase("disabled destination")]
    [TestCase("inactive destination")]
    [TestCase("disabled source")]
    [TestCase("inactive source")]
    [TestCase("null item")]
    [TestCase("missing item")]
    [TestCase("zero quantity")]
    [TestCase("negative quantity")]
    public void InvalidTransfer_DoesNotMutateOrNotify(string invalid)
    {
        var source = Inventory();
        var destination = Inventory();
        var item = Item();
        source.Add(item, 5);
        SimInventory target = destination;
        ItemDefinition requestedItem = item;
        int quantity = 1;
        switch (invalid)
        {
            case "self": target = source; break;
            case "null destination": target = null; break;
            case "disabled destination": destination.enabled = false; break;
            case "inactive destination": destination.gameObject.SetActive(false); break;
            case "disabled source": source.enabled = false; break;
            case "inactive source": source.gameObject.SetActive(false); break;
            case "null item": requestedItem = null; break;
            case "missing item": requestedItem = Item(); break;
            case "zero quantity": quantity = 0; break;
            case "negative quantity": quantity = -1; break;
        }
        int notifications = 0;
        source.Changed += () => notifications++;
        destination.Changed += () => notifications++;
        Assert.That(source.TransferTo(target, requestedItem, quantity), Is.Zero);
        Assert.That(source.Count(item), Is.EqualTo(5));
        Assert.That(destination.Count(item), Is.Zero);
        Assert.That(notifications, Is.Zero);
    }

    [Test]
    public void TransferAll_RepeatedStacksAndDifferentItemsMoveExactlyOnce()
    {
        var source = Inventory();
        var destination = Inventory();
        var ore = Item();
        var seed = Item();
        source.Add(ore, 47);
        source.Add(seed, 3);
        System.Action conserved = () =>
        {
            Assert.That(source.Count(ore) + destination.Count(ore), Is.EqualTo(47));
            Assert.That(source.Count(seed) + destination.Count(seed), Is.EqualTo(3));
        };
        source.Changed += conserved;
        destination.Changed += conserved;
        Assert.That(source.TransferAllTo(destination), Is.EqualTo(50));
        Assert.That(source.Stacks, Is.Empty);
        Assert.That(destination.Count(ore), Is.EqualTo(47));
        Assert.That(destination.Count(seed), Is.EqualTo(3));
        Assert.That(source.TransferAllTo(destination), Is.Zero);
        Assert.That(destination.TransferTo(source, ore, 100), Is.EqualTo(47));
        Assert.That(destination.Count(seed), Is.EqualTo(3));
    }

    [Test]
    public void TransferAll_PartialCapacityPreservesRepeatedStackRemainders()
    {
        var source = Inventory();
        var destination = Inventory(2);
        var ore = Item();
        var seed = Item();
        source.Add(ore, 47);
        source.Add(seed, 3);
        destination.Add(ore, 18);
        Assert.That(source.TransferAllTo(destination), Is.EqualTo(22));
        Assert.That(destination.Count(ore), Is.EqualTo(40));
        Assert.That(source.Count(ore), Is.EqualTo(25));
        Assert.That(source.Count(seed), Is.EqualTo(3));
        Assert.That(destination.Count(seed), Is.Zero);
    }

    [TestCase("self")]
    [TestCase("null")]
    [TestCase("disabled")]
    public void TransferAll_InvalidDestinationLeavesContentsIntact(string invalid)
    {
        var source = Inventory();
        var destination = Inventory();
        var item = Item();
        source.Add(item, 47);
        destination.enabled = false;
        int notifications = 0;
        source.Changed += () => notifications++;
        destination.Changed += () => notifications++;
        var target = invalid == "self" ? source : invalid == "null" ? null : destination;
        Assert.That(source.TransferAllTo(target), Is.Zero);
        Assert.That(source.Count(item), Is.EqualTo(47));
        Assert.That(destination.Stacks, Is.Empty);
        Assert.That(notifications, Is.Zero);
    }

    [Test]
    public void ReducingCapacity_PreservesItemsAndAllowsOnlyExistingStackSpace()
    {
        var inventory = Inventory();
        var item = Item();
        inventory.Add(item, 25);
        JsonUtility.FromJsonOverwrite("{\"capacity\":1}", inventory);
        Assert.That(inventory.Count(item), Is.EqualTo(25));
        Assert.That(inventory.UsedSlots, Is.EqualTo(2));
        Assert.That(inventory.HasSpaceFor(item), Is.True);
        Assert.That(inventory.HasSpaceFor(Item()), Is.False);
        Assert.That(inventory.Add(item, 20), Is.EqualTo(15));
        Assert.That(inventory.Count(item), Is.EqualTo(40));
        Assert.That(inventory.Add(item, 1), Is.Zero);
    }

    [TestCase(-1)]
    [TestCase(0)]
    public void InvalidCapacity_IsClampedAtRuntimeAndDuringValidation(int configured)
    {
        var inventory = Inventory();
        JsonUtility.FromJsonOverwrite("{\"capacity\":" + configured + "}", inventory);
        Assert.That(inventory.Capacity, Is.EqualTo(1));
        Invoke(inventory, "OnValidate");
        Assert.That(JsonUtility.ToJson(inventory), Does.Contain("\"capacity\":1"));
    }

    [Test]
    public void Serialization_RoundTripsStackReferencesAndQuantities()
    {
        var original = Inventory(4);
        var restored = Inventory();
        var first = Item();
        var second = Item();
        first.name = second.name = "Same asset name";
        original.Add(first, 47);
        original.Add(second, 3);
        JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(original), restored);
        Assert.That(restored.Capacity, Is.EqualTo(4));
        AssertStack(restored, 0, first, 20);
        AssertStack(restored, 1, first, 20);
        AssertStack(restored, 2, first, 7);
        AssertStack(restored, 3, second, 3);
        restored.TryRemove(first, 1);
        Assert.That(restored.Count(first), Is.EqualTo(46));
        Assert.That(original.Count(first), Is.EqualTo(47));
    }

    [Test]
    public void Stacks_ExposesALiveReadOnlyView()
    {
        var inventory = Inventory();
        var view = inventory.Stacks;
        inventory.Add(Item(), 1);
        Assert.That(view.Count, Is.EqualTo(1));
        Assert.Throws<System.NotSupportedException>(() => ((IList<InventoryStack>)view).Clear());
        Assert.That(inventory.UsedSlots, Is.EqualTo(1));
    }

    private static void AssertStack(SimInventory inventory, int index, ItemDefinition item, int quantity)
    {
        Assert.That(inventory.Stacks[index].Item, Is.SameAs(item));
        Assert.That(inventory.Stacks[index].Quantity, Is.EqualTo(quantity));
    }
}

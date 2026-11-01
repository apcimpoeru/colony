using NUnit.Framework;
using UnityEngine;

public sealed class WorldItemTests : SimulationTestFixture
{
    [Test]
    public void PickupPoint_DefaultsToObjectAndUsesAssignedTransform()
    {
        var world = Loose(Food());
        Assert.That(world.Position, Is.EqualTo(world.transform.position));
        var point = NewObject(position: new Vector3(7, 4, 2)).transform;
        Set(world, "pickupPoint", point);
        Assert.That(world.Position, Is.EqualTo(point.position));
    }

    [Test]
    public void Reservations_AreExclusiveAndOnlyOwnerCanRelease()
    {
        var first = Colonist(); var second = Colonist(); var world = Loose(Food());
        Assert.That(world.TryReserve(null), Is.False);
        Assert.That(world.TryReserve(Eating(first)), Is.True);
        Assert.That(world.TryReserve(Eating(first)), Is.True);
        Assert.That(world.TryReserve(Eating(second)), Is.False);
        world.Release(Eating(second));
        Assert.That(world.IsAvailableFor(Eating(second)), Is.False);
        world.Release(Eating(first));
        Assert.That(world.TryReserve(Eating(second)), Is.True);
    }

    [Test]
    public void NearestFood_UsesFixedNeighborhoodAndLevelThenDistance()
    {
        var sim = Colonist();
        var near = Loose(Food(1), 5); var better = Loose(Food(3), 6.5f);
        Loose(Food(5), 8); // Would qualify if the neighborhood expanded from 6.5.
        Assert.That(WorldItem.FindNearestFood(Eating(sim), Bag(sim), 2), Is.SameAs(better));
        Assert.That(WorldItem.FindNearestFood(Eating(sim), Bag(sim), 0), Is.SameAs(near));
        var equalCloser = Loose(Food(3), 6);
        Assert.That(WorldItem.FindNearestFood(Eating(sim), Bag(sim), 2), Is.SameAs(equalCloser));
        Assert.That(WorldItem.FindNearestFood(Eating(sim), Bag(sim), 2, w => w == near), Is.SameAs(near));
    }

    [Test]
    public void Search_RejectsNonFoodEmptyDisabledReservedAndUncarryableItems()
    {
        var sim = Colonist(); var other = Colonist();
        Loose(Item(), 1); Loose(Food(10, 0), 1); Loose(Food(), 1, 0);
        var disabled = Loose(Food(), 1); disabled.enabled = false;
        var reserved = Loose(Food(), 1); reserved.TryReserve(Eating(other));
        Assert.That(WorldItem.FindNearestFood(Eating(sim), Bag(sim)), Is.Null);
        var valid = Loose(Food(), 2);
        Assert.That(WorldItem.FindNearestFood(Eating(sim), Bag(sim)), Is.SameAs(valid));
        Set(Bag(sim), "capacity", 1); Bag(sim).Add(Item(), 20);
        Assert.That(WorldItem.FindNearestFood(Eating(sim), Bag(sim)), Is.Null);
    }
}

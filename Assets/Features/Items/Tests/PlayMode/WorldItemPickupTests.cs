using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class WorldItemPickupTests : SimulationTestFixture
{
    [Test]
    public void Disable_RemovesFromSearchAndClearsReservationOnReenable()
    {
        var first = Colonist(); var second = Colonist(); var world = Loose(Food());
        world.TryReserve(Eating(first)); world.enabled = false;
        Assert.That(WorldItem.FindNearestFood(Eating(first), Bag(first)), Is.Null);
        Assert.That(world.IsAvailableFor(Eating(first)), Is.False);
        world.enabled = true;
        Assert.That(world.TryReserve(Eating(second)), Is.True);
    }

    [UnityTest]
    public IEnumerator Pickup_IsHorizontalPartialAndImmediatelyUnavailableWhenEmptied()
    {
        var sim = Colonist(); sim.enabled = false; var item = Item();
        var world = Loose(item, 0.75f, 5); world.transform.position += Vector3.up * 100;
        Set(Bag(sim), "capacity", 1); Bag(sim).Add(item, 18);
        Assert.That(world.TryReserve(Eating(sim)), Is.True);
        Assert.That(world.TryPickup(Eating(sim), Bag(sim), 0.75f), Is.EqualTo(2));
        Assert.That(world.Quantity, Is.EqualTo(3)); Assert.That(world != null, Is.True);
        Bag(sim).TryRemove(item, 20);
        Assert.That(world.TryPickup(Eating(sim), Bag(sim), 0.75f), Is.EqualTo(3));
        Assert.That(world.IsAvailableFor(Eating(sim)), Is.False);
        Assert.That(world.TryPickup(Eating(sim), Bag(sim), 0.75f), Is.Zero);
        yield return null;
        Assert.That(world == null, Is.True);
        Assert.That(Bag(sim).Count(item), Is.EqualTo(3));
    }

    [TestCase("unreserved")]
    [TestCase("other carrier")]
    [TestCase("out of range")]
    [TestCase("null carrier")]
    [TestCase("null sim")]
    public void InvalidPickup_DoesNotChangeContents(string reason)
    {
        var sim = Colonist(); var other = Colonist(); var item = Item(); var world = Loose(item, 1, 5);
        var eater = Eating(sim); var bag = Bag(sim);
        if (reason != "unreserved") world.TryReserve(eater);
        if (reason == "other carrier") bag = Bag(other);
        if (reason == "null carrier") bag = null;
        if (reason == "null sim") eater = null;
        Assert.That(world.TryPickup(eater, bag, reason == "out of range" ? 0.5f : 1f), Is.Zero);
        Assert.That(world.Quantity, Is.EqualTo(5)); Assert.That(Bag(sim).UsedSlots, Is.Zero);
    }

    [Test]
    public void DestroyingReservedTarget_AllowsAnotherTargetAndCancellation()
    {
        var sim = Colonist(); var second = Colonist(); Needs(sim).Hunger = 80;
        var world = Loose(Food()); Eating(sim).GetDestination();
        Object.DestroyImmediate(world.gameObject); Eating(sim).Cancel();
        var replacement = Loose(Food());
        Assert.That(replacement.TryReserve(Eating(second)), Is.True);
        Assert.That(Eating(sim).HasTarget, Is.False);
    }
}

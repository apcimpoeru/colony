using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class SimEatingCycleTests : SimulationTestFixture
{
    private SimEating Eater()
    {
        var eater = NewObject().AddComponent<SimEating>();
        eater.GetComponent<Rigidbody>().useGravity = false;
        Set(eater.GetComponent<SimNeeds>(), "hungerIncreasePerSecond", 0f);
        Set(eater.GetComponent<SimNeeds>(), "energyDecreasePerSecond", 0f);
        eater.GetComponent<SimNeeds>().Hunger = 80;
        return eater;
    }

    [UnityTest]
    public IEnumerator Bites_ConsumeOneItemClampHungerAndRespectScaledCooldownAndPause()
    {
        var eater = Eater(); var bag = eater.GetComponent<SimInventory>();
        var needs = eater.GetComponent<SimNeeds>(); var food = Food(); bag.Add(food, 5);
        yield return Until(() => bag.Count(food) == 4, "First bite did not occur");
        Assert.That(needs.Hunger, Is.EqualTo(65)); Assert.That(eater.IsEating, Is.True);
        Time.timeScale = 0; yield return null; yield return null;
        Assert.That(bag.Count(food), Is.EqualTo(4)); Assert.That(needs.Hunger, Is.EqualTo(65));
        Time.timeScale = 1;
        float before = Time.time;
        yield return Until(() => Time.time - before >= 0.2f, "Time did not resume");
        Assert.That(bag.Count(food), Is.EqualTo(4));
        Time.timeScale = 5;
        yield return Until(() => bag.Count(food) < 4, "Cooldown never allowed another bite");
        Assert.That(bag.Count(food), Is.EqualTo(3)); Assert.That(needs.Hunger, Is.EqualTo(50));
        bag.TryRemove(food, 3); var largeMeal = Food(5, 100); bag.Add(largeMeal, 2);
        yield return Until(() => needs.Hunger == 0, "Large meal did not clamp hunger");
        Assert.That(bag.Count(largeMeal), Is.EqualTo(1));
        Assert.That(eater.WantsFood, Is.False);
    }

    [UnityTest]
    public IEnumerator RestingAndDisabledEating_DoNotConsumeCarriedFood()
    {
        var eater = Eater(); var bag = eater.GetComponent<SimInventory>(); var meal = Food(); bag.Add(meal, 3);
        var needs = eater.GetComponent<SimNeeds>(); Invoke(needs, "SetResting", true);
        yield return null; yield return null;
        Assert.That(bag.Count(meal), Is.EqualTo(3));
        Invoke(needs, "SetResting", false); eater.enabled = false;
        yield return null; yield return null;
        Assert.That(bag.Count(meal), Is.EqualTo(3));
    }

    [TestCase("manual")]
    [TestCase("disable eating")]
    [TestCase("disable controller")]
    [TestCase("cancel")]
    [TestCase("sleep")]
    public void Cancellation_ReleasesFoodReservations(string reason)
    {
        var sim = Colonist(); var other = Colonist(); Needs(sim).Hunger = 80;
        var loose = Loose(Food()); Eating(sim).GetDestination();
        Assert.That(loose.IsAvailableFor(Eating(other)), Is.False);
        switch (reason)
        {
            case "manual": sim.destinationOverride = NewObject(); Invoke(sim, "Update"); break;
            case "disable eating": Eating(sim).enabled = false; break;
            case "disable controller": sim.enabled = false; break;
            case "cancel": Eating(sim).Cancel(); break;
            case "sleep": Needs(sim).Hunger = 60; Needs(sim).Energy = 0; Invoke(sim, "Update"); break;
        }
        Assert.That(Eating(sim).HasTarget, Is.False);
        Assert.That(loose.TryReserve(Eating(other)), Is.True);
    }

    [UnityTest]
    public IEnumerator SearchInterval_TimeoutRetryAndDisappearingTargetsAreBounded()
    {
        var eater = Eater(); var bag = eater.GetComponent<SimInventory>();
        Assert.That(eater.GetDestination(), Is.Null); // Starts the search interval with no food.
        var first = Loose(Food(), 5);
        Assert.That(eater.GetDestination(), Is.Null);
        Set(eater, "nextSearchTime", 0f);
        Assert.That(eater.GetDestination().HasValue, Is.True);
        Set(eater, "noProgressTimeout", 0.1f); Set(eater, "unreachableRetryDelay", 0.2f);
        Set(eater, "lastProgressTime", Time.time - 1f);
        Assert.That(eater.GetDestination(), Is.Null);
        Assert.That(first.IsAvailableFor(eater), Is.True);
        Assert.That(eater.HasFoodSource(), Is.False);
        // The ignored source is available to another colonist immediately.
        var other = Eater(); Assert.That(first.TryReserve(other), Is.True); first.Release(other);
        yield return Until(() => eater.HasFoodSource(), "Ignored source was not retried");
        Set(eater, "nextSearchTime", 0f); eater.GetDestination();
        Assert.That(eater.CurrentTarget, Is.SameAs(first));
        Object.DestroyImmediate(first.gameObject);
        var replacement = Loose(Food(), 7);
        Set(eater, "nextSearchTime", 0f); eater.GetDestination();
        Assert.That(eater.CurrentTarget, Is.SameAs(replacement));
        Assert.That(bag.UsedSlots, Is.Zero);
    }

    [UnityTest]
    public IEnumerator AutomaticSeeking_PicksUpLooseFoodAndEatsIt()
    {
        var sim = Colonist(true); Needs(sim).Hunger = 80; var meal = Food();
        var world = Loose(meal, 1.5f, 3);
        yield return Until(() => Needs(sim).Hunger < 80, "Colonist never picked up and ate loose food");
        Assert.That(world == null, Is.True);
        Assert.That(Bag(sim).Count(meal), Is.EqualTo(2));
        Assert.That(Eating(sim).HasTarget, Is.False);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void BlockedSources_AreIgnoredAndFallbackDoesNotKeepTheirReservation(bool stored)
    {
        var eater = Eater(); var preferred = Food(3);
        ItemStorage storage = null; WorldItem world = null;
        if (stored) { storage = Storage(5); storage.Inventory.Add(preferred, 2); }
        else world = Loose(preferred, 5);
        var fallback = Loose(Food(), 6);
        eater.GetDestination();
        Assert.That(eater.CurrentStorage != null, Is.EqualTo(stored));
        Set(eater, "lastProgressTime", Time.time - 100f);
        Assert.That(eater.GetDestination(), Is.Null);
        eater.GetDestination();
        Assert.That(eater.CurrentTarget, Is.SameAs(fallback));
        if (world != null) Assert.That(world.TryReserve(Eater()), Is.True);
        if (storage != null) Assert.That(storage.Inventory.Count(preferred), Is.EqualTo(2));
    }
}

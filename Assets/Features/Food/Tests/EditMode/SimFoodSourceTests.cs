using NUnit.Framework;
using UnityEngine;

public sealed class SimFoodSourceTests : SimulationTestFixture
{
    private SimController Hungry()
    {
        var sim = Colonist(); Needs(sim).Hunger = 80; return sim;
    }

    [TestCase(true)]
    [TestCase(false)]
    public void SharedNeighborhood_PrefersHigherLevelWithoutChaining(bool nearestIsStorage)
    {
        var sim = Hungry(); var low = Food(1); var high = Food(3);
        if (nearestIsStorage) Storage(5).Inventory.Add(low, 2); else Loose(low, 5);
        var chosen = Storage(6.5f); chosen.Inventory.Add(high, 2);
        Loose(Food(5), 8);
        Eating(sim).GetDestination();
        Assert.That(Eating(sim).CurrentStorage, Is.SameAs(chosen));
    }

    [Test]
    public void EqualLevels_PreferDistanceThenLowerPositiveNourishment()
    {
        var sim = Hungry();
        Storage(6).Inventory.Add(Food(2, 1), 2);
        var highNourishment = Storage(5); highNourishment.Inventory.Add(Food(2, 30), 2);
        var lowNourishment = Storage(5); lowNourishment.Inventory.Add(Food(2, 10), 2);
        Eating(sim).GetDestination();
        Assert.That(Eating(sim).CurrentStorage, Is.SameAs(lowNourishment));
    }

    [Test]
    public void StoragePickup_WithdrawsOneMealAndClearsTarget()
    {
        var sim = Hungry(); var meal = Food(3); var storage = Storage(5);
        storage.Inventory.Add(meal, 2);
        Eating(sim).GetDestination(); sim.transform.position = storage.Position;
        Eating(sim).GetDestination();
        Assert.That(Bag(sim).Count(meal), Is.EqualTo(1));
        Assert.That(storage.Inventory.Count(meal), Is.EqualTo(1));
        Assert.That(Eating(sim).HasTarget, Is.False);
        Needs(sim).Hunger = 31; Assert.That(Eating(sim).WantsFood, Is.True);
        Needs(sim).Hunger = 30; Assert.That(Eating(sim).WantsFood, Is.False);
        Needs(sim).Hunger = 59; Assert.That(Eating(sim).WantsFood, Is.False);
    }

    [TestCase("disabled storage")]
    [TestCase("disabled inventory")]
    [TestCase("destroyed")]
    [TestCase("empty")]
    public void UnavailableStorage_FallsBackToLooseFood(string cause)
    {
        var sim = Hungry(); var meal = Food(3); var storage = Storage(5);
        storage.Inventory.Add(meal, 2); var fallback = Loose(Food(), 6);
        Eating(sim).GetDestination();
        Assert.That(Eating(sim).CurrentStorage, Is.SameAs(storage));
        switch (cause)
        {
            case "disabled storage": storage.enabled = false; break;
            case "disabled inventory": storage.Inventory.enabled = false; break;
            case "destroyed": Object.DestroyImmediate(storage.gameObject); break;
            case "empty": storage.Inventory.TryRemove(meal, 2); break;
        }
        Set(Eating(sim), "nextSearchTime", 0f); Eating(sim).GetDestination();
        Assert.That(Eating(sim).CurrentTarget, Is.SameAs(fallback));
    }

    [Test]
    public void Searches_ExcludeZeroNourishmentFullBagsAndReservedLooseFood()
    {
        var sim = Hungry(); var other = Hungry();
        Storage(1).Inventory.Add(Food(9, 0), 5);
        var loose = Loose(Food(), 3); loose.TryReserve(Eating(other));
        Assert.That(Eating(sim).HasFoodSource(), Is.False);
        loose.Release(Eating(other));
        Assert.That(Eating(sim).HasFoodSource(), Is.True);
        Set(Bag(sim), "capacity", 1); Bag(sim).Add(Item(), 20);
        Assert.That(Eating(sim).HasFoodSource(), Is.False);
    }
}

using NUnit.Framework;

public sealed class SimEatingTests : SimulationTestFixture
{
    [Test]
    public void Appetite_HasInclusiveStartStopAndHysteresis()
    {
        var sim = Colonist();
        Needs(sim).Hunger = 59; Assert.That(Eating(sim).WantsFood, Is.False);
        Needs(sim).Hunger = 60; Assert.That(Eating(sim).WantsFood, Is.True);
        Needs(sim).Hunger = 31; Assert.That(Eating(sim).WantsFood, Is.True);
        Needs(sim).Hunger = 30; Assert.That(Eating(sim).WantsFood, Is.False);
        Needs(sim).Hunger = 59; Assert.That(Eating(sim).WantsFood, Is.False);
    }

    [Test]
    public void CarriedFood_PrefersLevelThenLowestPositiveNourishmentThenInventoryOrder()
    {
        var sim = Colonist(); var bag = Bag(sim);
        bag.Add(Item(), 1); bag.Add(Food(10, 0), 1); bag.Add(Food(1, 1), 1);
        var big = Food(3, 20); var small = Food(3, 5); var tie = Food(3, 5);
        bag.Add(big, 1); bag.Add(small, 1); bag.Add(tie, 1);
        Assert.That(Invoke(Eating(sim), "FindPreferredFood"), Is.SameAs(small));
        bag.TryRemove(small, 1);
        Assert.That(Invoke(Eating(sim), "FindPreferredFood"), Is.SameAs(tie));
    }

    [Test]
    public void CarriedFood_CancelsSeekingAndReleasesReservedWorldItem()
    {
        var sim = Colonist(); var other = Colonist(); Needs(sim).Hunger = 80;
        var world = Loose(Food()); Eating(sim).GetDestination();
        Assert.That(world.IsAvailableFor(Eating(other)), Is.False);
        Bag(sim).Add(Food(), 1); Eating(sim).GetDestination();
        Assert.That(Eating(sim).HasTarget, Is.False);
        Assert.That(world.TryReserve(Eating(other)), Is.True);
    }
}

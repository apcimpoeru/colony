using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class SimControllerIntegrationTests : SimulationTestFixture
{
    [UnityTest]
    public IEnumerator CriticalNeeds_CarriedFoodResolvesHungerBeforeRecoveringEnergy()
    {
        yield return CriticalNeeds(false);
    }

    [UnityTest]
    public IEnumerator CriticalNeeds_PantryFoodResolvesHungerBeforeRecoveringEnergy()
    {
        yield return CriticalNeeds(true);
    }

    private IEnumerator CriticalNeeds(bool pantry)
    {
        var sim = Colonist(true); Needs(sim).Hunger = 100; Needs(sim).Energy = 0;
        // Preserve the legacy scenarios' default decay and recovery rates.
        Set(Needs(sim), "hungerIncreasePerSecond", 1f); Set(Needs(sim), "energyDecreasePerSecond", 0.5f);
        var meal = Food(); var bed = Bed(sim, -2);
        if (pantry) Storage(2).Inventory.Add(meal, 20); else Bag(sim).Add(meal, 4);
        Time.timeScale = 5;
        bool ate = false; int sleepTrips = 0; bool wasGoingToBed = false;
        float deadline = Time.realtimeSinceStartup + 12f;
        while (Needs(sim).Energy < 20 && Time.realtimeSinceStartup < deadline)
        {
            ate |= Needs(sim).Hunger < 85;
            var sleep = Sleeping(sim);
            if (!ate)
            {
                Assert.That(sleep.IsSleeping, Is.False, "Slept before resolving critical hunger");
                Assert.That(sleep.CurrentBed, Is.Null, "Started a bed trip before resolving critical hunger");
            }
            bool goingToBed = sleep.CurrentBed != null;
            if (goingToBed && !wasGoingToBed) sleepTrips++;
            wasGoingToBed = goingToBed;
            yield return null;
        }
        Assert.That(ate, Is.True); Assert.That(Needs(sim).Energy, Is.GreaterThanOrEqualTo(20));
        Assert.That(sleepTrips, Is.EqualTo(1), "Alternated between food and bed trips");
        Assert.That(Sleeping(sim).CurrentBed, Is.SameAs(bed));
    }

    [UnityTest]
    public IEnumerator ManualDestination_TracksMovesAndClearingDestroyingOrDisablingStopsMovement()
    {
        var sim = Colonist(true); var destination = NewObject(position: Vector3.right * 2);
        sim.destinationOverride = destination;
        yield return Until(() => sim.GetComponent<Rigidbody>().velocity.x > 0, "Did not follow manual target");
        Assert.That(sim.ActivityLabel, Is.EqualTo("Moving to destination"));
        destination.transform.position = Vector3.left * 2;
        yield return Until(() => sim.GetComponent<Rigidbody>().velocity.x < 0, "Did not follow moved target");
        sim.destinationOverride = null; yield return null;
        Assert.That(sim.GetComponent<Rigidbody>().velocity.x, Is.Zero);
        sim.destinationOverride = destination; yield return null;
        Object.DestroyImmediate(destination); yield return null;
        Assert.That(sim.Destination, Is.Null); Assert.That(sim.GetComponent<Rigidbody>().velocity.x, Is.Zero);
        sim.destinationOverride = NewObject(position: Vector3.right * 10); yield return null;
        sim.enabled = false;
        Assert.That(sim.GetComponent<Rigidbody>().velocity.x, Is.Zero);
        Assert.That(sim.ActivityLabel, Is.EqualTo("Controller disabled"));
    }

    [UnityTest]
    public IEnumerator ManualDestination_CancelsStorageAndRest()
    {
        var sim = Colonist(true); var item = Item(); Bag(sim).Add(item, 3);
        sim.StorageTransfer.Request(Storage(), SimStorageTransfer.TransferDirection.DepositAll);
        Needs(sim).Energy = 0;
        yield return Until(() => Sleeping(sim).IsSleeping, "Did not start resting");
        sim.destinationOverride = NewObject(position: Vector3.right * 5);
        yield return null;
        Assert.That(sim.StorageTransfer.HasRequest, Is.False);
        Assert.That(Sleeping(sim).IsSleeping, Is.False); Assert.That(Needs(sim).IsResting, Is.False);
        Assert.That(sim.ActivityDetails, Is.EqualTo("Following a manual destination."));
    }
}

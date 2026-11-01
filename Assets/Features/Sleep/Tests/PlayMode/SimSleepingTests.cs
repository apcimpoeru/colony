using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class SimSleepingTests : SimulationTestFixture
{
    [TestCase(0.25f)]
    [TestCase(0.5f)]
    [TestCase(1f)]
    public void FloorSleep_UsesConfiguredRecoveryAndClearsRestingOnWake(float multiplier)
    {
        var sim = Colonist(); var sleep = Sleeping(sim); var needs = Needs(sim);
        Set(sleep, "floorRecoveryMultiplier", multiplier); needs.Energy = 21;
        Assert.That(sleep.Tick(1, out _), Is.False);
        needs.Energy = 20; Assert.That(sleep.Tick(1, out var destination), Is.True);
        Assert.That(destination, Is.Null); Assert.That(sleep.IsSleepingOnFloor, Is.True);
        Assert.That(needs.IsResting, Is.True);
        Assert.That(needs.Energy, Is.EqualTo(20 + 5 * multiplier));
        Assert.That(sleep.CurrentRecoveryPerSecond, Is.EqualTo(5 * multiplier));
        needs.Energy = 89; sleep.Tick(100, out _);
        Assert.That(sleep.IsSleeping, Is.False); Assert.That(needs.IsResting, Is.False);
        Assert.That(sleep.CurrentBed, Is.Null);
    }

    [Test]
    public void AssignedBed_TravelsThenUsesFullRateAndFallsBackWhenReassignedOrDisabled()
    {
        var sim = Colonist(); var sleep = Sleeping(sim); Needs(sim).Energy = 10;
        var bed = Bed(sim, 5);
        Assert.That(sleep.Tick(1, out var destination), Is.True);
        Assert.That(destination, Is.EqualTo(bed.Position)); Assert.That(sleep.IsSleeping, Is.False);
        sim.transform.position = bed.Position; sleep.Tick(1, out _);
        Assert.That(Needs(sim).Energy, Is.EqualTo(15)); Assert.That(sleep.IsSleepingOnFloor, Is.False);
        Set(bed, "assignedColonist", null); sleep.Tick(1, out _);
        Assert.That(sleep.IsSleepingOnFloor, Is.True);
        Set(bed, "assignedColonist", sim); sleep.Tick(1, out _);
        Assert.That(sleep.CurrentBed, Is.SameAs(bed));
        bed.enabled = false; sleep.Tick(1, out _);
        Assert.That(sleep.IsSleepingOnFloor, Is.True);
        bed.enabled = true; sleep.Tick(1, out _);
        Object.DestroyImmediate(bed.gameObject); sleep.Tick(1, out _);
        Assert.That(sleep.IsSleepingOnFloor, Is.True);
    }

    [Test]
    public void AssigningBedDuringFloorSleep_StartsTravelAndReleasesRestingFlag()
    {
        var sim = Colonist(); Needs(sim).Energy = 10; var sleep = Sleeping(sim);
        sleep.Tick(1, out _); Assert.That(Needs(sim).IsResting, Is.True);
        var bed = Bed(sim, 5); sleep.Tick(1, out var destination);
        Assert.That(destination, Is.EqualTo(bed.Position));
        Assert.That(Needs(sim).IsResting, Is.False); Assert.That(sleep.IsSleeping, Is.False);
    }

    [TestCase("cancel")]
    [TestCase("manual")]
    [TestCase("disable")]
    [TestCase("controller")]
    public void Interruptions_ClearSleepingAndResting(string reason)
    {
        var sim = Colonist(); Needs(sim).Energy = 10; var sleep = Sleeping(sim); sleep.Tick(1, out _);
        switch (reason)
        {
            case "cancel": sleep.Cancel(); break;
            case "manual": sim.destinationOverride = NewObject(); sleep.Tick(1, out _); break;
            case "disable": sleep.enabled = false; break;
            case "controller": sim.enabled = false; break;
        }
        Assert.That(sleep.IsSleeping, Is.False); Assert.That(Needs(sim).IsResting, Is.False);
        Assert.That(sleep.CurrentBed, Is.Null);
    }

    [UnityTest]
    public IEnumerator AutomaticSleep_TravelsRecoversWhileHungerContinuesAndPauses()
    {
        var sim = Colonist(true); Needs(sim).Energy = 10;
        Set(Needs(sim), "hungerIncreasePerSecond", 2f); var bed = Bed(sim, 1.5f);
        yield return Until(() => Sleeping(sim).IsSleeping, "Colonist did not reach assigned bed");
        Assert.That(Sleeping(sim).CurrentBed, Is.SameAs(bed));
        float hunger = Needs(sim).Hunger, energy = Needs(sim).Energy;
        yield return null; yield return null;
        Assert.That(Needs(sim).Hunger, Is.GreaterThan(hunger)); Assert.That(Needs(sim).Energy, Is.GreaterThan(energy));
        Time.timeScale = 0; yield return null;
        hunger = Needs(sim).Hunger; energy = Needs(sim).Energy;
        yield return null; yield return null;
        Assert.That(Needs(sim).Hunger, Is.EqualTo(hunger)); Assert.That(Needs(sim).Energy, Is.EqualTo(energy));
        Sleeping(sim).Tick(0, out _);
        Assert.That(Needs(sim).Energy, Is.EqualTo(energy));
    }
}

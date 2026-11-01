using System;
using NUnit.Framework;

public sealed class SimControllerTests : SimulationTestFixture
{
    private static void Activity(SimController sim, string activity)
    {
        var field = typeof(SimController).GetField("activity",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        field.SetValue(sim, Enum.Parse(field.FieldType, activity));
    }

    [TestCase(100f, 0f, "None", "Food")]
    [TestCase(85f, 0f, "Sleep", "Food")]
    [TestCase(84f, 0f, "Food", "Sleep")]
    [TestCase(99f, 0f, "Sleep", "Food")]
    [TestCase(80f, 60f, "None", "Food")]
    [TestCase(80f, 52f, "Food", "Food")]
    [TestCase(80f, 40f, "Food", "Sleep")]
    [TestCase(84f, 64f, "Sleep", "Sleep")]
    [TestCase(84f, 80f, "Sleep", "Food")]
    public void Decision_RespectsUrgencyMarginAndCriticalThresholds(
        float hunger, float energy, string previous, string expected)
    {
        var sim = Colonist(); Needs(sim).Hunger = hunger; Needs(sim).Energy = energy;
        Set(Sleeping(sim), "wantsSleep", true); Bag(sim).Add(Food(), 4); Activity(sim, previous);
        Assert.That(Invoke(sim, "ChooseActivity").ToString(), Is.EqualTo(expected));
    }

    [TestCase(false, true, "Sleep")]
    [TestCase(true, false, "Food")]
    [TestCase(false, false, "None")]
    public void Decision_FallsBackToAvailableNeed(bool food, bool sleep, string expected)
    {
        var sim = Colonist(); Needs(sim).Hunger = 100; Needs(sim).Energy = 0;
        if (food) Bag(sim).Add(Food(), 1);
        Sleeping(sim).enabled = sleep;
        Assert.That(Invoke(sim, "ChooseActivity").ToString(), Is.EqualTo(expected));
    }

    [Test]
    public void CriticalHunger_KeepsFoodSelectedDuringBiteCooldownThenYieldsToSleep()
    {
        var sim = Colonist(); Needs(sim).Hunger = 85; Needs(sim).Energy = 0;
        Bag(sim).Add(Food(), 3); Set(Eating(sim), "nextEatTime", UnityEngine.Time.time + 1f);
        Assert.That(Invoke(sim, "ChooseActivity").ToString(), Is.EqualTo("Food"));
        Needs(sim).Hunger = 84;
        Assert.That(Invoke(sim, "ChooseActivity").ToString(), Is.EqualTo("Sleep"));
    }

    [Test]
    public void ActivityReadouts_DoNotSearchReserveOrChangeDecisions()
    {
        var sim = Colonist(); var other = Colonist(); Needs(sim).Hunger = 80;
        var world = Loose(Food());
        Invoke(sim, "ChooseActivity");
        float searchTime = Get<float>(Eating(sim), "nextSearchTime");
        for (int i = 0; i < 10; i++)
        {
            Assert.That(sim.ActivityLabel, Is.EqualTo("Idle"));
            Assert.That(sim.ActivityDetails, Does.Contain("Food:"));
            Assert.That(sim.Destination, Is.Null);
        }
        Assert.That(Eating(sim).HasTarget, Is.False);
        Assert.That(Get<float>(Eating(sim), "nextSearchTime"), Is.EqualTo(searchTime));
        Assert.That(world.TryReserve(Eating(other)), Is.True);
        sim.enabled = false;
        Assert.That(sim.ActivityLabel, Is.EqualTo("Controller disabled"));
        Assert.That(sim.ActivityDetails, Is.EqualTo("Automatic decisions paused."));
    }

    [Test]
    public void ConfiguredTrip_CannotStartOutsidePlayMode()
    {
        Assert.That(Colonist().StartConfiguredStorageTransfer(), Is.False);
    }

    [TestCase("idle", "Idle", "satisfied")]
    [TestCase("manual moving", "Moving to destination", "manual destination")]
    [TestCase("manual arrived", "At destination", "manual destination")]
    [TestCase("eating", "Eating", "eating carried food")]
    [TestCase("loose food", "Seeking food", "collecting food")]
    [TestCase("stored food", "Seeking stored food", "collecting from storage")]
    [TestCase("floor", "Sleeping on floor", "on the floor")]
    [TestCase("bed", "Sleeping", "in bed")]
    [TestCase("bed travel", "Going to bed", "travelling to assigned bed")]
    public void Readouts_DescribeActualActivity(string state, string label, string detail)
    {
        var sim = Colonist();
        switch (state)
        {
            case "manual moving": sim.destinationOverride = NewObject(position: UnityEngine.Vector3.right * 5); break;
            case "manual arrived": sim.destinationOverride = NewObject(); break;
            case "eating": Set(Eating(sim), "nextEatTime", UnityEngine.Time.time + 1f); break;
            case "loose food":
                Needs(sim).Hunger = 80; Loose(Food()); Eating(sim).GetDestination(); break;
            case "stored food":
                Needs(sim).Hunger = 80; Storage().Inventory.Add(Food(), 3); Eating(sim).GetDestination(); break;
            case "floor": Needs(sim).Energy = 0; Sleeping(sim).Tick(1, out _); break;
            case "bed": Needs(sim).Energy = 0; Bed(sim); Sleeping(sim).Tick(1, out _); break;
            case "bed travel": Needs(sim).Energy = 0; Bed(sim, 5); Sleeping(sim).Tick(1, out _); break;
        }
        Invoke(sim, "ChooseActivity"); // Populate the cached availability used by details.
        Assert.That(sim.ActivityLabel, Is.EqualTo(label));
        Assert.That(sim.ActivityDetails, Does.Contain(detail));
    }
}

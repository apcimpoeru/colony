using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class SimNeedsTimeTests : SimulationTestFixture
{
    [UnityTest]
    public IEnumerator ConfiguredDecay_FollowsScaledTimeAndClamps()
    {
        var needs = NewObject().AddComponent<SimNeeds>();
        Set(needs, "hungerIncreasePerSecond", 4f); Set(needs, "energyDecreasePerSecond", 3f);
        needs.Hunger = 20; needs.Energy = 80;
        yield return null;
        float hunger = needs.Hunger, energy = needs.Energy, start = Time.time;
        yield return Until(() => Time.time - start >= 0.2f, "Scaled time did not advance");
        float elapsed = Time.time - start;
        Assert.That(needs.Hunger, Is.EqualTo(hunger + elapsed * 4).Within(0.015f));
        Assert.That(needs.Energy, Is.EqualTo(energy - elapsed * 3).Within(0.015f));
        needs.Hunger = 99.99f; needs.Energy = 0.01f;
        yield return Until(() => needs.Hunger == 100 && needs.Energy == 0,
            "Decay did not clamp at the need boundaries");
        Assert.That(needs.Hunger, Is.EqualTo(100)); Assert.That(needs.Energy, Is.Zero);
    }

    [UnityTest]
    public IEnumerator Resting_ContinuesHungerButPausesEnergyDecay()
    {
        var needs = NewObject().AddComponent<SimNeeds>(); needs.Energy = 50;
        Invoke(needs, "SetResting", true);
        yield return null;
        float hunger = needs.Hunger;
        yield return null; yield return null;
        Assert.That(needs.Hunger, Is.GreaterThan(hunger));
        Assert.That(needs.Energy, Is.EqualTo(50));
        Invoke(needs, "SetResting", false);
        yield return null;
        Assert.That(needs.Energy, Is.LessThan(50));
    }

    [UnityTest]
    public IEnumerator PauseAndDisable_StopDecay()
    {
        var needs = NewObject().AddComponent<SimNeeds>(); needs.Hunger = needs.Energy = 50;
        Time.timeScale = 0; yield return null;
        float hunger = needs.Hunger, energy = needs.Energy;
        yield return null; yield return null;
        Assert.That(needs.Hunger, Is.EqualTo(hunger)); Assert.That(needs.Energy, Is.EqualTo(energy));
        needs.enabled = false; Time.timeScale = 1;
        yield return null; yield return null;
        Assert.That(needs.Hunger, Is.EqualTo(hunger)); Assert.That(needs.Energy, Is.EqualTo(energy));
    }
}

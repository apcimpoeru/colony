using NUnit.Framework;

public sealed class SimNeedsTests : SimulationTestFixture
{
    [TestCase(-100f, 0f)]
    [TestCase(0f, 0f)]
    [TestCase(37.5f, 37.5f)]
    [TestCase(100f, 100f)]
    [TestCase(150f, 100f)]
    public void NeedSetters_ClampToValidRange(float input, float expected)
    {
        var needs = NewObject().AddComponent<SimNeeds>();
        needs.Hunger = needs.Energy = input;
        Assert.That(needs.Hunger, Is.EqualTo(expected));
        Assert.That(needs.Energy, Is.EqualTo(expected));
    }

    [Test]
    public void Validation_ClampsSerializedValuesAndNegativeRates()
    {
        var needs = NewObject().AddComponent<SimNeeds>();
        Set(needs, "hunger", -10f); Set(needs, "energy", 120f);
        Set(needs, "hungerIncreasePerSecond", -2f); Set(needs, "energyDecreasePerSecond", -3f);
        Invoke(needs, "OnValidate");
        Assert.That(needs.Hunger, Is.Zero); Assert.That(needs.Energy, Is.EqualTo(100));
        Assert.That(Get<float>(needs, "hungerIncreasePerSecond"), Is.Zero);
        Assert.That(Get<float>(needs, "energyDecreasePerSecond"), Is.Zero);
    }
}

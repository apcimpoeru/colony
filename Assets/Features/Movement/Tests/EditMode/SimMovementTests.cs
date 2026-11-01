using NUnit.Framework;
using UnityEngine;

public sealed class SimMovementTests : SimulationTestFixture
{
    [TestCase(0f, true)]
    [TestCase(0.2f, true)]
    [TestCase(0.201f, false)]
    [TestCase(-0.201f, false)]
    public void Arrival_UsesHorizontalDistanceAndInclusiveBoundary(float x, bool expected)
    {
        var movement = NewObject().AddComponent<SimMovement>();
        Assert.That(movement.IsAt(new Vector3(x, 100, 0)), Is.EqualTo(expected));
    }
}

using NUnit.Framework;
using UnityEngine;

public sealed class SimBedTests : SimulationTestFixture
{
    [Test]
    public void AssignedBeds_ArePrivateAndNearestActiveAssignmentWins()
    {
        var sim = Colonist(); var other = Colonist();
        Bed(null, 0); Bed(other, 1); var far = Bed(sim, 5); var near = Bed(sim, 2);
        Assert.That(SimBed.FindAssignedBed(null), Is.Null);
        Assert.That(SimBed.FindAssignedBed(sim), Is.SameAs(near));
        Assert.That(near.IsAssignedTo(other), Is.False);
        near.enabled = false;
        Assert.That(SimBed.FindAssignedBed(sim), Is.SameAs(far));
        Object.DestroyImmediate(far.gameObject);
        Assert.That(SimBed.FindAssignedBed(sim), Is.Null);
        near.enabled = true; Set(near, "assignedColonist", other);
        Assert.That(SimBed.FindAssignedBed(sim), Is.Null);
        Assert.That(near.IsAssignedTo(other), Is.True);
    }

    [Test]
    public void SleepPoint_DefaultsToTransformAndControlsSearchDistance()
    {
        var sim = Colonist(); var bed = Bed(sim, 5); var other = Bed(sim, 3);
        Assert.That(bed.Position, Is.EqualTo(bed.transform.position));
        var point = NewObject(position: Vector3.right).transform;
        Set(bed, "sleepPoint", point);
        Assert.That(bed.Position, Is.EqualTo(point.position));
        Assert.That(SimBed.FindAssignedBed(sim), Is.SameAs(bed));
    }
}

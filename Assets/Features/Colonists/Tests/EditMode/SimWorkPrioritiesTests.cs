using System;
using NUnit.Framework;
using UnityEngine;

public sealed class SimWorkPrioritiesTests
{
    private static SimWorkPriorities Priorities(int mining, int farming)
        => JsonUtility.FromJson<SimWorkPriorities>("{\"mining\":" + mining + ",\"farming\":" + farming + "}");

    [Test]
    public void Defaults_AreNormalWithMiningWinningTie()
    {
        var priorities = new SimWorkPriorities();
        Assert.That(priorities.GetPriority(SimWorkType.Mining), Is.EqualTo(3));
        Assert.That(priorities.GetPriority(SimWorkType.Farming), Is.EqualTo(3));
        Assert.That(priorities.ChooseAvailableWorkType(_ => true), Is.EqualTo(SimWorkType.Mining));
    }

    [Test, Combinatorial]
    public void Selection_CoversAllPrioritiesAndAvailability(
        [NUnit.Framework.Range(0, 4)] int mining, [NUnit.Framework.Range(0, 4)] int farming,
        [Values(false, true)] bool miningAvailable, [Values(false, true)] bool farmingAvailable)
    {
        var priorities = Priorities(mining, farming);
        Assert.That(priorities.GetPriority(SimWorkType.Mining), Is.EqualTo(mining));
        Assert.That(priorities.GetPriority(SimWorkType.Farming), Is.EqualTo(farming));
        bool canMine = mining > 0 && miningAvailable;
        bool canFarm = farming > 0 && farmingAvailable;
        SimWorkType? expected = null;
        if (canMine && canFarm)
            expected = mining <= farming ? SimWorkType.Mining : SimWorkType.Farming;
        else if (canMine)
            expected = SimWorkType.Mining;
        else if (canFarm)
            expected = SimWorkType.Farming;

        Assert.That(priorities.ChooseAvailableWorkType(type =>
            type == SimWorkType.Mining ? miningAvailable : farmingAvailable), Is.EqualTo(expected));
    }

    [TestCase(int.MinValue)]
    [TestCase(-1)]
    [TestCase(5)]
    [TestCase(int.MaxValue)]
    public void InvalidSerializedPriority_IsDisabledAndFallsBackToOtherWork(int invalid)
    {
        var invalidMining = Priorities(invalid, 4);
        Assert.That(invalidMining.GetPriority(SimWorkType.Mining), Is.Zero);
        Assert.That(invalidMining.ChooseAvailableWorkType(_ => true), Is.EqualTo(SimWorkType.Farming));
        var invalidFarming = Priorities(4, invalid);
        Assert.That(invalidFarming.GetPriority(SimWorkType.Farming), Is.Zero);
        Assert.That(invalidFarming.ChooseAvailableWorkType(_ => true), Is.EqualTo(SimWorkType.Mining));
        Assert.That(Priorities(invalid, invalid).ChooseAvailableWorkType(_ => true), Is.Null);
    }

    [Test]
    public void DisabledWork_DoesNotQueryAvailability()
    {
        Assert.That(Priorities(0, 0).ChooseAvailableWorkType(_ =>
            throw new AssertionException("Disabled work must not query availability")), Is.Null);
    }

    [TestCase(-1)]
    [TestCase(2)]
    [TestCase(int.MaxValue)]
    public void UnknownWorkType_IsRejected(int value)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SimWorkPriorities().GetPriority((SimWorkType)value));
        Assert.That(exception.ParamName, Is.EqualTo("workType"));
    }

    [TestCase(0)]
    [TestCase(3)]
    public void NullAvailability_IsRejectedEvenWhenWorkIsDisabled(int priority)
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            Priorities(priority, priority).ChooseAvailableWorkType(null));
        Assert.That(exception.ParamName, Is.EqualTo("hasAvailableTask"));
    }
}

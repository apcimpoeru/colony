using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class SimInventoryInitializationTests
{
    private readonly List<Object> objects = new List<Object>();
    private float originalTimeScale;

    [SetUp]
    public void SetUp() => originalTimeScale = Time.timeScale;

    [TearDown]
    public void TearDown()
    {
        for (int i = objects.Count - 1; i >= 0; i--)
            if (objects[i] != null)
                Object.DestroyImmediate(objects[i]);
        objects.Clear();
        Time.timeScale = originalTimeScale;
    }

    [Test]
    public void Awake_RepacksOversizedAndRepeatedStacksUsingAssetIdentity()
    {
        var first = Item();
        var second = Item();
        first.name = second.name = "Same name";
        var inventory = InactiveInventory(4,
            Stack(first, 23) + "," + Stack(first, 2) + "," + Stack(second, 3));
        inventory.gameObject.SetActive(true);
        Assert.That(inventory.UsedSlots, Is.EqualTo(3));
        Assert.That(inventory.Count(first), Is.EqualTo(25));
        Assert.That(inventory.Count(second), Is.EqualTo(3));
        Assert.That(inventory.Stacks[0].Quantity, Is.EqualTo(20));
        Assert.That(inventory.Stacks[1].Quantity, Is.EqualTo(5));
        Assert.That(inventory.Stacks[2].Item, Is.SameAs(second));
    }

    [Test]
    public void Awake_PreservesOverflowWarnsAndRefusesAdditionalSlots()
    {
        var item = Item();
        var inventory = InactiveInventory(1, Stack(item, 47));
        LogAssert.Expect(LogType.Warning,
            "Temporary initialization test: initial contents exceed capacity; items were preserved. Remove items before adding new stacks.");
        inventory.gameObject.SetActive(true);
        Assert.That(inventory.Count(item), Is.EqualTo(47));
        Assert.That(inventory.UsedSlots, Is.EqualTo(3));
        Assert.That(inventory.Add(Item(), 1), Is.Zero);
        Assert.That(inventory.Add(item, 20), Is.EqualTo(13));
        Assert.That(inventory.Count(item), Is.EqualTo(60));
        Assert.That(inventory.Add(item, 1), Is.Zero);
    }

    [Test]
    public void Awake_DiscardsInvalidEntriesAndKeepsValidContents()
    {
        var item = Item();
        var inventory = InactiveInventory(2,
            Stack(null, 4) + "," + Stack(item, 0) + "," + Stack(item, -3) + "," + Stack(item, 5));
        inventory.gameObject.SetActive(true);
        Assert.That(inventory.Count(item), Is.EqualTo(5));
        Assert.That(inventory.UsedSlots, Is.EqualTo(1));
        Assert.That(inventory.Stacks[0].Item, Is.SameAs(item));
    }

    private ItemDefinition Item()
    {
        var item = ScriptableObject.CreateInstance<ItemDefinition>();
        objects.Add(item);
        return item;
    }

    private SimInventory InactiveInventory(int capacity, string stacks)
    {
        var go = new GameObject("Temporary initialization test");
        objects.Add(go);
        go.SetActive(false);
        var inventory = go.AddComponent<SimInventory>();
        JsonUtility.FromJsonOverwrite("{\"capacity\":" + capacity + ",\"items\":[" + stacks + "]}", inventory);
        return inventory;
    }

    private static string Stack(ItemDefinition item, int quantity)
        => "{\"item\":{\"instanceID\":" + (item == null ? 0 : item.GetInstanceID())
            + "},\"quantity\":" + quantity + "}";
}

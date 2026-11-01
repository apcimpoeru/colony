using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class ItemDefinitionTests
{
    private readonly List<Object> temporaryObjects = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        for (int i = temporaryObjects.Count - 1; i >= 0; i--)
            Object.DestroyImmediate(temporaryObjects[i]);
        temporaryObjects.Clear();
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" \t\r\n")]
    public void DisplayName_BlankNameFallsBackToAssetName(string displayName)
    {
        ItemDefinition item = CreateItem("Fallback name");
        SetDisplayName(item, displayName);

        Assert.That(item.DisplayName, Is.EqualTo("Fallback name"));
        item.name = "Renamed asset";
        Assert.That(item.DisplayName, Is.EqualTo("Renamed asset"));
    }

    [TestCase("Apple")]
    [TestCase(" Apple ")]
    public void DisplayName_ConfiguredNameIsPreserved(string displayName)
    {
        ItemDefinition item = CreateItem("Asset name");
        SetDisplayName(item, displayName);

        Assert.That(item.DisplayName, Is.EqualTo(displayName));
    }

    [Test]
    public void MaxStackSize_NewDefinitionDefaultsToTwenty()
    {
        Assert.That(CreateItem("Item").MaxStackSize, Is.EqualTo(20));
    }

    [TestCase(int.MinValue, 1)]
    [TestCase(-1, 1)]
    [TestCase(0, 1)]
    [TestCase(1, 1)]
    [TestCase(37, 37)]
    [TestCase(int.MaxValue, int.MaxValue)]
    public void MaxStackSize_UnvalidatedSerializedValueIsClamped(int configured, int expected)
    {
        ItemDefinition item = CreateItem("Item");
        // Json overwrite bypasses Inspector validation, exercising the runtime guard.
        JsonUtility.FromJsonOverwrite("{\"maxStackSize\":" + configured + "}", item);

        Assert.That(item.MaxStackSize, Is.EqualTo(expected));
    }

    [TestCase(-7, 1)]
    [TestCase(0, 1)]
    [TestCase(1, 1)]
    [TestCase(37, 37)]
    public void OnValidate_NormalizesSerializedStackSize(int configured, int expected)
    {
        ItemDefinition item = CreateItem("Item");
        JsonUtility.FromJsonOverwrite("{\"maxStackSize\":" + configured + "}", item);

        // Invoke Unity's validation callback without creating an asset on disk.
        typeof(ItemDefinition).GetMethod("OnValidate",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .Invoke(item, null);

        var serialized = new SerializedObject(item);
        Assert.That(serialized.FindProperty("maxStackSize").intValue, Is.EqualTo(expected));
    }

    [Test]
    public void SameNameDefinitions_RemainSeparateItemsInInventory()
    {
        ItemDefinition first = CreateItem("Apple");
        ItemDefinition second = CreateItem("Apple");
        SetDisplayName(first, "Shared display name");
        SetDisplayName(second, "Shared display name");
        var carrier = new GameObject("Item identity test");
        temporaryObjects.Add(carrier);
        var inventory = carrier.AddComponent<SimInventory>();

        Assert.That(first == second, Is.False);
        Assert.That(inventory.Add(first, 2), Is.EqualTo(2));
        Assert.That(inventory.Count(second), Is.Zero);
        Assert.That(inventory.Add(second, 3), Is.EqualTo(3));
        Assert.That(inventory.UsedSlots, Is.EqualTo(2));
        Assert.That(inventory.Count(first), Is.EqualTo(2));
        Assert.That(inventory.Count(second), Is.EqualTo(3));
        Assert.That(inventory.TryRemove(first, 2), Is.True);
        Assert.That(inventory.Count(first), Is.Zero);
        Assert.That(inventory.Count(second), Is.EqualTo(3));
    }

    private ItemDefinition CreateItem(string assetName)
    {
        var item = ScriptableObject.CreateInstance<ItemDefinition>();
        temporaryObjects.Add(item);
        item.name = assetName;
        return item;
    }

    private static void SetDisplayName(ItemDefinition item, string value)
    {
        var serialized = new SerializedObject(item);
        serialized.FindProperty("displayName").stringValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}

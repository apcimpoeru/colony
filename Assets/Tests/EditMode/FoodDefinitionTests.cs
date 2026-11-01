using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class FoodDefinitionTests : InventoryTestFixture
{
    [Test]
    public void NewFood_HasExpectedDefaults()
    {
        var food = Track(ScriptableObject.CreateInstance<FoodDefinition>());
        Assert.That(food.Level, Is.EqualTo(1));
        Assert.That(food.Nourishment, Is.EqualTo(15f));
        Assert.That(food.Happiness, Is.Zero);
        Assert.That(food.MaxStackSize, Is.EqualTo(20));
    }

    [TestCase(-20, -3f, -7f, 1, 0f, 0f)]
    [TestCase(0, 0f, 0f, 1, 0f, 0f)]
    [TestCase(1, 0.5f, 0.25f, 1, 0.5f, 0.25f)]
    [TestCase(4, 75f, 12f, 4, 75f, 12f)]
    public void FoodValues_ClampUnvalidatedDataAndNormalizeOnValidate(
        int level, float nourishment, float happiness, int expectedLevel,
        float expectedNourishment, float expectedHappiness)
    {
        var food = Track(ScriptableObject.CreateInstance<FoodDefinition>());
        // Invariant formatting keeps the serialized numeric input valid on any locale.
        string json = string.Format(System.Globalization.CultureInfo.InvariantCulture,
            "{{\"level\":{0},\"nourishment\":{1},\"happiness\":{2},\"maxStackSize\":0}}",
            level, nourishment, happiness);
        JsonUtility.FromJsonOverwrite(json, food);
        Assert.That(food.Level, Is.EqualTo(expectedLevel));
        Assert.That(food.Nourishment, Is.EqualTo(expectedNourishment));
        Assert.That(food.Happiness, Is.EqualTo(expectedHappiness));
        Assert.That(food.MaxStackSize, Is.EqualTo(1));

        Invoke(food, "OnValidate");
        var serialized = new SerializedObject(food);
        Assert.That(serialized.FindProperty("level").intValue, Is.EqualTo(expectedLevel));
        Assert.That(serialized.FindProperty("nourishment").floatValue, Is.EqualTo(expectedNourishment));
        Assert.That(serialized.FindProperty("happiness").floatValue, Is.EqualTo(expectedHappiness));
        Assert.That(serialized.FindProperty("maxStackSize").intValue, Is.EqualTo(1));
    }

    [TestCase("")]
    [TestCase(" \t")]
    [TestCase("Meal")]
    public void Food_InheritsDisplayNameAndStackSettings(string displayName)
    {
        var food = Track(ScriptableObject.CreateInstance<FoodDefinition>());
        food.name = "Food asset";
        var serialized = new SerializedObject(food);
        serialized.FindProperty("displayName").stringValue = displayName;
        serialized.FindProperty("maxStackSize").intValue = 3;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        Assert.That(food.DisplayName, Is.EqualTo(string.IsNullOrWhiteSpace(displayName) ? "Food asset" : displayName));
        var inventory = Inventory(1);
        Assert.That(inventory.Add(food, 5), Is.EqualTo(3));
        Assert.That(inventory.Stacks[0].Item, Is.SameAs(food));
    }

    [Test]
    public void EatingPreference_ExcludesZeroNourishmentEvenAtHigherLevelAndHappiness()
    {
        var inventory = Inventory();
        var eating = inventory.gameObject.AddComponent<SimEating>();
        Invoke(eating, "Awake");
        var emptyFood = Track(ScriptableObject.CreateInstance<FoodDefinition>());
        JsonUtility.FromJsonOverwrite("{\"level\":9,\"nourishment\":0,\"happiness\":100}", emptyFood);
        inventory.Add(emptyFood, 2);
        Assert.That(Invoke(eating, "FindPreferredFood"), Is.Null);

        var meal = Track(ScriptableObject.CreateInstance<FoodDefinition>());
        inventory.Add(meal, 1);
        Assert.That(Invoke(eating, "FindPreferredFood"), Is.SameAs(meal));
        Assert.That(inventory.Count(emptyFood), Is.EqualTo(2));
        Assert.That(emptyFood.Happiness, Is.EqualTo(100));
    }
}

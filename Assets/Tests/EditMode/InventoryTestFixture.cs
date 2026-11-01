using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public abstract class InventoryTestFixture
{
    private readonly List<Object> objects = new List<Object>();

    protected T Track<T>(T value) where T : Object
    {
        objects.Add(value);
        return value;
    }

    protected ItemDefinition Item(int stackSize = 20)
    {
        var item = Track(ScriptableObject.CreateInstance<ItemDefinition>());
        JsonUtility.FromJsonOverwrite("{\"maxStackSize\":" + stackSize + "}", item);
        return item;
    }

    protected SimInventory Inventory(int capacity = 8)
    {
        var inventory = Track(new GameObject("Temporary inventory test")).AddComponent<SimInventory>();
        var serialized = new SerializedObject(inventory);
        serialized.FindProperty("capacity").intValue = capacity;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return inventory;
    }

    protected static object Invoke(object target, string method)
        => target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(target, null);

    [TearDown]
    public void DestroyTemporaryObjects()
    {
        for (int i = objects.Count - 1; i >= 0; i--)
            if (objects[i] != null)
                Object.DestroyImmediate(objects[i]);
        objects.Clear();
    }
}

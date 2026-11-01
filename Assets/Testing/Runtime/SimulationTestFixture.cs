using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

// Shared component setup; no saved scenes, asset files, or production test hooks.
public abstract class SimulationTestFixture
{
    private readonly List<Object> objects = new List<Object>();
    private readonly List<Action> restoreRegistries = new List<Action>();
    private float timeScale, fixedDeltaTime;

    [SetUp]
    public void SetUpSimulation()
    {
        timeScale = Time.timeScale;
        fixedDeltaTime = Time.fixedDeltaTime;
        Time.timeScale = 1f;
        // Edit Mode Test Runner may have a populated user scene open. Isolate
        // searches without disabling, dirtying, or saving any of its objects.
        IsolateRegistry(typeof(WorldItem), "activeItems");
        IsolateRegistry(typeof(ItemStorage), "activeStorages");
        IsolateRegistry(typeof(SimBed), "activeBeds");
    }

    private void IsolateRegistry(Type type, string name)
    {
        object registry = type.GetField(name, BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        var snapshot = new List<Behaviour>();
        foreach (Behaviour entry in (IEnumerable)registry)
            snapshot.Add(entry);
        MethodInfo clear = registry.GetType().GetMethod("Clear");
        MethodInfo add = registry.GetType().GetMethod("Add");
        clear.Invoke(registry, null);
        restoreRegistries.Add(() =>
        {
            clear.Invoke(registry, null);
            foreach (Behaviour entry in snapshot)
                if (entry != null && entry.isActiveAndEnabled)
                    add.Invoke(registry, new object[] { entry });
        });
    }

    protected T Track<T>(T value) where T : Object { objects.Add(value); return value; }
    protected GameObject NewObject(string name = "Temporary simulation test", Vector3? position = null)
    {
        var go = Track(new GameObject(name));
        if (position.HasValue) go.transform.position = position.Value;
        return go;
    }

    protected SimController Colonist(bool automatic = false)
    {
        var go = NewObject();
        go.SetActive(false);
        var sim = go.AddComponent<SimController>();
        go.GetComponent<Rigidbody>().useGravity = false;
        Set(go.GetComponent<SimNeeds>(), "hungerIncreasePerSecond", 0f);
        Set(go.GetComponent<SimNeeds>(), "energyDecreasePerSecond", 0f);
        // Keep GUI out of behavior/physics tests; display has its own smoke suite.
        go.GetComponent<SimStatusDisplay>().enabled = false;
        go.SetActive(true);
        if (!Application.isPlaying)
            foreach (string component in new[] { "SimMovement", "SimEating", "SimSleeping", "SimController" })
                Invoke(go.GetComponent(component), "Awake");
        if (!automatic)
        {
            // Leave the controller enabled for CanSleep, but freeze time in Edit
            // Mode and let Play Mode tests explicitly disable it where needed.
            go.GetComponent<SimNeeds>().Hunger = 0;
        }
        return sim;
    }

    protected SimInventory Inventory(int capacity = 8)
    {
        var inventory = NewObject().AddComponent<SimInventory>();
        Set(inventory, "capacity", capacity);
        return inventory;
    }
    protected ItemDefinition Item()
        => Track(ScriptableObject.CreateInstance<ItemDefinition>());
    protected FoodDefinition Food(int level = 1, float nourishment = 15f)
    {
        var food = Track(ScriptableObject.CreateInstance<FoodDefinition>());
        Set(food, "level", level); Set(food, "nourishment", nourishment);
        return food;
    }
    protected WorldItem Loose(ItemDefinition item, float x = 5f, int quantity = 3)
    {
        var world = NewObject(position: new Vector3(x, 0, 0)).AddComponent<WorldItem>();
        Set(world, "item", item); Set(world, "quantity", quantity);
        if (!Application.isPlaying) Invoke(world, "OnEnable");
        return world;
    }
    protected ItemStorage Storage(float x = 5f)
    {
        var storage = NewObject(position: new Vector3(x, 0, 0)).AddComponent<ItemStorage>();
        if (!Application.isPlaying) Invoke(storage, "OnEnable");
        return storage;
    }
    protected SimBed Bed(SimController sim, float x = 0f)
    {
        var bed = NewObject(position: new Vector3(x, 0, 0)).AddComponent<SimBed>();
        Set(bed, "assignedColonist", sim);
        if (!Application.isPlaying) Invoke(bed, "OnEnable");
        return bed;
    }
    protected static void Set(object target, string field, object value)
        => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    protected static T Get<T>(object target, string field)
        => (T)target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    protected static object Invoke(object target, string method, params object[] args)
        => target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
    protected static SimEating Eating(SimController sim) => sim.GetComponent<SimEating>();
    protected static SimNeeds Needs(SimController sim) => sim.GetComponent<SimNeeds>();
    protected static SimInventory Bag(SimController sim) => sim.GetComponent<SimInventory>();
    protected static SimSleeping Sleeping(SimController sim) => sim.GetComponent<SimSleeping>();

    protected static IEnumerator Until(Func<bool> predicate, string failure, float timeout = 8f)
    {
        float deadline = Time.realtimeSinceStartup + timeout;
        while (!predicate() && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(predicate(), Is.True, failure);
    }

    [TearDown]
    public void TearDownSimulation()
    {
        try
        {
            for (int i = objects.Count - 1; i >= 0; i--)
                if (objects[i] != null) Object.DestroyImmediate(objects[i]);
        }
        finally
        {
            objects.Clear();
            foreach (Action restore in restoreRegistries) restore();
            restoreRegistries.Clear();
            Time.timeScale = timeScale;
            Time.fixedDeltaTime = fixedDeltaTime;
        }
    }
}

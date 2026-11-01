using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Uses real Unity components and temporary objects; no scene assets are changed.
public static class StorageVerification
{
    private static int assertions;

    [MenuItem("Tools/Verify Storage and Food")]
    public static void Run()
    {
        if (Object.FindObjectsOfType<ItemStorage>().Length > 0 || Object.FindObjectsOfType<WorldItem>().Length > 0)
            throw new InvalidOperationException("Run storage verification in an empty scene or an isolated batch project.");
        assertions = 0;
        CheckTransfers();
        CheckDepositAll();
        CheckOverflow();
        CheckStorageAccess();
        CheckTrips();
        CheckFoodSelection();
        CheckCriticalNeeds();
        Debug.Log($"Storage verification passed: {assertions} assertions.");
    }

    public static void RunBatch()
    {
        try { Run(); EditorApplication.Exit(0); }
        catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
    }

    private static void CheckTransfers()
    {
        using (var f = new Fixture())
        {
            ItemDefinition item = f.Item();
            SimInventory source = f.Inventory();
            SimInventory destination = f.Inventory();
            Set(destination, "capacity", 1);
            source.Add(item, 47);
            destination.Add(item, 18);
            int sourceEvents = 0, destinationEvents = 0;
            source.Changed += () => { sourceEvents++; Assert(source.Count(item) + destination.Count(item) == 65, "Source observer sees conserved total"); };
            destination.Changed += () => { destinationEvents++; Assert(source.Count(item) + destination.Count(item) == 65, "Destination observer sees conserved total"); };
            Assert(source.TransferTo(destination, item, 47) == 2, "Only destination space transfers");
            Assert(source.Count(item) == 45 && destination.Count(item) == 20, "Partial transfer preserves remainder");
            Assert(sourceEvents == 1 && destinationEvents == 1, "One notification per changed inventory");
            Assert(source.TransferTo(destination, item, 1) == 0, "Full inventory refuses transfer");
            Assert(source.TransferTo(source, item, 1) == 0, "Self transfer refused");
            Assert(source.TransferTo(null, item, 1) == 0, "Null destination refused");
            Assert(source.TransferTo(destination, null, 1) == 0, "Null item refused");
            Assert(source.TransferTo(destination, item, -1) == 0, "Negative quantity refused");
            Assert(sourceEvents == 1 && destinationEvents == 1, "Rejected transfers do not notify");
        }
    }

    private static void CheckDepositAll()
    {
        using (var f = new Fixture())
        {
            ItemDefinition ore = f.Item(), seed = f.Item();
            SimInventory source = f.Inventory(), destination = f.Inventory();
            source.Add(ore, 47);
            source.Add(seed, 3);
            Assert(source.TransferAllTo(destination) == 50, "Deposit all handles repeated item stacks");
            Assert(source.UsedSlots == 0 && destination.Count(ore) == 47 && destination.Count(seed) == 3,
                "Deposit all preserves all item types and amounts");
            Assert(destination.TransferTo(source, ore, 100) == 47, "Oversized request is limited by source quantity");
        }
    }

    private static void CheckOverflow()
    {
        using (var f = new Fixture())
        {
            ItemDefinition item = f.Item();
            SimInventory inventory = f.Inventory();
            inventory.Add(item, 25);
            Set(inventory, "capacity", 1);
            Invoke(inventory, "Awake");
            Assert(inventory.Count(item) == 25 && inventory.UsedSlots == 2, "Initialization preserves over-capacity contents");
        }
    }

    private static void CheckStorageAccess()
    {
        using (var f = new Fixture())
        {
            ItemDefinition item = f.Item();
            ItemStorage storage = f.Storage(new Vector3(10005, 0, 0));
            SimInventory first = f.Inventory(), second = f.Inventory();
            storage.Inventory.Add(item, 1);
            Assert(storage.Withdraw(first, item, 1) == 0, "Cannot withdraw remotely");
            first.transform.position = second.transform.position = storage.Position;
            Assert(storage.Withdraw(first, item, 1) == 1, "First carrier receives final item");
            Assert(storage.Withdraw(second, item, 1) == 0, "Second carrier cannot duplicate final item");
            Assert(first.Count(item) + second.Count(item) + storage.Inventory.Count(item) == 1, "Concurrent requests conserve quantity");
            storage.enabled = false;
            Assert(storage.DepositAll(first) == 0, "Disabled storage rejects deposits");
            storage.enabled = true;
            Assert(storage.DepositAll(first) == 1, "Nearby carrier can deposit");
            Assert(storage.DepositAll(storage.Inventory) == 0, "Container cannot transfer to itself");
        }
    }

    private static void CheckTrips()
    {
        using (var f = new Fixture())
        {
            ItemDefinition item = f.Item();
            SimInventory carrier = f.Inventory();
            ItemStorage storage = f.Storage(new Vector3(10005, 0, 0));
            carrier.Add(item, 25);
            var trip = new SimStorageTransfer();
            Assert(trip.Request(storage, SimStorageTransfer.TransferDirection.DepositAll), "Deposit request starts");
            Assert(trip.Tick(carrier).HasValue && trip.IsTravelling, "Trip supplies a destination");
            trip.Pause();
            Assert(trip.HasRequest && !trip.IsTravelling, "Need interruption retains request");
            Set(trip, "lastProgressTime", Time.time - 100f);
            Assert(trip.Tick(carrier).HasValue, "Resuming after needs resets timeout");
            carrier.transform.position = storage.Position;
            Assert(!trip.Tick(carrier).HasValue && !trip.HasRequest, "Arrival completes request");
            Assert(carrier.Count(item) == 0 && storage.Inventory.Count(item) == 25, "Trip deposited all stacks");
            Assert(trip.Request(storage, SimStorageTransfer.TransferDirection.PickUpItem, item, 3), "Pickup request starts");
            trip.Tick(carrier);
            Assert(carrier.Count(item) == 3 && storage.Inventory.Count(item) == 22, "Pickup transfers requested amount");
            carrier.transform.position = new Vector3(10000, 0, 0);
            trip.Request(storage, SimStorageTransfer.TransferDirection.DepositAll);
            trip.Tick(carrier);
            Set(trip, "lastProgressTime", Time.time - 100f);
            Assert(!trip.Tick(carrier).HasValue && !trip.HasRequest, "Blocked trip times out");
            Assert(carrier.Count(item) == 3, "Blocked trip keeps carried items");
            trip.Request(storage, SimStorageTransfer.TransferDirection.DepositAll);
            trip.Cancel();
            Assert(!trip.HasRequest && !trip.Destination.HasValue, "Cancellation clears trip");
        }
    }

    private static void CheckFoodSelection()
    {
        using (var f = new Fixture())
        {
            FoodDefinition low = f.Food(1), high = f.Food(3), far = f.Food(5);
            SimEating eating = f.Inventory().gameObject.AddComponent<SimEating>();
            Invoke(eating, "Awake");
            SimInventory carrier = eating.GetComponent<SimInventory>();
            SimNeeds needs = eating.GetComponent<SimNeeds>();
            needs.Hunger = 80;
            f.LooseFood(low, new Vector3(10005, 0, 0));
            f.LooseFood(far, new Vector3(10020, 0, 0));
            ItemStorage storage = f.Storage(new Vector3(10006, 0, 0));
            storage.Inventory.Add(high, 2);
            Assert(eating.HasFoodSource(), "Stored and loose food are discoverable");
            eating.GetDestination();
            Assert(eating.CurrentStorage == storage, "Higher-level nearby storage beats loose food; distant premium food ignored");
            carrier.transform.position = storage.Position;
            eating.GetDestination();
            Assert(carrier.Count(high) == 1 && storage.Inventory.Count(high) == 1, "Food pickup transfers one item from pantry");
            Assert(!eating.HasTarget, "Food arrival clears target");
            needs.Hunger = 31;
            Assert(eating.WantsFood, "Appetite continues below starting threshold");
            needs.Hunger = 30;
            Assert(!eating.WantsFood, "Appetite stops at stop threshold");
            needs.Hunger = 59;
            Assert(!eating.WantsFood, "Appetite waits for starting threshold");
            carrier.TryRemove(high, 1);
            carrier.transform.position = new Vector3(10000, 0, 0);
            needs.Hunger = 80;
            Set(eating, "nextSearchTime", 0f);
            eating.GetDestination();
            storage.enabled = false;
            Set(eating, "nextSearchTime", 0f);
            eating.GetDestination();
            Assert(eating.CurrentStorage == null && eating.CurrentTarget != null,
                "Unavailable storage falls back to loose food");
            eating.Cancel();
            Assert(!eating.HasTarget, "Cancellation releases food source");
            Set(carrier, "capacity", 1);
            carrier.Add(f.Item(), 20);
            Assert(!eating.HasFoodSource(), "Full carrier cannot seek food it cannot carry");
        }
    }

    private static void CheckCriticalNeeds()
    {
        using (var f = new Fixture())
        {
            SimController sim = f.Inventory().gameObject.AddComponent<SimController>();
            SimEating eating = sim.GetComponent<SimEating>();
            SimSleeping sleeping = sim.GetComponent<SimSleeping>();
            SimNeeds needs = sim.GetComponent<SimNeeds>();
            Invoke(sim, "Awake");
            Invoke(eating, "Awake");
            Invoke(sleeping, "Awake");
            FoodDefinition food = f.Food(1);
            ItemStorage storage = f.Storage(new Vector3(10004, 0, 0));
            storage.Inventory.Add(food, 10);
            needs.Energy = 0;
            needs.Hunger = 99;
            Assert(Choose(sim) == "Food", "Critical hunger wins over exhaustion before food pickup");
            sim.GetComponent<SimInventory>().Add(food, 3);
            needs.Hunger = 100;
            Assert(Choose(sim) == "Food", "Starving exhausted Sim chooses carried food");
            // One default meal reduces 100 hunger to 85, still critically hungry.
            needs.Hunger = 85;
            Set(eating, "nextEatTime", Time.time + 1f);
            Assert(Choose(sim) == "Food", "Critical hunger keeps food selected during the bite cooldown");
            needs.Hunger = 84;
            Assert(Choose(sim) == "Sleep", "Exhaustion takes over after hunger leaves critical range");
            needs.Hunger = 99;
            Assert(Choose(sim) == "Food", "Critical hunger can interrupt rest even while energy is critical");
            sim.GetComponent<SimInventory>().TryRemove(food, 3);
            storage.enabled = false;
            Assert(Choose(sim) == "Sleep", "Unavailable food allows exhausted Sim to sleep");
        }
    }

    private static string Choose(SimController sim)
    {
        object activity = typeof(SimController).GetMethod("ChooseActivity",
            BindingFlags.NonPublic | BindingFlags.Instance).Invoke(sim, null);
        Set(sim, "activity", activity);
        return activity.ToString();
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception("Storage verification: " + message);
        assertions++;
    }

    private static void Set(object target, string field, object value)
        => target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
    private static void Invoke(object target, string method)
        => target.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, null);

    private sealed class Fixture : IDisposable
    {
        private readonly List<Object> objects = new List<Object>();
        public SimInventory Inventory()
        {
            var go = new GameObject("Storage verification (temporary)") { hideFlags = HideFlags.HideAndDontSave };
            objects.Add(go);
            go.transform.position = new Vector3(10000, 0, 0);
            return go.AddComponent<SimInventory>();
        }
        public ItemDefinition Item()
        {
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            objects.Add(item);
            return item;
        }
        public FoodDefinition Food(int level)
        {
            var food = ScriptableObject.CreateInstance<FoodDefinition>();
            objects.Add(food);
            Set(food, "level", level);
            return food;
        }
        public ItemStorage Storage(Vector3 position)
        {
            var storage = Inventory().gameObject.AddComponent<ItemStorage>();
            storage.transform.position = position;
            Invoke(storage, "OnEnable");
            return storage;
        }
        public void LooseFood(FoodDefinition food, Vector3 position)
        {
            var world = Inventory().gameObject.AddComponent<WorldItem>();
            Set(world, "item", food);
            world.transform.position = position;
            Invoke(world, "OnEnable");
        }
        public void Dispose()
        {
            for (int i = objects.Count - 1; i >= 0; i--)
                if (objects[i] != null) Object.DestroyImmediate(objects[i]);
        }
    }
}

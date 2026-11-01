using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Run in an isolated empty Unity project with -executeMethod
// CriticalNeedsVerification.RunBatch. Exercises real Update/LateUpdate/physics.
public static class CriticalNeedsVerification
{
    private const string SessionKey = "CriticalNeedsVerification.Running";
    private static GameObject root;
    private static FoodDefinition food;
    private static SimController sim;
    private static SimNeeds needs;
    private static float started;
    private static double wallStarted;
    private static int scenario;
    private static bool ate;

    public static void RunBatch()
    {
        if (!Application.isBatchMode)
            throw new InvalidOperationException("Run this verification in an isolated batch project.");
        StorageVerification.Run();
        SessionState.SetBool(SessionKey, true);
        EditorApplication.EnterPlaymode();
    }

    [InitializeOnLoadMethod]
    private static void Register() => EditorApplication.update += Poll;

    private static void Poll()
    {
        if (!SessionState.GetBool(SessionKey, false) || !EditorApplication.isPlaying)
            return;
        try
        {
            if (root == null)
                StartScenario();
            if (EditorApplication.timeSinceStartup - wallStarted > 30 || Time.time - started > 40)
                throw new Exception($"Critical-needs scenario {scenario} stalled: hunger={needs.Hunger}, energy={needs.Energy}, activity={sim.ActivityLabel}.");

            ate |= needs.Hunger < 85f;
            SimSleeping sleeping = sim.GetComponent<SimSleeping>();
            if (!ate && (sleeping.IsSleeping || sleeping.CurrentBed != null))
                throw new Exception("Sim chose sleep before reducing critical hunger.");

            if (!ate || needs.Energy < 20f)
                return;
            Debug.Log($"Critical-needs Play Mode scenario {scenario} passed: hunger={needs.Hunger:0.0}, energy={needs.Energy:0.0}.");
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(food);
            scenario++;
            if (scenario < 2)
                return;

            SessionState.SetBool(SessionKey, false);
            Time.timeScale = 1f;
            Debug.Log("Critical-needs Play Mode verification passed: carried food and storage food, starting at 100 hunger / 0 energy.");
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            SessionState.SetBool(SessionKey, false);
            Time.timeScale = 1f;
            if (root != null) Object.DestroyImmediate(root);
            if (food != null) Object.DestroyImmediate(food);
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    private static void StartScenario()
    {
        if (Object.FindObjectsOfType<ItemStorage>().Length > 0 || Object.FindObjectsOfType<WorldItem>().Length > 0)
            throw new InvalidOperationException("Critical-needs verification requires an empty scene.");
        root = new GameObject("Critical-needs verification");
        var colonist = new GameObject("Exhausted hungry Sim");
        colonist.SetActive(false);
        colonist.transform.parent = root.transform;
        sim = colonist.AddComponent<SimController>();
        colonist.GetComponent<Rigidbody>().useGravity = false;
        needs = colonist.GetComponent<SimNeeds>();
        needs.Hunger = 100f;
        needs.Energy = 0f;
        food = ScriptableObject.CreateInstance<FoodDefinition>();

        var bedObject = new GameObject("Assigned bed");
        bedObject.transform.parent = root.transform;
        bedObject.transform.position = new Vector3(-2, 0, 0);
        SimBed bed = bedObject.AddComponent<SimBed>();
        typeof(SimBed).GetField("assignedColonist", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(bed, sim);

        if (scenario == 0)
            colonist.GetComponent<SimInventory>().Add(food, 4);
        else
        {
            var pantry = new GameObject("Pantry");
            pantry.transform.parent = root.transform;
            pantry.transform.position = new Vector3(2, 0, 0);
            pantry.AddComponent<ItemStorage>().Inventory.Add(food, 20);
        }
        colonist.SetActive(true);
        Time.timeScale = 5f;
        started = Time.time;
        wallStarted = EditorApplication.timeSinceStartup;
        ate = false;
    }
}

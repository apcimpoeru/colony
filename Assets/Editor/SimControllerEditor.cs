using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SimController))]
public sealed class SimControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        SimController sim = (SimController)target;
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Configure Storage Transfer above, then start a trip in Play Mode. "
            + "Food and sleep pause the trip; a manual destination cancels it.", MessageType.Info);
        using (new EditorGUI.DisabledScope(!Application.isPlaying || !sim.isActiveAndEnabled
            || sim.destinationOverride != null))
        {
            if (GUILayout.Button("Start storage transfer"))
                sim.StartConfiguredStorageTransfer();
            using (new EditorGUI.DisabledScope(!sim.StorageTransfer.HasRequest))
                if (GUILayout.Button("Cancel storage transfer"))
                    sim.CancelStorageTransfer();
        }
        if (!string.IsNullOrEmpty(sim.StorageTransfer.Status))
            EditorGUILayout.HelpBox(sim.StorageTransfer.Status, MessageType.Info);
    }

    public override bool RequiresConstantRepaint() => Application.isPlaying;
}

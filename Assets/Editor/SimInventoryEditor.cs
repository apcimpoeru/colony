using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SimInventory))]
public sealed class SimInventoryEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.HelpBox("Items are the actual inventory contents. During Play Mode, "
            + "eating decreases these quantities. Play Mode changes reset when you stop.", MessageType.Info);
    }

    public override bool RequiresConstantRepaint() => Application.isPlaying;
}

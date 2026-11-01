using NUnit.Framework;
using UnityEditor;

public sealed class SimControllerEditorTests : SimulationTestFixture
{
    [Test]
    public void CustomInspector_ExposesPriorityAndStorageConfiguration()
    {
        var sim = Colonist(); var editor = Track(Editor.CreateEditor(sim));
        Assert.That(editor, Is.TypeOf<SimControllerEditor>());
        Assert.That(editor.RequiresConstantRepaint(), Is.False);
        var serialized = editor.serializedObject; serialized.Update();
        var priorities = serialized.FindProperty("workPriorities");
        Assert.That(priorities.FindPropertyRelative("mining"), Is.Not.Null);
        Assert.That(priorities.FindPropertyRelative("farming"), Is.Not.Null);
        priorities.FindPropertyRelative("mining").intValue = 1;
        var trip = serialized.FindProperty("storageTransfer");
        foreach (string name in new[] { "storage", "direction", "item", "quantity", "noProgressTimeout" })
            Assert.That(trip.FindPropertyRelative(name), Is.Not.Null, name);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        Assert.That(sim.WorkPriorities.GetPriority(SimWorkType.Mining), Is.EqualTo(1));
        Assert.That(sim.StartConfiguredStorageTransfer(), Is.False);
        sim.CancelStorageTransfer(); Assert.That(sim.StorageTransfer.HasRequest, Is.False);
    }
}

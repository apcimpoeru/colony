using NUnit.Framework;
using UnityEditor;

public sealed class SimInventoryEditorTests : SimulationTestFixture
{
    [Test]
    public void CustomInspector_ResolvesAndSerializedContentsRemainEditable()
    {
        var inventory = Inventory(); var item = Item(); inventory.Add(item, 3);
        var editor = Track(Editor.CreateEditor(inventory));
        Assert.That(editor, Is.TypeOf<SimInventoryEditor>());
        Assert.That(editor.RequiresConstantRepaint(), Is.False);
        editor.serializedObject.Update();
        Assert.That(editor.serializedObject.FindProperty("capacity"), Is.Not.Null);
        var contents = editor.serializedObject.FindProperty("items");
        Assert.That(contents.arraySize, Is.EqualTo(1));
        var quantity = contents.GetArrayElementAtIndex(0).FindPropertyRelative("quantity");
        quantity.intValue = 7; editor.serializedObject.ApplyModifiedPropertiesWithoutUndo();
        Assert.That(inventory.Count(item), Is.EqualTo(7));
    }
}

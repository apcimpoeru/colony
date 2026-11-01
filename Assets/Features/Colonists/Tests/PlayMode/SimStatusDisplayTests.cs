using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class SimStatusDisplayTests : SimulationTestFixture
{
    [UnityTest]
    public IEnumerator Display_ToleratesMissingCameraControllerAndOffscreenAnchors()
    {
        // A standalone readout has needs but deliberately has no controller.
        var display = NewObject().AddComponent<SimStatusDisplay>();
        Assert.That(display.GetComponent<SimController>(), Is.Null);
        yield return null; yield return null;
        var camera = NewObject().AddComponent<Camera>(); camera.tag = "MainCamera";
        camera.transform.position = Vector3.zero;
        display.transform.position = new Vector3(0, 0, -10);
        yield return null; yield return null;
        display.transform.position = new Vector3(10000, 0, 10);
        yield return null; yield return null;
        Time.timeScale = 0; yield return null;
        display.enabled = false; yield return null;
        LogAssert.NoUnexpectedReceived();
    }
}

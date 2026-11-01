using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class SimMovementPhysicsTests : SimulationTestFixture
{
    private SimMovement Mover()
    {
        var movement = NewObject().AddComponent<SimMovement>();
        movement.GetComponent<Rigidbody>().useGravity = false; return movement;
    }

    [UnityTest]
    public IEnumerator Movement_UsesConfiguredSpeedTracksNewDestinationAndStopsWithoutOvershoot()
    {
        var movement = Mover(); var body = movement.GetComponent<Rigidbody>();
        Set(movement, "moveSpeed", 4f); movement.SetDestination(new Vector3(2, 100, 0));
        yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
        Assert.That(body.velocity.x, Is.EqualTo(4).Within(0.01f));
        Assert.That(body.position.y, Is.Zero.Within(0.01f));
        movement.SetDestination(new Vector3(0, 50, 1));
        yield return Until(() => movement.IsAt(new Vector3(0, 0, 1)), "Did not arrive at updated destination");
        yield return new WaitForFixedUpdate();
        Assert.That(body.position.z, Is.LessThanOrEqualTo(1.001f));
        Assert.That(new Vector2(body.velocity.x, body.velocity.z).magnitude, Is.LessThan(0.01f));
        Assert.That(body.constraints & RigidbodyConstraints.FreezeRotation, Is.EqualTo(RigidbodyConstraints.FreezeRotation));
    }

    [UnityTest]
    public IEnumerator Movement_PreservesVerticalVelocityAndStopDisableClearHorizontalMovement()
    {
        var movement = Mover(); var body = movement.GetComponent<Rigidbody>();
        body.velocity = new Vector3(0, 2, 0); movement.SetDestination(Vector3.right * 100);
        yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
        Assert.That(body.velocity.y, Is.EqualTo(2).Within(0.01f));
        movement.Stop(); Assert.That(body.velocity.x, Is.Zero); Assert.That(body.velocity.y, Is.EqualTo(2).Within(0.01f));
        movement.SetDestination(Vector3.right * 100);
        yield return new WaitForFixedUpdate(); movement.enabled = false;
        Assert.That(body.velocity.x, Is.Zero);
    }

    [UnityTest]
    public IEnumerator KinematicBody_AndPausedPhysicsRemainStationary()
    {
        var movement = Mover(); var body = movement.GetComponent<Rigidbody>();
        body.isKinematic = true; movement.SetDestination(Vector3.right * 100);
        yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
        Assert.That(body.position, Is.EqualTo(Vector3.zero));
        body.isKinematic = false; Time.timeScale = 0;
        yield return null; var position = body.position;
        yield return null; yield return null;
        Assert.That(body.position, Is.EqualTo(position));
    }
}

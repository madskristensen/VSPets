using VSPets.Models;

namespace VSPets.Test;

[TestClass]
public class BallTests
{
    [TestMethod]
    public void Constructor_SetInitialPosition()
    {
        var ball = new Ball(100.0, throwRight: true);

        Assert.AreEqual(100.0, ball.X);
    }

    [TestMethod]
    public void Constructor_IsActive()
    {
        var ball = new Ball(50.0);

        Assert.IsTrue(ball.IsActive);
        Assert.AreEqual(BallState.Rolling, ball.State);
    }

    [TestMethod]
    public void Constructor_ThrowRight_HasPositiveVelocity()
    {
        var ball = new Ball(100.0, throwRight: true);

        Assert.IsTrue(ball.Velocity > 0, "Velocity should be positive when throwing right.");
    }

    [TestMethod]
    public void Constructor_ThrowLeft_HasNegativeVelocity()
    {
        var ball = new Ball(100.0, throwRight: false);

        Assert.IsTrue(ball.Velocity < 0, "Velocity should be negative when throwing left.");
    }

    [TestMethod]
    public void Update_MovesBallInVelocityDirection()
    {
        var ball = new Ball(100.0, throwRight: true);
        double initialX = ball.X;

        ball.Update(0.1, 500.0);

        Assert.IsTrue(ball.X > initialX, "Ball should move right when velocity is positive.");
    }

    [TestMethod]
    public void Update_AppliesFriction_ReducesVelocity()
    {
        var ball = new Ball(100.0, throwRight: true);
        double initialVelocity = ball.Velocity;

        ball.Update(0.1, 500.0);

        Assert.IsTrue(Math.Abs(ball.Velocity) < Math.Abs(initialVelocity),
            "Friction should reduce velocity magnitude.");
    }

    [TestMethod]
    public void Update_BouncesOffLeftEdge()
    {
        var ball = new Ball(5.0, throwRight: false);

        // Run enough updates to hit the left boundary
        for (int i = 0; i < 10; i++)
        {
            ball.Update(0.1, 500.0);
        }

        Assert.IsTrue(ball.X >= 0, "Ball should not go below 0 after bouncing.");
    }

    [TestMethod]
    public void Update_BouncesOffRightEdge()
    {
        double canvasWidth = 200.0;
        var ball = new Ball(190.0, throwRight: true);

        // Run enough updates to hit the right boundary
        for (int i = 0; i < 10; i++)
        {
            ball.Update(0.1, canvasWidth);
        }

        Assert.IsTrue(ball.X <= canvasWidth - ball.Size,
            "Ball should not exceed canvas width minus ball size.");
    }

    [TestMethod]
    public void Update_EventuallyStops()
    {
        var ball = new Ball(100.0, throwRight: true);

        // Simulate many frames — friction should bring it to a stop
        for (int i = 0; i < 1000; i++)
        {
            ball.Update(0.016, 500.0);
        }

        Assert.AreEqual(0, ball.Velocity, "Ball should stop after enough friction.");
        Assert.AreEqual(BallState.Stopped, ball.State);
    }

    [TestMethod]
    public void Update_DoesNotMoveWhenInactive()
    {
        var ball = new Ball(100.0, throwRight: true);
        ball.IsActive = false;
        double initialX = ball.X;

        ball.Update(0.1, 500.0);

        Assert.AreEqual(initialX, ball.X, "Inactive ball should not move.");
    }

    [TestMethod]
    public void Update_DoesNotMoveWhenCaught()
    {
        var ball = new Ball(100.0, throwRight: true);
        ball.Catch();
        double initialX = ball.X;

        ball.Update(0.1, 500.0);

        Assert.AreEqual(initialX, ball.X, "Caught ball should not move.");
    }

    [TestMethod]
    public void Catch_SetsStateAndDeactivates()
    {
        var ball = new Ball(100.0);

        ball.Catch();

        Assert.AreEqual(BallState.Caught, ball.State);
        Assert.AreEqual(0, ball.Velocity);
        Assert.IsFalse(ball.IsActive);
    }

    [TestMethod]
    public void Size_Returns16()
    {
        var ball = new Ball(0);

        Assert.AreEqual(16, ball.Size);
    }
}

using VSPets.Models;
using VSPets.Pets;

namespace VSPets.Test;

[TestClass]
public class BasePetTests
{
    private Cat CreateCat() => new Cat(PetColor.Orange);

    [TestMethod]
    public void NewPet_StartsInIdleState()
    {
        Cat pet = CreateCat();

        Assert.AreEqual(PetState.Idle, pet.CurrentState);
    }

    [TestMethod]
    public void NewPet_HasRightDirection()
    {
        Cat pet = CreateCat();

        Assert.AreEqual(PetDirection.Right, pet.Direction);
    }

    [TestMethod]
    public void NewPet_HasDefaultBreathingScale()
    {
        Cat pet = CreateCat();

        Assert.AreEqual(1.0, pet.BreathingScale, 0.01);
    }

    [TestMethod]
    public void NewPet_HasNonEmptyName()
    {
        Cat pet = CreateCat();

        Assert.IsFalse(string.IsNullOrEmpty(pet.Name));
    }

    [TestMethod]
    public void NewPet_HasUniqueId()
    {
        Cat pet1 = CreateCat();
        Cat pet2 = CreateCat();

        Assert.AreNotEqual(pet1.Id, pet2.Id);
    }

    [TestMethod]
    public void SetState_ChangesCurrentState()
    {
        Cat pet = CreateCat();

        pet.SetState(PetState.Walking);

        Assert.AreEqual(PetState.Walking, pet.CurrentState);
    }

    [TestMethod]
    public void SetState_FiresStateChangedEvent()
    {
        Cat pet = CreateCat();
        PetState? newState = null;

        pet.StateChanged += (s, e) => newState = e.NewState;
        pet.SetState(PetState.Walking);

        Assert.AreEqual(PetState.Walking, newState);
    }

    [TestMethod]
    public void SetState_SameState_DoesNotFireEvent()
    {
        Cat pet = CreateCat();
        int eventCount = 0;

        pet.StateChanged += (s, e) => eventCount++;
        pet.SetState(PetState.Idle); // Already idle

        Assert.AreEqual(0, eventCount);
    }

    [TestMethod]
    public void SetPosition_UpdatesCoordinates()
    {
        Cat pet = CreateCat();

        pet.SetPosition(50.0, 10.0);

        Assert.AreEqual(50.0, pet.X, 0.001);
        Assert.AreEqual(10.0, pet.Y, 0.001);
    }

    [TestMethod]
    public void SetPosition_FiresPositionChangedEvent()
    {
        Cat pet = CreateCat();
        bool eventFired = false;

        pet.PositionChanged += (s, e) => eventFired = true;
        pet.SetPosition(100.0, 0.0);

        Assert.IsTrue(eventFired);
    }

    [TestMethod]
    public void SetDirection_ChangesDirection()
    {
        Cat pet = CreateCat();

        pet.SetDirection(PetDirection.Left);

        Assert.AreEqual(PetDirection.Left, pet.Direction);
    }

    [TestMethod]
    public void SetDirection_FiresDirectionChangedEvent()
    {
        Cat pet = CreateCat();
        PetDirection? newDirection = null;

        pet.DirectionChanged += (s, e) => newDirection = e.NewDirection;
        pet.SetDirection(PetDirection.Left);

        Assert.AreEqual(PetDirection.Left, newDirection);
    }

    [TestMethod]
    public void SetDirection_SameDirection_DoesNotFireEvent()
    {
        Cat pet = CreateCat();
        int eventCount = 0;

        pet.DirectionChanged += (s, e) => eventCount++;
        pet.SetDirection(PetDirection.Right); // Already right

        Assert.AreEqual(0, eventCount);
    }

    [TestMethod]
    public void Update_WalkingState_MovesPosition()
    {
        Cat pet = CreateCat();
        pet.SetState(PetState.Walking);
        pet.SetPosition(100.0, 0.0);
        pet.SetDirection(PetDirection.Right);

        pet.Update(0.1, 500.0);

        Assert.IsTrue(pet.X > 100.0, "Walking pet should move in its direction.");
    }

    [TestMethod]
    public void Update_RunningState_MovesPosition()
    {
        Cat pet = CreateCat();
        pet.SetState(PetState.Running);
        pet.SetPosition(100.0, 0.0);
        pet.SetDirection(PetDirection.Right);

        pet.Update(0.1, 500.0);

        Assert.IsTrue(pet.X > 100.0, "Running pet should move in its direction.");
    }

    [TestMethod]
    public void Update_RunningFasterThanWalking()
    {
        Cat walker = CreateCat();
        walker.SetState(PetState.Walking);
        walker.SetPosition(100.0, 0.0);
        walker.SetDirection(PetDirection.Right);

        Cat runner = CreateCat();
        runner.SetState(PetState.Running);
        runner.SetPosition(100.0, 0.0);
        runner.SetDirection(PetDirection.Right);

        walker.Update(0.1, 500.0);
        runner.Update(0.1, 500.0);

        Assert.IsTrue(runner.X > walker.X, "Running should be faster than walking.");
    }

    [TestMethod]
    public void Update_IdleState_DoesNotMovePosition()
    {
        Cat pet = CreateCat();
        pet.SetPosition(100.0, 0.0);

        pet.Update(0.1, 500.0);

        Assert.AreEqual(100.0, pet.X, 0.001, "Idle pet should not move.");
    }

    [TestMethod]
    public void Update_WalkingLeft_DecreasesX()
    {
        Cat pet = CreateCat();
        pet.SetState(PetState.Walking);
        pet.SetPosition(100.0, 0.0);
        pet.SetDirection(PetDirection.Left);

        pet.Update(0.1, 500.0);

        Assert.IsTrue(pet.X < 100.0, "Walking left should decrease X.");
    }

    [TestMethod]
    public void StartDrag_SetsStateToDragging()
    {
        Cat pet = CreateCat();

        pet.StartDrag();

        Assert.AreEqual(PetState.Dragging, pet.CurrentState);
        Assert.IsTrue(pet.IsDragging);
    }

    [TestMethod]
    public void EndDrag_SetsStateToIdle()
    {
        Cat pet = CreateCat();
        pet.StartDrag();

        pet.EndDrag();

        Assert.AreEqual(PetState.Idle, pet.CurrentState);
        Assert.IsFalse(pet.IsDragging);
    }

    [TestMethod]
    public void Update_WhenDragging_DoesNotMove()
    {
        Cat pet = CreateCat();
        pet.SetState(PetState.Walking);
        pet.SetPosition(100.0, 0.0);
        pet.StartDrag();

        pet.Update(0.5, 500.0);

        Assert.AreEqual(100.0, pet.X, 0.001, "Dragging pet should not move.");
    }

    [TestMethod]
    public void TriggerHappy_SetsHappyState()
    {
        Cat pet = CreateCat();

        pet.TriggerHappy();

        Assert.AreEqual(PetState.Happy, pet.CurrentState);
    }

    [TestMethod]
    public void TriggerHappy_WhenAlreadyHappy_DoesNothing()
    {
        Cat pet = CreateCat();
        pet.TriggerHappy();
        int eventCount = 0;

        pet.StateChanged += (s, e) => eventCount++;
        pet.TriggerHappy();

        Assert.AreEqual(0, eventCount);
    }

    [TestMethod]
    public void ForceState_SetsRequestedState()
    {
        Cat pet = CreateCat();

        pet.ForceState(PetState.Sleeping);

        Assert.AreEqual(PetState.Sleeping, pet.CurrentState);
    }

    [TestMethod]
    public void ShowSpeech_FiresSpeechEvent()
    {
        Cat pet = CreateCat();
        string? receivedMessage = null;

        pet.Speech += (s, e) => receivedMessage = e.Message;
        pet.ShowSpeech("Hello!", 1000);

        Assert.AreEqual("Hello!", receivedMessage);
    }

    [TestMethod]
    public void ReactToBuild_Success_SetsHappyState()
    {
        Cat pet = CreateCat();

        pet.ReactToBuild(true);

        Assert.AreEqual(PetState.Happy, pet.CurrentState);
    }

    [TestMethod]
    public void ReactToBuild_Success_ShowsSpeech()
    {
        Cat pet = CreateCat();
        string? message = null;

        pet.Speech += (s, e) => message = e.Message;
        pet.ReactToBuild(true);

        Assert.IsNotNull(message, "Build success should trigger a speech bubble.");
    }

    [TestMethod]
    public void ReactToBuild_Failure_ShowsSpeech()
    {
        Cat pet = CreateCat();
        string? message = null;

        pet.Speech += (s, e) => message = e.Message;
        pet.ReactToBuild(false);

        Assert.IsNotNull(message, "Build failure should trigger a speech bubble.");
    }

    [TestMethod]
    public void Update_BreathingScaleChangesWhileIdle()
    {
        Cat pet = CreateCat();
        pet.SetPosition(100.0, 0.0);

        // Track whether breathing ever deviates from 1.0 during idle updates
        bool breathingChanged = false;

        for (int i = 0; i < 20; i++)
        {
            pet.Update(0.05, 500.0);

            if (Math.Abs(pet.BreathingScale - 1.0) > 0.001)
            {
                breathingChanged = true;
                break;
            }
        }

        Assert.IsTrue(breathingChanged,
            "Breathing scale should deviate from 1.0 at some point while idle.");
    }

    [TestMethod]
    public void GetPossibleBehaviors_ReturnsNonEmptyArray()
    {
        Cat pet = CreateCat();

        string[] behaviors = pet.GetPossibleBehaviors();

        Assert.IsTrue(behaviors.Length > 0);
    }
}

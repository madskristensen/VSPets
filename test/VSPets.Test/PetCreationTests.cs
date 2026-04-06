using VSPets.Models;
using VSPets.Pets;
using VSPets.Services;

namespace VSPets.Test;

[TestClass]
public class PetCreationTests
{
    [TestMethod]
    public void Cat_HasCorrectPetType()
    {
        var cat = new Cat(PetColor.Orange);

        Assert.AreEqual(PetType.Cat, cat.PetType);
    }

    [TestMethod]
    public void Dog_HasCorrectPetType()
    {
        var dog = new Dog(PetColor.Brown);

        Assert.AreEqual(PetType.Dog, dog.PetType);
    }

    [TestMethod]
    public void Cat_HasExpectedEmoji()
    {
        var cat = new Cat(PetColor.Orange);

        Assert.AreEqual("🐱", cat.Emoji);
    }

    [TestMethod]
    public void Dog_HasExpectedEmoji()
    {
        var dog = new Dog(PetColor.Brown);

        Assert.AreEqual("🐕", dog.Emoji);
    }

    [TestMethod]
    public void Cat_CreateRandom_ReturnsValidCat()
    {
        Cat cat = Cat.CreateRandom();

        Assert.IsNotNull(cat);
        Assert.AreEqual(PetType.Cat, cat.PetType);
        Assert.IsFalse(string.IsNullOrEmpty(cat.Name));
    }

    [TestMethod]
    public void Dog_CreateRandom_ReturnsValidDog()
    {
        Dog dog = Dog.CreateRandom();

        Assert.IsNotNull(dog);
        Assert.AreEqual(PetType.Dog, dog.PetType);
        Assert.IsFalse(string.IsNullOrEmpty(dog.Name));
    }

    [TestMethod]
    public void Cat_CustomName_IsPreserved()
    {
        var cat = new Cat(PetColor.Black, "Pixel");

        Assert.AreEqual("Pixel", cat.Name);
    }

    [TestMethod]
    public void Cat_NameCanBeChanged()
    {
        var cat = new Cat(PetColor.Orange);

        cat.Name = "Debug";

        Assert.AreEqual("Debug", cat.Name);
    }

    [TestMethod]
    public void Cat_ColorIsPreserved()
    {
        var cat = new Cat(PetColor.Black);

        Assert.AreEqual(PetColor.Black, cat.Color);
    }

    [TestMethod]
    public void Cat_DefaultSizeIsSmall()
    {
        var cat = new Cat(PetColor.Orange);

        Assert.AreEqual(PetSize.Small, cat.Size);
    }

    [TestMethod]
    public void Cat_GetPossibleColors_ReturnsNonEmptyArray()
    {
        var cat = new Cat(PetColor.Orange);

        PetColor[] colors = cat.GetPossibleColors();

        Assert.IsTrue(colors.Length > 0);
    }

    [TestMethod]
    public void Cat_HelloMessageIsNotEmpty()
    {
        var cat = new Cat(PetColor.Orange);

        Assert.IsFalse(string.IsNullOrEmpty(cat.HelloMessage));
    }

    [TestMethod]
    public void PetManager_CreatePet_ReturnsCorrectType()
    {
        IPet cat = PetManager.Instance.CreatePet(PetType.Cat, PetColor.Orange);
        IPet dog = PetManager.Instance.CreatePet(PetType.Dog, PetColor.Brown);

        Assert.AreEqual(PetType.Cat, cat.PetType);
        Assert.AreEqual(PetType.Dog, dog.PetType);
    }

    [TestMethod]
    public void PetManager_CreatePet_NullColor_CreatesRandom()
    {
        IPet cat = PetManager.Instance.CreatePet(PetType.Cat, null);

        Assert.IsNotNull(cat);
        Assert.AreEqual(PetType.Cat, cat.PetType);
    }

    [TestMethod]
    [DataRow(PetType.Cat)]
    [DataRow(PetType.Dog)]
    [DataRow(PetType.Fox)]
    [DataRow(PetType.Bear)]
    [DataRow(PetType.Axolotl)]
    [DataRow(PetType.Clippy)]
    [DataRow(PetType.RubberDuck)]
    [DataRow(PetType.Turtle)]
    [DataRow(PetType.Bunny)]
    [DataRow(PetType.Raccoon)]
    [DataRow(PetType.TRex)]
    [DataRow(PetType.Wolf)]
    public void PetManager_CreatePet_AllTypesSucceed(PetType petType)
    {
        IPet pet = PetManager.Instance.CreatePet(petType, null);

        Assert.IsNotNull(pet);
        Assert.AreEqual(petType, pet.PetType);
    }

    [TestMethod]
    public void PetData_PropertiesRoundTrip()
    {
        var data = new PetData
        {
            Name = "Tester",
            PetType = PetType.Cat,
            Color = PetColor.Orange,
            Size = PetSize.Medium
        };

        Assert.AreEqual("Tester", data.Name);
        Assert.AreEqual(PetType.Cat, data.PetType);
        Assert.AreEqual(PetColor.Orange, data.Color);
        Assert.AreEqual(PetSize.Medium, data.Size);
    }
}

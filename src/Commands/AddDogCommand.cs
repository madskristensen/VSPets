using VSPets.Models;
using VSPets.Services;

namespace VSPets.Commands
{
    [Command(PackageIds.AddDogCommand)]
    internal sealed class AddDogCommand : BaseCommand<AddDogCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            try
            {
                var pet = await PetManager.Instance.AddPetAsync(PetType.Dog);

                if (pet != null)
                {
                    await VS.StatusBar.ShowMessageAsync($"🐕 {pet.Name} the dog joined your IDE!");
                }
                else
                {
                    await VS.StatusBar.ShowMessageAsync($"🐕 Maximum pets reached ({PetManager.Instance.MaxPets})");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"VSPets: AddDog failed: {ex.Message}");
                await VS.StatusBar.ShowMessageAsync("🐕 Failed to add dog");
            }
        }
    }
}

using VSPets.Pets;
using VSPets.Services;

namespace VSPets.Commands
{
    [Command(PackageIds.AddPetCommand)]
    internal sealed class AddPetCommand : BaseCommand<AddPetCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            try
            {
                IPet pet = await PetManager.Instance.AddRandomPetAsync();

                if (pet != null)
                {
                    await VS.StatusBar.ShowMessageAsync($"🐾 {pet.Name} the {pet.PetType} joined your IDE!");
                }
                else
                {
                    await VS.StatusBar.ShowMessageAsync($"🐾 Maximum pets reached ({PetManager.Instance.MaxPets})");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"VSPets: AddPet failed: {ex.Message}");
                await VS.StatusBar.ShowMessageAsync("🐾 Failed to add pet");
            }
        }
    }
}

using VSPets.Models;
using VSPets.Pets;
using VSPets.Services;

namespace VSPets.Commands
{
    [Command(PackageIds.AddCatCommand)]
    internal sealed class AddCatCommand : BaseCommand<AddCatCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            try
            {
                IPet pet = await PetManager.Instance.AddPetAsync(PetType.Cat);

                if (pet != null)
                {
                    await VS.StatusBar.ShowMessageAsync($"🐱 {pet.Name} the cat joined your IDE!");
                }
                else
                {
                    await VS.StatusBar.ShowMessageAsync($"🐱 Maximum pets reached ({PetManager.Instance.MaxPets})");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"VSPets: AddCat failed: {ex.Message}");
                await VS.StatusBar.ShowMessageAsync("🐱 Failed to add cat");
            }
        }
    }
}

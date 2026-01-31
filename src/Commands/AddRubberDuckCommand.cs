using VSPets.Models;
using VSPets.Pets;
using VSPets.Services;

namespace VSPets.Commands
{
    [Command(PackageIds.AddRubberDuckCommand)]
    internal sealed class AddRubberDuckCommand : BaseCommand<AddRubberDuckCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            try
            {
                IPet pet = await PetManager.Instance.AddPetAsync(PetType.RubberDuck);

                if (pet != null)
                {
                    await VS.StatusBar.ShowMessageAsync($"🦆 {pet.Name} is ready to debug with you!");
                }
                else
                {
                    await VS.StatusBar.ShowMessageAsync($"🦆 Maximum pets reached ({PetManager.Instance.MaxPets})");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"VSPets: AddRubberDuck failed: {ex.Message}");
                await VS.StatusBar.ShowMessageAsync("🦆 Failed to add rubber duck");
            }
        }
    }
}

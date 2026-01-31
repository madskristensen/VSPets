using VSPets.Models;
using VSPets.Services;

namespace VSPets.Commands
{
    [Command(PackageIds.AddFoxCommand)]
    internal sealed class AddFoxCommand : BaseCommand<AddFoxCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            try
            {
                var pet = await PetManager.Instance.AddPetAsync(PetType.Fox);

                if (pet != null)
                {
                    await VS.StatusBar.ShowMessageAsync($"🦊 {pet.Name} the fox joined your IDE!");
                }
                else
                {
                    await VS.StatusBar.ShowMessageAsync($"🦊 Maximum pets reached ({PetManager.Instance.MaxPets})");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"VSPets: AddFox failed: {ex.Message}");
                await VS.StatusBar.ShowMessageAsync("🦊 Failed to add fox");
            }
        }
    }
}

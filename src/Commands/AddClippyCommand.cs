using VSPets.Models;
using VSPets.Pets;
using VSPets.Services;

namespace VSPets.Commands
{
    [Command(PackageIds.AddClippyCommand)]
    internal sealed class AddClippyCommand : BaseCommand<AddClippyCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            try
            {
                IPet pet = await PetManager.Instance.AddPetAsync(PetType.Clippy);

                if (pet != null)
                {
                    await VS.StatusBar.ShowMessageAsync($"📎 {pet.Name} is here to help!");
                }
                else
                {
                    await VS.StatusBar.ShowMessageAsync($"📎 Maximum pets reached ({PetManager.Instance.MaxPets})");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"VSPets: AddClippy failed: {ex.Message}");
                await VS.StatusBar.ShowMessageAsync("📎 Failed to add Clippy");
            }
        }
    }
}

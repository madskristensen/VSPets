using VSPets.Services;

namespace VSPets.Commands
{
    [Command(PackageIds.RemoveAllPetsCommand)]
    internal sealed class RemoveAllPetsCommand : BaseCommand<RemoveAllPetsCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            try
            {
                var count = PetManager.Instance.PetCount;

                if (count == 0)
                {
                    await VS.StatusBar.ShowMessageAsync("🐾 No pets to remove");
                    return;
                }

                await PetManager.Instance.RemoveAllPetsAsync();
                await VS.StatusBar.ShowMessageAsync($"🐾 {count} pet(s) said goodbye!");
            }
            catch (Exception ex)
            {
                await ex.LogAsync();
                await VS.StatusBar.ShowMessageAsync("🐾 Failed to remove pets");
            }
        }
    }
}
